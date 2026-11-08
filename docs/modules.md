---
summary: The six parts of the plugin, what each owns, its front door and status. Must match the folder READMEs and tests/TopPicksPlus.Tests/BoundaryTests.cs.
links: [decisions.md, spec.md, data.md]
---

# Parts (modules)

One DLL, `TopPicksPlus.dll`. Each part is a folder and namespace under `src/TopPicksPlus/`. The plugin root (`TopPicksPlus` namespace: the plugin class) only wires parts together; no part may use a root type (the boundary check bans them). Files a part needs (page HTML, font) live inside that part's folder.

| Part | Owns | Front door (what others may use) | May use | Status | Contract |
|------|------|-----------------------------------|---------|--------|----------|
| Logic | The rules of F2, F3 naming (SafeName), F5 comparison, F6 counting, F9 takeover | Pure functions and plain records, including the paths record and log interface every part may use | nothing | planned | 1 |
| Store | managed.json, plays.json, run.json, staging\, backups\ (in `programdata\data\TopPicksPlus\`, D23) | The `IStore` interface: load/save each file, empty staging, make and prune backups | Logic | planned | 1 |
| Artwork | Drawing folder.png / thumb.png | The `IArtwork` interface: "draw rank N from these images" → PNG bytes or failed | Logic | planned | 1 |
| EmbyBridge | Every call to Emby; the scheduled task, play listener, settings API; reading and saving settings | Adapter interfaces (used by Run), hook interfaces (implemented by Run) and the static `Hooks` registry the root fills at start-up | Logic, Settings (configuration class), Emby SDK | planned | 1 |
| Run | The weekly steps, resume, order guard, takeover, writing copy folders | Implements EmbyBridge's hooks | Logic, Store, Artwork, EmbyBridge | planned | 1 |
| Settings | TopPicksPlus.xml settings, settings page and report | The configuration class (used only by EmbyBridge) | Logic, Emby SDK | planned | 1 |

A new part, or a new "may use" link, needs the owner's approval and an update here, in the READMEs and in BoundaryTests.cs.
