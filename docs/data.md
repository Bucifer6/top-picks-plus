---
summary: What the plugin stores, where and in what shape. Kept minimal on purpose.
links: [spec.md]
---

# Data

All plugin data lives in `programdata\plugins\TopPicksPlus\` on the server (JSON files written atomically with System.Text.Json, D24: write a temp file, then rename). Nothing is sent anywhere.

## Settings (Emby plugin configuration, `plugins\configurations\TopPicksPlus.xml`)
| Field | Meaning |
|-------|---------|
| LibraryIds | Emby IDs of the libraries to choose from (empty = all movie and TV libraries except Top Picks) |
| Pins | Ordered list of source title Emby IDs (need not be in LibraryIds, D23) |
| RowCheckUserId | User the row check looks through; empty = default rule (D20) |
| TakeoverDone | True once the first-run takeover (F9) has happened |
| PlaybackReportingImported | True once the one-off import (F6) has happened |

## managed.json – the managed list
One entry per copy: `sourceId` (Emby ID of the source title, empty for an adopted folder that matched nothing), `kind` (Movie / Series), `folder` (folder name inside the Top Picks folder), `state` (`incoming` / `active` / `leaving`, D26), `rank` (1–10, or none if adopted and not yet removed), `rankDateUtc`, `adopted` (true if taken over from the old plugin), `items` (each video file in the copy: path relative to the folder → source item ID, D28), `files` (every other file the plugin wrote in the folder).
Rules: a folder is only ever deleted if it is in this list; inside a staying copy, only files in `items` or `files` are ever deleted. If this file can't be read, every run refuses (D32).

## plays.json – the play log
One entry per playback stopped: `userId`, `sourceId` (movie or series, never the copy), `episodeId` (if any), `dayUk` (YYYY-MM-DD), `minutes` (stop − start position, D29), `markedPlayed` (true/false), `runtimeMinutes` (of the movie or episode, or null).
Entries older than the 90 full UK days before the run day are removed (plays from the run day itself are kept) on each run (D21). If unreadable, it is renamed `plays.damaged-YYYY-MM-DD.json` and a new log started (D32). A viewer-day (F2) is computed from these entries, not stored.

## run.json – current or last run
`runId`, `startedUtc`, `rankTimeUtc` (D27), `step` (prepare / swap / rescan / dates / artwork / check / record / done), `status` (running / finished / incomplete / failed), `resumeCount`, `picks` (rank, sourceId, folder, reason `pinned`/`viewerDays`/`filler`, viewerDays, copyApplied, dateApplied, artworkFailed), `missingPins`, `playsLast7Days`, `seedSource` (none / playbackReporting / lastPlayed, D25), `rowCheck` (passed, user used, what Emby returned), `pluginVersion`, `embyVersion`, `message`, `lastSuccessfulRunUtc` (the only record of it; finished or incomplete counts, D31), `lastRefusal` (time, reason `alreadyRunning`/`oldPluginLoaded`/`libraryMissing`/`dataDamaged`; never overwrites the run).
Saved at every step change. Used to resume an unfinished run (F5, D26) and to show the report (F8).

## staging\ – scratch folder
Only used during a run; emptied at the start of every new run, never on a resume (D26). The only place outside the Top Picks folder the plugin deletes from (D35).
