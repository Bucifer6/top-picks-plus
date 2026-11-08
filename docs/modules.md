---
summary: The six parts of the plugin, what each owns, its front door and status. Must match the folder READMEs and tests/TopPicksPlus.Tests/BoundaryTests.cs.
links: [decisions.md, spec.md, data.md]
---

# Parts (modules)

One DLL, `TopPicksPlus.dll`. Each part is a folder and namespace under `src/TopPicksPlus/`. The plugin root (`TopPicksPlus` namespace: the plugin class) only wires parts together; no part may use a root type (the boundary check bans them). Files a part needs (page HTML, font) live inside that part's folder. Changed 2026-11-08 by D28 (owner's choice), D29 and D34 (takeover and import removed; Store no longer uses System.IO.Compression).

| Part | Owns | Front door (what others may use) | May use | Status | Contract |
|------|------|-----------------------------------|---------|--------|----------|
| Logic | The rules of F2, F3 naming (SafeName), F5 comparison, F6 counting and roll-up, F7 forward-only rule (D38), F9 path checks and the data.md field rules | Pure functions and plain records, including the paths record, the settings record and the log interface every part may use | nothing | planned | 1 |
| Store | managed.json, plays.json, run.json, run.previous.json, state.json, staging\ emptying and backups\ (in `<programdata>\data\TopPicksPlus\`, D23, D29, D37) | The `IStore` interface: load/save each file (handling damaged files as spec F9 says), roll-up under the plays lock, empty staging, make and prune backups | Logic, plain .NET file access, System.Text.Json (compile only, D30) | planned | 1 |
| Artwork | Drawing folder.png / thumb.png | The `IArtwork` interface: "draw rank N from these images" → PNG bytes or failed | Logic, SkiaSharp (compile only, D15) | planned | 1 |
| EmbyBridge | Every call to Emby; the scheduled task, timers, play and user-data listeners, settings API; the read-only **source files** adapter (D28); checking an item's path is inside the Top Picks folder before changing it (F9) | Adapter interfaces (used by Run), hook interfaces (implemented by Run) and the static `Hooks` registry the root entry point fills | Logic, Settings (configuration class), Emby SDK, read-only file access to source libraries | planned | 1 |
| Run | The weekly steps, resume, the lock, the order guard, writing copy folders, the write/delete checks | Implements EmbyBridge's hooks | Logic, Store, Artwork, EmbyBridge (interfaces only), file access to the Top Picks folder and staging only | planned | 1 |
| Settings | TopPicksPlus.xml settings (LibraryIds, Pins), settings page and report page | The configuration class (used only by EmbyBridge and the root) | Logic, Emby SDK | planned | 1 |

**Root** (`TopPicksPlus` namespace): the plugin class and the start-up entry point. It may use every part, only to build them and wire them together; no part may use a root type (the boundary check bans them).

**Banned for every part:** `System.Net` (no network calls, F9), `System.Diagnostics.Process` (no other programs started), SkiaSharp everywhere except Artwork, and the Emby SDK (`MediaBrowser.*` and `Emby.*`) everywhere except EmbyBridge and Settings. Logic and Artwork may not touch files at all (`System.IO.File`, `Directory`, `FileStream`, `FileInfo`, `DirectoryInfo`). Every plugin type must be in the root namespace or a part's namespace. Read-only for EmbyBridge's file access, "Top Picks folder and staging only" for Run, and Run using only EmbyBridge's interfaces (never its concrete classes) can't be checked by the tool; the Stage 7 review checks them.

A new part, or a new "may use" link, needs the owner's approval and an update here, in the READMEs and in BoundaryTests.cs.
