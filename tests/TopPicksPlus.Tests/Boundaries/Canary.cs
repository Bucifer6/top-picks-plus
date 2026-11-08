// Deliberate wall-breakers used only by BoundaryTests.Canaries_prove_every_wall_bites: one type per
// forbidden entry per part, each marked [Breaks("entry")]. That test fails if a forbidden list and
// these canaries get out of step, so add a canary whenever you add a forbidden entry.
// They live in the TEST assembly, so they never reach the plugin. Never copy these into src/.
#pragma warning disable CS0436 // local fakes of TopPicksPlus.Plugin/PluginConfiguration win over the plugin's own
#nullable disable
namespace TopPicksPlus.Tests.Boundaries
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class BreaksAttribute : System.Attribute
    {
        public BreaksAttribute(string entry) { Entry = entry; }
        public string Entry { get; }
    }
}
namespace MediaBrowser.CanaryTarget { public class Thing { } }
namespace Emby.CanaryTarget { public class Thing { } }
namespace SkiaSharp { public class CanaryTarget { } }
namespace Newtonsoft.CanaryTarget { public class Thing { } }
namespace Microsoft.Extensions.CanaryTarget { public class Thing { } }
namespace SQLitePCL.CanaryTarget { public class Thing { } }
namespace TopPicksPlus { public class Plugin { } public class PluginConfiguration { } }
namespace TopPicksPlus.Storage { public class CanaryTarget { } }
namespace TopPicksPlus.Artwork { public class CanaryTarget { } }
namespace TopPicksPlus.EmbyLink { public class CanaryTarget { } }
namespace TopPicksPlus.Settings { public class CanaryTarget { } }
namespace TopPicksPlus.Logic.BoundaryCanaries
{
    using TopPicksPlus.Tests.Boundaries;
    [Breaks("MediaBrowser")] public class Breaks00 { public static global::MediaBrowser.CanaryTarget.Thing Field; }
    [Breaks("Emby")] public class Breaks01 { public static global::Emby.CanaryTarget.Thing Field; }
    [Breaks("System.IO")] public class Breaks02 { public static global::System.IO.FileInfo Field; }
    [Breaks("System.Net")] public class Breaks03 { public static global::System.Net.IPAddress Field; }
    [Breaks("System.TimeZoneInfo")] public class Breaks04 { public static global::System.TimeZoneInfo Field; }
    [Breaks("Newtonsoft")] public class Breaks05 { public static global::Newtonsoft.CanaryTarget.Thing Field; }
    [Breaks("Microsoft.Extensions")] public class Breaks06 { public static global::Microsoft.Extensions.CanaryTarget.Thing Field; }
    [Breaks("SQLitePCL")] public class Breaks07 { public static global::SQLitePCL.CanaryTarget.Thing Field; }
    [Breaks("SkiaSharp")] public class Breaks08 { public static global::SkiaSharp.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Storage")] public class Breaks09 { public static global::TopPicksPlus.Storage.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Artwork")] public class Breaks10 { public static global::TopPicksPlus.Artwork.CanaryTarget Field; }
    [Breaks("TopPicksPlus.EmbyLink")] public class Breaks11 { public static global::TopPicksPlus.EmbyLink.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Settings")] public class Breaks12 { public static global::TopPicksPlus.Settings.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Plugin")] public class Breaks13 { public static global::TopPicksPlus.Plugin Field; }
    [Breaks("TopPicksPlus.PluginConfiguration")] public class Breaks14 { public static global::TopPicksPlus.PluginConfiguration Field; }
}
namespace TopPicksPlus.Storage.BoundaryCanaries
{
    using TopPicksPlus.Tests.Boundaries;
    [Breaks("MediaBrowser")] public class Breaks00 { public static global::MediaBrowser.CanaryTarget.Thing Field; }
    [Breaks("Emby")] public class Breaks01 { public static global::Emby.CanaryTarget.Thing Field; }
    [Breaks("SkiaSharp")] public class Breaks02 { public static global::SkiaSharp.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Artwork")] public class Breaks03 { public static global::TopPicksPlus.Artwork.CanaryTarget Field; }
    [Breaks("TopPicksPlus.EmbyLink")] public class Breaks04 { public static global::TopPicksPlus.EmbyLink.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Settings")] public class Breaks05 { public static global::TopPicksPlus.Settings.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Plugin")] public class Breaks06 { public static global::TopPicksPlus.Plugin Field; }
    [Breaks("TopPicksPlus.PluginConfiguration")] public class Breaks07 { public static global::TopPicksPlus.PluginConfiguration Field; }
}
namespace TopPicksPlus.Artwork.BoundaryCanaries
{
    using TopPicksPlus.Tests.Boundaries;
    [Breaks("MediaBrowser")] public class Breaks00 { public static global::MediaBrowser.CanaryTarget.Thing Field; }
    [Breaks("Emby")] public class Breaks01 { public static global::Emby.CanaryTarget.Thing Field; }
    [Breaks("TopPicksPlus.Storage")] public class Breaks02 { public static global::TopPicksPlus.Storage.CanaryTarget Field; }
    [Breaks("TopPicksPlus.EmbyLink")] public class Breaks03 { public static global::TopPicksPlus.EmbyLink.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Settings")] public class Breaks04 { public static global::TopPicksPlus.Settings.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Plugin")] public class Breaks05 { public static global::TopPicksPlus.Plugin Field; }
    [Breaks("TopPicksPlus.PluginConfiguration")] public class Breaks06 { public static global::TopPicksPlus.PluginConfiguration Field; }
}
namespace TopPicksPlus.EmbyLink.BoundaryCanaries
{
    using TopPicksPlus.Tests.Boundaries;
    [Breaks("SkiaSharp")] public class Breaks00 { public static global::SkiaSharp.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Settings")] public class Breaks01 { public static global::TopPicksPlus.Settings.CanaryTarget Field; }
}
namespace TopPicksPlus.Settings.BoundaryCanaries
{
    using TopPicksPlus.Tests.Boundaries;
    [Breaks("SkiaSharp")] public class Breaks00 { public static global::SkiaSharp.CanaryTarget Field; }
    [Breaks("TopPicksPlus.Artwork")] public class Breaks01 { public static global::TopPicksPlus.Artwork.CanaryTarget Field; }
}
