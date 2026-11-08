---
summary: What Top Picks Plus does, in plain English. Every feature has a check in checks.md. Version 4, after the spec v3 re-review.
links: [decisions.md, checks.md, data.md, modules.md, security.md, reviews/2026-11-08-spec-v3-review.md]
---

# Top Picks Plus – spec (v4, 2026-11-08)

**Purpose:** Every week, fill the "Latest Top Picks" row on the Emby home screen with the 10 titles people are watching most, numbered 1–10 in the right order every time, with no manual step. The owner can pin titles if they want to.

**Replaces:** the old third-party plugin Emby.TopPicks (file `Emby.TopPicks.dll`), abandoned and closed source. It orders the row by writing a "date added" into each title's .nfo file. Emby only reads that date the first time it sees a title, so when last week's folders are still there the order breaks (card 0001).

**Rigour level:** 1 – Personal: a home Emby server with a handful of users. No viewing history is kept: only how many people watched each title on each day, for 90 days (D21, D33).

## Words used here
- **Source title:** a real movie or series in the owner's normal libraries (N:\Movies, K:\Series). **Source item:** a source movie or source episode (the thing with a video file).
- **Movie or TV library:** an Emby library whose content type is "movies" or "tvshows". Mixed or other libraries are never used.
- **Copy:** the Top Picks version of a source title: a folder of .strm, .nfo, subtitle and image files in the Top Picks folder. A .strm file is a one-line text file holding the full path of the real video file; Emby plays the real file through it. **Copy item:** the Emby item Emby makes for a copy (a copy movie, a copy series, or a copy episode).
- **Programdata:** Emby's data folder, given by Emby at start-up (here `C:\Users\Admin\AppData\Roaming\Emby-Server\programdata`).
- **Top Picks folder:** always `<programdata>\data\top-picks`. Fixed, not a setting (F9 "Finding the Top Picks library").
- **Top Picks library:** the existing Emby library whose only folder is the Top Picks folder. Its "Latest" home-screen row is the "Latest Top Picks" row. The plugin never creates or deletes libraries.
- **Plugin data folder:** `<programdata>\data\TopPicksPlus\` (D23). Holds managed.json, plays.json, run.json, state.json, `staging\` and `backups\`. Never inside any library and never under `programdata\plugins`.
- **Date added:** Emby's own record of when an item was added (DateCreated). The Latest row shows the newest date added first.
- **Run start:** the moment Run creates this run's run.json (the first thing it does), in UTC. **Rank time** = run start rounded down to the whole minute. Both are stored in run.json and never change, even on resume. **Rank date** of pick N = rank time minus (N − 1) hours. **The run's UK day** = the UK day of the rank time.
- **UK day:** a calendar date in the Europe/London time zone (Windows: "GMT Standard Time"). EmbyBridge passes the zone into Logic; Logic never reads the clock or the zone itself.
- **Managed list:** managed.json, the plugin's record of every copy it looks after (data.md). A folder is a **managed folder** only if it is on this list.
- **Candidate:** a title that may be picked (F2 step 5).
- **Viewer-day:** one user counted as watching one title on one UK day (F6).
- **Roll-up:** turning finished UK days' plays into daily counts, deleting their user IDs, and removing counts older than 90 days (F6, D33).
- **Can be reached** (a library folder): the folder exists and listing its top level succeeds within 30 seconds.
- **Text comparisons** for ordering and ties are ordinal (character by character, not by language rules); "ignoring case" means ordinal ignoring case.

## F1 – When it runs
- Emby scheduled task **"Refresh Top Picks"**, default trigger every **Sunday at 05:00 on the server's clock**. The server is set to UK time; if the server's time zone isn't Europe/London, the run still happens and the report warns about it.
- **Run now** (on the settings page, or the task's Run button in Emby's Scheduled Tasks) starts that same task, so it shows in Scheduled Tasks with its progress.
- **One run at a time (D32).** Every way of starting a run (the task, Run now, resume, the missed-run rule) goes through one Run entry holding one lock inside the plugin. If a run holds the lock, or run.json says a run is waiting to resume, the request is turned down: the settings page says "a run is already in progress", and a task start returns at once with that message in the report log. Emby's own task queue is not relied on. run.json records what started each run (`trigger`).
- **Start-up order** (timers owned by EmbyBridge, each calling a Run hook):
  1. +1 minute: roll-up (F6).
  2. +2 minutes: order guard (F5), unless a run holds the lock or is waiting to resume.
  3. +3 minutes: resume, if run.json says a run was in progress (F5).
  4. +10 minutes: **missed-run rule**: if no run is waiting and state.json's last successful run is more than 7 days ago, or there has never been one (first install), start a run.
- Also every day at 00:05 UK time: roll-up (D33).
- **Successful run** = status `finished` or `incomplete`. `failed` and `refused` runs don't count, so the missed-run rule tries again at the next start-up.

## F2 – Choosing the 10 (fully automatic)
No manual step is ever needed. Pins are optional. All of this is Logic, tested without Emby.
1. **Pins (optional) first**, in the owner's order. A pin can be any movie or series in any movie or TV library except the Top Picks library. A pin is **missing** if its source title is gone, or a movie's video file can't be found, or a series has no episode whose file can be found. Missing pins are skipped and the rest move up; each is listed in the run report (with its last-known title from state.json) and marked "missing" on the settings page. Only the first 10 usable pins are used. Duplicates are removed when settings are saved (F8).
2. **The remaining places go to the most watched candidates**, by **viewer-days in the 7 UK days before the run's UK day** (for a Sunday run, the previous Sunday to Saturday). Plays on the run's own day count next week. Episodes count towards their series. A pinned title isn't picked again.
3. **Order, first to last** (for watched titles; filler uses b–e):
   a. more viewer-days first;
   b. newest content first: a movie's date added; a series' newest available episode's date added;
   c. newer premiere date first; no premiere date goes after one that has it;
   d. title, A to Z, ignoring case;
   e. source Emby ID, ordinal, lowest first (so the order is always the same).
4. **Filler:** if fewer than 10 places are filled, the rest go to candidates nobody watched, ordered by 3b–3e.
5. **Candidates:** movies and series in the chosen libraries (default: every movie and TV library apart from the Top Picks library). Logic itself drops anything in the Top Picks library or with a file under the Top Picks folder, whatever the settings say. A title appears once. A movie is left out if its video file can't be found. A series is left out only if none of its episode files can be found; missing episodes are left out of its copy.
6. With fewer than 10 candidates the row is shorter. The run doesn't fail.
7. **Always 10.** The number of picks is fixed (D11).
8. **Offline drives (D25):** at F4 step 1, every folder of every chosen library, and of every pinned title's library, must be reachable (EmbyBridge's source-files adapter, D28). If any can't, the run is **refused** and the report names the folder.

## F3 – What each copy contains
**One naming rule for every folder and file the plugin makes** (Logic, `SafeName`):
- Remove `< > : " / \ | ? *` and control characters, turn runs of spaces into one space, then trim leading spaces and trailing dots and spaces.
- If the result is empty, or a name Windows reserves (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`, any case, with or without an extension), use the fallback given (always the item's Emby ID).
- Shorten to the length limit at a whole character, then trim again.
- **Lengths:** copy folder name at most 60 characters; episode file name (before the extension and language parts) at most 80. Every full path must be at most 240 characters both in the Top Picks folder and in `staging\` (Logic checks the longer of the two). If a path is too long, the episode title part is shortened; if it still can't fit, that episode is left out and the report says so.
- Names are compared ignoring case when checking for clashes, as Windows does.

**Names:**
- **Copy folder:** `Title (Year)`, or `Title` with no year; fallback the source ID. If two picks get the same name, or the name is already used (ignoring case) by any folder not on the managed list, the name is cut to 55 characters and ` - 2`, ` - 3` … added until free. The plugin never writes into a folder that isn't managed.
- **A title that stays keeps its folder name** for as long as it stays, even if its title or year changes, so Emby keeps the same item. A new name is only used when a title comes back after dropping out.
- **Movie files:** `<folder>.strm` and `<folder>.nfo`. The .strm points to the movie's main video file (for a movie with several versions, the one Emby plays by default).
- **Series:** `tvshow.nfo` in the copy folder, then one folder per season: `Season 1`, `Season 2` … (`Specials` for season 0). Episode files are `<Series> - S01E02 - <Episode title>` (`.strm` and `.nfo`), where `<Series>` is SafeName of the source series' name cut to 40 characters, and numbers are padded to at least 2 digits. With no episode title: `<Series> - S01E02`. A file holding several episodes uses its first and last, `S01E01-E02`, and its managed entry lists every episode it holds. An episode with no season or episode number goes in `Specials` as `<Series> - S00E<Emby ID>`.
- **Subtitles:** for movies and episodes, external subtitle files (.srt, .ass, .ssa, .vtt, .sub) next to the source video are copied (read through the source-files adapter, D28), keeping the source's language and forced/default parts: `<copy file name>.en.forced.srt`.
- **The .strm content** is exactly the source file's full path and nothing else. If the path contains a line break, that item is left out and reported.

**.nfo files** are written with an XML writer (any text from Emby is escaped, never pasted in raw), UTF-8:
- Movie: `title`, `originaltitle`, `year`, `premiered` (yyyy-MM-dd), `plot`, `mpaa` (age rating), `genre` (one per genre), `runtime` (minutes), `uniqueid` (each provider ID, e.g. imdb, tmdb), `dateadded`.
- Series (`tvshow.nfo`): `title`, `originaltitle`, `year`, `premiered`, `plot`, `mpaa`, `genre`, `uniqueid`, `dateadded`.
- Episode: `title`, `showtitle`, `season`, `episode`, `aired`, `plot`, `runtime`, `mpaa`, `dateadded`.
- `dateadded` is the rank date in **UTC**, `yyyy-MM-dd HH:mm:ss`, so the autumn clock change can't make two ranks share a time. Every episode of a series gets the series' rank date. The plugin sets the date itself at step 11; if Emby ever re-reads the .nfo as local time, every copy moves by the same amount, so the order still holds, and the order guard puts the exact values back.

**Artwork** (images come from EmbyBridge as bytes; Artwork never touches files):
- `fanart.jpg`, or `fanart.png` if the source backdrop is a PNG: the source's first backdrop, as-is.
- `logo.png`: the source logo, if there is one.
- `backdrops\theme.mp4`: the source theme video, if there is one, copied (opened through the source-files adapter) only when a copy is first created.
- `folder.png` and `thumb.png` (the same picture), drawn at **1000×562** (D6) with the embedded font **Montserrat ExtraBold** (SIL OFL, D15):
  - Background: the backdrop scaled to cover the whole picture, centred. No backdrop: the poster, scaled to the full height and centred on dark grey `#111111`. Neither: plain `#111111`.
  - A dark fade over the left 45%: 70% black at the left edge to clear at 45%.
  - The rank number in white. The font size is chosen so the digit "0" is 390 px tall (measured from the drawn glyph), the number starts 40 px from the left edge and is centred top to bottom. If the number is wider than 360 px ("10"), it is scaled down to 360 px wide, so it never reaches the logo box.
  - The logo fitted inside the box x 420–960, y 60–300, keeping its shape, centred in the box. No logo: the title in white in the same box, at most 2 lines, as large as fits between 72 px and 36 px, then cut with "…".
  - **If drawing fails** (an image that can't be read, or SkiaSharp can't load): the copy's `folder` and `thumb` pictures are the source backdrop (or, with no backdrop, the poster) saved as-is with their own extension (`folder.jpg` / `thumb.jpg` for a JPEG). Any old `folder.*` / `thumb.*` of another extension is deleted. With neither image, there is no folder picture. The report says drawing failed for that title.

## F4 – Each week's update, in order
Run carries out these steps. run.json's `step` names the step in progress, so a restart can carry on (F5). A run that refuses at step 1 changes nothing at all.
1. **Create the run and refuse or go:** write run.json (`runId`, run start, rank time, `trigger`, `step` = `guards`, status `running`). Then the guards, in order: old-plugin guard, finding the Top Picks library (F9), damaged managed.json or state.json, offline-drive check (F2 step 8). If any fails, status `refused`, the report says why, and nothing else is changed.
2. **Roll-up** the play log (F6). Safe to repeat.
3. **Back up** (F9) into `backups\YYYY-MM-DD-HHMM\` (the run start, UK time). If the backup fails, the run is refused.
4. **First-run takeover** (only while state.json `takeoverDone` is false, F9): zip, check the zip, adopt the old plugin's folders, then set `takeoverDone`.
5. **One-off import** (only while state.json `importTried` is false, F6): read the 7 UK days before the run's UK day from Playback Reporting into daily counts, then set `importTried`.
6. **Choose** the 10 (F2).
7. **Plan:** compare the 10 with the managed list and write every operation into run.json before anything is changed: `add` a new title, `update` a stayer (including a picked `adopted` entry), `delete` a drop-out (including an unpicked `adopted` entry). Leftovers from an earlier stopped run are tidied first: an `incoming` entry whose folder is in the Top Picks folder becomes `active`; one whose folder isn't there is removed.
8. **Prepare:** Store empties `staging\`. Run writes every new title's copy and every stayer's new files into `staging\`, which Emby never sees.
9. **Swap**, one operation at a time, each marked done in run.json when it finishes:
   - **add:** write its entry to managed.json as `incoming`, move its folder from staging into the Top Picks folder, then mark it `active`. So a folder in the Top Picks folder is never unknown to the managed list.
   - **update:** the same folder is kept. Its .strm, .nfo, subtitle and artwork files are replaced one by one, each written to a temporary name and renamed over the old one. Files no longer needed (an episode that has gone) are deleted through the delete guard (F9). The entry becomes `active` with this week's rank.
   - **delete:** delete its folder through the delete guard (F9), then remove its entry.
10. **Rescan** only the Top Picks library, and wait until every copy is in the library: for a movie, its copy item; for a series, the copy series and a copy episode for every file in its managed entry. Up to 15 minutes, or 30 minutes if a series has more than 200 episodes. Each copy item's Emby ID is stored in managed.json as it is found.
11. **Set the order and reset played state:** set each copy item's date added to its rank date (for a series, the series and every episode). Set every copy item to unplayed with no resume point for every user (D26). The same date is already in the .nfo (card 0001).
12. **Refresh artwork** for each copy so the number on the picture matches. Then read each date added back and set it again if the refresh changed it.
13. **Check the row** (D22): ask Emby for the Latest Top Picks row as the apps do (the "latest items" request for the Top Picks library, 10 items, grouped by series), as the **check user**: the first user, by user ID (ordinal), who isn't an administrator, isn't disabled and can see the Top Picks library; if there is none, the first administrator the same way, and the report says so.
    - Each returned item is matched by its Emby ID to the managed list. An episode (Emby sometimes returns one instead of its series) counts as its series. An item that matches no managed copy is reported by name and makes the check fail.
    - A pick the check user isn't allowed to see (parental controls) is left out of what is expected, and the report says so. Because copies are never played (step 11, D26), "hide watched from Latest" can't hide a pick.
    - **passed** = the returned titles, in order, are exactly the expected picks in rank order. **partial** = some copies hadn't appeared by the end of step 10; the ones that had are returned in the right order. **failed** = anything else, including appeared copies out of order. **not run** = the run stopped before this step.
14. **Record:** set status `finished` (or `incomplete`), `finishedUtc`, and state.json's `lastSuccessfulRunUtc`. Release the lock. Run the order guard once.

**If the step 10 wait runs out:** steps 11–13 run for every copy that has appeared, the status is `incomplete`, and the order guard (F5) finishes the job when the rest appear.

**If something goes wrong** (an error, not a restart) at any step after step 1: the run stops, the status is `failed`, and the report says which step and what failed in plain words; before step 9 it adds "nothing in the Top Picks folder was changed". No operation is undone. The order guard keeps the row ordered for the copies in place. The next run starts fresh (step 7 tidies leftovers).

## F5 – Order guard and resume
- **Order guard:** 2 minutes after Emby starts, once after every run, and 2 minutes after the last Emby library change to a Top Picks item (item added or updated in the Top Picks library: EmbyBridge waits for 2 quiet minutes, so a full scan triggers it once at the end). It compares each `active` copy item's date added (the series and each episode) with its rank date and sets any that differ by more than 1 second. It finds copy items by the IDs in managed.json; an ID that no longer exists is looked up again by path and stored. **It does nothing while a run holds the lock or is waiting to resume.** The exact Emby events are confirmed in the first EmbyBridge build chunk (C4, C32).
- **Resume (D32 lock held throughout):** if Emby restarts during a run, at +3 minutes Run takes the lock and, by run.json `step`:
  - `guards` to `plan`: start again from step 1 with the same `runId`, run start and rank time. Each of these steps is safe to repeat: guards only read; roll-up repeats harmlessly; the backup is skipped if this run's backup folder is complete; takeover and import are skipped once their state.json flag is set, and a takeover that stopped half-way starts over (its zip is remade; adopting a folder already on the list changes nothing). If a guard now refuses, the status is `refused`.
  - `prepare`: Store empties staging and step 8 starts again.
  - `swap`: carry on from the first operation not marked done, using the files already in staging. Repeating is safe: a folder already gone is done; a new folder whose staging copy has gone and whose target exists is done; a stayer's files are written again. If an operation can't be finished (its staging folder is missing and the target isn't there), it is skipped and reported, and the run ends `incomplete`.
  - `rescan`, `dates`, `artwork`, `check`: start again from step 10 (each is safe to repeat).
  - `done`: nothing to resume.
  - The guards are checked again on any resume; if one now fails after the swap began, the status is `failed` with the guard's message.
- What the row may show while a run is in progress: for a few minutes, a mix of last week's and this week's titles. When the run finishes, the row is complete and in order.

## F6 – Counting plays
- EmbyBridge listens for Emby's "playback started", "playback stopped" and "user data changed" (played state set or cleared by hand) events. It ignores trailers, extras and anything that isn't a movie or an episode.
- **Which title it counts for:** a play on a copy item counts for the source title. The copy is found in the managed list by its path (each managed file lists the source items it holds, data.md). A play on a source item counts for its movie or series, wherever it was started.
- **Minutes watched in a sitting** = the stop position minus the start position (from the matching "playback started", held in memory only), in whole minutes rounded down, never below 0 or above the runtime. If the start wasn't seen (for example Emby restarted), the stop position is used.
- **Marked by hand:** setting a movie or episode (source or copy) to played by hand adds a sitting with 0 minutes and `markedPlayed` true, on the UK day it happened. Clearing played state adds nothing and removes nothing already recorded.
- **Which users count:** every Emby user, administrators included.
- **What counts as watched:** a user counts for a title on a UK day if, that day, they finished an item in it (Emby marked it played at playback stopped) or marked one played by hand, or their minutes on the title that day add up to at least 50% of the runtime, or 20 minutes when the runtime isn't known. For a series, the runtime is that of the episode watched, and each episode is judged on its own: one qualifying episode is enough. Two sittings of the same film at 30% each count. A sitting belongs to the UK day on which it stopped.
- **No viewing history (D21, D33):** who watched what is held only in plays.json's `today` section, so each person is counted once a day. **Roll-up** happens 1 minute after start-up, at 00:05 UK time every day, and at F4 step 2: every `today` entry from a UK day before the current one is turned into "title, day, number of viewers", and those entries (with their user IDs) are deleted; daily counts more than 90 days before the current UK day are removed. Store keeps one lock around every change to plays.json, so the listener and a run never overwrite each other.
- Plays seen before start-up has finished wiring the plugin are dropped (logged at Debug without user).
- **Logging:** plays are never logged at Info level. Debug logs may name a title, never a user.
- **One-off import (D9, D24, F4 step 5):** if the Playback Reporting plugin's database (`<programdata>\data\playback_reporting.db`) exists, the 7 UK days before the run's UK day are read from it, read-only, once. Each row's item is mapped to its movie or series (rows for items that no longer exist are skipped), each user's minutes per title per day are added up, the same "what counts as watched" rule is applied (no "marked played" information exists there), and the results go straight into daily counts with no user IDs. If anything about this fails, the run carries on and the report says the first list mostly used recently added titles. Either way `importTried` is set.

## F7 – Watched status goes to the real title (D26)
- **Copies never look watched.** When a user's played state or resume point changes on a copy item (playback stopped, or marked played by hand), the plugin sets the same played state and resume point on the matching source item(s) for that user, then sets the copy item back to unplayed with no resume point. The copy is matched by its path in the managed list, as in F6.
- So the real title shows as watched, and a half-watched film appears in Continue Watching as the real title. Starting the copy again from the row starts from the beginning: to carry on, use the real title or Continue Watching. Accepted by the owner (D26).
- Copies never take played state from their source; at each run every copy item is reset to unplayed (F4 step 11).
- The plugin ignores user-data changes it made itself, so nothing chases itself.
- **Known limitation, accepted by the owner:** copies appear in search next to the real titles, as they do today.

## F8 – Settings page and run report (Dashboard → Plugins → Top Picks Plus)
- **Administrators only.** Every action of the plugin's API (save settings, pin search, Run now, read the report) takes the signed-in user from Emby's own authorisation of the request, never from a user ID in the request, and refuses anyone who isn't an Emby administrator with "Administrators only" and no data. Hiding the page is not relied on.
- **Libraries** to choose from (movie and TV libraries only). The Top Picks library is never offered.
- **Pins:** search for a movie or series, add it, drag to reorder, remove. The page marks a pin "missing" by asking the server when the page opens (live), and the report shows which were missing at the last run.
- **Saving checks every value:** library and pin IDs must be Emby IDs that exist and are a movie or TV library, or a movie or series; the Top Picks library and its items are refused; duplicate pins are removed; at most 20 pins. A bad value is refused with a plain message and nothing is saved.
- **Run now** button (starts the scheduled task, F1), plus a "run in progress" indicator.
- **Last run report:** plugin version and Emby version; what started it; when it ran; status (finished / incomplete / failed / refused) and a plain-words message; each pick with rank, title, why it was picked (pinned / N viewer-days / filler) and whether its copy, date, artwork drawing and artwork refresh worked; missing pins; the row check result (passed / partial / failed / not run, which kind of user was used, and exactly what Emby returned if it didn't pass); total viewer-days in the 7-day window; a time-zone warning if needed. No user names or IDs appear anywhere.
- **Text from Emby** (titles, names, messages) is put on the page as plain text only, never as HTML.

## F9 – Safety
- **Finding the Top Picks library:** the Top Picks folder is `<programdata>\data\top-picks`. The run is refused if no library has it as a folder, if more than one does, if the library holding it has any other folder, or if the Top Picks folder is inside, or contains, any other library's folder, the plugin data folder or programdata itself (other than being under it).
- **Before every write, move, delete or replace** in the Top Picks folder or `staging\`, Run checks (and skips and reports the item if any check fails):
  1. the target folder is on the managed list (for a file: it is inside a managed folder), or is inside `staging\`;
  2. a copy folder's name is a single name (no `\`, `/`, `..` or drive letter), and each part of a file's path inside the folder is a single name;
  3. its real location, with every shortcut resolved, is directly inside the Top Picks folder (for files: inside the managed folder), or inside `staging\`;
  4. neither it nor any folder on the way to it is a junction or symbolic link, and deleting never follows one: a link found inside a managed folder is removed as a link, never entered.
  **In practice:** the plugin deletes only folders on the managed list and the files inside them. If someone puts their own file inside a managed folder, it goes when that folder is deleted. Unmanaged folders and loose files in the Top Picks folder are never touched. `staging\` can never be on the managed list (managed names are folders directly in the Top Picks folder).
- **Store's own deletes** (emptying `staging\`, pruning `backups\`, removing old damaged files) are only ever inside the plugin data folder, with the same real-location and no-link checks, and backups are only pruned by their own name patterns (`YYYY-MM-DD-HHMM`).
- **First-run takeover** (F4 step 4):
  1. Store copies the whole Top Picks folder into `backups\takeover-YYYY-MM-DD.zip` and checks it by reopening it and comparing every entry's name and size with the folder. Theme videos are left out only for folders that match a source title that still has its own theme video; otherwise they go in, since they may be the only copy. If the zip can't be made or doesn't match, the run is refused and takeover is tried again next run.
  2. A folder directly inside the Top Picks folder is the old plugin's if it holds at least one .strm and one .nfo file, counting its own files and those one level down (season folders).
  3. It is matched to a source title by its .strm files: the first .strm whose content is the path of a source item's file gives the movie, or the episode's series.
  4. Matched folders go on the managed list as `adopted`; step 7 then updates or deletes them like any copy. A folder that qualifies but matches nothing is also adopted (no source) and deleted on that run.
  5. Anything else is left alone forever. `takeoverDone` is then set in state.json (D29), so a settings save or restore can never re-run it.
- **Old-plugin guard:** if Emby has loaded a plugin whose assembly is `Emby.TopPicks`, every run is refused and the report says to uninstall it first. Nothing else is matched by name.
- **Damaged files** ("damaged" = can't be read as JSON, or has a `schemaVersion` the plugin doesn't know):
  - **managed.json** and **state.json** are left exactly where they are and every run is refused until they are fixed. The report says to restore from the newest `backups\` folder (steps below).
  - **run.json** is renamed to `run.json.damaged-YYYYMMDD-HHMM` and the run carries on as a fresh run (nothing to resume). The newest 3 such files are kept.
  - **plays.json** is renamed aside the same way, counting starts again from empty, the run carries on, and the report says the popularity history was lost. Because it may hold user IDs, Store deletes a damaged plays file at the first roll-up after the UK day it was renamed.
  - A **missing** file starts empty, except: a missing managed.json or state.json when the Top Picks folder already holds managed-looking folders (any folder with a .strm) and `backups\` has a backup is treated as damaged.
- **Backups:** at F4 step 3, managed.json, state.json, plays.json **without its `today` section** (so no user IDs) and `TopPicksPlus.xml` are copied into `backups\YYYY-MM-DD-HHMM\`; the 8 newest are kept. To restore: stop Emby, copy the files back over the live ones, start Emby.
- **Personal data:** none is kept (D21, D33). Today's not-yet-counted plays hold user IDs until the next roll-up after their day ends. Nothing is sent anywhere; the plugin makes no network calls (no part may use `System.Net`).
- **Age ratings:** each copy's .nfo carries the source title's age rating, so Emby's parental controls apply to copies as to the originals.

## F10 – Staying working through Emby updates
- The build targets a fixed Emby plugin SDK version (D14).
- After every Emby server upgrade, the owner runs check C20. The plugin writes its version and the Emby version at the top of each run report.

## How the parts connect (for the builder)
- Emby creates the plugin's root class and the start-up entry point (both in the root `TopPicksPlus` namespace), plus EmbyBridge's scheduled task, event listeners and API service.
- The entry point receives Emby's services, builds EmbyBridge's adapters with them, builds the **paths** record (programdata, Top Picks folder, plugin data folder) and the **log** (both plain types defined in Logic), then builds Store, Artwork and Run, handing each what it needs. It then fills EmbyBridge's static `Hooks` registry with Run's hook implementations and a settings source (reading the plugin's configuration). Emby-created EmbyBridge classes reach Run and the settings only through that registry. Until it is filled, events are dropped (F6) and API calls answer "starting up".
- Run uses Store, Artwork and EmbyBridge only through interfaces those parts define (`IStore`, `IArtwork`, the EmbyBridge adapter interfaces), so Run is tested with fakes.
- Run gets settings as a plain Logic record and never uses Settings; run state is in Store's state.json (D29).

## Before building (prerequisites, not open questions)
Read from the server before the chunk that needs it: the SkiaSharp version Emby loads (Artwork, D15), the SQLite library version (import, D24), the .NET version Emby runs on (System.Text.Json, D30), and the exact names of the library-change and task events (EmbyBridge, F5). The first build chunk proves that a date added set by the plugin survives a full scan and a restart (C34).

## Not included (on purpose)
- Different picks for different users. It's one shared row, like today.
- Trailers or "coming soon" items.
- A different number of picks.
- Hiding the copies from search.
- Resuming a half-watched film from its copy (F7).
