using System;

namespace TopPicksPlus.Logic;

/// <summary>
/// The name rules Logic checks before Storage deletes, replaces or writes anything (D37 for folders, D82 for paths inside a copy).
/// Storage does the rest: where a path resolves and whether anything on the way is a junction or link.
/// Each check returns null when the name is safe, or a short reason for <c>refusedDeletes</c>.
/// </summary>
public static class PathRules
{
    /// <summary>
    /// D37: a managed folder name is safe to act on only if it is a single plain name: not empty, not "." or "..", no path
    /// separator, no character Windows forbids, no trailing dot or space (Windows would quietly drop them and reach a different
    /// folder), and not a device name such as CON.
    /// </summary>
    public static string? FolderNameProblem(string? folder)
    {
        if (string.IsNullOrEmpty(folder))
            return "empty folder name";
        return SegmentProblem(folder!, "folder name");
    }

    /// <summary>
    /// D82: a path from a managed entry's <c>items</c> or <c>files</c> is safe only if it is relative (no drive letter, no
    /// leading <c>\</c>, no <c>\\server</c>), made only of plain names separated by <c>\</c>, none of them empty, "." or "..",
    /// with no forbidden character (so a <c>/</c> is refused too), no trailing dot or space and no device name.
    /// </summary>
    public static string? RelativePathProblem(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "empty path";
        var p = path!;
        if (p[0] == '\\' || p[0] == '/')
            return "rooted path";
        if (p.Length >= 2 && p[1] == ':')
            return "path with a drive letter";
        var parts = p.Split('\\');
        foreach (var part in parts)
        {
            if (part.Length == 0)
                return "empty part in path";
            var problem = SegmentProblem(part, "path part");
            if (problem != null)
                return problem;
        }
        return null;
    }

    private static string? SegmentProblem(string name, string what)
    {
        if (name == "." || name == "..")
            return what + " is . or ..";
        foreach (var c in name)
        {
            if (c == '\\' || c == '/')
                return what + " holds a path separator";
            if (Naming.IsForbiddenChar(c))
                return what + " holds a forbidden character";
        }
        var last = name[name.Length - 1];
        if (last == '.' || last == ' ')
            return what + " ends in a dot or space";
        if (Naming.IsReservedName(name))
            return what + " is a Windows device name";
        return null;
    }
}
