# src/TopPicksPlus – the plugin

Builds into the single `TopPicksPlus.dll` the owner installs (D17). Targets netstandard2.0 against Emby SDK MediaBrowser.Server.Core 4.9.1.90, pinned (D14).

The project root (namespace `TopPicksPlus`) holds only the plugin entry class and the plugin configuration class (Emby needs both). Everything else lives in one of the five parts below.

| Part | Namespace | May use | Must not use |
|------|-----------|---------|--------------|
| Logic/ | TopPicksPlus.Logic | nothing else in the plugin; .NET only | Emby SDK, SkiaSharp, Storage, Artwork, EmbyLink, Settings, project root |
| Storage/ | TopPicksPlus.Storage | Logic | Emby SDK, SkiaSharp, Artwork, EmbyLink, Settings, project root |
| Artwork/ | TopPicksPlus.Artwork | Logic, SkiaSharp | Emby SDK, Storage, EmbyLink, Settings, project root |
| EmbyLink/ | TopPicksPlus.EmbyLink | Logic, Storage, Artwork, project root, Emby SDK | Settings |
| Settings/ | TopPicksPlus.Settings | Logic, Storage, EmbyLink, project root, Emby SDK | Artwork |

The boundary check (`tests/TopPicksPlus.Tests/Boundaries`) fails the build if any of these walls is crossed. A new part, or a new arrow in this table, needs the owner's approval.
