using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Entities;
using TopPicksPlus.Logic;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// D152: puts the Top Picks slideshow on every user's home screen in place of the Latest Top Picks row, or takes it away again,
/// as the setting says. Emby's own home-screen sections, so every Emby app that shows home sections shows it; nothing is added
/// to Emby's web files. Never throws: a problem with one user is logged and the others carry on.
/// </summary>
public static class HomeScreen
{
    private static readonly object Gate = new object();

    public static void Apply(Host h)
    {
        lock (Gate)
        {
            try
            {
                var plugin = Plugin.Instance;
                var library = EmbyFacts.FindTopPicksLibrary(h);
                if (plugin == null || library == null)
                    return;
                var settings = plugin.Configuration;
                var on = settings.ShowAsSlideshow;
                if (!on && !settings.SlideshowWasOn)
                    return;
                var libraryId = library.Id.ToString(CultureInfo.InvariantCulture);
                var excluded = HomeScreenRules.ExcludedFolders(h.Library.GetVirtualFolders().Select(f =>
                {
                    var id = h.Library.GetInternalId(f.ItemId);
                    return (id.ToString(CultureInfo.InvariantCulture), h.Library.GetItemById(id)?.Id.ToString("N"));
                }), libraryId).ToArray();
                if (on && !settings.SlideshowWasOn)
                {
                    // Saved before any home screen changes, so a stop part-way still lets "off" clean up later.
                    settings.SlideshowWasOn = true;
                    plugin.SaveConfiguration();
                }

                var changed = 0;
                var failed = 0;
                foreach (var user in EmbyFacts.AllUsers(h))
                {
                    try
                    {
                        if (ApplyTo(h, user.InternalId, libraryId, on, excluded))
                            changed++;
                    }
                    catch (Exception e)
                    {
                        failed++;
                        h.Log.ErrorException("Top Picks Plus: couldn't update the home screen of " + user.Name, e);
                    }
                }
                if (!on && failed == 0)
                {
                    // Off everywhere: from now on, with the option off, home screens are left alone.
                    settings.SlideshowWasOn = false;
                    plugin.SaveConfiguration();
                }
                if (changed > 0)
                    h.Log.Info("Top Picks Plus: home screen {0} for {1} user(s) (D152)", on ? "slideshow set" : "row put back", changed);
            }
            catch (Exception e)
            {
                h.Log.ErrorException("Top Picks Plus: couldn't update the home screens", e);
            }
        }
    }

    private static bool ApplyTo(Host h, long userId, string libraryId, bool on, string[] excluded)
    {
        var ct = CancellationToken.None;
        var plan = HomeScreenRules.Plan(Sections(h, userId), libraryId, on, wasOn: true);
        if (plan.Empty)
            return false;
        // The row goes back before the slideshow is removed, so a stop in between can never leave a user with neither.
        if (plan.AddRow)
        {
            h.Users.AddHomeSection(userId, new ContentSection
            {
                Id = HomeScreenRules.RowId(libraryId),
                SectionType = HomeScreenRules.LatestSection,
                ParentId = libraryId,
            }, ct);
        }
        if (plan.UpdateSlideshow != null)
            h.Users.UpdateHomeSection(userId, Slideshow(plan.UpdateSlideshow, libraryId, excluded), ct);
        if (plan.AddSlideshow)
        {
            h.Users.AddHomeSection(userId, Slideshow(null, libraryId, excluded), ct);
            var added = Sections(h, userId).FirstOrDefault(s => HomeScreenRules.IsSlideshow(s, libraryId));
            if (added != null)
                h.Users.MoveHomeSections(userId, new[] { added.Id }, 0, ct);
            else
                h.Log.Warn("Top Picks Plus: the slideshow was added for user {0} but Emby didn't list it afterwards", userId);
        }
        if (plan.Delete.Count > 0)
            h.Users.DeleteHomeSections(userId, plan.Delete.ToArray(), ct);
        // A refresh of an existing slideshow happens every time and isn't counted as a change.
        return plan.AddSlideshow || plan.AddRow || plan.Delete.Count > 0;
    }

    private static HomeSectionInfo[] Sections(Host h, long userId) =>
        (h.Users.GetHomeSections(userId, CancellationToken.None)?.Sections ?? Array.Empty<ContentSection>())
            .Where(s => s != null && !string.IsNullOrEmpty(s.Id))
            .Select(s => new HomeSectionInfo(s.Id, s.SectionType, s.ViewType, s.ParentId, s.CustomName))
            .ToArray();

    /// <summary>
    /// The slideshow: the Top Picks library's movies and series, newest date added first. Rank 1 has the newest date (F4), so this
    /// is rank order. Other libraries are listed as excluded too, as Emby's own spotlight sections do.
    /// </summary>
    private static ContentSection Slideshow(string? id, string libraryId, string[] excluded) => new ContentSection
    {
        Id = id,
        Name = HomeScreenRules.Title,
        CustomName = HomeScreenRules.Title,
        SectionType = HomeScreenRules.ItemsSection,
        ViewType = HomeScreenRules.Spotlight,
        ParentId = libraryId,
        ItemTypes = new[] { "Movie", "Series" },
        ExcludedFolders = excluded,
        SortBy = "DateCreated",
        SortOrder = "Descending",
        CardSizeOffset = 0,
    };
}
