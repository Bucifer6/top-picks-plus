# Code map: Top Picks Plus
Updated: 2026-11-15 (build/chunk4-embylink)

## What this is
An Emby Server plugin (one DLL, netstandard2.0, Emby SDK 4.9.1.90 pinned) that fills the "Latest Top Picks" home row with the 10 most-watched or pinned titles each week, numbered and in order. One home server; replaces the old Emby.TopPicks plugin. Built only by GitHub Actions (D5). Plain-English spec: docs/spec.md; checks: docs/checks.md.

## How it fits together
```
Emby (task, events, library)  ->  EmbyLink (RunEngine -> RunSteps; EntryPoint; OrderGuard; PlayLogger; Watched)
                                     |-> Logic    pure rules: choose 10, count plays, names, rank dates, swap plan
                                     |-> Storage  the four JSON files + every file change in top-picks and staging
                                     |-> Artwork  numbered 1000x562 pictures (Emby's own SkiaSharp)
Settings page + run report (chunk 5, not built yet) -> EmbyLink, Storage, Logic
proof/TopPicksProof  separate throwaway plugin; shares nothing with the above (D76)
```
Walls between the parts are enforced by tests/TopPicksPlus.Tests/Boundaries (table in src/TopPicksPlus/README.md).

## Run and test
Nothing runs locally (D5). Open a normal (not draft) pull request: build.yml builds, tests on Linux and Windows, runs Stryker (80% bar, changed code only on PRs), audits packages, scans for secrets, and builds the proof plugin. Artifacts: `TopPicksPlus-<version>` (DLL, 3 days), `TopPicksProof-0.0.0-proof.<run>` (3 days), `artwork-samples-<version>`, `stryker-report-<run>`. Release: push tag `v1.2.3` on main.

## Areas
### Logic (`src/TopPicksPlus/Logic/`) — details: its README
- Does: every rule as a function of plain values; no clock, no files, no Emby.
- Key files: `Choosing.cs`, `Counting.cs`, `Naming.cs`, `PathRules.cs`, `RunRules.cs`, `SwapPlan.cs`, `Takeover.cs`, `CopyText.cs` (.nfo text, `DateAddedText` D50).
- Watch out: no namespace-looking string constants (card 0012).

### Storage (`src/TopPicksPlus/Storage/`) — details: its README
- Does: managed.json, plays.json, run.json, state.json; atomic saves; all deletes/moves in top-picks and staging.
- Owns data: `programdata\plugins\TopPicksPlus\*.json`, the top-picks and staging folders.
- Watch out: Windows 8.3 short names (card 0015, D119); deletes re-check for links at the last moment (D126); unreadable file = damaged, nothing written (D128).

### Artwork (`src/TopPicksPlus/Artwork/`) — details: its README
- Does: `RankArtwork` draws the numbered picture or returns "failed"; never throws. Anton font embedded.

### EmbyLink (`src/TopPicksPlus/EmbyLink/`) — details: its README (file table)
- Does: the scheduled task and start-up plan, one run at a time (`RunEngine`), each step in D80's order (`RunSteps`), copy files (`CopyBuilder`, worked out again each time, D140), the order guard, play logging and watched sync.
- Uses: Logic for every rule, Storage for every file change (D43), Artwork for pictures, Emby's library, users, user data, sessions and providers.
- Side effects: Emby dates (`DateCreated`) and user data (watched ticks, resume points) on copies and sources; rescans and image refreshes of the Top Picks library.
- Watch out: nothing compiles locally (D145, card 0018); a vanished title or an offline drive must never fail every resume or empty the row (D147); watched state only moves forward (D147); System.Memory 4.6.0 is compiled against for the SDK (D146).

### Settings (`src/TopPicksPlus/Settings/`) — built in chunk 5 (PR #11).

### Proof plugin (`proof/TopPicksProof/`) — details: its README
- Does: Simple UI page; proves loading in 4.10.2 and how Emby reads .nfo `<dateadded>` (D131).
- Side effects: writes three test folders (six new files) under `programdata\data\top-picks-proof\` once, when the owner ticks the box; deletes nothing. Emby saves the page's fields (not the results) in `programdata` plugin configurations.
- Watch out: run after the whole build; deleted by the pull request that records its results (D130, D134).

## Change -> retest
| If you change... | Also check... | Tests to run |
|---|---|---|
| anything in Logic | Stryker on the changed file (80%) | all tests (CI) |
| Storage delete/move paths | Windows-only `_junction`/`_windows` tests must run and pass (at least 11, D125) | Windows CI job |
| a test file | Stryker becomes a full run (card 0014) | CI |
| build.yml | each self-test canary (SkiaSharp, audit filter, SQLite) still bites | CI on the PR |
| the Emby SDK version | proof job's "no SQLite" step (D129), D14 | CI |
| a wall (namespace or allowed arrow) | owner approval; canary in Boundaries | Boundaries tests |
| `CopyText.DateAddedText` | the proof result (D132), C62 | Logic tests |
| anything in EmbyLink | the go-live checks it touches (D73, D79); walls | all tests (CI), Boundaries |
| the .csproj package pins | the SDK still compiles (System.Memory, D146); nothing new ships (no Skia, no runtime assets) | CI build job |

## Shared things
- No env vars or secrets. Pinned versions: global.json (SDK 8.0.4xx), .config/dotnet-tools.json (Stryker), the .csproj files.
- Decisions: docs/decisions.md (newest last); lessons: docs/logbook/cards; set-aside findings: docs/backlog.md.
