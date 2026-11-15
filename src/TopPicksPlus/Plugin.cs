using System;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using TopPicksPlus.Logic;

namespace TopPicksPlus;

/// <summary>The plugin's entry class (D33: its ID never changes). The work is done by EmbyLink's entry point and task.</summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>The loaded plugin, so EmbyLink can read the owner's settings.</summary>
    public static Plugin? Instance { get; private set; }

    public override string Name => "Top Picks Plus";

    public override string Description => "Fills the Latest Top Picks row with the 10 most watched or pinned titles each week, numbered and in order.";

    public override Guid Id => EmbyRules.ThisPluginId;

    /// <summary>The build's version (CI stamps it, D16); shown at the top of each run report (F10).</summary>
    public static string BuildVersion =>
        typeof(Plugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
}
