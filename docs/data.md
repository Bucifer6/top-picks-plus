---
summary: What the plugin stores, where, and the exact shape of each field. Kept minimal on purpose; no viewing history (D21).
links: [spec.md, decisions.md]
---

# Data

Everything lives on the server; nothing is sent anywhere.

- **Plugin data folder:** `programdata\data\TopPicksPlus\` (D23). Holds managed.json, plays.json, run.json, `staging\` and `backups\`.
- **Settings:** Emby's own plugin configuration file, `programdata\plugins\configurations\TopPicksPlus.xml`.

**Common rules for the JSON files**
- UTF-8, written atomically: write `<name>.tmp`, then replace the old file with it in one step.
- Each file starts with `"schemaVersion": 1`. A file with a different number is treated as damaged (spec F9).
- **IDs** are Emby item IDs as strings (Emby's own text form). **UTC times** are ISO 8601 with a `Z` and whole seconds, e.g. `2026-11-15T05:00:00Z`. **UK days** are `YYYY-MM-DD` in Europe/London.
- Paths are stored relative to the Top Picks folder, with `\` between parts, e.g. `Heat (1995)\Heat (1995).strm`.

## Settings (`TopPicksPlus.xml`)
| Field | Type | Meaning |
|-------|------|---------|
| LibraryIds | list of IDs | Libraries to choose from. Empty = every movie and TV library except Top Picks |
| Pins | ordered list of IDs | Source movies or series, in the owner's order |
| LastSuccessfulRunUtc | UTC time or empty | End of the last `finished` or `incomplete` run (F1). Empty = never |
| TakeoverDone | true/false | True once the first-run takeover (F9) has happened |
| PlaybackReportingImported | true/false | True once the one-off import (F6) has been tried, whether or not it worked |

## managed.json – the managed list
```
{ "schemaVersion": 1, "copies": [ ... ] }
```
One entry per copy:
| Field | Type | Meaning |
|-------|------|---------|
| folder | text | Folder name directly inside the Top Picks folder (unique) |
| sourceId | ID or null | The source movie or series. Null only for an adopted folder that matched nothing (F9) |
| kind | `Movie` / `Series` / `Unknown` | `Unknown` only with a null sourceId |
| state | `incoming` / `active` / `adopted` | `incoming` = entry written, folder not yet moved in (F4 step 7); `adopted` = taken over and not yet updated or deleted |
| rank | 1–10 or null | This week's rank; null for adopted entries |
| rankDateUtc | UTC time or null | Date added this copy should have (F5) |
| files | list | Every .strm the plugin wrote in this copy: `{ "path": relative path, "sourceItemId": ID of the source movie or episode }`. Used to match plays and watched state (F6, F7) |

Rule: a folder is only ever deleted if it is in this list, and then only through the delete guard (spec F9).

## plays.json – popularity counts (D21: no viewing history)
```
{ "schemaVersion": 1, "today": [ ... ], "daily": [ ... ] }
```
- **today**: sittings from UK days not yet counted (normally just today). One entry per playback stopped:
  `userId` (ID), `sourceId` (movie or series, never the copy), `itemId` (the movie or episode watched), `dayUk`, `minutes` (whole number ≥ 0), `markedPlayed` (true/false), `runtimeMinutes` (whole number or null).
  Held only so each person counts once a day. When `dayUk` is before the current UK day, its entries become `daily` counts and are deleted, at the first play or run after midnight.
- **daily**: one entry per title per finished UK day: `sourceId`, `dayUk`, `viewers` (whole number ≥ 1). No user IDs.
  Entries whose `dayUk` is more than 90 days before the current UK day are removed on every run (F4 step 3), whatever the run's outcome.
- **Viewer-days** for a title (F2) = the sum of its `viewers` over the 7 UK days before the run's UK day.

## run.json – the current or last run
```
{ "schemaVersion": 1, ... }
```
| Field | Type | Meaning |
|-------|------|---------|
| runId | text | New for each run |
| rankTimeUtc | UTC time | Fixed when the run starts choosing; never changed on resume |
| startedUtc, finishedUtc | UTC time / null | |
| step | `guards` / `backup` / `count` / `choose` / `plan` / `prepare` / `swap` / `rescan` / `dates` / `artwork` / `check` / `done` | Last step reached (spec F4) |
| status | `running` / `finished` / `incomplete` / `failed` / `refused` | `refused` = stopped by a guard before any change; `failed` = error after changes began; `incomplete` = rescan wait ran out |
| message | text | Plain words for the report |
| picks | list | `rank`, `sourceId`, `title`, `kind`, `reason` (`pinned` / `watched` / `filler`), `viewerDays`, `copyApplied`, `dateApplied`, `artworkDrawn` (true/false each) |
| missingPins | list | `sourceId`, `title` (last known, may be empty) |
| operations | list | The plan (F4 step 5): `kind` (`add` / `update` / `delete`), `folder`, `sourceId`, `done` (true/false), `note` (why skipped, if it was) |
| skipped | list | Items left out and why (name too long, line break in path, drawing failed …) |
| rowCheck | object | `result` (`passed` / `partial` / `failed` / `not run`), `userKind` (`non-admin` / `admin`), `expected` (list of sourceIds), `returned` (list of `{id, name}` exactly as Emby gave them), `hiddenByParentalControls` (list of sourceIds) |
| viewerDaysTotal | whole number | Sum of viewer-days in this run's 7-day window |
| pluginVersion, embyVersion | text | |

Used to resume an unfinished run (F5) and to show the report (F8). Holds no user IDs or names.

## staging\ – scratch folder
Inside the plugin data folder. Emptied at the start of every new run, never on resume.

## backups\
- `YYYY-MM-DD\` – managed.json, plays.json and TopPicksPlus.xml, copied before each run's swap; the 8 newest are kept.
- `takeover-YYYY-MM-DD.zip` – the Top Picks folder as it was before the first-run takeover (theme videos left out); kept until the owner deletes it.
