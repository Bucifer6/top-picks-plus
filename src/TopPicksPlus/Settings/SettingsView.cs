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

/// <summary>The settings page as Emby's plugin UI sees it (F8): one page, the plugin's main configuration page.</summary>
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
/// Run now asks EmbyLink, which applies the normal guards.
/// </summary>
public sealed class SettingsView : IPluginPageView
{
    private const int SearchLimit = 25;

    public SettingsView(string pluginId)
    {
        PluginId = pluginId;
        ContentData = Fill(new SettingsPage(), null);
    }

    public event EventHandler<GenericEventArgs<IPluginUIView>>? UIViewInfoChanged;

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

    private SettingsPage Page => ContentData as SettingsPage ?? new SettingsPage();

    public bool IsCommandAllowed(string commandKey) => true;

    public Task Cancel() => Task.CompletedTask;

    public void OnDialogResult(IPluginUIView dialogView, bool completedOk, object data)
    {
    }

    public Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        var page = Page;
        string? message = null;
        switch (commandId)
        {
            case SettingsPage.RunNowCommand:
                message = RunNow();
                break;
            case SettingsPage.SearchCommand:
                page.SearchChoices = SearchResults(page.SearchText);
                page.SearchResult = page.SearchChoices.FirstOrDefault()?.Value ?? string.Empty;
                message = page.SearchChoices.Any() ? null : "Nothing found.";
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
        }
        ContentData = Fill(page, message);
        return Task.FromResult<IPluginUIView>(this);
    }

    public Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
    {
        var page = Page;
        var plugin = Plugin.Instance;
        if (plugin != null)
        {
            var allowed = new HashSet<string>(page.LibraryChoices.Select(c => c.Value));
            plugin.Configuration.LibraryIds = Ids(page.Libraries).Where(id => allowed.Contains(id.ToString(CultureInfo.InvariantCulture))).ToArray();
            plugin.Configuration.RowCheckUserId = page.UserChoices.Any(c => c.Value == page.RowCheckUser) ? page.RowCheckUser ?? string.Empty : string.Empty;
            plugin.SaveConfiguration();
        }
        ContentData = Fill(page, "Saved.");
        return Task.FromResult<IPluginUIView>(this);
    }

    // ---------------- commands ----------------

    private static string RunNow()
    {
        var h = Host.Current;
        if (h == null)
            return "Emby hasn't finished starting; try again in a minute.";
        if (RunEngine.LockHeld)
            return "A run is already in progress.";
        // The run carries on in the background; the guards decide whether it may start (D94).
        Task.Run(() => RunEngine.Request(h, Trigger.RunNow, CancellationToken.None));
        return "Run started. Press \"Refresh this page\" to follow it.";
    }

    private static string? AddPin(string? value)
    {
        var h = Host.Current;
        var plugin = Plugin.Instance;
        if (h == null || plugin == null || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return "Search for a title, choose it in the results, then press \"Pin it\".";
        var item = h.Library.GetItemById(id);
        var pins = (plugin.Configuration.Pins ?? new long[0]).ToList();
        var refusal = PinRules.CheckAdd(pins, id, item?.GetType().Name, h.RelativeToTopPicks(item?.Path) != null);
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
        var pins = (plugin.Configuration.Pins ?? new long[0]).Where(p => p != id).ToList();
        SavePins(plugin, pins);
        return "Pin removed.";
    }

    private static void SavePins(Plugin plugin, List<long> pins)
    {
        plugin.Configuration.Pins = pins.Take(PinRules.MaxPins).ToArray();
        plugin.SaveConfiguration();
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
        return h.Library.GetItemList(new InternalItemsQuery
            {
                SearchTerm = text!.Trim(),
                IncludeItemTypes = new[] { "Movie", "Series" },
                Recursive = true,
                Limit = SearchLimit,
            })
            .Where(i => h.RelativeToTopPicks(i.Path) == null)
            .Select(i => Option(i.InternalId.ToString(CultureInfo.InvariantCulture), Title(i) + " – " + i.GetType().Name))
            .ToList();
    }

    // ---------------- filling the page ----------------

    /// <summary>Fills every choice, the pins, the run status and the report from the settings and run.json (read only).</summary>
    private static SettingsPage Fill(SettingsPage page, string? message)
    {
        var h = Host.Current;
        var settings = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        page.PinMessage = new LabelItem(message ?? string.Empty) { IsVisible = !string.IsNullOrEmpty(message) };
        var held = RunEngine.HeldMessage;
        page.Held = new LabelItem(held ?? string.Empty) { IsVisible = !string.IsNullOrEmpty(held) };
        page.RunStatus = RunEngine.LockHeld
            ? new StatusItem("Status", "A run is in progress.", ItemStatus.InProgress)
            : new StatusItem("Status", "No run in progress.", ItemStatus.None);
        page.RunNow = new ButtonItem("Run now") { Data1 = SettingsPage.RunNowCommand, IsEnabled = !RunEngine.LockHeld };
        if (h == null)
        {
            page.Report = "Emby hasn't finished starting yet.";
            return page;
        }

        var topPicks = EmbyFacts.FindTopPicksLibrary(h)?.Id;
        page.LibraryChoices = h.Library.GetVirtualFolders()
            .Where(f => h.Library.GetInternalId(f.ItemId) != topPicks)
            .Select(f => Option(h.Library.GetInternalId(f.ItemId).ToString(CultureInfo.InvariantCulture), f.Name))
            .ToList();
        page.Libraries = string.Join(",", (settings.LibraryIds ?? new long[0]).Select(id => id.ToString(CultureInfo.InvariantCulture)));

        var users = EmbyFacts.AllUsers(h);
        page.UserChoices = new[] { Option(string.Empty, "Default") }
            .Concat(users.Select(u => Option(EmbyFacts.UserKey(u), u.Name)))
            .ToList();
        page.RowCheckUser = settings.RowCheckUserId ?? string.Empty;

        var run = h.Data.LoadRun();
        var missing = new HashSet<long>(run.Value?.MissingPins ?? new List<long>());
        var pins = settings.Pins ?? new long[0];
        var list = new GenericItemList();
        var pinChoices = new List<EditorSelectOption>();
        for (var i = 0; i < pins.Length; i++)
        {
            var item = h.Library.GetItemById(pins[i]);
            var name = item == null ? "Title no longer in Emby (" + pins[i].ToString(CultureInfo.InvariantCulture) + ")" : Title(item);
            var note = item == null || missing.Contains(pins[i]) ? "missing: not used in the last run" : item.GetType().Name;
            list.Add(new GenericListItem { PrimaryText = (i + 1).ToString(CultureInfo.InvariantCulture) + ". " + name, SecondaryText = note });
            pinChoices.Add(Option(pins[i].ToString(CultureInfo.InvariantCulture), (i + 1).ToString(CultureInfo.InvariantCulture) + ". " + name));
        }
        page.PinList = list;
        page.PinChoices = pinChoices;
        if (!pinChoices.Any(c => c.Value == page.SelectedPin))
            page.SelectedPin = pinChoices.FirstOrDefault()?.Value ?? string.Empty;

        page.Report = ReportText.Build(h, run.Value, run.Status, held);
        return page;
    }

    private static EditorSelectOption Option(string value, string? name) =>
        new EditorSelectOption { Value = value, Name = name ?? value, IsEnabled = true };

    private static string Title(BaseItem? item) =>
        item == null ? "?" : item.ProductionYear.HasValue
            ? item.Name + " (" + item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture) + ")"
            : item.Name ?? "?";

    private static IEnumerable<long> Ids(string? text) =>
        (text ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

    /// <summary>Tells Emby the page changed (kept for the interface; the page is refreshed by its own buttons).</summary>
    internal void Changed() => UIViewInfoChanged?.Invoke(this, new GenericEventArgs<IPluginUIView>(this));
}
