using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using MediaBrowser.Controller.Library;
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
    private static bool warned;

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
                if (!Supported)
                {
                    if (!warned)
                        h.Log.Warn("Top Picks Plus: the home-screen slideshow needs Emby 4.10 or later; the row is left as it is");
                    warned = true;
                    return;
                }
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
            var row = NewSection();
            Set(row, "Id", HomeScreenRules.RowId(libraryId));
            Set(row, "SectionType", HomeScreenRules.LatestSection);
            Set(row, "ParentId", libraryId);
            Call(h, "AddHomeSection", userId, row, ct);
        }
        if (plan.UpdateSlideshow != null)
            Call(h, "UpdateHomeSection", userId, Slideshow(plan.UpdateSlideshow, libraryId, excluded), ct);
        if (plan.AddSlideshow)
        {
            Call(h, "AddHomeSection", userId, Slideshow(null, libraryId, excluded), ct);
            var added = Sections(h, userId).FirstOrDefault(s => HomeScreenRules.IsSlideshow(s, libraryId));
            if (added != null)
                Call(h, "MoveHomeSections", userId, new[] { added.Id }, 0, ct);
            else
                h.Log.Warn("Top Picks Plus: the slideshow was added for user {0} but Emby didn't list it afterwards", userId);
        }
        if (plan.Delete.Count > 0)
            Call(h, "DeleteHomeSections", userId, plan.Delete.ToArray(), ct);
        // A refresh of an existing slideshow happens every time and isn't counted as a change.
        return plan.AddSlideshow || plan.AddRow || plan.Delete.Count > 0;
    }

    private static HomeSectionInfo[] Sections(Host h, long userId)
    {
        var result = Invoke(h, "GetHomeSections", userId, CancellationToken.None);
        var sections = result?.GetType().GetProperty("Sections")?.GetValue(result) as IEnumerable;
        if (sections == null)
            return Array.Empty<HomeSectionInfo>();
        return sections.OfType<object>()
            .Where(s => !string.IsNullOrEmpty(Get(s, "Id")))
            .Select(s => new HomeSectionInfo(Get(s, "Id")!, Get(s, "SectionType"), Get(s, "ViewType"), Get(s, "ParentId"), Get(s, "CustomName")))
            .ToArray();
    }

    /// <summary>
    /// The slideshow: the Top Picks library's movies and series, newest date added first. Rank 1 has the newest date (F4), so this
    /// is rank order. Other libraries are listed as excluded too, as Emby's own spotlight sections do.
    /// </summary>
    private static object Slideshow(string? id, string libraryId, string[] excluded)
    {
        var s = NewSection();
        if (id != null)
            Set(s, "Id", id);
        Set(s, "Name", HomeScreenRules.Title);
        Set(s, "CustomName", HomeScreenRules.Title);
        Set(s, "SectionType", HomeScreenRules.ItemsSection);
        Set(s, "ViewType", HomeScreenRules.Spotlight);
        Set(s, "ParentId", libraryId);
        Set(s, "ItemTypes", new[] { "Movie", "Series" });
        Set(s, "ExcludedFolders", excluded);
        Set(s, "SortBy", "DateCreated");
        Set(s, "SortOrder", "Descending");
        Set(s, "CardSizeOffset", 0);
        return s;
    }

    // ---------------- Emby 4.10 members (D152) ----------------
    // The pinned SDK (4.9, D14) lacks most of Emby's home-section API: the calls that change sections and most of the section's
    // fields (CI runs 142, 143). Emby 4.10 has them all, so everything here is found at run time on the server's own types,
    // with no compile-time use at all; without them the slideshow isn't available and that is said once in the log.

    private static readonly string[] SectionProperties =
        { "Id", "Name", "CustomName", "SectionType", "ViewType", "ParentId", "ItemTypes", "ExcludedFolders", "SortBy", "SortOrder" };

    /// <summary>True when the running Emby has every home-section call and section field the slideshow needs.</summary>
    public static bool Supported
    {
        get
        {
            if (!new[] { "GetHomeSections", "UpdateHomeSection", "DeleteHomeSections", "MoveHomeSections" }.All(n => Method(n) != null))
                return false;
            var type = SectionType();
            return type != null && type.GetConstructor(Type.EmptyTypes) != null && SectionProperties.All(p => type.GetProperty(p) != null);
        }
    }

    private static MethodInfo? Method(string name) =>
        typeof(IUserManager).GetMethods().FirstOrDefault(m => m.Name == name && m.GetParameters().LastOrDefault()?.ParameterType == typeof(CancellationToken));

    /// <summary>The section type, taken from AddHomeSection's second parameter.</summary>
    private static Type? SectionType()
    {
        var p = Method("AddHomeSection")?.GetParameters();
        return p != null && p.Length == 3 ? p[1].ParameterType : null;
    }

    private static object NewSection() =>
        Activator.CreateInstance(SectionType() ?? throw new NotSupportedException("this Emby has no AddHomeSection"))!;

    private static object? Invoke(Host h, string name, params object[] args)
    {
        var m = Method(name) ?? throw new NotSupportedException("this Emby has no " + name);
        try
        {
            return m.Invoke(h.Users, args);
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    private static void Call(Host h, string name, params object[] args) => Invoke(h, name, args);

    private static string? Get(object s, string property) => s.GetType().GetProperty(property)?.GetValue(s) as string;

    private static void Set(object s, string property, object value) => s.GetType().GetProperty(property)?.SetValue(s, value);
}