---
summary: What the plugin stores, where and in what shape. Kept minimal on purpose.
links: [spec.md]
---

# Data

All plugin data lives in `programdata\plugins\TopPicksPlus\` on the server (JSON files written atomically: write a temp file, then rename). Nothing is sent anywhere.

## Settings (Emby plugin configuration, `plugins\configurations\TopPicksPlus.xml`)
| Field | Meaning |
|-------|---------|
| LibraryIds | Emby IDs of the libraries to choose from (empty = all movie and TV libraries except Top Picks) |
| Pins | Ordered list of source title Emby IDs |
| LastSuccessfulRunUtc | Used for the missed-run rule (F1) |
| TakeoverDone | True once the first-run takeover (F9) has happened |
| PlaybackReportingImported | True once the one-off import (F6) has happened |

## managed.json – the managed list
One entry per copy: `sourceId` (Emby ID of the source title), `kind` (Movie / Series), `folder` (folder name inside the Top Picks folder), `rank` (1–10, or none if adopted and not yet removed), `rankDateUtc`, `adopted` (true if taken over from the old plugin).
Rule: a folder is only ever deleted if it is in this list.

## plays.json – the play log
One entry per playback stopped: `userId`, `sourceId` (movie or series, never the copy), `episodeId` (if any), `dayUk` (YYYY-MM-DD), `minutes`, `markedPlayed` (true/false), `runtimeMinutes` (or null).
Entries older than 90 days are removed on each run. A viewer-day (F2) is computed from these entries, not stored.

## run.json – current or last run
`runId`, `startedUtc`, `step` (prepare / swap / rescan / dates / artwork / check / done), `status` (running / finished / incomplete / failed / refused), `picks` (rank, sourceId, reason, viewerDays, copyApplied, dateApplied), `rowCheck` (passed, what Emby returned), `pluginVersion`, `embyVersion`, `message`.
Used to resume an unfinished run (F5) and to show the report (F8).

## staging\ – scratch folder
Only used during a run; emptied at the start of every run.
