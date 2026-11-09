---
summary: What the plugin stores, where and in what shape. Kept minimal on purpose.
links: [spec.md, decisions.md]
---

# Data

Each JSON file (managed.json, plays.json, run.json, state.json) carries `"version": 1`; a newer version in any of them makes runs refuse (`dataNewer`, D72) and stops all writing to all four files (D85, D93). File-shape names are camelCase; Logic's records carry no JSON attributes, Storage maps them (D88). All plugin data lives in `programdata\plugins\TopPicksPlus\` on the server (JSON files written atomically with System.Text.Json, D24: write a temp file, then rename). Nothing is sent anywhere.

## Settings (Emby plugin configuration, `plugins\configurations\TopPicksPlus.xml`)
| Field | Meaning |
|-------|---------|
| LibraryIds | Emby IDs of the libraries to choose from (empty = all movie and TV libraries except Top Picks) |
| Pins | Ordered list of up to 10 movie or series Emby IDs (need not be in LibraryIds, D23, D61) |
| RowCheckUserId | User the row check looks through; empty = default rule (D20) |

The settings XML holds only what the owner sets on the settings page. The plugin's own flags are in state.json (D93), so saving the page can't change them.

**IDs:** every item and library ID stored anywhere here is Emby's numeric InternalId; user IDs are Emby user GUIDs (D48).

## state.json – the plugin's own flags (D93)
Owned by Storage; written atomically; never written by the settings page. Missing file = all three unset (first install).
| Field | Meaning |
|-------|---------|
| version | 1 (D72, D85) |
| takeoverDone | True once the first-run takeover (F9) has happened. Saved only after managed.json is saved (D38) |
| playbackReportingImported | True once the one-off import (F6) has been tried. Saved only after plays.json holds the import's rows and marker (D80, D93) |
| listeningSinceUtc | When the plugin first started listening for plays (the first start with the old plugin not loaded); the import cut-off (D46, D91). Set once |

If state.json can't be read, every run refuses (`stateDamaged`); it is renamed and rebuilt from plays.json, managed.json and run.json, with a notice on the report; only if one of those is also unreadable does the run refuse (D93, D95).

## managed.json – the managed list
One entry per copy folder, **keyed by `folder`** (never by sourceId): `sourceId` (Emby ID of the source title, empty for an adopted folder that matched nothing), `kind` (Movie / Series), `folder` (folder name inside the Top Picks folder), `state` (`incoming` / `active` / `leaving`, D26), `rank` (1–10, or none for any unranked copy, D74), `rankDateUtc` (none when unranked; the order guard skips those); set and cleared only by the swap, so no two entries ever share a rank (D101), `adopted` (true if taken over from the old plugin), `items` (each video file in the copy: path relative to the folder → source item ID, D28), `files` (every other file the plugin wrote in the folder). For adopted folders, `kind`, `items` and `files` are filled as D90 says.
Rules: a folder is only ever deleted if it is in this list; inside a staying copy, only files in `items` or `files` are ever deleted, and only after the path checks in D82 (relative, plain names, inside the copy, no reparse points). If this file can't be read, every run refuses (D32).

## plays.json – the play log
One entry per playback stopped: `userId`, `sourceId` (movie or series, never the copy), `episodeId` (if any), `dayUk` (YYYY-MM-DD), `minutes` (stop − start position, D29), `markedPlayed` (true/false), `runtimeMinutes` (of the movie or episode, or null), `origin` (`live` / `playbackReporting` / `lastPlayed`; imported rows are written in one save, D80).
Also, once the import has run: `importMarker` (`runId` of the run that imported, `source`: playbackReporting / lastPlayed / none), saved in the same save as the imported rows, even when there were no rows; it is not removed by the 90-day trim. Any run that finds an import marker (from any run) skips the import (D96); a resume at the import step that finds one and only sets `playbackReportingImported` in state.json (D93).
Entries older than the 90 full UK days before the run day are removed (plays from the run day itself are kept) on each run (D21). If unreadable, it is renamed `plays.damaged-YYYY-MM-DD.json` and a new log started (D32). A viewer-day (F2) is computed from these entries, not stored.

## run.json – current or last run
`runId`, `startedUtc`, `rankTimeUtc` (D27), `step` (import / takeover / leftovers / prepare / swap / rescan / dates / artwork / check / record / done, D80), `status` (running / finished / incomplete / failed; failed beats incomplete beats finished, D84), `failReason` (when failed: `error` plus `message`, D81, D94; a refusal never fails a run), `resumeCount` (resumes started, D78), `picks` (rank, sourceId, folder, reason `pinned`/`viewerDays`/`filler`, viewerDays, copyApplied, dateApplied, artworkFailed, episodesSkipped), `missingPins`, `playsLast7Days`, `seedSource` (none / playbackReporting / lastPlayed, D25), `importIgnored`, `refusedDeletes` (entry or path, reason), `unmanagedFolders` (folders not on the list that hold a .strm or video file, D102), `rowCheck` (passed, user used, fellBackToAdmin, what Emby returned, extras, retried; D20, D83), `pluginVersion`, `embyVersion`, `timeZone`, `message`, `lastSuccessfulRunUtc` (the only record of it; finished or incomplete counts, D31, D90), `lastRefusal` (time, reason `alreadyRunning`/`oldPluginLoaded`/`libraryMissing`/`librarySetup`/`dataDamaged`/`stateDamaged`/`dataNewer`, `detail`: which setting or file; written alone: a refusal leaves the run's other fields exactly as they were, D94; for `dataNewer` it is not written at all, only kept in memory, D85), `notices` (time, kind, detail: e.g. a damaged file renamed, D32; cleared when a run reaches done). Field details: D81.
Saved at every step change. Used to resume an unfinished run (F5, D26) and to show the report (F8).

## staging\ – scratch folder
Only used during a run; emptied at the start of every new run, never on a resume (D26). Owned by Storage (D43). The only place outside the Top Picks folder the plugin deletes from (D35).
