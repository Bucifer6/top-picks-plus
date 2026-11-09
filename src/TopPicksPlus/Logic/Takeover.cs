using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Logic;

/// <summary>
/// A folder in top-picks as Storage sees it: its name and every file inside it, including subfolders, as relative paths separated
/// by <c>\</c>. Storage lists it; Logic decides.
/// </summary>
public sealed class FolderListing
{
    public FolderListing(string name, IReadOnlyList<string> files)
    {
        Name = name;
        Files = files;
    }

    public string Name { get; }
    public IReadOnlyList<string> Files { get; }
}

/// <summary>Takeover of old-plugin folders and unmanaged folders (F9, D28, D38, D73, D90, D102).</summary>
public static class Takeover
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    private static readonly string[] VideoExtensions =
        { ".strm", ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".ts", ".m2ts", ".iso", ".mpg", ".mpeg", ".webm", ".flv" };

    private static bool HasExtension(string path, string ext) => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// D38 (card 0005): a folder is the old plugin's if it, <em>including its subfolders</em>, holds at least one .strm and at
    /// least one .nfo.
    /// </summary>
    public static bool IsOldPluginFolder(FolderListing folder) =>
        folder.Files.Any(f => HasExtension(f, ".strm")) && folder.Files.Any(f => HasExtension(f, ".nfo"));

    /// <summary>
    /// The folders takeover adopts (D38): old-plugin folders not already on the managed list, whose names pass the D37 rules
    /// (a folder that fails them could never be deleted safely, so it is left alone and reported as unmanaged).
    /// </summary>
    public static IReadOnlyList<FolderListing> ToAdopt(IEnumerable<FolderListing> foldersInTopPicks, IEnumerable<string> managedFolders)
    {
        var managed = new HashSet<string>(managedFolders, Names);
        return foldersInTopPicks
            .Where(f => !managed.Contains(f.Name))
            .Where(IsOldPluginFolder)
            .Where(f => PathRules.FolderNameProblem(f.Name) == null)
            .OrderBy(f => f.Name, Names)
            .ToList();
    }

    /// <summary>
    /// The managed entry for an adopted folder (D28, D74, D90, D101): active, unranked, adopted. <paramref name="strmSources"/>
    /// maps each .strm path to the source item matched through its contents (null when nothing matched); for a series that is the
    /// episode. <paramref name="seriesOf"/> gives an episode's series. The folder's source is the first matched .strm's movie or
    /// series. With no source, the kind is Series if the folder has tvshow.nfo or a Season folder, else Movie.
    /// </summary>
    public static ManagedEntry Adopt(FolderListing folder, IReadOnlyDictionary<string, long?> strmSources, IReadOnlyDictionary<long, long> seriesOf)
    {
        var items = new Dictionary<string, long?>(Names);
        foreach (var f in folder.Files.Where(f => HasExtension(f, ".strm")).OrderBy(f => f, Names))
            items[f] = strmSources.TryGetValue(f, out var matched) ? matched : null;
        var files = folder.Files.Where(f => !HasExtension(f, ".strm")).OrderBy(f => f, Names).ToList();

        long? source = null;
        TitleKind? kind = null;
        foreach (var id in items.Values)
        {
            if (!id.HasValue)
                continue;
            if (seriesOf.TryGetValue(id.Value, out var series))
            {
                source = series;
                kind = TitleKind.Series;
            }
            else
            {
                source = id.Value;
                kind = TitleKind.Movie;
            }
            break;
        }

        var looksLikeSeries = folder.Files.Any(f => Names.Equals(f, "tvshow.nfo"))
            || folder.Files.Any(f => f.StartsWith("Season ", StringComparison.OrdinalIgnoreCase) && f.IndexOf('\\') > 0);
        return new ManagedEntry(folder.Name, source, kind ?? (looksLikeSeries ? TitleKind.Series : TitleKind.Movie),
            EntryState.Active, rank: null, rankDateUtc: null, adopted: true, items, files);
    }

    /// <summary>
    /// D102 (B6): a folder in top-picks is reported as unmanaged when it is not on the managed list and holds a video file
    /// (.strm or a common video type) anywhere inside it, because only those can show in the row. A folder with no video, such as
    /// the owner's "Keep me" folder, is never reported.
    /// </summary>
    public static IReadOnlyList<string> UnmanagedFolders(IEnumerable<FolderListing> foldersInTopPicks, IEnumerable<string> managedFolders)
    {
        var managed = new HashSet<string>(managedFolders, Names);
        return foldersInTopPicks
            .Where(f => !managed.Contains(f.Name))
            .Where(f => f.Files.Any(file => VideoExtensions.Any(x => HasExtension(file, x))))
            .Select(f => f.Name)
            .OrderBy(n => n, Names)
            .ToList();
    }
}
