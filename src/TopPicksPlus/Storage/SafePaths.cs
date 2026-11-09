using System;
using System.IO;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Storage;

/// <summary>
/// The file-system half of the delete and write checks (D37, D82, D119): where a name resolves and whether anything on the way is a
/// junction, symbolic link or other reparse point. Logic's <see cref="PathRules"/> checks the names first. Every check returns
/// null when it is safe, or a short reason for <c>refusedDeletes</c>.
/// </summary>
internal static class SafePaths
{
    private static readonly StringComparison PathCase = StringComparison.OrdinalIgnoreCase;

    /// <summary>The reason given when a name reaches a file or folder listed under another name (D119).</summary>
    public const string ShortNameProblem = "name reaches a different file or folder (short name)";

    /// <summary>A full path without a trailing separator, so two spellings of one folder compare equal.</summary>
    public static string Full(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>True if the file or folder exists and is a junction, symbolic link or other reparse point.</summary>
    public static bool IsLink(string path)
    {
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }

    public static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    /// <summary>
    /// D37: a folder name on the managed list (or a staging folder name) is safe to act on only if Logic's name rules pass and it
    /// resolves to a direct child of <paramref name="root"/>. Returns the folder's full path, or the reason in <paramref name="problem"/>.
    /// The caller checks that an existing folder is not a link (<see cref="FolderProblem"/>).
    /// </summary>
    public static string? ChildFolder(string root, string? name, out string? problem)
    {
        problem = PathRules.FolderNameProblem(name);
        if (problem != null)
            return null;
        var rootFull = Full(root);
        string full;
        try
        {
            full = Full(Path.Combine(rootFull, name!));
        }
        catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is PathTooLongException)
        {
            problem = "folder name can't be resolved";
            return null;
        }
        var parent = Path.GetDirectoryName(full);
        // Stryker disable once all : defence in depth (D37); Logic's name rules above already refuse every name that could fail it.
        if (parent == null || !string.Equals(parent, rootFull, PathCase) || full.Length <= rootFull.Length)
        {
            problem = "folder is not directly inside its parent folder";
            return null;
        }
        // D119: on Windows, GetFullPath expands a short name (LONGFO~1) into the long one, which would reach another folder.
        if (!string.Equals(Path.GetFileName(full), name, PathCase))
        {
            problem = ShortNameProblem;
            return null;
        }
        return full;
    }

    /// <summary>D37 for an existing folder: refuse a junction or link (its target must never be touched).</summary>
    public static string? FolderProblem(string full) =>
        IsLink(full) ? "folder is a junction or link" : null;

    /// <summary>
    /// D82: a path from an entry's <c>items</c>/<c>files</c> (relative, <c>\</c>-separated) inside the copy folder
    /// <paramref name="folderFull"/>. Logic's name rules first; then the full path must resolve inside the folder, and no folder
    /// between the copy folder and the file, nor the file itself, may be a junction or link. Returns the full path or null.
    /// </summary>
    public static string? FileInside(string folderFull, string? relative, out string? problem)
    {
        problem = PathRules.RelativePathProblem(relative);
        if (problem != null)
            return null;
        if (IsLink(folderFull))
        {
            problem = "folder is a junction or link";
            return null;
        }
        var parts = relative!.Split('\\');
        string full;
        try
        {
            full = Full(Path.Combine(folderFull, string.Join(Path.DirectorySeparatorChar.ToString(), parts)));
        }
        catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is PathTooLongException)
        {
            problem = "path can't be resolved";
            return null;
        }
        if (!full.StartsWith(folderFull + Path.DirectorySeparatorChar, PathCase))
        {
            problem = "path resolves outside the copy";
            return null;
        }
        // D119: every part must come back as written (GetFullPath expands short names on Windows).
        if (!string.Equals(full, folderFull + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar.ToString(), parts), PathCase))
        {
            problem = ShortNameProblem;
            return null;
        }
        var walk = folderFull;
        for (var i = 0; i < parts.Length; i++)
        {
            walk = Path.Combine(walk, parts[i]);
            if (IsLink(walk))
            {
                problem = i == parts.Length - 1 ? "file is a link" : "path goes through a junction or link";
                return null;
            }
        }
        return full;
    }

    /// <summary>
    /// Deletes a folder and everything in it without ever following a link (D37): a junction or symbolic link inside is removed as
    /// a link and its target is not touched. The folder itself must already have passed <see cref="FolderProblem"/>.
    /// </summary>
    public static void DeleteTree(string folderFull)
    {
        // D126: checked again here, at the moment of deleting. A folder swapped for a junction or link after the D37 check is
        // removed as a link; its target is never entered.
        if (IsLink(folderFull))
        {
            RemoveLink(new DirectoryInfo(folderFull));
            return;
        }
        foreach (var entry in new DirectoryInfo(folderFull).EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                RemoveLink(entry);
            else if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                DeleteTree(entry.FullName);
            else
                DeleteFile(entry.FullName);
        }
        Directory.Delete(folderFull, recursive: false);
    }

    /// <summary>Removes a link itself (a junction, a symbolic link to a folder, or to a file); never its target.</summary>
    public static void RemoveLink(FileSystemInfo link)
    {
        if ((link.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
        {
            try
            {
                // Non-recursive: on a junction or folder link this removes the link only.
                Directory.Delete(link.FullName, recursive: false);
                return;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Some systems report a folder link that only File.Delete removes; fall through.
            }
        }
        File.Delete(link.FullName);
    }

    public static void DeleteFile(string full)
    {
        var attributes = File.GetAttributes(full);
        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            File.SetAttributes(full, attributes & ~FileAttributes.ReadOnly);
        File.Delete(full);
    }

    /// <summary>
    /// Writes a file atomically (D24, D26): the bytes go to a temp file in the same folder, flushed to disk, then renamed over the
    /// old file in one step, so a stop at any moment leaves either the old file or the new one, never half of one. The temp name is
    /// unique, so two writers never share it.
    /// </summary>
    /// <param name="backupFull">If set, the old file is kept under this name in the same single replace (D127); else it is gone.</param>
    public static void WriteAtomic(string full, byte[] bytes, string? backupFull = null)
    {
        var dir = Path.GetDirectoryName(full) ?? throw new IOException("no folder for " + full);
        var temp = Path.Combine(dir, Path.GetFileName(full) + "." + Guid.NewGuid().ToString("N") + TempSuffix);
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
            BeforeReplace?.Invoke(full);
            Replace(temp, full, backupFull);
        }
        finally
        {
            // Clean-up must never hide the real error (or turn a success into a failure): a temp file left behind is harmless.
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// Called with the target path after the temp file is complete and before it is renamed over the target. Null in the plugin;
    /// the Storage tests set it (per thread) to stop a save at that exact moment and prove nothing half-written is left (C36, C48, C78).
    /// </summary>
    [ThreadStatic]
    internal static Action<string>? BeforeReplace;

    /// <summary>
    /// Temp files written by <see cref="WriteAtomic"/> end in this. Only a power cut between writing and renaming can leave one;
    /// it is never read as data (a load reads only the real file name), and emptying staging removes any there.
    /// </summary>
    public const string TempSuffix = ".tpp-tmp";

    /// <summary>
    /// Reads a whole file while letting a writer replace it at the same moment (FileShare.Delete), so a reader such as the
    /// settings page never makes a save fail on Windows (D120).
    /// </summary>
    public static byte[] ReadAll(string full)
    {
        using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static void Replace(string temp, string full, string? backupFull)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                ReplaceOnce(temp, full, backupFull);
                return;
            }
            catch (IOException) when (attempt < ReplaceAttempts && File.Exists(temp))
            {
                // Windows can refuse for a moment while another program (a virus scanner, say) has the file open.
                System.Threading.Thread.Sleep(100 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < ReplaceAttempts && File.Exists(temp))
            {
                System.Threading.Thread.Sleep(100 * attempt);
            }
        }
    }

    /// <summary>Tries before a save gives up: waits of 0.1 to 0.6 s, about 2 s in all.</summary>
    private const int ReplaceAttempts = 7;

    private static void ReplaceOnce(string temp, string full, string? backupFull)
    {
        if (File.Exists(full))
        {
            File.Replace(temp, full, destinationBackupFileName: backupFull, ignoreMetadataErrors: true);
            return;
        }
        try
        {
            File.Move(temp, full);
        }
        catch (IOException) when (File.Exists(full))
        {
            // Another writer created it in between: replace it instead.
            File.Replace(temp, full, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
    }

    /// <summary>A free name for setting a file aside: <paramref name="stem"/> + extension, else " - 2", " - 3" ... (D32).</summary>
    public static string FreeName(string folder, string stem, string extension)
    {
        var name = stem + extension;
        for (var n = 2; Exists(Path.Combine(folder, name)); n++)
            name = stem + " - " + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + extension;
        return name;
    }
}
