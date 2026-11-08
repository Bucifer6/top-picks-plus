# Top Picks Plus (Emby plugin)

**Purpose:** each week, put the 10 best titles (manual pins + most watched) in the "Latest Top Picks" home-screen row, numbered and in the right order every time.

**Rigour level:** 1 – Personal.
**Stage:** 4 – Scaffold, not yet passed. Branch `scaffold`, pull request 1 unmerged. Spec v9 re-review found 2 narrow Blockers (docs/reviews/2026-11-08-spec-v9-review.md); spec v10 next. Claude merges PRs once build, tests and review pass (D27).

## Rules
Follows the build-software-safely method: talk and docs first, no code before the readiness gate; the owner makes big decisions; reviews come from fresh agents; one job per session with a handover in docs/handover/.

## Standing rules
- **R1** (2026-11-08, card 0005, approved by owner): ALWAYS, after any spec change, check every new or changed rule against the F4 step list, run.json `step`/`status`, data.md and the part walls before asking for a review. ALWAYS also walk one concrete case of every kind of operation (delete, update, add) and each restart point through every older rule that a new pre-check, lock or wait now runs before or around (added 2026-11-08, widened the same day after the v8 review, both with owner's OK, card 0005 seen 5).

## Docs
- docs/spec.md – what it does (F1–F10)
- docs/checks.md – how the owner tests it (C1–C83)
- docs/decisions.md – what's decided (D1–D55) and what's still open
- docs/data.md – settings, run state, managed list, play log, run record
- docs/modules.md – the six parts and their walls
- docs/security.md – level 1 security baseline
- docs/protected.md – protected journeys P1–P2
- docs/releases.md – release plan and log

## Folders
| Folder | Purpose |
|--------|---------|
| docs/ | Spec, checks, decisions, handover, logbook, reviews |
| src/TopPicksPlus/Logic | The rules, pure and fully tested |
| src/TopPicksPlus/Store | The plugin's own data files |
| src/TopPicksPlus/Artwork | Drawing the numbered pictures |
| src/TopPicksPlus/EmbyBridge | Everything that talks to Emby |
| src/TopPicksPlus/Run | The weekly steps in order |
| src/TopPicksPlus/Settings | Settings page and report |
| tests/TopPicksPlus.Tests | Unit tests and the boundary check |
| .github/workflows | The build (D16) |

## Building
GitHub Actions builds the plugin DLL (decision D5). Nothing is built on the owner's PC.

## Facts found 2026-11-08
- Emby Server 4.10.2.0, programdata at C:\Users\Admin\AppData\Roaming\Emby-Server\programdata; media on N:\Movies and K:\Series.
- The old plugin orders the row by writing <dateadded> into each .nfo (rank 1 = run time, each rank 1 hour earlier). Emby ignores it for titles already in the library, so leftover folders keep their old dates and the order breaks.
- Owner, 2026-11-08 10:06: in today's Latest Top Picks row (old plugin, copies carry provider IDs), films already watched in the real library show as watched. So Emby shares watched state between a copy and its real title (v5 review Blocker 5, card 0008).
- Owner, 2026-11-08 (D49, D50): a series' copy can be deleted without losing episode watch history; Emby's Nfo saver and save-artwork-into-folders are now off for the Top Picks library.
- Owner, 2026-11-08 10:52 (D43): marking a copy watched marks the real film; removing copy folders and scanning keeps the real films' watched ticks and resume points; episodes share both ways.
