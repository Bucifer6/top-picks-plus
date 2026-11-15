using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using TopPicksPlus.Logic;

namespace TopPicksPlus.EmbyLink;

/// <summary>Reads movies, series and episodes from Emby for choosing (F2) and for building copies (F3).</summary>
public static class Catalog
{
    /// <summary>A title's kind, or null when it is neither a movie nor a series.</summary>
    public static TitleKind? KindOf(BaseItem? item) =>
        item is Movie ? TitleKind.Movie : item is Series ? TitleKind.Series : (TitleKind?)null;

    /// <summary>A real file Emby can play exists for this item (F2 step 5).</summary>
    public static bool FileFound(BaseItem item) =>
        !string.IsNullOrEmpty(item.Path) && SafeExists(item.Path);

    private static bool SafeExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>A series' episodes that are real files (not missing or virtual), in no particular order.</summary>
    public static IReadOnlyList<Episode> Episodes(Host h, long seriesId) =>
        h.Library.GetItemList(new InternalItemsQuery
        {
            AncestorIds = new[] { seriesId },
            IncludeItemTypes = new[] { "Episode" },
            Recursive = true,
            IsVirtualItem = false,
        }).OfType<Episode>().ToList();

    /// <summary>
    /// The candidates in the given libraries (F2 step 5): movies whose file can be found and series with at least one episode file
    /// that can be found; newest content = a movie's date added or a series' newest episode's date added (F2 step 3).
    /// </summary>
    public static IReadOnlyList<Candidate> Candidates(Host h, IEnumerable<long> libraryIds)
    {
        var result = new List<Candidate>();
        foreach (var lib in libraryIds)
        {
            foreach (var movie in Query(h, lib, "Movie").OfType<Movie>())
                result.Add(new Candidate(movie.InternalId, TitleKind.Movie, movie.DateCreated.UtcDateTime, movie.PremiereDate?.UtcDateTime, FileFound(movie)));

            var episodes = Query(h, lib, "Episode").OfType<Episode>().ToLookup(e => e.SeriesId);
            foreach (var series in Query(h, lib, "Series").OfType<Series>())
            {
                var eps = episodes[series.InternalId].ToList();
                var newest = eps.Count > 0 ? eps.Max(e => e.DateCreated.UtcDateTime) : series.DateCreated.UtcDateTime;
                result.Add(new Candidate(series.InternalId, TitleKind.Series, newest, series.PremiereDate?.UtcDateTime, eps.Any(FileFound)));
            }
        }
        return result;
    }

    private static BaseItem[] Query(Host h, long libraryId, string type) =>
        h.Library.GetItemList(new InternalItemsQuery
        {
            AncestorIds = new[] { libraryId },
            IncludeItemTypes = new[] { type },
            Recursive = true,
            IsVirtualItem = false,
        });

    /// <summary>
    /// The pins with whether each can be used (D23, D61): the title still exists, is a movie or series, and a file of it can be
    /// found. A pin need not be in the chosen libraries.
    /// </summary>
    public static IReadOnlyList<PinStatus> Pins(Host h, IEnumerable<long> pins) =>
        pins.Select(id =>
        {
            var item = h.Library.GetItemById(id);
            var usable = item switch
            {
                Movie m => FileFound(m),
                Series s => Episodes(h, s.InternalId).Any(FileFound),
                _ => false,
            };
            return new PinStatus(id, usable);
        }).ToList();
}
