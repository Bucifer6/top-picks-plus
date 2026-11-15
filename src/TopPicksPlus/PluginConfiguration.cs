using MediaBrowser.Model.Plugins;

namespace TopPicksPlus;

/// <summary>
/// The owner's settings, kept by Emby in <c>plugins\configurations\TopPicksPlus.xml</c> (data.md). Only what the settings page
/// sets lives here; the plugin's own flags are in Storage's state.json (D93), so saving the page can't change them.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Emby InternalIds of the libraries to choose from; empty = every movie and TV library except Top Picks (D48).</summary>
    public long[] LibraryIds { get; set; } = new long[0];

    /// <summary>Up to 10 movie or series InternalIds, in the owner's order (D23, D61).</summary>
    public long[] Pins { get; set; } = new long[0];

    /// <summary>The user GUID (32 hex digits) the row check looks through; empty = the default rule (D20).</summary>
    public string RowCheckUserId { get; set; } = string.Empty;
}
