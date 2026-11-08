---
summary: What the plugin stores, where, and the exact shape of each field. Kept minimal on purpose; no viewing history (D21, D33). Version 9 (Store's writes never go through a link, D55).
links: [spec.md, decisions.md]
---

# Data

Everything lives on the server; nothing is sent anywhere.

- **Plugin data folder:** `<programdata>\data\TopPicksPlus\` (D23). Holds managed.json, plays.json, run.json, run.previous.json, state.json, `staging\` and `backups\`. Owned by Store.
- **Settings:** Emby's own plugin configuration file, `<programdata>\plugins\configurations\TopPicksPlus.xml`. Only what the owner chooses (D29). Store copies it into backups as a file and never reads it.

**Common rules for the JSON files**
- Read and written with System.Text.Json (D30). UTF-8, written atomically: write `<name>.tmp`, created new (if one is already there it is deleted first, as a link if it is a link), then replace the old file with it in one step. Just before the replace, the live file is checked not to be a link; if it is, it is removed as a link and the `.tmp` renamed into its place. A file that is a link is never read: it counts as missing and is removed as a link. If the write fails, Store deletes its `<name>.tmp` at once (so a failed plays.json write never leaves user IDs behind). Leftover `*.tmp` files in the plugin data folder are also removed at start-up.
- Each file starts with `"schemaVersion": 1`. A different number, or any field that breaks the rules below, means **damaged** (spec F9).
- **IDs** are Emby IDs (spec "Words used here") as strings, exactly as Emby gives them, compared ordinal. **UTC times** are ISO 8601 with a `Z` and whole seconds, e.g. `2026-11-15T05:00:00Z`. **UK days** are `YYYY-MM-DD` in Europe/London. Names of dated files and folders use UTC: `YYYY-MM-DD-HHMMZ` (folders) and `YYYYMMDD-HHMMZ` (damaged files).
- Paths are stored relative to the Top Picks folder, with `\` between parts, e.g. `Heat (1995)\Heat (1995).strm`. Every part is a single name (spec "Words used here": SafeName's cleaning leaves it unchanged, so no `..`, `/`, `:`, drive letter, trailing dot or space, `~`, reserved name or empty part).
- All fields listed are required (a field allowed to be null must be present as null).
- **A missing file is not damaged.** Missing state.json = never run (`lastSuccessfulRunUtc` null, `pinTitles` empty, both pending flags false). Missing managed.json = no copies (but see the copy-looking-folder rule, spec F9). Missing plays.json = no plays yet. Missing run.json = no run yet, nothing to resume. Missing run.previous.json = no earlier successful run to show. Each is written when first needed.
- A file that exists but can't be opened or read (locked, disk error) is not damaged either: it is left alone and the action fails as an error (spec F9).

## Settings (`TopPicksPlus.xml`)
| Field | Type | Meaning |
|-------|------|---------|
| LibraryIds | list of IDs | Movie and TV libraries to choose from. Empty = every movie and TV library except Top Picks |
| Pins | ordered list of IDs, at most 20 | Source movies or series, in the owner's order; no duplicates (checked on save, F8) |

## state.json – run state (D29, D37)
| Field | Type | Meaning |
|-------|------|---------|
| lastSuccessfulRunUtc | UTC time or null | The last `finished` or `incomplete` run's `finishedUtc` (F1). Null = never |
| pinTitles | list of `{ sourceId, title }` | Last-known title of each pin, updated at each run's step 12, so a missing pin can still be named |
| historyLostPending | true/false | Set by Store when it deletes a damaged plays.json. Carried into run.json `historyLost` at steps 1 and 12 (spec "Pending flags"); cleared only at step 12 |
| stateResetPending | true/false | True in the empty state.json Store writes after renaming a damaged one. Carried into run.json `stateReset` at steps 1 and 12; cleared only at step 12 |

Store holds one lock around every read-change-write of state.json, so a pending flag set by a play event can't be lost under a run's write. A damaged state.json is renamed `state.json.damaged-YYYYMMDD-HHMMZ` (the 3 newest kept) and replaced by an empty one (spec F9). It is not backed up.

## managed.json – the managed list
```
{ "schemaVersion": 1, "copies": [ ... ] }
```
One entry per copy:
| Field | Type | Meaning |
|-------|------|---------|
| folder | text, a single name | Folder name directly inside the Top Picks folder (unique, ignoring case) |
| sourceId | ID | The source movie or series |
| kind | `Movie` / `Series` | |
| state | `incoming` / `active` / `stale` | `incoming` = entry written, folder not yet moved in (F4 step 7); `active` = in place; `stale` = its delete or update was skipped, so it is left for the next run and its items get the stale date `2000-01-01T00:00:00Z` (spec "Stale copy", D53) |
| rank | 1–10, or null only when `stale` | This week's rank |
| rankDateUtc | UTC time, or null only when `stale` | Date added this copy should have (F5); a `stale` copy's target is the stale date instead |
| itemId | ID or null | The copy item: the copy movie or copy series (F4 step 8). Null until found |
| files | list | Every .strm the plugin wrote in this copy: `{ "path": relative path, "sourceItemIds": [IDs of the source movie or episodes it holds, at least one], "itemId": ID of the copy movie or copy episode, or null until found }`. Used to match plays (F6), the rescan wait, dates and the order guard. For a `stale` entry from a skipped update: every .strm in the old entry or the planned one (spec F4 step 7) |

Rule: a folder is only ever deleted if it is in this list, and then only through the checks in spec F9. Only Run writes this file, through `IStore`, which holds one lock around each read-change-write. A damaged managed.json (or a missing one while the Top Picks folder holds a copy-looking folder) refuses every run until fixed as spec F9 says. It is not backed up.

## plays.json – popularity counts (D21, D33: no viewing history)
```
{ "schemaVersion": 1, "today": [ ... ], "daily": [ ... ] }
```
- **today**: sittings from UK days not yet rolled up (normally just today). One entry per playback stopped or played-by-hand mark:
  `userId` (ID), `sourceId` (movie or series, never the copy), `itemId` (the source movie or episode watched; for a copy .strm holding several episodes, the first of them), `dayUk`, `minutes` (whole number ≥ 0, rounded down), `finished` (true/false: it became played during this playback, spec F6), `markedPlayed` (true/false: marked played by hand), `runtimeMinutes` (whole number ≥ 1 or null; the source item's runtime).
  Roll-up (1 minute after start-up, 00:05 UK daily, each run's step 2) turns entries whose `dayUk` is before the current UK day into `daily` counts and deletes them, in the same atomic write.
- **daily**: one entry per title per finished UK day: `sourceId`, `dayUk`, `viewers` (whole number ≥ 1). No user IDs. At most one entry per `sourceId` and `dayUk`. Entries more than 90 days before the current UK day are removed at every roll-up.
- **Viewer-days** for a title (F2) = the sum of its `viewers` over the 7 UK days before the run's UK day.
- Store holds one lock around every read-change-write of this file.
- A damaged plays.json is deleted at once (spec F9, D37), never renamed aside, because it may hold user IDs.
- A backup copy is a valid plays.json with `"today": []`, so it can be copied back over the live file and loads normally (spec F9, C63).

## run.json – the current or last run
```
{ "schemaVersion": 1, ... }
```
| Field | Type | Meaning |
|-------|------|---------|
| runId | text | New for each run; kept on resume |
| trigger | `task` / `run now` / `missed run` | What started it (spec F1, D35) |
| resumed | true/false | True if the run was carried on after a restart |
| resumeCount | whole number 0–3 | Starts 0. Resume adds 1 each time it takes this run; when it reaches 3 (the third stop) the run is given up as `failed` (F5, D36) |
| startedUtc | UTC time | Run start (spec "Words used here") |
| rankTimeUtc | UTC time | Run start rounded down to the minute; never changed |
| finishedUtc | UTC time or null | Set when status leaves `running` |
| step | `guards` / `rollup` / `backup` / `choose` / `plan` / `prepare` / `swap` / `rescan` / `dates` / `artwork` / `check` / `done` | Step in progress (spec F4 steps 1–11, each step names its value); `done` once recorded (step 12) or once the run ends `failed` or `refused` |
| status | `running` / `finished` / `incomplete` / `failed` / `refused` | As defined in spec "Words used here". `running` with no run holding the lock = waiting to resume (D36) |
| message | text | Plain words for the report; empty until the run ends or is refused |
| timeZoneWarning | true/false | Server time zone isn't Europe/London; set at step 1 from the zone EmbyBridge passes in, kept on resume |
| picks | list | `rank`, `sourceId`, `title`, `kind`, `reason` (`pinned` / `watched` / `filler`), `viewerDays` (whole number), and true/false flags, each false when step 5 writes the pick and set true when it works: `artworkDrawn` (step 6: drawn, not the fallback), `copyApplied` (step 7: its add or update done and its entry `active`), `dateApplied` (step 9, or step 10's re-set), `artworkRefreshed` (step 10) |
| missingPins | list | `sourceId`, `title` (from state.json pinTitles; may be empty) |
| ignoredLibraries | list of IDs | Chosen libraries that no longer exist or aren't movie or TV libraries (F2 step 5) |
| operations | list | The plan (F4 step 5), in swap order: `kind` (`delete` / `update` / `add`), `folder`, `sourceId`, `entry` (for `update` and `add`: the full planned managed.json entry, same fields and rules as a managed.json entry, with `state` `active` and the `itemId` fields null; for `delete`: null), `done` (true/false; true once it finished or was skipped, written after the operation's managed.json change, spec F4 step 7), `note` (text or null: why skipped, if it was). An `add`'s `entry` is stored with `state` `active`; the swap writes it to managed.json as `incoming` first |
| skipped | list | `{ what, why }`, both text: items left out and why (no copy possible, copy not updated, name too long, line break in path, drawing failed, check failed …) |
| rowCheck | object | `result` (`passed` / `partial` / `failed` / `not run`; `not run` until step 11), `userKind` (`non-admin` / `admin` / null before step 11), `expected` (list of sourceIds), `leftOut` (list of `{ sourceId, why }`: `parental controls`, `no copy`, `not in row`), `returned` (list of `{id, name}` exactly as Emby gave them), `unmatched` (names of returned items that match no copy), `stale` (names of returned items that match a `stale` copy). Verdict rules: spec F4 step 11 |
| viewerDaysTotal | whole number | Sum of viewer-days in this run's 7-day window |
| historyLost | true/false | plays.json was found damaged and deleted since the last run's step 12 (from the pending flag, spec "Pending flags"); once true it stays true, even on resume |
| stateReset | true/false | state.json was found damaged and reset since the last run's step 12 (same rule) |
| pluginVersion, embyVersion | text | |

**Starting values** (step 1): `runId` new, `trigger` as started, `resumed` false, `resumeCount` 0, `startedUtc` and `rankTimeUtc` set, `finishedUtc` null, `step` `guards`, `status` `running`, `message` empty, `timeZoneWarning` as above, `picks`/`missingPins`/`ignoredLibraries`/`operations`/`skipped` empty, `rowCheck` `{ result: "not run", userKind: null, expected: [], leftOut: [], returned: [], unmatched: [], stale: [] }`, `viewerDaysTotal` 0, `historyLost` and `stateReset` false (then the pending flags), `pluginVersion` and `embyVersion` set.

Used to resume (F5) and to show the report (F8). Holds no user IDs or names. A resumed run keeps every field (spec F5). At a new run's step 1, the previous run.json is copied to **run.previous.json** (same shape) only if it was `finished` or `incomplete`, so run.previous.json is always the last successful run before the current one; it is shown on the report page when the latest run was `refused` or `failed`. A damaged run.json is renamed `run.json.damaged-YYYYMMDD-HHMMZ`; the 3 newest are kept.

## staging\ – scratch folder
Inside the plugin data folder. Emptied by Store at F4 step 6 (and when a resume restarts that step), never during the swap. Layout: `staging\<copy folder name>\…`, each copy exactly as it will be in the Top Picks folder (spec F4 step 6).

## backups\
- `YYYY-MM-DD-HHMMZ\` (run start, UTC) – plays.json with `"today": []`, and TopPicksPlus.xml, copied at F4 step 3 (a file that doesn't exist yet is skipped), then an empty `complete` file written last. Only folders with `complete` count; the 8 newest are kept, and an unfinished folder older than the newest complete one is deleted. Holds no user IDs.
