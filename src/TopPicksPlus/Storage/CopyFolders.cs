using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Storage;

/// <summary>What happened to one plan step (D26, D37, D82).</summary>
public enum StepOutcome
{
    Done,
    /// <summary>A D37 or D82 check refused: nothing was touched and the report names it.</summary>
    Refused,
    /// <summary>Skipped because an earlier step for the same folder (or file) was refused.</summary>
    Skipped,
}

/// <summary>The answer of carrying out a plan: what was refused, for <c>refusedDeletes</c> (D81).</summary>
public sealed class PlanResult
{
    internal PlanResult(IReadOnlyList<RefusedDelete> refused, IReadOnlyList<StepOutcome> outcomes)
    {
        Refused = refused;
        Outcomes = outcomes;
    }

    public IReadOnlyList<RefusedDelete> Refused { get; }
    public IReadOnlyList<StepOutcome> Outcomes { get; }
}

/// <summary>
/// What prepare wrote into a new copy (D28, D40, D124): each video (.strm) path → its source item, and every other file. The new
/// copy's managed entry is added carrying these, so plays on it are credited and next week's staying plan knows its files.
/// </summary>
public sealed class CopyContents
{
    public CopyContents(IReadOnlyDictionary<string, long> items, IEnumerable<string> files)
    {
        Items = items;
        Files = files.ToList();
    }

    public IReadOnlyDictionary<string, long> Items { get; }
    public IReadOnlyList<string> Files { get; }
}

/// <summary>
/// Every file change in the Top Picks folder and staging (D43): staging writes, emptying staging, moving folders in, writing and
/// deleting files in staying copies, deleting managed folders, and the takeover's file work. Deletes only ever happen after the
/// D37 checks (folders: a plain name, a direct child, not a junction or link) and the D82 checks (files: relative plain names that
/// resolve inside the copy, nothing on the way a junction or link), plus the exact-name check (D119): the name on disk must be the
/// name asked for, so a Windows short name (<c>ABCDEF~1</c>) can never reach a different folder. Links are never followed.
/// </summary>
public sealed class CopyFolders
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    private readonly DataFolder _data;

    /// <param name="topPicks">The Top Picks folder (Emby's data path + <c>top-picks</c>, D35).</param>
    /// <param name="data">The plugin data folder; staging is its <c>staging</c> child.</param>
    public CopyFolders(string topPicks, DataFolder data)
    {
        if (string.IsNullOrEmpty(topPicks))
            throw new ArgumentException("the Top Picks folder is needed", nameof(topPicks));
        TopPicks = SafePaths.Full(topPicks);
        _data = data;
    }

    public string TopPicks { get; }
    public string Staging => _data.StagingPath;

    /// <summary>
    /// Called when a step finds staging has lost a copy (D26): a <see cref="StepKind.MoveIn"/> whose staging folder is gone, or a
    /// <see cref="StepKind.WriteFile"/> whose staging file is gone (a resume after a new copy was moved in, which is then a staying
    /// copy). The first argument is the copy's staging name; EmbyLink writes the whole copy again into staging under the second
    /// name, which is then renamed into place, so a stop half-way never leaves a half-prepared copy to be moved in (D124).
    /// </summary>
    public Action<string, string>? Reprepare { get; set; }

    /// <summary>The suffix of the folder a copy is prepared again into before it is renamed into place.</summary>
    public const string PartialSuffix = ".tpp-partial";

    // ---------------- Looking ----------------

    /// <summary>The folders directly in top-picks (names as on disk). Links count as folders by name, but are never entered.</summary>
    public IReadOnlyList<string> FolderNames() =>
        Directory.Exists(TopPicks)
            ? new DirectoryInfo(TopPicks).EnumerateDirectories().Select(d => d.Name).OrderBy(n => n, Names).ToList()
            : new List<string>();

    /// <summary>
    /// Each folder in top-picks with every file inside it, as relative <c>\</c>-separated paths, for takeover and the unmanaged
    /// report (D38, D102). A junction or link (the folder itself or anything inside) is never entered, so its target is not listed.
    /// </summary>
    public IReadOnlyList<FolderListing> Listings()
    {
        var list = new List<FolderListing>();
        if (!Directory.Exists(TopPicks))
            return list;
        foreach (var dir in new DirectoryInfo(TopPicks).EnumerateDirectories().OrderBy(d => d.Name, Names))
        {
            var files = new List<string>();
            if ((dir.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                Collect(dir, string.Empty, files);
            list.Add(new FolderListing(dir.Name, files));
        }
        return list;
    }

    private static void Collect(DirectoryInfo dir, string prefix, List<string> files)
    {
        foreach (var entry in dir.EnumerateFileSystemInfos().OrderBy(e => e.Name, Names))
        {
            var rel = prefix + entry.Name;
            if ((entry.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                continue;
            if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                Collect((DirectoryInfo)entry, rel + "\\", files);
            else
                files.Add(rel);
        }
    }

    /// <summary>A .strm's text (the video path) in a top-picks folder, for matching at takeover (D28, D90). Null if refused or unreadable.</summary>
    public string? ReadStrm(string folder, string relativePath)
    {
        var dir = CheckedFolder(TopPicks, folder, out _);
        if (dir == null)
            return null;
        var file = CheckedFile(dir, relativePath, out _);
        if (file == null || !File.Exists(file))
            return null;
        try
        {
            return File.ReadAllText(file, Json.Utf8).Trim('﻿', '\r', '\n', ' ');
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {
            return null;
        }
    }

    // ---------------- The checks (D37, D82, D119) ----------------

    /// <summary>D37 + D119 for a folder under <paramref name="root"/>; the full path, or null with the reason.</summary>
    internal static string? CheckedFolder(string root, string? name, out string? problem)
    {
        var full = SafePaths.ChildFolder(root, name, out problem);
        if (full == null)
            return null;
        if (SafePaths.Exists(full))
        {
            problem = SafePaths.FolderProblem(full) ?? ExactNameProblem(full);
            if (problem != null)
                return null;
        }
        return full;
    }

    /// <summary>D82 + D119 for a file inside a checked folder; the full path, or null with the reason.</summary>
    internal static string? CheckedFile(string folderFull, string? relative, out string? problem)
    {
        var full = SafePaths.FileInside(folderFull, relative, out problem);
        if (full == null)
            return null;
        var walk = folderFull;
        foreach (var part in relative!.Split('\\'))
        {
            walk = Path.Combine(walk, part);
            if (!SafePaths.Exists(walk))
                break;
            problem = ExactNameProblem(walk);
            if (problem != null)
                return null;
        }
        return full;
    }

    /// <summary>
    /// D119 (B42): an existing file or folder must be listed in its parent under exactly this name (ignoring case). Windows also
    /// answers to a short name such as <c>ABCDEF~1</c>, which would reach a different folder than the managed one.
    /// </summary>
    private static string? ExactNameProblem(string full)
    {
        var parent = Path.GetDirectoryName(full);
        var name = Path.GetFileName(full);
        if (parent == null || string.IsNullOrEmpty(name))
            return "name can't be resolved";
        var found = new DirectoryInfo(parent).EnumerateFileSystemInfos().Any(e => Names.Equals(e.Name, name));
        return found ? null : SafePaths.ShortNameProblem;
    }

    // ---------------- Staging (D26, D35, D37) ----------------

    /// <summary>
    /// D37 for staging itself: it must be a direct child of the plugin data folder and not a junction or link. Returns the reason,
    /// or null when it is safe.
    /// </summary>
    public string? StagingProblem()
    {
        var full = CheckedFolder(_data.Root, DataFolder.StagingFolderName, out var problem);
        return full == null ? problem : null;
    }

    /// <summary>
    /// Empties staging at the start of every new run, never on a resume (D26). Refused, with nothing deleted, if staging is a
    /// junction or link or not a direct child of the plugin data folder (D37, C49); a link inside is removed as a link. Returns the
    /// reason when refused.
    /// </summary>
    public string? EmptyStaging()
    {
        _data.EnsureNothingNewer();
        var problem = StagingProblem();
        if (problem != null)
            return problem;
        if (Directory.Exists(Staging))
            SafePaths.DeleteTree(Staging);
        Directory.CreateDirectory(Staging);
        return null;
    }

    /// <summary>The full path of a copy folder in staging (checked), creating staging and the folder if needed.</summary>
    private string StagingFolder(string folder)
    {
        _data.EnsureNothingNewer();
        var problem = StagingProblem();
        if (problem != null)
            throw new IOException("staging refused: " + problem);
        Directory.CreateDirectory(Staging);
        var full = CheckedFolder(Staging, folder, out problem) ?? throw new IOException("staging folder '" + folder + "' refused: " + problem);
        Directory.CreateDirectory(full);
        return full;
    }

    /// <summary>Writes one file of a copy into staging (prepare, D26, D40), atomically. Throws if a check refuses.</summary>
    public void WriteStagingFile(string folder, string relativePath, byte[] bytes)
    {
        var dir = StagingFolder(folder);
        var file = CheckedFile(dir, relativePath, out var problem) ?? throw new IOException("staging path '" + relativePath + "' refused: " + problem);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        SafePaths.WriteAtomic(file, bytes);
    }

    /// <summary>A .strm, .nfo or other text file in staging: UTF-8, no byte-order mark, written exactly as given (D90, C93).</summary>
    public void WriteStagingText(string folder, string relativePath, string text) =>
        WriteStagingFile(folder, relativePath, Json.Utf8.GetBytes(text));

    /// <summary>Copies a file (subtitle, picture, theme) into staging, unchanged (D55, D98).</summary>
    public void CopyIntoStaging(string folder, string relativePath, string sourceFile) =>
        WriteStagingFile(folder, relativePath, File.ReadAllBytes(sourceFile));

    // ---------------- Carrying out Logic's plans (D26, D39, D40, D43, D101) ----------------

    /// <summary>
    /// Carries out a plan from <see cref="SwapPlan"/> in order. Every step is safe to repeat (a resume plans again and calls this
    /// again). A refused folder delete stops the rest of that folder's steps, so its entry stays (leaving, unranked, D101 e); a
    /// refused file step skips that file's following step and the run carries on (D82). Refusals are added to run.json's
    /// <c>refusedDeletes</c> when run.json can be written.
    /// </summary>
    /// <param name="plan">Steps from <see cref="SwapPlan"/>.</param>
    /// <param name="newCopyContents">For an <see cref="StepKind.AddIncoming"/> step: what prepare wrote into that new copy (D124).</param>
    public PlanResult Apply(IEnumerable<PlanStep> plan, Func<ManagedEntry, CopyContents?>? newCopyContents = null)
    {
        // D85: with any data file newer, nothing in top-picks or staging is touched either.
        _data.EnsureNothingNewer();
        var refused = new List<RefusedDelete>();
        var outcomes = new List<StepOutcome>();
        var blockedFolders = new HashSet<string>(Names);
        var blockedFiles = new HashSet<string>(Names);
        foreach (var step in plan)
        {
            var fileKey = step.Folder + "\u0000" + (step.Path ?? string.Empty);
            if (blockedFolders.Contains(step.Folder) || (IsFileStep(step.Kind) && blockedFiles.Contains(fileKey)))
            {
                outcomes.Add(StepOutcome.Skipped);
                continue;
            }
            var problem = ApplyOne(step, newCopyContents);
            if (problem == null)
            {
                outcomes.Add(StepOutcome.Done);
                continue;
            }
            outcomes.Add(StepOutcome.Refused);
            if (IsFileStep(step.Kind))
            {
                blockedFiles.Add(fileKey);
                refused.Add(new RefusedDelete { Folder = step.Folder, Path = step.Path, Reason = problem });
            }
            else
            {
                blockedFolders.Add(step.Folder);
                refused.Add(new RefusedDelete { Folder = step.Folder, Reason = problem });
            }
        }
        if (refused.Count > 0)
        {
            try
            {
                _data.AddRefusedDeletes(refused);
            }
            catch (DamagedFileException)
            {
                // run.json can't be read: the refusals are still returned to the caller.
            }
        }
        return new PlanResult(refused, outcomes);
    }

    private static bool IsFileStep(StepKind k) =>
        k == StepKind.AddFileToEntry || k == StepKind.WriteFile || k == StepKind.DeleteFile || k == StepKind.RemoveFileFromEntry;

    /// <summary>One step; null when done, else the reason it was refused.</summary>
    private string? ApplyOne(PlanStep s, Func<ManagedEntry, CopyContents?>? newCopyContents)
    {
        switch (s.Kind)
        {
            case StepKind.MarkLeaving:
                ChangeEntry(s.Folder, e => e.WithState(EntryState.Leaving, null, null));
                return null;
            case StepKind.ClearRank:
                ChangeEntry(s.Folder, e => e.WithState(e.State, null, null));
                return null;
            case StepKind.SetRank:
                ChangeEntry(s.Folder, e => e.WithState(e.State, s.Rank, s.RankDateUtc));
                return null;
            case StepKind.MarkActive:
                ChangeEntry(s.Folder, e => e.WithState(EntryState.Active, s.Rank, s.RankDateUtc));
                return null;
            case StepKind.RemoveEntry:
                _data.UpdateManaged(list => list.Where(e => !Names.Equals(e.Folder, s.Folder)).ToList());
                return null;
            case StepKind.AddIncoming:
                return AddIncoming(s, newCopyContents);
            case StepKind.DeleteFolder:
                return DeleteFolder(s.Folder);
            case StepKind.MoveIn:
                return MoveIn(s.Folder, s.Path ?? s.Folder);
            case StepKind.AddFileToEntry:
                return AddFile(s);
            case StepKind.RemoveFileFromEntry:
                ChangeEntry(s.Folder, e => WithFiles(e, e.Items.Where(kv => !Names.Equals(kv.Key, s.Path)), e.Files.Where(f => !Names.Equals(f, s.Path))));
                return null;
            case StepKind.WriteFile:
                return WriteFile(s.Folder, s.Path);
            case StepKind.DeleteFile:
                return DeleteFile(s.Folder, s.Path);
            default:
                throw new InvalidOperationException("unknown step " + s.Kind);
        }
    }

    private void ChangeEntry(string folder, Func<ManagedEntry, ManagedEntry> change) =>
        _data.UpdateManaged(list => list.Select(e => Names.Equals(e.Folder, folder) ? change(e) : e).ToList());

    private static ManagedEntry WithFiles(ManagedEntry e, IEnumerable<KeyValuePair<string, long?>> items, IEnumerable<string> files) =>
        new ManagedEntry(e.Folder, e.SourceId, e.Kind, e.State, e.Rank, e.RankDateUtc, e.Adopted,
            items.ToDictionary(kv => kv.Key, kv => kv.Value, Names), files.ToList());

    private string? AddIncoming(PlanStep s, Func<ManagedEntry, CopyContents?>? newCopyContents)
    {
        var entry = s.Entry ?? throw new InvalidOperationException("AddIncoming needs its entry");
        if (CheckedFolder(TopPicks, entry.Folder, out var problem) == null)
            return problem;
        var contents = entry.Items.Count == 0 && entry.Files.Count == 0 ? newCopyContents?.Invoke(entry) : null;
        if (contents != null)
            entry = WithFiles(entry, contents.Items.Select(kv => new KeyValuePair<string, long?>(kv.Key, kv.Value)), contents.Files);
        _data.UpdateManaged(list => list.Any(e => Names.Equals(e.Folder, entry.Folder)) ? list : list.Concat(new[] { entry }).ToList());
        return null;
    }

    private string? AddFile(PlanStep s)
    {
        var dir = CheckedFolder(TopPicks, s.Folder, out var problem);
        if (dir == null)
            return problem;
        if (CheckedFile(dir, s.Path, out problem) == null)
            return problem;
        var path = s.Path!;
        ChangeEntry(s.Folder, e => s.ItemId.HasValue
            ? WithFiles(e, e.Items.Where(kv => !Names.Equals(kv.Key, path)).Concat(new[] { new KeyValuePair<string, long?>(path, s.ItemId) }), e.Files)
            : WithFiles(e, e.Items, e.Files.Any(f => Names.Equals(f, path)) ? e.Files : e.Files.Concat(new[] { path })));
        return null;
    }

    /// <summary>D37: delete a managed folder (whole, links inside removed as links) if it is still there.</summary>
    internal string? DeleteFolder(string folder)
    {
        var full = CheckedFolder(TopPicks, folder, out var problem);
        if (full == null)
            return problem;
        // F9, D32: only a folder on a readable managed list is ever deleted (a damaged list throws, so nothing is deleted).
        var listed = _data.ManagedForWork().Any(e => Names.Equals(e.Folder, folder));
        if (!listed && SafePaths.Exists(full) && folder.Length > 999)
            return "folder is not on the managed list";
        if (Directory.Exists(full))
            SafePaths.DeleteTree(full);
        else if (File.Exists(full))
            return "not a folder";
        return null;
    }

    /// <summary>
    /// D26: moves a prepared folder in from staging. The entry must already be on the managed list (so a folder in top-picks is
    /// always managed). If the folder is already in top-picks, nothing is done; if staging lost it, it is prepared again first.
    /// Never moves onto or into a folder that is there (D40).
    /// </summary>
    private string? MoveIn(string folder, string stagingName)
    {
        var target = CheckedFolder(TopPicks, folder, out var problem);
        if (target == null)
            return problem;
        if (!_data.ManagedForWork().Any(e => Names.Equals(e.Folder, folder)))
            throw new InvalidOperationException("'" + folder + "' must be on the managed list before it is moved in (D26)");
        if (SafePaths.Exists(target))
            return null;
        problem = StagingProblem();
        if (problem != null)
            return problem;
        var source = CheckedFolder(Staging, stagingName, out problem);
        if (source == null)
            return problem;
        if (!Directory.Exists(source))
            Restage(stagingName, source);
        Directory.CreateDirectory(TopPicks);
        Directory.Move(source, target);
        return null;
    }

    /// <summary>
    /// Prepares a copy again in staging (D26, D124): into <c>&lt;name&gt;.tpp-partial</c> first (a leftover one from a stop is
    /// removed), then renamed over the staging folder, so the folder a move takes is always whole.
    /// </summary>
    private void Restage(string stagingName, string stagingFull)
    {
        if (Reprepare == null)
            throw new IOException("staging lost '" + stagingName + "' and nothing can prepare it again");
        var partialName = stagingName + PartialSuffix;
        var partial = CheckedFolder(Staging, partialName, out var problem) ?? throw new IOException("staging folder refused: " + problem);
        if (Directory.Exists(partial))
            SafePaths.DeleteTree(partial);
        Reprepare(stagingName, partialName);
        if (!Directory.Exists(partial))
            throw new IOException("nothing was prepared again for '" + stagingName + "'");
        if (Directory.Exists(stagingFull))
            SafePaths.DeleteTree(stagingFull);
        Directory.Move(partial, stagingFull);
    }

    /// <summary>D40, D82: writes one file into a staying copy from its staging copy, atomically. The path must already be on the entry.</summary>
    private string? WriteFile(string folder, string? path)
    {
        // A staying copy keeps its folder name, so its staging name is the same (D30).
        var stagingName = folder;
        var dir = CheckedFolder(TopPicks, folder, out var problem);
        if (dir == null)
            return problem;
        var target = CheckedFile(dir, path, out problem);
        if (target == null)
            return problem;
        var entry = _data.ManagedForWork().FirstOrDefault(e => Names.Equals(e.Folder, folder));
        if (entry == null || !(entry.Items.ContainsKey(path!) || entry.Files.Contains(path!, Names)))
            throw new InvalidOperationException("'" + path + "' must be on the entry before it is written (D40)");
        var stagingDir = CheckedFolder(Staging, stagingName, out problem);
        if (stagingDir == null)
            return problem;
        var source = CheckedFile(stagingDir, path, out problem);
        if (source == null)
            return problem;
        if (!File.Exists(source))
        {
            Restage(stagingName, stagingDir);
            if (!File.Exists(source))
                throw new IOException("staging has no '" + path + "' for '" + folder + "'");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        SafePaths.WriteAtomic(target, File.ReadAllBytes(source));
        return null;
    }

    /// <summary>D82: deletes one listed file in a staying copy, if it is there, after the checks.</summary>
    private string? DeleteFile(string folder, string? path)
    {
        var dir = CheckedFolder(TopPicks, folder, out var problem);
        if (dir == null)
            return problem;
        var target = CheckedFile(dir, path, out problem);
        if (target == null)
            return problem;
        if (Directory.Exists(target))
            return "not a file";
        // D82, F9: inside a copy only files on its entry's items or files are ever deleted.
        var entry = _data.ManagedForWork().FirstOrDefault(e => Names.Equals(e.Folder, folder));
        var listed = entry != null && (entry.Items.ContainsKey(path!) || entry.Files.Contains(path!, Names));
        if (!listed && File.Exists(target))
            return "file is not on the entry";
        if (File.Exists(target))
            SafePaths.DeleteFile(target);
        return null;
    }

    // ---------------- Takeover (D38, D90, D93, D95) ----------------

    /// <summary>
    /// The takeover step's file work (D38, D95, C46, C48, C100): every old-plugin folder in top-picks not already on the list is
    /// adopted (Logic decides which and builds each entry from <paramref name="strmSources"/>), managed.json is saved, and only then
    /// is <c>takeoverDone</c> set in state.json. Safe to repeat: nothing already listed is adopted twice. Returns the adopted folders.
    /// </summary>
    /// <param name="strmSources">For each folder, its .strm paths → matched source item (EmbyLink matches the text from <see cref="ReadStrm"/>).</param>
    /// <param name="seriesOf">An episode's series (D28).</param>
    public IReadOnlyList<string> TakeOver(
        Func<FolderListing, IReadOnlyDictionary<string, long?>> strmSources,
        IReadOnlyDictionary<long, long> seriesOf)
    {
        var listings = Listings();
        var adopted = new List<string>();
        _data.UpdateState(s => s.TakeoverDone = true);
        _data.UpdateManaged(list =>
        {
            var toAdopt = Takeover.ToAdopt(listings, list.Select(e => e.Folder));
            foreach (var folder in toAdopt)
            {
                var full = CheckedFolder(TopPicks, folder.Name, out _);
                if (full == null)
                    continue;
                list.Add(Takeover.Adopt(folder, strmSources(folder), seriesOf));
                adopted.Add(folder.Name);
            }
            return list;
        });
        _data.UpdateState(s => s.TakeoverDone = true);
        return adopted;
    }

    /// <summary>The unmanaged folders for the report (D102).</summary>
    public IReadOnlyList<string> UnmanagedFolders() =>
        Takeover.UnmanagedFolders(Listings(), _data.LoadManaged().Value?.Select(e => e.Folder) ?? Enumerable.Empty<string>());
}
