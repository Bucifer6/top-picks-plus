using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Logic;

/// <summary>The kinds of step Storage carries out for a plan (D26, D39, D40, D43). Each one is safe to repeat.</summary>
public enum StepKind
{
    /// <summary>Set the entry's state to leaving and clear its rank and rank date (D101), then save managed.json.</summary>
    MarkLeaving,
    /// <summary>Clear a staying entry's rank and rank date before it gets a different one, so no two entries ever share a rank (D101).</summary>
    ClearRank,
    /// <summary>Set a staying entry's rank and rank date (D101), then save managed.json.</summary>
    SetRank,
    /// <summary>Delete the folder if it is still there, after the D37 checks. A refused delete stops this entry's steps; it stays leaving.</summary>
    DeleteFolder,
    /// <summary>Remove the entry from managed.json and save.</summary>
    RemoveEntry,
    /// <summary>Add the entry as incoming, with its rank and rank date, and save, before its folder exists (D26).</summary>
    AddIncoming,
    /// <summary>Move the folder in from staging if staging still has it and it isn't in top-picks yet; re-prepare it if staging lost it (D26).</summary>
    MoveIn,
    /// <summary>Set the entry to active (keeping <see cref="PlanStep.Rank"/>, which is null for an unranked leftover) and save.</summary>
    MarkActive,
    /// <summary>Add a file path to a staying entry's <c>items</c> or <c>files</c> and save, before the file is written (D40).</summary>
    AddFileToEntry,
    /// <summary>Write (or replace) the file from staging (temp file, then rename).</summary>
    WriteFile,
    /// <summary>Delete the file if it is there, after the D82 checks.</summary>
    DeleteFile,
    /// <summary>Remove a file path from the entry's <c>items</c> or <c>files</c> and save.</summary>
    RemoveFileFromEntry,
}

/// <summary>One step of a plan. Which fields are set depends on <see cref="Kind"/>.</summary>
public sealed class PlanStep
{
    public PlanStep(StepKind kind, string folder, int? rank = null, DateTime? rankDateUtc = null, string? path = null, long? itemId = null, ManagedEntry? entry = null)
    {
        Kind = kind;
        Folder = folder;
        Rank = rank;
        RankDateUtc = rankDateUtc;
        Path = path;
        ItemId = itemId;
        Entry = entry;
    }

    public StepKind Kind { get; }
    public string Folder { get; }
    public int? Rank { get; }
    public DateTime? RankDateUtc { get; }

    /// <summary>For file steps: the path relative to the copy folder. For <see cref="StepKind.MoveIn"/>: the folder's name in staging (the name fixed at prepare).</summary>
    public string? Path { get; }

    /// <summary>For file steps on a video (.strm): its source item ID, so it goes in <c>items</c>; null means <c>files</c>.</summary>
    public long? ItemId { get; }

    /// <summary>For <see cref="StepKind.AddIncoming"/>: the full entry to add.</summary>
    public ManagedEntry? Entry { get; }

}

/// <summary>One of this week's picks as the swap needs it: its rank, source, kind and the folder name fixed at prepare (saved in run.json).</summary>
public sealed class PlannedPick
{
    public PlannedPick(int rank, long sourceId, TitleKind kind, string folder)
    {
        Rank = rank;
        SourceId = sourceId;
        Kind = kind;
        Folder = folder;
    }

    public int Rank { get; }
    public long SourceId { get; }
    public TitleKind Kind { get; }

    /// <summary>For a staying copy, its existing folder; for a new one, the name chosen at prepare.</summary>
    public string Folder { get; }
}

/// <summary>A path a plan refused to touch, for <c>refusedDeletes</c> (D81, D82).</summary>
public sealed class RefusedPath
{
    public RefusedPath(string folder, string path, string reason)
    {
        Folder = folder;
        Path = path;
        Reason = reason;
    }

    public string Folder { get; }
    public string Path { get; }
    public string Reason { get; }
}

/// <summary>A file plan for one staying copy, plus the paths it refused (D82).</summary>
public sealed class FilePlan
{
    public FilePlan(IReadOnlyList<PlanStep> steps, IReadOnlyList<RefusedPath> refused)
    {
        Steps = steps;
        Refused = refused;
    }

    public IReadOnlyList<PlanStep> Steps { get; }
    public IReadOnlyList<RefusedPath> Refused { get; }
}

/// <summary>
/// Plans the swap and the leftovers as ordered steps for Storage (D26, D39, D40, D43, D101). Plans are pure functions of the
/// managed list, the folders present and the picks, so a resume simply plans again from what managed.json now says and carries
/// on: every step is safe to repeat, and planning again after any number of steps leads to the same end (C36).
/// </summary>
public static class SwapPlan
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Which existing entry stays for a pick (F4 step 2): an active, not adopted entry with the pick's source. Its folder is the
    /// pick's folder (prepare keeps a staying copy's folder name, D30). Adopted folders never stay (D40).
    /// </summary>
    public static bool IsStaying(ManagedEntry e, long sourceId) =>
        e.State == EntryState.Active && !e.Adopted && e.SourceId == sourceId;

    /// <summary>An incoming entry left by an interrupted swap of this run, for the same source (D26, resume).</summary>
    private static bool IsContinuingNew(ManagedEntry e, long sourceId) =>
        e.State == EntryState.Incoming && !e.Adopted && e.SourceId == sourceId;

    private static (Dictionary<long, ManagedEntry> Staying, Dictionary<long, ManagedEntry> Continuing) Match(List<ManagedEntry> entries, IEnumerable<PlannedPick> picks)
    {
        var staying = new Dictionary<long, ManagedEntry>();
        var continuing = new Dictionary<long, ManagedEntry>();
        foreach (var p in picks)
        {
            if (staying.ContainsKey(p.SourceId) || continuing.ContainsKey(p.SourceId))
                continue;
            var stay = entries.FirstOrDefault(e => IsStaying(e, p.SourceId));
            if (stay != null)
            {
                staying[p.SourceId] = stay;
                continue;
            }
            var cont = entries.FirstOrDefault(e => IsContinuingNew(e, p.SourceId));
            if (cont != null)
                continuing[p.SourceId] = cont;
        }
        return (staying, continuing);
    }

    /// <summary>
    /// The first half of the swap (D26, D101): every drop-out marked leaving with its rank cleared; every staying copy whose rank
    /// changes has its old rank cleared, then gets its new rank and rank date; all before any folder is touched, so no two entries
    /// ever hold the same rank, even after a stop between any two steps; then each drop-out
    /// deleted and removed from the list. File work inside staying copies (<see cref="StayingFiles"/>) follows, then
    /// <see cref="SwapNew"/>, planned from managed.json and top-picks as they are then.
    /// </summary>
    public static IReadOnlyList<PlanStep> SwapOut(IEnumerable<ManagedEntry> managed, IEnumerable<PlannedPick> picks, DateTime rankTimeUtc)
    {
        var entries = managed.ToList();
        var pickList = picks.OrderBy(p => p.Rank).ToList();
        var (staying, continuing) = Match(entries, pickList);
        var keep = new HashSet<ManagedEntry>(staying.Values.Concat(continuing.Values));
        var drops = entries.Where(e => !keep.Contains(e)).OrderBy(e => e.Folder, Names).ToList();
        var steps = new List<PlanStep>();

        foreach (var e in drops)
        {
            if (e.State != EntryState.Leaving || e.Rank.HasValue || e.RankDateUtc.HasValue)
                { }
        }
        var changes = pickList
            .Where(p => staying.ContainsKey(p.SourceId))
            .Select(p => (Entry: staying[p.SourceId], Pick: p, Date: RankTimes.RankDate(rankTimeUtc, p.Rank)))
            .Where(c => c.Entry.Rank != c.Pick.Rank || c.Entry.RankDateUtc != c.Date)
            .ToList();
        foreach (var c in changes)
        {
            if (c.Entry.Rank.HasValue || c.Entry.RankDateUtc.HasValue)
                steps.Add(new PlanStep(StepKind.ClearRank, c.Entry.Folder));
        }
        foreach (var c in changes)
            steps.Add(new PlanStep(StepKind.SetRank, c.Entry.Folder, c.Pick.Rank, c.Date));
        foreach (var e in drops)
        {
            steps.Add(new PlanStep(StepKind.DeleteFolder, e.Folder));
            steps.Add(new PlanStep(StepKind.RemoveEntry, e.Folder));
        }
        return steps;
    }

    /// <summary>
    /// The second half of the swap (D26, D40): for each new pick, added as incoming with its rank, moved in from staging, then
    /// marked active. The folder is the name fixed at prepare, unless a folder or entry with that name is still there (a refused
    /// delete), when it is the next free " - N" name (D40), never onto or into the folder that is still there. An incoming entry
    /// left by an interrupted swap is carried on under its own name.
    /// </summary>
    public static IReadOnlyList<PlanStep> SwapNew(IEnumerable<ManagedEntry> managed, IEnumerable<PlannedPick> picks, DateTime rankTimeUtc, IEnumerable<string> foldersInTopPicks)
    {
        var entries = managed.ToList();
        var pickList = picks.OrderBy(p => p.Rank).ToList();
        var (staying, continuing) = Match(entries, pickList);
        var taken = new HashSet<string>(foldersInTopPicks.Concat(entries.Select(e => e.Folder)), Names);
        var steps = new List<PlanStep>();

        foreach (var p in pickList)
        {
            if (staying.ContainsKey(p.SourceId))
                continue;
            var date = RankTimes.RankDate(rankTimeUtc, p.Rank);
            string folder;
            if (continuing.TryGetValue(p.SourceId, out var cont))
            {
                folder = cont.Folder;
            }
            else
            {
                folder = Naming.Unique(p.Folder, taken);
                taken.Add(folder);
                var entry = new ManagedEntry(folder, p.SourceId, p.Kind, EntryState.Incoming, p.Rank, date, adopted: false);
                steps.Add(new PlanStep(StepKind.AddIncoming, folder, p.Rank, date, entry: entry));
            }
            steps.Add(new PlanStep(StepKind.MoveIn, folder, p.Rank, date, path: p.Folder));
            steps.Add(new PlanStep(StepKind.MarkActive, folder, p.Rank, date));
        }
        return steps;
    }

    /// <summary>
    /// Leftovers from an earlier run, before prepare (D39): a leaving entry is deleted if its folder is still there and removed;
    /// an incoming entry whose folder is in top-picks becomes an unranked active copy (it then drops out normally); an incoming
    /// entry with no folder is removed. Active entries are left as they are.
    /// </summary>
    public static IReadOnlyList<PlanStep> Leftovers(IEnumerable<ManagedEntry> managed, IEnumerable<string> foldersInTopPicks)
    {
        var present = new HashSet<string>(foldersInTopPicks, Names);
        var steps = new List<PlanStep>();
        foreach (var e in managed.OrderBy(e => e.Folder, Names))
        {
            switch (e.State)
            {
                case EntryState.Leaving:
                    steps.Add(new PlanStep(StepKind.DeleteFolder, e.Folder));
                    steps.Add(new PlanStep(StepKind.RemoveEntry, e.Folder));
                    break;
                case EntryState.Incoming:
                    steps.Add(present.Contains(e.Folder)
                        ? new PlanStep(StepKind.MarkActive, e.Folder)
                        : new PlanStep(StepKind.RemoveEntry, e.Folder));
                    break;
            }
        }
        return steps;
    }

    /// <summary>
    /// Folder names for this week's new copies, chosen at prepare (F3, D30, D40): a name taken by any folder in top-picks
    /// (managed or not, ignoring case) gets " - 2", " - 3"; folders about to drop out (adopted ones included) don't count;
    /// two new picks never get the same name. Staying picks keep their folder and are not passed here.
    /// </summary>
    public static IReadOnlyList<string> NewFolderNames(IEnumerable<string> baseNames, IEnumerable<string> foldersInTopPicks, IEnumerable<string> droppingOut)
    {
        var dropping = new HashSet<string>(droppingOut, Names);
        var taken = new HashSet<string>(foldersInTopPicks.Where(f => !dropping.Contains(f)), Names);
        var result = new List<string>();
        foreach (var name in baseNames)
        {
            var unique = Naming.Unique(name, taken);
            taken.Add(unique);
            result.Add(unique);
        }
        return result;
    }

    /// <summary>
    /// The file plan for one staying copy (D30, D40, D82, D90, D110). <paramref name="wantedItems"/> are this week's videos
    /// (path → source item ID) and <paramref name="wantedFiles"/> the other files written every run (.nfo, artwork, subtitles,
    /// thumbs, season posters). Themes already in the copy are kept as they are and never added later (D110).
    /// New paths are added to the entry before being written; listed paths no longer wanted are deleted, then removed from the
    /// entry. Any path failing the D82 name rules is left alone and reported. Files not on the entry are never touched.
    /// </summary>
    public static FilePlan StayingFiles(ManagedEntry entry, IReadOnlyDictionary<string, long> wantedItems, IEnumerable<string> wantedFiles)
    {
        var steps = new List<PlanStep>();
        var refused = new List<RefusedPath>();
        var listed = new HashSet<string>(entry.Items.Keys.Concat(entry.Files), Names);
        var wanted = new Dictionary<string, long?>(Names);
        foreach (var kv in wantedItems)
            wanted[kv.Key] = kv.Value;
        foreach (var f in wantedFiles)
        {
            if (!wanted.ContainsKey(f))
                wanted[f] = null;
        }

        foreach (var kv in wanted.OrderBy(k => k.Key, Names))
        {
            var problem = PathRules.RelativePathProblem(kv.Key);
            if (problem != null)
            {
                refused.Add(new RefusedPath(entry.Folder, kv.Key, problem));
                continue;
            }
            if (IsTheme(kv.Key))
                continue;
            if (!listed.Contains(kv.Key))
                steps.Add(new PlanStep(StepKind.AddFileToEntry, entry.Folder, path: kv.Key, itemId: kv.Value));
            steps.Add(new PlanStep(StepKind.WriteFile, entry.Folder, path: kv.Key, itemId: kv.Value));
        }

        foreach (var path in listed.OrderBy(p => p, Names))
        {
            if (wanted.ContainsKey(path) || IsTheme(path))
                continue;
            var problem = PathRules.RelativePathProblem(path);
            if (problem != null)
            {
                refused.Add(new RefusedPath(entry.Folder, path, problem));
                continue;
            }
            steps.Add(new PlanStep(StepKind.DeleteFile, entry.Folder, path: path));
            steps.Add(new PlanStep(StepKind.RemoveFileFromEntry, entry.Folder, path: path));
        }

        return new FilePlan(steps, refused);
    }

    /// <summary>A theme video (<c>backdrops\theme.*</c>) or theme song (<c>theme.*</c>) in a copy (D55, D90).</summary>
    public static bool IsTheme(string path) =>
        path.StartsWith("theme.", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("backdrops\\theme.", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The order guard's list (F5, D52, D101): only active entries with a rank and rank date are checked; unranked, leaving and
    /// incoming entries are skipped.
    /// </summary>
    public static IReadOnlyList<ManagedEntry> Guarded(IEnumerable<ManagedEntry> managed) =>
        managed.Where(e => e.State == EntryState.Active && e.Rank.HasValue && e.RankDateUtc.HasValue).ToList();
}
