# Top Picks Plus (Emby plugin)

**Purpose:** each week, put the 10 best titles (manual pins + most watched) in the "Latest Top Picks" home-screen row, numbered and in the right order every time.

**Rigour level:** 1 – Personal.
**Stage:** 5 readiness in progress – reviews scored 72 then 71; fixes D43–D75 and checks C51–C75 on branch spec/readiness-fixes (owner to merge). Next: a fresh re-score.

## Rules
Follows the build-software-safely method: talk and docs first, no code before the readiness gate; the owner makes big decisions; reviews come from fresh agents; one job per session with a handover in docs/handover/.

## Standing rules
(none yet)

## Docs
- docs/spec.md – what it does (F1–F10, v5)
- docs/checks.md – how the owner tests it (C1–C75)
- docs/decisions.md – what's decided (D1–D75) and what's still open
- docs/data.md – settings, managed list, play log, run record
- docs/reviews/ – challenge round and scaffold review, with what's still owed

## Folders
| Folder | Purpose |
|--------|---------|
| docs/ | Spec, checks, decisions, handover, logbook |
| src/TopPicksPlus/ | The plugin (one DLL). Parts and their walls: src/TopPicksPlus/README.md |
| src/TopPicksPlus/Logic | Pure rules (choosing, counting, naming, rank times, takeover); no Emby |
| src/TopPicksPlus/Storage | managed.json, plays.json, run.json, staging, and every file change in top-picks (D43); atomic writes |
| src/TopPicksPlus/Artwork | Numbered 1000x562 pictures (SkiaSharp, not shipped) |
| src/TopPicksPlus/EmbyLink | Everything that talks to Emby: task, events, swap, dates, guard, row check |
| src/TopPicksPlus/Settings | Admin settings page and run report |
| tests/ | xUnit tests for Logic; Boundaries/ is the boundary check |
| .github/workflows/ | build.yml: build, tests, version check, audit, secret scan; Release on a v1.2.3 tag on main. A Windows test job, global.json and Stryker are added by chunk 1 (D43, D57, D66) |

## Building
Boundary check: tests/TopPicksPlus.Tests/Boundaries (runs on every change; layout approved by the owner 2026-11-08).
GitHub Actions builds the plugin DLL (decision D5). Nothing is built on the owner's PC.

## Facts found 2026-11-08
- Emby Server 4.10.2.0, programdata at C:\Users\Admin\AppData\Roaming\Emby-Server\programdata; media on N:\Movies and K:\Series.
- Emby runs on .NET 8.0.32 and ships System.Text.Json 8, SkiaSharp 2.88.3, SQLitePCL.pretty 1.2.2. Old plugin: Emby.TopPicks.dll 1.0.0.63. Playback Reporting history: data\playback_reporting.db, table PlaybackActivity.
- The old plugin orders the row by writing <dateadded> into each .nfo (rank 1 = run time, each rank 1 hour earlier). Emby ignores it for titles already in the library, so leftover folders keep their old dates and the order breaks.
