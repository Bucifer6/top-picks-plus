using System.Collections.Generic;
using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Model.Attributes;

namespace TopPicksPlus.Settings;

/// <summary>
/// The settings page's fields (F8). Emby draws the page from these properties (its plugin UI framework), so there is no HTML,
/// script or web endpoint of the plugin's own (D148). Buttons send their <c>Data1</c> as the command to <see cref="SettingsView"/>.
/// </summary>
public class SettingsPage : EditableOptionsBase
{
    public const string RunNowCommand = "RunNow";
    public const string SearchCommand = "Search";
    public const string AddPinCommand = "AddPin";
    public const string MoveUpCommand = "MoveUp";
    public const string MoveDownCommand = "MoveDown";
    public const string RemovePinCommand = "RemovePin";
    public const string RefreshCommand = "Refresh";

    public override string EditorTitle => "Top Picks Plus";

    public override string EditorDescription =>
        "Each week the Latest Top Picks row gets the 10 most watched titles, numbered and in order. Pins come first. "
        + "Press Save after changing the libraries or the row-check user; pin changes are saved straight away.";

    // ---------------- Run ----------------

    public CaptionItem CaptionRun { get; set; } = new CaptionItem("Run");

    public StatusItem RunState { get; set; } = new StatusItem("Status", string.Empty, ItemStatus.None);

    public ButtonItem RunNow { get; set; } = new ButtonItem("Run now") { Data1 = RunNowCommand };

    public ButtonItem Refresh { get; set; } = new ButtonItem("Refresh this page") { Data1 = RefreshCommand };

    public LabelItem Held { get; set; } = new LabelItem(string.Empty) { IsVisible = false };

    /// <summary>What the last button did (shown next to the Run buttons).</summary>
    public LabelItem LastAction { get; set; } = new LabelItem(string.Empty) { IsVisible = false };

    // ---------------- Libraries and row check ----------------

    public SpacerItem Spacer1 { get; set; } = new SpacerItem();

    public CaptionItem CaptionLibraries { get; set; } = new CaptionItem("Libraries and row check");

    [DisplayName("Libraries to choose from")]
    [Description("Leave empty to use every movie and TV library. The Top Picks library is never offered.")]
    [EditMultilSelect]
    [SelectItemsSource(nameof(LibraryChoices))]
    public string Libraries { get; set; } = string.Empty;

    [Browsable(false)]
    public IEnumerable<EditorSelectOption> LibraryChoices { get; set; } = new List<EditorSelectOption>();

    [DisplayName("Row-check user")]
    [Description("The user whose view of the row is checked after each run. Default: the first user who is not an administrator and has no restrictions.")]
    [SelectItemsSource(nameof(UserChoices))]
    public string RowCheckUser { get; set; } = string.Empty;

    [Browsable(false)]
    public IEnumerable<EditorSelectOption> UserChoices { get; set; } = new List<EditorSelectOption>();

    // ---------------- Pins ----------------

    public SpacerItem Spacer2 { get; set; } = new SpacerItem();

    public CaptionItem CaptionPins { get; set; } = new CaptionItem("Pins (up to 10, in this order)");

    public GenericItemList PinList { get; set; } = new GenericItemList();

    [DisplayName("Pin to change")]
    [SelectItemsSource(nameof(PinChoices))]
    public string SelectedPin { get; set; } = string.Empty;

    [Browsable(false)]
    public IEnumerable<EditorSelectOption> PinChoices { get; set; } = new List<EditorSelectOption>();

    public ButtonItem MoveUp { get; set; } = new ButtonItem("Move up") { Data1 = MoveUpCommand };

    public ButtonItem MoveDown { get; set; } = new ButtonItem("Move down") { Data1 = MoveDownCommand };

    public ButtonItem RemovePin { get; set; } = new ButtonItem("Remove") { Data1 = RemovePinCommand };

    [DisplayName("Find a movie or series to pin")]
    public string SearchText { get; set; } = string.Empty;

    public ButtonItem Search { get; set; } = new ButtonItem("Search") { Data1 = SearchCommand };

    [DisplayName("Search results")]
    [SelectItemsSource(nameof(SearchChoices))]
    public string SearchResult { get; set; } = string.Empty;

    [Browsable(false)]
    public IEnumerable<EditorSelectOption> SearchChoices { get; set; } = new List<EditorSelectOption>();

    public ButtonItem AddPin { get; set; } = new ButtonItem("Pin it") { Data1 = AddPinCommand };

    // ---------------- Report ----------------

    public SpacerItem Spacer3 { get; set; } = new SpacerItem();

    public CaptionItem CaptionReport { get; set; } = new CaptionItem("Last run report");

    // No [ReadOnly(true)]: Emby 4.10.2 leaves such properties off the page entirely (B55). The report is rebuilt
    // every time the page opens, so anything typed here is simply replaced.
    [DisplayName("")]
    [EditMultiline(24)]
    public string Report { get; set; } = string.Empty;
}
