---
summary: What the plugin stores, where, and the exact shape of each field. Kept minimal on purpose; no viewing history (D21, D33). Version 4.
links: [spec.md, decisions.md]
---

# Data

Everything lives on the server; nothing is sent anywhere.

- **Plugin data folder:** `<programdata>\data\TopPicksPlus\` (D23). Holds managed.json, plays.json, run.json, state.json, `staging\` and `backups\`. Owned by Store.
- **Settings:** Emby's own plugin configuration file, `<programdata>\plugins\configurations\TopPicksPlus.xml`. Only what the owner chooses (D29).

**Common rules for the JSON files**
- Read and written with System.Text.Json (D30). UTF-8, written atomically: write `<name>.tmp`, then replace the old file with it in one step.
- Each file starts with `"schemaVersion": 1`. A different number means damaged (spec F9).
- **IDs** are Emby item IDs as strings, exactly as Emby gives them, compared ordinal. **UTC times** are ISO 8601 with a `Z` and whole seconds, e.g. `2026-11-15T05:00:00Z`. **UK days** are `YYYY-MM-DD` in Europe/London.
- Paths are stored relative to the Top Picks folder, with `\` between parts, e.g. `Heat (1995)\Heat (1995).strm`.

## Settings (`TopPicksPlus.xml`)
| Field | Type | Meaning |
|-------|------|---------|
| LibraryIds | list of IDs | Movie and TV libraries to choose from. Empty = every movie and TV library except Top Picks |
| Pins | ordered list of IDs, at most 20 | Source movies or series, in the owner's order; no duplicates (checked on save, F8) |

## state.json – run state (D29)
| Field | Type | Meaning |
|-------|------|---------|
| lastSuccessfulRunUtc | UTC time or null | End of the last `finished` or `incomplete` run (F1). Null = never |
| takeoverDone | true/false | True once the first-run takeover (F9) has completed |
| importTried | true/false | True once the one-off import (F6) has been tried, whether or not it worked |
| pinTitles | list of `{ sourceId, title }` | Last-known title of each pin, updated each run, so a missing pin can still be named |

## managed.json – the managed list
```
{ "schemaVersion": 1, "copies": [ ... ] }
```
One entry per copy:
| Field | Type | Meaning |
|-------|------|---------|
| folder | text | Folder name directly inside the Top Picks folder (unique, ignoring case) |
| sourceId | ID or null | The source movie or series. Null only for an adopted folder that matched nothing (F9) |
| kind | `Movie` / `Series` / `Unknown` | `Unknown` only with a null sourceId |
| state | `incoming` / `active` / `adopted` | `incoming` = entry written, folder not yet moved in (F4 step 9); `adopted` = taken over, not yet updated or deleted |
| rank | 1–10 or null | This week's rank; null for adopted entries |
| rankDateUtc | UTC time or null | Date added this copy should have (F5) |
| itemId | ID or null | The copy item: the copy movie or copy series (F4 step 10). Null until found |
| files | list | Every .strm the plugin wrote in this copy: `{ "path": relative path, "sourceItemIds": [IDs of the source movie or episodes it holds], "itemId": ID of the copy movie or copy episode, or null until found }`. Used to match plays and watched state (F6, F7), the rescan wait, dates and the order guard |

Rule: a folder is only ever deleted if it is in this list, and then only through the checks in spec F9.

## plays.json – popularity counts (D21, D33: no viewing history)
```
{ "schemaVersion": 1, "today": [ ... ], "daily": [ ... ] }
```
- **today**: sittings from UK days not yet rolled up (normally just today). One entry per playback stopped or played-by-hand mark:
  `userId` (ID), `sourceId` (movie or series, never the copy), `itemId` (the source movie or episode), `dayUk`, `minutes` (whole number ≥ 0, rounded down), `markedPlayed` (true/false), `runtimeMinutes` (whole number or null).
  Roll-up (1 minute after start-up, 00:05 UK daily, each run's step 2) turns entries whose `dayUk` is before the current UK day into `daily` counts and deletes them.
- **daily**: one entry per title per finished UK day: `sourceId`, `dayUk`, `viewers` (whole number ≥ 1). No user IDs. Entries more than 90 days before the current UK day are removed at every roll-up.
- **Viewer-days** for a title (F2) = the sum of its `viewers` over the 7 UK days before the run's UK day.
- Store holds one lock around every read-change-write of this file.

## run.json – the current or last run
```
{ "schemaVersion": 1, ... }
```
| Field | Type | Meaning |
|-------|------|---------|
| runId | text | New for each run; kept on resume |
| trigger | `schedule` / `run now` / `missed run` | What started it. A resume keeps the original trigger and sets `resumed` |
| resumed | true/false | True if the run was carried on after a restart |
| startedUtc | UTC time | Run start (spec "Words used here") |
| rankTimeUtc | UTC time | Run start rounded down to the minute; never changed |
| finishedUtc | UTC time or null | |
| step | `guards` / `rollup` / `backup` / `takeover` / `import` / `choose` / `plan` / `prepare` / `swap` / `rescan` / `dates` / `artwork` / `check` / `done` | Step in progress (spec F4); `done` once recorded |
| status | `running` / `finished` / `incomplete` / `failed` / `refused` | `refused` = stopped by a guard (nothing changed, or on resume a guard that failed before the swap); `failed` = an error at any step after step 1, or a guard failing on resume after the swap began; `incomplete` = rescan wait ran out or a swap operation was skipped |
| message | text | Plain words for the report |
| timeZoneWarning | true/false | Server time zone isn't Europe/London |
| picks | list | `rank`, `sourceId`, `title`, `kind`, `reason` (`pinned` / `watched` / `filler`), `viewerDays`, `copyApplied`, `dateApplied`, `artworkDrawn` (drawn, not the fallback), `artworkRefreshed` (true/false each) |
| missingPins | list | `sourceId`, `title` (from state.json pinTitles; may be empty) |
| operations | list | The plan (F4 step 7): `kind` (`add` / `update` / `delete`), `folder`, `sourceId`, `done` (true/false), `note` (why skipped, if it was) |
| skipped | list | Items left out and why (name too long, line break in path, drawing failed, check failed …) |
| rowCheck | object | `result` (`passed` / `partial` / `failed` / `not run`), `userKind` (`non-admin` / `admin`), `expected` (list of sourceIds), `returned` (list of `{id, name}` exactly as Emby gave them), `unmatched` (names of returned items that match no copy), `hiddenByParentalControls` (list of sourceIds) |
| viewerDaysTotal | whole number | Sum of viewer-days in this run's 7-day window |
| pluginVersion, embyVersion | text | |

Used to resume (F5) and to show the report (F8). Holds no user IDs or names. A damaged run.json is renamed `run.json.damaged-YYYYMMDD-HHMM`; the 3 newest are kept.

## staging\ – scratch folder
Inside the plugin data folder. Emptied by Store at F4 step 8 (and when a resume restarts that step), never during the swap.

## backups\
- `YYYY-MM-DD-HHMM\` (run start, UK time) – managed.json, state.json, plays.json without `today`, and TopPicksPlus.xml, copied at F4 step 3; the 8 newest are kept. A folder is complete when all four files are present.
- `takeover-YYYY-MM-DD.zip` – the Top Picks folder before the first-run takeover (spec F9 for theme videos); checked after writing; kept until the owner deletes it.
