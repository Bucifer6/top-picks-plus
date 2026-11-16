using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Logic;

/// <summary>One section of a user's home screen as Emby lists it (D152).</summary>
public sealed class HomeSectionInfo
{
    public HomeSectionInfo(string id, string? sectionType, string? viewType, string? parentId, string? customName = null)
    {
        Id = id;
        SectionType = sectionType;
        ViewType = viewType;
        ParentId = parentId;
        CustomName = customName;
    }

    public string Id { get; }
    public string? SectionType { get; }
    public string? ViewType { get; }
    public string? ParentId { get; }
    public string? CustomName { get; }
}

/// <summary>What to change on one user's home screen (D152). Empty when nothing needs changing.</summary>
public sealed class HomeScreenPlan
{
    public HomeScreenPlan(bool addSlideshow, string? updateSlideshow, IReadOnlyList<string> delete, bool addRow)
    {
        AddSlideshow = addSlideshow;
        UpdateSlideshow = updateSlideshow;
        Delete = delete;
        AddRow = addRow;
    }

    /// <summary>Add the slideshow (then move it to the top).</summary>
    public bool AddSlideshow { get; }

    /// <summary>The slideshow already there, refreshed so its settings stay current; null if none.</summary>
    public string? UpdateSlideshow { get; }

    /// <summary>Sections to remove: extra copies of the slideshow, the Latest Top Picks row, or the slideshow when turned off.</summary>
    public IReadOnlyList<string> Delete { get; }

    /// <summary>Put the Latest Top Picks row back (the slideshow was turned off and had replaced it).</summary>
    public bool AddRow { get; }

    public bool Empty => !AddSlideshow && UpdateSlideshow == null && Delete.Count == 0 && !AddRow;
}

/// <summary>
/// D152: the Top Picks slideshow on each user's home screen. The slideshow is Emby's own "items" section in the "spotlight" view,
/// recognised by its parent being the Top Picks library; the row is Emby's "latestmedia" section for that library.
/// </summary>
public static class HomeScreenRules
{
    public const string ItemsSection = "items";
    public const string Spotlight = "spotlight";
    public const string LatestSection = "latestmedia";

    /// <summary>The slideshow's title on the home screen.</summary>
    public const string Title = "Top Picks";

    private static readonly HomeScreenPlan Nothing = new HomeScreenPlan(false, null, Array.Empty<string>(), false);

    /// <summary>Emby's id for a library's Latest row.</summary>
    public static string RowId(string libraryId) => LatestSection + "_" + libraryId;

    /// <summary>
    /// The plugin's slideshow: an items section in the spotlight view whose parent is the Top Picks library, or, should Emby have
    /// stored it without its parent, one with no parent titled <see cref="Title"/> (so a second copy is never added).
    /// </summary>
    public static bool IsSlideshow(HomeSectionInfo s, string libraryId) =>
        Same(s.SectionType, ItemsSection) && Same(s.ViewType, Spotlight)
        && (Same(s.ParentId, libraryId) || (string.IsNullOrEmpty(s.ParentId) && Same(s.CustomName, Title)));

    public static bool IsRow(HomeSectionInfo s, string libraryId) =>
        Same(s.Id, RowId(libraryId)) || (Same(s.SectionType, LatestSection) && Same(s.ParentId, libraryId));

    /// <summary>
    /// With the slideshow on: exactly one slideshow (added if missing, extra copies removed, the one kept refreshed) and no Latest
    /// Top Picks row. With it off, nothing is touched unless the plugin itself turned it on before (<paramref name="wasOn"/>), so a
    /// spotlight a user made on the Top Picks library is never removed; then the slideshow goes and the row is put back only if
    /// a slideshow was there to take its place, so a row the user removed themselves stays removed.
    /// </summary>
    public static HomeScreenPlan Plan(IEnumerable<HomeSectionInfo> sections, string libraryId, bool slideshowOn, bool wasOn)
    {
        if (!slideshowOn && !wasOn)
            return Nothing;
        var list = sections.ToList();
        var shows = list.Where(s => IsSlideshow(s, libraryId)).ToList();
        var rows = list.Where(s => IsRow(s, libraryId)).ToList();
        if (slideshowOn)
        {
            var delete = shows.Skip(1).Concat(rows).Select(s => s.Id).ToList();
            return new HomeScreenPlan(shows.Count == 0, shows.FirstOrDefault()?.Id, delete, false);
        }
        return new HomeScreenPlan(false, null, shows.Select(s => s.Id).ToList(), shows.Count > 0 && rows.Count == 0);
    }

    /// <summary>
    /// The folders the slideshow leaves out: every library but the Top Picks one, each by every id Emby knows it by (its number and
    /// its GUID), so only Top Picks titles show whatever form a client uses.
    /// </summary>
    public static IReadOnlyList<string> ExcludedFolders(IEnumerable<(string InternalId, string? Guid)> libraries, string topPicksId)
    {
        var result = new List<string>();
        foreach (var (internalId, guid) in libraries)
        {
            if (Same(internalId, topPicksId))
                continue;
            result.Add(internalId);
            if (!string.IsNullOrEmpty(guid) && !Same(guid, internalId))
                result.Add(guid!);
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool Same(string? a, string? b) =>
        !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
