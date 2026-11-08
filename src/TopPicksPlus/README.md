# src/TopPicksPlus – the plugin

Builds into the single `TopPicksPlus.dll` the owner installs (D17). Targets netstandard2.0 against Emby SDK MediaBrowser.Server.Core 4.9.1.90, pinned (D14).

The project root (namespace `TopPicksPlus`) holds only `Plugin` (the entry class) and `PluginConfiguration` (Emby needs both), plus at most one `Polyfills.cs` that may only declare `System.Runtime.CompilerServices` (for `IsExternalInit`, so records work on netstandard2.0). Everything else lives in one of the five parts below. There is no `Properties/` folder: assembly settings such as InternalsVisibleTo go in the .csproj.

| Part | Namespace | May use | Must not use |
|------|-----------|---------|--------------|
| project root | TopPicksPlus | everything below, Emby SDK | SkiaSharp |
| Logic/ | TopPicksPlus.Logic | nothing else in the plugin; .NET only | Emby SDK, SkiaSharp, System.IO, System.Net, TimeZoneInfo, other packages, Storage, Artwork, EmbyLink, Settings, project root |
| Storage/ | TopPicksPlus.Storage | Logic | Emby SDK, SkiaSharp, Artwork, EmbyLink, Settings, project root |
| Artwork/ | TopPicksPlus.Artwork | Logic, SkiaSharp | Emby SDK, Storage, EmbyLink, Settings, project root |
| EmbyLink/ | TopPicksPlus.EmbyLink | Logic, Storage, Artwork, project root, Emby SDK | Settings, SkiaSharp |
| Settings/ | TopPicksPlus.Settings | Logic, Storage, EmbyLink, project root, Emby SDK | Artwork, SkiaSharp |

The boundary check (`tests/TopPicksPlus.Tests/Boundaries`) fails the build if any of these walls is crossed, if anything else appears in the project root, or if a file's namespace doesn't match its folder. Canary tests prove the rules can fail. Not checked automatically: the project root's own row (reviewed by hand), and Logic reading the clock (`DateTime.Now`), which reviewers check. A new part, or a new arrow in this table, needs the owner's approval.
