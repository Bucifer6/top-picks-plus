# Top Picks Plus (Emby plugin)

**Purpose:** each week, put the 10 best titles (manual pins + most watched) in the "Latest Top Picks" home-screen row, numbered and in the right order every time.

**Rigour level:** 1 – Personal.
**Autonomy:** high (owner prefers fewer questions; rigour 1). **Change size:** Big (new plugin replacing an old one).
**Stage:** 6 Build. Stage 5 passed 2026-11-08. Chunk 3 (Artwork) done: review keep, samples approved, pull request open (owner merges after the docs pull request #5). Next: chunk 1 (Logic). Then 2 Storage, proof plugin, 4 EmbyLink, 5 Settings (D92).

## Rules
Follows the build-software-safely method: talk and docs first, no code before the readiness gate; the owner makes big decisions; reviews come from fresh agents; one job per session with a handover in docs/handover/.

## Standing rules
(none yet)

## Docs
- docs/spec.md – what it does (F1–F10, v7)
- docs/checks.md – how the owner tests it (C1–C100)
- docs/decisions.md – what's decided (D1–D100) and what's still open
- docs/data.md – settings, plugin flags (state.json), managed list, play log, run record
- docs/backlog.md – findings set aside for the chunk that deals with them; each chunk's builder fixes or decides its items
- docs/reviews/ – challenge round and scaffold review, with what's still owed

## Folders
| Folder | Purpose |
|--------|---------|
| docs/ | Spec, checks, decisions, handover, logbook |
| src/TopPicksPlus/ | The plugin (one DLL). Parts and their walls: src/TopPicksPlus/README.md |
| src/TopPicksPlus/Logic | Pure rules (choosing, counting, naming, rank times, takeover); no Emby |
| src/TopPicksPlus/Storage | managed.json, plays.json, run.json, state.json (the plugin's own flags, D93), staging, and every file change in top-picks (D43); atomic writes |
| src/TopPicksPlus/Artwork | Numbered 1000x562 pictures (SkiaSharp, not shipped) |
| src/TopPicksPlus/EmbyLink | Everything that talks to Emby: task, events, swap, dates, guard, row check |
| src/TopPicksPlus/Settings | Admin settings page and run report |
| tests/ | xUnit tests for Logic, Storage and Artwork; Boundaries/ is the boundary check |
| proof/TopPicksProof/ | Separate tiny proof plugin (never Top Picks Plus), installed before chunk 4 is kept to prove things on the server (D76, D77) |
| .github/workflows/ | build.yml: build, tests, version check, audit, secret scan; Release on a v1.2.3 tag on main. Also keeps the C65 artwork samples and checks no SkiaSharp ships; the SkiaSharp audit exception is in (D97). A Windows test job, global.json and Stryker are added by chunk 1, the proof job by chunk 4 (D87) |

## Building
Boundary check: tests/TopPicksPlus.Tests/Boundaries (runs on every change; layout approved by the owner 2026-11-08).
GitHub Actions builds the plugin DLL (decision D5). Nothing is built on the owner's PC.

## Facts found 2026-11-08
- Emby Server 4.10.2.0, programdata at C:\Users\Admin\AppData\Roaming\Emby-Server\programdata; media on N:\Movies and K:\Series.
- Emby runs on .NET 8.0.32 and ships System.Text.Json 8, SkiaSharp 2.88.3, SQLitePCL.pretty 1.2.2. Old plugin: Emby.TopPicks.dll 1.0.0.63. Playback Reporting history: data\playback_reporting.db, table PlaybackActivity.
- The old plugin orders the row by writing <dateadded> into each .nfo (rank 1 = run time, each rank 1 hour earlier). Emby ignores it for titles already in the library, so leftover folders keep their old dates and the order breaks.
