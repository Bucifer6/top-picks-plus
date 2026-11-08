---
summary: What Top Picks Plus does, in plain English. Every feature has a check in checks.md. Version 3, after the scaffold review.
links: [decisions.md, checks.md, data.md, modules.md, reviews/2026-11-08-scaffold-review.md]
---

# Top Picks Plus – spec (v3, 2026-11-08)

**Purpose:** Every week, fill the "Latest Top Picks" row on the Emby home screen with the 10 titles people are watching most, numbered 1–10 in the right order every time, with no manual step. The owner can pin titles if they want to.

**Replaces:** the old third-party plugin Emby.TopPicks (file `Emby.TopPicks.dll`), which is abandoned and closed source. It orders the row by writing a "date added" into each title's .nfo file. Emby only reads that date the first time it sees a title, so when last week's folders are still there the order breaks (card 0001).

**Rigour level:** 1 – Personal: a home Emby server with a handful of users. The plugin keeps no viewing history: only how many people watched each title on each day, for 90 days (D21).

## Words used here
- **Source title:** a real movie or series in the owner's normal libraries (N:\Movies, K:\Series). **Source item:** a source movie or a source episode (the thing with a video file).
- **Copy:** the Top Picks version of a source title: a folder of .strm, .nfo, subtitle and image files in the Top Picks folder. A .strm file is a one-line text file holding the full path of the real video file; Emby plays the real file through it.
- **Top Picks folder:** `C:\Users\Admin\AppData\Roaming\Emby-Server\programdata\data\top-picks`. The plugin takes it from the Top Picks library's folder setting, not from this text.
- **Top Picks library:** the existing Emby library whose only folder is the Top Picks folder. Its "Latest" home-screen row is the "Latest Top Picks" row. The plugin finds it by folder path and never creates or deletes libraries.
- **Plugin data folder:** `programdata\data\TopPicksPlus\` (D23). Holds managed.json, plays.json, run.json, `staging\` and `backups\`. Never inside any library and never under `programdata\plugins`.
- **Date added:** Emby's own record of when an item was added (DateCreated). The Latest row shows the newest date added first.
- **Rank time:** the time this week's run started, in UTC, rounded down to the whole minute. Stored in run.json and never changed on resume. **Rank date** of pick N = rank time minus (N − 1) hours.
- **UK day:** a calendar date in the Europe/London time zone (Windows: "GMT Standard Time"). EmbyBridge passes the zone into Logic; Logic never reads the server's clock or zone.
- **Managed list:** managed.json, the plugin's record of every copy it looks after (data.md). A folder is a **managed folder** only if it is on this list.
- **Candidate:** a title that may be picked (F2 step 5).
- **Viewer-day:** one user counted as watching one title on one UK day (F6).

## F1 – When it runs
- Emby scheduled task **"Refresh Top Picks"**, default trigger every **Sunday at 05:00 on the server's clock** (the server is on UK time).
- **Run now** (on the plugin's settings page or in Emby's Scheduled Tasks) starts that same Emby scheduled task, so it shows in Scheduled Tasks with its progress.
- **Only one run at a time.** If a run is asked for while one is going (or an unfinished run is waiting to resume), the request is turned down and the settings page says "a run is already in progress".
- **Start-up order** (timers owned by EmbyBridge, each calling into Run):
  1. +2 minutes: order guard (F5), unless a run is in progress or waiting to resume.
  2. +3 minutes: resume an unfinished run, if run.json says one was in progress (F5).
  3. +10 minutes: **missed-run rule**: if no run is in progress and the last successful run was more than 7 days ago, or there has never been one (first install), run once.
- **Successful run** = status `finished` or `incomplete` (copies were swapped). `failed` and `refused` runs don't count, so the missed-run rule tries again at the next start-up.

## F2 – Choosing the 10 (fully automatic)
The list is built automatically every week. **No manual step is ever needed.** Pins are optional. All of this is Logic, tested without Emby.
1. **Pins (optional) first**, in the owner's order. A pin can be any movie or series in any movie or TV library except the Top Picks library (not only the chosen libraries). A pin is **missing** if its source title is gone, or a movie's video file can't be found, or a series has no episode whose file can be found. Missing pins are skipped, the rest move up, and each missing pin is listed in the run report and marked "missing" on the settings page. Only the first 10 usable pins are used. The same title pinned twice counts once.
2. **The remaining places go to the most watched candidates**, by **viewer-days in the 7 UK days before the run's UK day** (for a Sunday run, the previous Sunday to Saturday). Plays from the run's own day count next week. Episodes count towards their series.
3. **Order, from first to last** (used for watched titles, and the same chain without step a for filler):
   a. more viewer-days first;
   b. newest content first: a movie's date added; a series' newest available episode's date added;
   c. newer premiere date first; a title with no premiere date goes after one that has it;
   d. title, A to Z (ignoring case);
   e. source Emby ID, lowest first (so the order is always the same).
4. **Filler:** if fewer than 10 places are filled, the rest go to the candidates nobody watched, ordered by 3b–3e.
5. **Candidates:** movies and series in the chosen libraries (default: every movie and TV library apart from the Top Picks library). Logic itself drops anything in the Top Picks library or with a file under the Top Picks folder, whatever the settings say. A title appears only once. A movie is left out if its video file can't be found. A series is left out only if none of its episode files can be found; missing episodes are left out of its copy.
6. With fewer than 10 candidates the row is shorter. The run doesn't fail.
7. **Always 10.** The number of picks is fixed (D11).
8. **Offline drives (D25):** before choosing, the run checks that every folder of every chosen library (and of every pinned title's library) can be reached. If any can't, the run is **refused**: nothing is changed and the report names the folder. This stops a disconnected drive from looking like "every file is missing" and replacing good copies.

## F3 – What each copy contains
**One naming rule for every folder and file the plugin makes** (Logic, `SafeName`):
- Remove `< > : " / \ | ? *` and control characters, turn runs of spaces into one space, then trim leading spaces and trailing dots and spaces.
- If the result is empty, or a name Windows reserves (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`, in any case, with or without an extension), use the fallback given (always the item's Emby ID).
- Shorten to the length limit at a whole character, then trim again.
- **Lengths:** copy folder name at most 60 characters; episode file name (before the extension) at most 80; every full path the plugin writes at most 240 characters. If a path would still be longer, the episode part is shortened further; if it still can't fit, that episode is left out and the report says so.

**Names:**
- **Copy folder:** `Title (Year)`, or `Title` with no year; fallback the source ID. If two picks get the same name, or the name is already used by a folder that isn't on the managed list, ` - 2`, ` - 3` … is added until it is free. The plugin never writes into a folder that isn't managed.
- **A title that stays keeps its folder name** for as long as it stays, even if its title or year changes, so Emby keeps the same item. A new name is only used when a title comes back after dropping out.
- **Movie files:** `<folder>.strm` and `<folder>.nfo`. The .strm points to the movie's main video file (for a movie with several versions, the one Emby plays by default).
- **Series:** `tvshow.nfo` in the copy folder, then one folder per season: `Season 1`, `Season 2` … (`Specials` for season 0). Episode files are `Series - S01E02 - Episode title` (`.strm` and `.nfo`); with no episode title just `Series - S01E02`; a file holding several episodes uses its first and last, `S01E01-E02`. An episode with no season or episode number goes in `Specials` and uses its Emby ID as the number part.
- **Subtitles:** for movies and episodes, external subtitle files (.srt, .ass, .ssa, .vtt, .sub) next to the source video are copied, with the source's language and forced/default parts kept: `<copy file name>.en.forced.srt`.
- **The .strm content** is exactly the source file's full path and nothing else. If the path contains a line break, that item is left out and reported.

**.nfo files** are written with an XML writer (so any text from Emby is escaped, never pasted in raw), UTF-8:
- Movie: `title`, `originaltitle`, `year`, `premiered` (yyyy-MM-dd), `plot`, `mpaa` (age rating), `genre` (one per genre), `runtime` (minutes), `uniqueid` (each provider ID, e.g. imdb, tmdb), `dateadded`.
- Series (`tvshow.nfo`): `title`, `originaltitle`, `year`, `premiered`, `plot`, `mpaa`, `genre`, `uniqueid`, `dateadded`.
- Episode: `title`, `showtitle`, `season`, `episode`, `aired`, `plot`, `runtime`, `mpaa`, `dateadded`.
- `dateadded` is the rank date in the server's local time, `yyyy-MM-dd HH:mm:ss`. Every episode of a series gets the series' rank date.

**Artwork** (inputs come from EmbyBridge as image bytes; Artwork never touches files):
- `fanart.jpg`, or `fanart.png` if the source backdrop is a PNG: the source's first backdrop, as-is.
- `logo.png`: the source logo, if there is one.
- `backdrops\theme.mp4`: the source theme video, if there is one, copied only when a copy is first created.
- `folder.png` and `thumb.png` (the same picture), drawn at **1000×562** (D6) with the embedded font **Montserrat ExtraBold** (SIL OFL, D15):
  - Background: the backdrop scaled to cover the whole picture, centred. No backdrop: the poster, scaled to the full height and centred on dark grey `#111111`. Neither: plain `#111111`.
  - A dark fade over the left 45%: 70% black at the left edge to clear at 45%.
  - The rank number in white, 390 px tall, 40 px from the left edge, centred top to bottom. "10" uses the same height.
  - The logo fitted inside the box x 420–960, y 60–300, keeping its shape, centred in the box. No logo: the title in white in the same box, at most 2 lines, as large as fits between 72 px and 36 px, then cut with "…".
  - If drawing fails (for example an image that can't be read), the copy uses the source backdrop as `folder.png` and `thumb.png` instead, and the report says drawing failed for that title.

## F4 – Each week's update, in order
Run carries out these steps; every step's progress is written to run.json so a restart can carry on (F5).
1. **Refuse or go:** the old-plugin guard, missing-library guard and offline-drive check (F9, F2 step 8). A damaged managed.json or run.json also stops the run here (F9). If any of these fail, the status is `refused` and nothing is changed.
2. **Back up** (F9): copy managed.json, plays.json and the plugin settings file into `backups\YYYY-MM-DD\`.
3. **Count:** turn finished days' plays into daily counts and remove counts older than 90 days (F6). This happens on every run, even one that is later refused or fails.
4. **Choose** the 10 (F2) and set the rank time.
5. **Plan:** work out each operation (delete a drop-out, update a stayer, add a new title) and write the plan into run.json before anything is changed.
6. **Prepare** every new title's copy and every stayer's new files in `staging\`, which Emby never sees. `staging\` is emptied at the start of a new run only, never on resume.
7. **Swap**, one operation at a time, each marked done in run.json when it finishes:
   - **New title:** add its entry to managed.json (marked `incoming`) first, then move its folder from staging into the Top Picks folder, then mark it `active`. So a folder in the Top Picks folder is never unknown to the managed list.
   - **Stayer:** the same folder is kept. Its .strm, .nfo, subtitle and artwork files are replaced one by one, each written to a temporary name and then renamed over the old one. Files that are no longer needed (an episode that has gone) are deleted, using the delete guard (F9).
   - **Drop-out:** delete its folder with the delete guard (F9), then remove its entry from managed.json.
8. **Rescan** only the Top Picks library, and wait until every copy is in the library: up to 15 minutes, or 30 minutes if a series has more than 200 episodes.
9. **Set the order:** set each copy's date added in Emby to its rank date (for a series, the series and every one of its episodes). The same date is already in the .nfo, so a later refresh that re-reads the .nfo agrees (card 0001).
10. **Refresh artwork** for each copy so the number on the picture matches. Then read each date added back and set it again if the refresh changed it.
11. **Check the row** (D22): ask Emby for the Latest Top Picks row, as the apps do (the "latest items" request for the Top Picks library, 10 items, grouped by series), as one non-administrator user who can see the Top Picks library; if there is none, as an administrator, and the report says so.
    - Each returned item is matched by its Emby ID to the managed list (an episode counts as its series).
    - A pick the check user isn't allowed to see (parental controls) is left out of what is expected, and the report says so.
    - **Pass** = the returned titles, in order, are exactly the expected picks in rank order. If some copies hadn't appeared yet (step 8 ran out), the expected list is only those that had, and the result says "partial".
12. **Record** the result in run.json, set `finished` (or `incomplete`), and update `LastSuccessfulRunUtc`.

**If the step 8 wait runs out:** steps 9–11 still run for every copy that has appeared, the status is `incomplete`, and the order guard (F5) finishes the job when the rest appear.

**If something goes wrong after the swap has started** (an error, not a restart): the run stops, the status is `failed`, the report says which step and what failed in plain words, and no operation is undone. The order guard keeps the row ordered for the copies that are in place.

## F5 – Order guard and resume
- **Order guard:** after every library scan, and 2 minutes after Emby starts up, the plugin compares each active copy's date added (the series and each episode) with its rank date and sets any that differ by more than 1 second. **It does nothing while a run is in progress or waiting to resume**; the run sets dates itself. It runs once when a run ends.
- **Resume:** if Emby restarts during a run, the run carries on from the first operation in run.json not marked done, using the stored rank time and the files already in staging. It never rolls back.
  - Each operation is safe to repeat: deleting a folder that's already gone is done; moving a new folder whose staging copy has gone and whose target exists is done; a stayer's files are written again.
  - If the plan can't be finished (for example a staging folder is missing and the target isn't there), that operation is skipped and reported, and the run ends `incomplete`.
- What the row may show while a run is in progress: for a few minutes, a mix of last week's and this week's titles. When the run finishes, the row is complete and in order.

## F6 – Counting plays
- EmbyBridge listens for Emby's "playback started" and "playback stopped" events. It ignores trailers, extras and anything that isn't a movie or an episode.
- **Which title it counts for:** a play on a copy file counts for the source title. The copy file is found in the managed list by its path (each managed entry lists its files and their source items, data.md). A play on a source item counts for its movie or series, wherever the play was started.
- **Minutes watched in a sitting** = the position when playback stopped minus the position when it started (taken from the matching "playback started" event, held in memory only), never less than 0 or more than the runtime. If the start wasn't seen (for example Emby restarted), the stop position is used.
- **Which users count:** every Emby user, administrators included.
- **What counts as watched:** a user counts for a title on a UK day if, that day, Emby marked any item in the title as played for them, or their minutes on the title that day add up to at least 50% of the runtime, or at least 20 minutes when the runtime isn't known. For a series, the runtime is that of the episode watched, and each episode is judged on its own: one qualifying episode is enough. Two sittings of the same film at 30% each count. A sitting belongs to the UK day on which it **stopped**.
- **No viewing history (D21):** who watched what is held only for the current UK day, in plays.json's `today` section, so each person is counted once. When the day has ended, its entries are boiled down to "title, day, number of viewers" and the user IDs are deleted. This happens at the first play or run after midnight. Daily counts are kept while their day is within 90 days of the current UK day.
- **Logging:** plays are never logged at the normal (Info) level. Debug-level logs may mention a title, never a user.
- **First run, one-off import (D9, D24):** if the Playback Reporting plugin's database (`programdata\data\playback_reporting.db`) exists, the last 7 UK days are read from it, read-only, once. Each row's item is mapped to its movie or series (rows for items that no longer exist are skipped), each user's minutes per title per day are added up, the same "what counts as watched" rule is applied (no "marked played" information is available), and the results go straight into daily counts with no user IDs. If anything about this fails, the plugin carries on and the report says the first list mostly used recently added titles.

## F7 – Watched status follows the real title
- **Copy to source, as it happens:** when a user's played state or resume point changes on a copy movie or copy episode (playback stopped, or marking it played or unplayed by hand), the plugin sets the same played state and resume point on the matching source item for that user. The copy is matched by its path in the managed list, as in F6.
- **Source to copy, at the weekly run only:** when a copy is created or updated, each of its items takes each user's played state and resume point from its source item.
- The plugin ignores changes it made itself, so the two never chase each other.
- **Known limitation, accepted by the owner:** the copies appear in search next to the real titles, as they do today.

## F8 – Settings page and run report (Dashboard → Plugins → Top Picks Plus)
- **Administrators only.** Every action of the plugin's API (save settings, pin search, Run now, read the report) checks on the server that the signed-in user is an Emby administrator, and refuses anyone else with "Administrators only" and no data. Hiding the page is not relied on.
- **Libraries** to choose from. The Top Picks library is never offered.
- **Pins:** search for a movie or series, add it, drag to reorder, remove. Pins that can't be used are marked "missing".
- **Run now** button (starts the scheduled task, F1), plus a "run in progress" indicator.
- **Last run report:** plugin version and Emby version; when it ran; its status (finished / incomplete / failed / refused) and a plain-words message; each pick with rank, title, why it was picked (pinned / N viewer-days / filler) and whether its copy, date and artwork were applied; missing pins; the row check result (passed / partial / failed, which kind of user was used, and exactly what Emby returned if it didn't pass); and the total viewer-days counted in the 7-day window. No user names or IDs appear anywhere.

## F9 – Safety
- **Delete guard.** Before deleting or replacing any folder or file, Run checks all of these and refuses (skips it and reports it) if any fail:
  1. the folder is on the managed list (for a file: it is inside a managed folder);
  2. its name is a single name (no `\`, `/`, `..` or drive letter);
  3. its real location, with every shortcut resolved, is directly inside the Top Picks folder (for files: inside the managed folder) or inside `staging\`;
  4. it is not a junction or symbolic link, and deleting never follows one: a link found inside a managed folder is removed as a link, never entered.
  **What this means in practice:** the plugin deletes only folders on the managed list and the files inside them. If someone puts their own file inside a managed folder, it goes when that folder is deleted. Folders that aren't managed, and loose files in the Top Picks folder, are never touched.
- **First-run takeover.** Before the first run:
  1. The whole Top Picks folder is copied into `backups\takeover-YYYY-MM-DD.zip` (theme videos left out: they are copies of the source).
  2. A folder directly inside the Top Picks folder is the old plugin's if it holds at least one .strm and one .nfo file, counting its own files and those one level down (season folders).
  3. It is matched to a source title by its .strm files: the first .strm whose content is the path of a source item's file gives the movie, or the episode's series.
  4. Matched folders are added to the managed list as `adopted`; if the title is picked this week it stays (keeping its folder name) and is updated in place, otherwise it is deleted on that run. A folder that qualifies but matches nothing is also adopted (with no source) and deleted on that run.
  5. Anything else is left alone forever. `TakeoverDone` is then set so this never runs again.
- **Old-plugin guard:** if Emby has loaded a plugin whose assembly is `Emby.TopPicks`, every run is refused and the report says to uninstall it first. Nothing else is matched by name.
- **Missing library:** if no library uses the Top Picks folder, the run is refused and the report says so.
- **Damaged files** ("damaged" = can't be read as JSON, or has a `schemaVersion` the plugin doesn't know):
  - **managed.json** is left exactly where it is and **every run is refused** until it is fixed, because without it the plugin can't tell its folders from anyone else's. The report says to restore it from the newest `backups\` folder (steps below).
  - **run.json** is renamed to `run.json.damaged-YYYYMMDD-HHMM`, that run is refused with a message, and the next run starts clean.
  - **plays.json** is renamed aside the same way, counting starts again from empty, the run carries on, and the report says the popularity history was lost.
  - A **missing** file (first install) simply starts empty. A missing managed.json after `TakeoverDone` is treated as damaged.
- **Backups:** before every run's swap (F4 step 2), managed.json, plays.json and `TopPicksPlus.xml` are copied into `backups\YYYY-MM-DD\`; the 8 newest are kept. To restore: stop Emby, copy the files back over the live ones, start Emby.
- **Personal data:** none is kept (D21). Today's not-yet-counted plays hold user IDs until the day ends; after that only daily viewer counts per title remain. Nothing is sent anywhere; the plugin makes no network calls.
- **Age ratings:** each copy's .nfo carries the source title's age rating, so Emby's parental controls apply to copies just as they do to the originals.

## F10 – Staying working through Emby updates
- The build targets a fixed Emby plugin SDK version, recorded in the repo (D14).
- After every Emby server upgrade, the owner runs check C20. The plugin writes its version and the Emby version at the top of each run report.

## How the parts connect (for the builder)
- Emby creates the plugin's root class, the scheduled task, the start-up entry point, the event listeners and the API service. The root class (start-up) creates Run, Store and Artwork and puts Run's hook implementations into EmbyBridge's `Hooks` registry (a static holder owned by EmbyBridge). Emby-created EmbyBridge classes only ever reach Run through that registry.
- EmbyBridge hands every other part a plain **paths** record (Top Picks folder, plugin data folder, programdata) and a **log** interface; both are plain types defined in Logic, so Store and Artwork never need Emby.
- Run uses Store and Artwork only through interfaces those parts define (`IStore`, `IArtwork`), so Run can be tested with fakes.

## Not included (on purpose)
- Different picks for different users. It's one shared row, like today.
- Trailers or "coming soon" items.
- A different number of picks.
- Hiding the copies from search.
