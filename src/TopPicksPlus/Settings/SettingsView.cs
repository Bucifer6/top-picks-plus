using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.GenericEdit;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Plugins.UI.Views;
using TopPicksPlus.EmbyLink;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Settings;

/// <summary>The settings page as Emby's plugin UI sees it (F8, D148): one page, the plugin's main configuration page.</summary>
public sealed class SettingsController : IPluginUIPageController
{
    private readonly string _pluginId;

    public SettingsController(string pluginId)
    {
        _pluginId = pluginId;
        PageInfo = new PluginPageInfo
        {
            Name = "TopPicksPlusSettings",
            DisplayName = "Top Picks Plus",
            // D148: also listed in the dashboard's side menu, so the report is one click away.
            EnableInMainMenu = true,
            MenuIcon = "list_alt",
            IsMainConfigPage = true,
        };
    }

    public PluginPageInfo PageInfo { get; }

    public Task Initialize(CancellationToken token) => Task.CompletedTask;

    public Task<IPluginUIView> CreateDefaultPageView() => Task.FromResult<IPluginUIView>(new SettingsView(_pluginId));
}

/// <summary>
/// What the page does (F8, D61, D74, D93, D148): fills the choices and the report, carries out the buttons, and saves the owner's
/// settings (libraries, row-check user, pins) in Emby's plugin settings. It never writes state.json or any other data file (D93):
/// Run now asks EmbyLink, which applies the normal guards (D94). Every allowed value is worked out from Emby on the server, never
/// trusted from what the page posts back.
/// </summary>
public sealed class SettingsView : IPluginPageView
{
    private const int SearchLimit = 25;

    public SettingsView(string pluginId)
    {
        PluginId = pluginId;
        var page = new SettingsPage();
        var settings = Settings;
        page.Libraries = string.Join(",", (settings.LibraryIds ?? new long[0]).Select(Text));
        page.RowCheckUser = settings.RowCheckUserId ?? string.Empty;
        page.CreateLibraryIfMissing = settings.CreateLibraryIfMissing;
        page.ShowAsSlideshow = settings.ShowAsSlideshow;
        ContentData = Fill(page, null);
    }

    // Emby's interface needs the event; this page never raises it (it is refreshed by its own buttons).
    public event EventHandler<GenericEventArgs<IPluginUIView>>? UIViewInfoChanged
    {
        add { }
        remove { }
    }

    public string Caption => "Top Picks Plus";

    public string SubCaption => "Settings and last run report";

    public string PluginId { get; }

    /// <summary>Emby replaces this with what the page posts before each command, so it is always read afresh.</summary>
    public IEditableObject ContentData { get; set; }

    public UserDto? User { get; set; }

    public string? RedirectViewUrl { get; set; }

    public bool ShowSave { get; set; } = true;

    public bool ShowBack { get; set; }

    public bool AllowSave { get; set; } = true;

    public bool AllowBack { get; set; } = true;

    private static PluginConfiguration Settings => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    public bool IsCommandAllowed(string commandKey) => true;

    public Task Cancel() => Task.CompletedTask;

    public void OnDialogResult(IPluginUIView dialogView, bool completedOk, object data)
    {
    }

    public Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        if (!(ContentData is SettingsPage page))
        {
            ContentData = Fill(new SettingsPage(), "The page couldn't be read; open it again.");
            return Task.FromResult<IPluginUIView>(this);
        }
        string? message = null;
        switch (commandId)
        {
            case SettingsPage.RunNowCommand:
                message = RunNow();
                break;
            case SettingsPage.SearchCommand:
                page.SearchChoices = SearchResults(page.SearchText);
                page.SearchResult = page.SearchChoices.FirstOrDefault()?.Value ?? string.Empty;
                message = page.SearchChoices.Any() ? null : Host.Current == null ? NotStarted : "Nothing found.";
                break;
            case SettingsPage.AddPinCommand:
                message = AddPin(page.SearchResult);
                break;
            case SettingsPage.MoveUpCommand:
                message = MovePin(page.SelectedPin, -1);
                break;
            case SettingsPage.MoveDownCommand:
                message = MovePin(page.SelectedPin, +1);
                break;
            case SettingsPage.RemovePinCommand:
                message = RemovePin(page.SelectedPin);
                break;
            // "Refresh this page" needs nothing more: the page is filled again below.
        }
        // Unsaved library and row-check choices are kept as posted; only Save stores them.
        ContentData = Fill(page, message);
        return Task.FromResult<IPluginUIView>(this);
    }

    public Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
    {
        var plugin = Plugin.Instance;
        var h = Host.Current;
        if (!(ContentData is SettingsPage page) || plugin == null || h == null)
        {
            ContentData = Fill(ContentData as SettingsPage ?? new SettingsPage(), "Not saved: " + NotStarted);
            return Task.FromResult<IPluginUIView>(this);
        }
        // The allowed values come from Emby now, not from the lists the page posted back.
        var libraries = new HashSet<long>(LibraryOptions(h).Select(o => long.Parse(o.Value, CultureInfo.InvariantCulture)));
        var users = new HashSet<string>(EmbyFacts.AllUsers(h).Select(EmbyFacts.UserKey), StringComparer.OrdinalIgnoreCase);
        plugin.Configuration.LibraryIds = Ids(page.Libraries).Where(libraries.Contains).ToArray();
        plugin.Configuration.RowCheckUserId = users.Contains(page.RowCheckUser ?? string.Empty) ? page.RowCheckUser! : string.Empty;
        plugin.Configuration.CreateLibraryIfMissing = page.CreateLibraryIfMissing;
        plugin.Configuration.ShowAsSlideshow = page.ShowAsSlideshow;
        plugin.SaveConfiguration();
        HomeScreen.Apply(h);
        page.Libraries = string.Join(",", plugin.Configuration.LibraryIds.Select(Text));
        page.RowCheckUser = plugin.Configuration.RowCheckUserId;
        page.CreateLibraryIfMissing = plugin.Configuration.CreateLibraryIfMissing;
        page.ShowAsSlideshow = plugin.Configuration.ShowAsSlideshow;
        ContentData = Fill(page, "Saved.");
        return Task.FromResult<IPluginUIView>(this);
    }

    // ---------------- commands ----------------

    private const string NotStarted = "Emby hasn't finished starting; try again in a minute.";

    private static string RunNow()
    {
        var h = Host.Current;
        if (h == null)
            return NotStarted;
        // Always through the normal guards (D94): a refusal, such as a run already in progress, is recorded in the report.
        Task.Run(() => RunEngine.Request(h, Trigger.RunNow, CancellationToken.None));
        return "Run requested. Press \"Refresh this page\" to see what happened.";
    }

    private static string? AddPin(string? value)
    {
        var h = Host.Current;
        var plugin = Plugin.Instance;
        if (h == null || plugin == null)
            return NotStarted;
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return "Search for a title, choose it in the results, then press \"Pin it\".";
        var item = h.Library.GetItemById(id);
        if (item == null)
            return "That title is no longer in Emby; search again.";
        var pins = (plugin.Configuration.Pins ?? new long[0]).ToList();
        var refusal = PinRules.CheckAdd(pins, id, item.GetType().Name, h.RelativeToTopPicks(item.Path) != null);
        if (refusal != PinRefusal.None)
            return PinRefusalText(refusal);
        pins.Add(id);
        SavePins(plugin, pins);
        return "Pinned: " + Title(item);
    }

    private static string? MovePin(string? value, int by)
    {
        var plugin = Plugin.Instance;
        if (plugin == null || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return "Choose a pin first.";
        var pins = (plugin.Configuration.Pins ?? new long[0]).ToList();
        var at = pins.IndexOf(id);
        var to = at + by;
        if (at < 0 || to < 0 || to >= pins.Count)
            return null;
        pins.RemoveAt(at);
        pins.Insert(to, id);
        SavePins(plugin, pins);
        return null;
    }

    private static string? RemovePin(string? value)
    {
        var plugin = Plugin.Instance;
        if (plugin == null || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return "Choose a pin first.";
        SavePins(plugin, (plugin.Configuration.Pins ?? new long[0]).Where(p => p != id).ToList());
        return "Pin removed.";
    }

    // Moving or removing never drops pins; only adding is limited to 10 (PinRules.CheckAdd).
    private static readonly object PinGate = new object();

    private static void SavePins(Plugin plugin, List<long> pins)
    {
        lock (PinGate)
        {
            plugin.Configuration.Pins = pins.ToArray();
            plugin.SaveConfiguration();
        }
    }

    private static string PinRefusalText(PinRefusal refusal) => refusal switch
    {
        PinRefusal.TooMany => "There are already 10 pins. Remove one first.",
        PinRefusal.Duplicate => "That title is already pinned.",
        PinRefusal.NotMovieOrSeries => "Only a whole movie or series can be pinned.",
        PinRefusal.InTopPicksLibrary => "That is a Top Picks copy; pin the real title instead.",
        _ => "That title can't be pinned.",
    };

    private static List<EditorSelectOption> SearchResults(string? text)
    {
        var h = Host.Current;
        if (h == null || string.IsNullOrWhiteSpace(text))
            return new List<EditorSelectOption>();
        // Asks for more than shown, so Top Picks copies (left out) can't use up the places.
        return h.Library.GetItemList(new InternalItemsQuery
            {
                SearchTerm = text!.Trim(),
                IncludeItemTypes = new[] { "Movie", "Series" },
                Recursive = true,
                Limit = SearchLimit * 3,
            })
            .Where(i => h.RelativeToTopPicks(i.Path) == null)
            .Take(SearchLimit)
            .Select(i => Option(Text(i.InternalId), Title(i) + " – " + i.GetType().Name))
            .ToList();
    }

    // ---------------- filling the page ----------------

    private static IEnumerable<EditorSelectOption> LibraryOptions(Host h)
    {
        var topPicks = EmbyFacts.FindTopPicksLibrary(h)?.Id;
        return h.Library.GetVirtualFolders()
            .Select(f => (Id: h.Library.GetInternalId(f.ItemId), f.Name))
            .Where(f => f.Id != topPicks)
            .Select(f => Option(Text(f.Id), f.Name))
            .ToList();
    }

    /// <summary>Fills every choice, the pins, the run status and the report from Emby, the settings and run.json (read only).</summary>
    private static SettingsPage Fill(SettingsPage page, string? message)
    {
        var h = Host.Current;
        var settings = Settings;
        page.LastAction = new LabelItem(message ?? string.Empty) { IsVisible = !string.IsNullOrEmpty(message) };
        page.RunState = RunEngine.LockHeld
            ? new StatusItem("Status", "A run is in progress.", ItemStatus.InProgress)
            : new StatusItem("Status", "No run in progress.", ItemStatus.None);
        page.RunNow = new ButtonItem("Run now") { Data1 = SettingsPage.RunNowCommand, IsEnabled = !RunEngine.LockHeld };
        var held = RunEngine.HeldMessage;
        page.Held = new LabelItem(held ?? string.Empty) { IsVisible = !string.IsNullOrEmpty(held) };
        if (h == null)
        {
            page.Report = NotStarted;
            return page;
        }

        try
        {
            page.LibraryChoices = LibraryOptions(h);
            var users = EmbyFacts.AllUsers(h);
            page.UserChoices = new[] { Option(string.Empty, "Default") }
                .Concat(users.Select(u => Option(EmbyFacts.UserKey(u), u.Name)))
                .ToList();

            var run = h.Data.LoadRun();
            var missing = new HashSet<long>(run.Value?.MissingPins ?? new List<long>());
            var pins = settings.Pins ?? new long[0];
            var list = new GenericItemList();
            var pinChoices = new List<EditorSelectOption>();
            for (var i = 0; i < pins.Length; i++)
            {
                var item = h.Library.GetItemById(pins[i]);
                var name = item == null ? "Title no longer in Emby (" + Text(pins[i]) + ")" : Title(item);
                var note = item == null || missing.Contains(pins[i]) ? "missing: not used in the last run" : item.GetType().Name;
                list.Add(new GenericListItem { PrimaryText = Text(i + 1) + ". " + name, SecondaryText = note });
                pinChoices.Add(Option(Text(pins[i]), Text(i + 1) + ". " + name));
            }
            page.PinList = list;
            page.PinChoices = pinChoices;
            if (!pinChoices.Any(c => c.Value == page.SelectedPin))
                page.SelectedPin = pinChoices.FirstOrDefault()?.Value ?? string.Empty;

            page.Report = ReportText.Build(h, run.Value, run.Status, users);
        }
        catch (Exception e)
        {
            // The page still opens (the run controls above work) if Emby can't answer one of these.
            h.Log.ErrorException("Top Picks Plus: couldn't fill the settings page", e);
            page.Report = "Part of this page couldn't be read from Emby: " + e.Message;
        }
        return page;
    }

    private static EditorSelectOption Option(string value, string? name) =>
        new EditorSelectOption { Value = value, Name = name ?? value, IsEnabled = true };

    private static string Text(long n) => n.ToString(CultureInfo.InvariantCulture);

    private static string Title(BaseItem item) =>
        item.ProductionYear.HasValue ? item.Name + " (" + Text(item.ProductionYear.Value) + ")" : item.Name ?? "?";

    private static IEnumerable<long> Ids(string? text) =>
        (text ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();
}
