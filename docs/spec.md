---
summary: What Top Picks Plus does, in plain English. Every feature has a check in checks.md. Version 5, after the spec v4 re-review and D34 (no takeover, no import).
links: [decisions.md, checks.md, data.md, modules.md, security.md, reviews/2026-11-08-spec-v4-review.md, reviews/2026-11-08-spec-v4-handling.md]
---

# Top Picks Plus – spec (v5, 2026-11-08)

**Purpose:** Every week, fill the "Latest Top Picks" row on the Emby home screen with the 10 titles people are watching most, numbered 1–10 in the right order every time, with no manual step. The owner can pin titles if they want to.

**Replaces:** the old third-party plugin Emby.TopPicks (file `Emby.TopPicks.dll`), abandoned and closed source. It orders the row by writing a "date added" into each title's .nfo file. Emby only reads that date the first time it sees a title, so when last week's folders are still there the order breaks (card 0001).

**Install (D34):** the owner uninstalls the old plugin, empties the top-picks folder by hand (its folders are only copies) and installs this plugin. Exact steps are in docs/releases.md. The plugin never adopts or imports anything from the old plugin: the first week's row is pins plus the newest titles, and real viewing takes over from the next week.

**Rigour level:** 1 – Personal: a home Emby server with a handful of users. No viewing history is kept: only how many people watched each title on each day, for 90 days (D21, D33).

## Words used here
- **Source title:** a real movie or series in the owner's normal libraries (for example N:\Movies, K:\Series). **Source item:** a source movie or source episode (the thing with a video file).
- **Emby ID:** an item's `Id` exactly as Emby's own API gives it, as text (on Emby 4.10 a number written as text). The first EmbyBridge build chunk confirms which Emby property this is (C34).
- **Movie or TV library:** an Emby library whose content type is "movies" or "tvshows". Mixed or other libraries are never used as sources.
- **Copy:** the Top Picks version of a source title: a folder of .strm, .nfo, subtitle and image files in the Top Picks folder. A .strm file is a one-line text file holding the full path of the real video file; Emby plays the real file through it. **Copy item:** the Emby item Emby makes for a copy (a copy movie, a copy series, or a copy episode).
- **Copy-looking folder:** a folder directly inside the Top Picks folder that holds a .strm file, in itself or one level down (a season folder). Used only by the missing-managed.json rule (F9).
- **Programdata:** Emby's data folder, given by Emby at start-up (here `C:\Users\Admin\AppData\Roaming\Emby-Server\programdata`).
- **Top Picks folder:** always `<programdata>\data\top-picks`. Fixed, not a setting (F9 "Finding the Top Picks library").
- **Top Picks library:** the existing Emby library whose only folder is the Top Picks folder. Its "Latest" home-screen row is the "Latest Top Picks" row. The plugin never creates or deletes libraries.
- **Plugin data folder:** `<programdata>\data\TopPicksPlus\` (D23). Holds managed.json, plays.json, run.json, run.previous.json, state.json, `staging\` and `backups\`. Never inside any library and never under `programdata\plugins`.
- **Date added:** Emby's own record of when an item was added (DateCreated). The Latest row shows the newest date added first.
- **Run start:** the moment Run creates this run's run.json (the first thing it does), in UTC. **Rank time** = run start rounded down to the whole minute. Both are stored in run.json and never change, even on resume. **Rank date** of pick N = rank time minus (N − 1) hours. **The run's UK day** = the UK day of the rank time.
- **UK day:** a calendar date in the Europe/London time zone (Windows: "GMT Standard Time"). EmbyBridge passes the zone and the current time into Run and Logic; Logic never reads the clock or the zone itself.
- **Managed list:** managed.json, the plugin's record of every copy it looks after (data.md). A folder is a **managed folder** only if it is on this list.
- **Candidate:** a title that may be picked (F2 step 5).
- **Viewer-day:** one user counted as watching one title on one UK day (F6).
- **Roll-up:** turning finished UK days' plays into daily counts, deleting their user IDs, and removing counts older than 90 days (F6, D33).
- **Can be reached** (a library folder): the folder exists and listing its top level succeeds within 30 seconds.
- **Run lock:** the one lock, held in memory inside the plugin, that a run holds from its start to its end (D32). It does not survive an Emby restart.
- **Waiting to resume (D36):** run.json exists, can be read, and its status is `running`, and no run holds the run lock. This only happens after Emby stopped during a run. Any other status (`finished`, `incomplete`, `failed`, `refused`) is never waiting, so a failed run can never block later runs.
- **Run statuses** (data.md run.json `status`): `running`; `finished`; `incomplete` (finished, but at least one pick's copy wasn't fully put in place or hadn't appeared by the end of the wait, F4 step 8); `failed` (an error after step 1, or a guard failing on resume after the swap began); `refused` (a guard said no before anything in the Top Picks folder was changed). A **successful run** is `finished` or `incomplete`.
- **Text comparisons** for ordering and ties are ordinal (character by character, not by language rules); "ignoring case" means ordinal ignoring case. Paths are compared as full resolved paths, ignoring case.

## F1 – When it runs
- Emby scheduled task **"Refresh Top Picks"**, default trigger every **Sunday at 05:00 on the server's clock**. The server is set to UK time; if the server's time zone isn't Europe/London, the run still happens and the report warns about it.
- **Run now (D35):** the settings page's Run now button asks Run directly (through EmbyBridge's API and the start-run hook). Run takes the run lock in that same call, so a second press is turned down at once. The run then carries on in the background, and the settings page shows "run in progress" until it ends. It does not show in Scheduled Tasks. The task's own Run button in Scheduled Tasks starts the task as usual.
- **One run at a time (D32, D36).** Every way of starting a run (the task, Run now, resume, the missed-run rule) goes through one Run entry, which takes the run lock. Run now, the task and the missed-run rule are turned down if a run holds the lock or a run is waiting to resume: the settings page says "a run is already in progress", and a task start returns at once with that message in the report log. Only resume may take the lock while a run is waiting to resume. Emby's own task queue is not relied on; if Emby queues a second task start, it runs later as an ordinary run, which is safe (C3).
- run.json records what started each run (`trigger`: `task`, `run now`, `missed run`). Emby doesn't tell the task whether its Sunday trigger or its Run button started it, so both are `task`. A resumed run keeps its trigger and sets `resumed`.
- **Start-up order** (timers owned by EmbyBridge, each calling a Run hook):
  1. +1 minute: roll-up (F6).
  2. +2 minutes: order guard (F5), unless a run holds the lock or a run is waiting to resume.
  3. +3 minutes: resume, if a run is waiting to resume (F5).
  4. +10 minutes: **missed-run rule**: if no run holds the lock, no run is waiting to resume, and state.json's last successful run is more than 7 days ago, or there has never been one (first install), start a run (`missed run`).
- Also every day at 00:05 UK time: roll-up (D33).
- `failed` and `refused` runs don't count as successful, so the missed-run rule tries again at the next start-up.

## F2 – Choosing the 10 (fully automatic)
No manual step is ever needed. Pins are optional. All of this is Logic, tested without Emby.
1. **Pins (optional) first**, in the owner's order. A pin can be any movie or series in any movie or TV library except the Top Picks library. A pin is **missing** if its source title is gone, is no longer a movie or series in a movie or TV library, or a movie's video file can't be found, or a series has no episode whose file can be found. Missing pins are skipped and the rest move up; each is listed in the run report (with its last-known title from state.json) and marked "missing" on the settings page. Only the first 10 usable pins are used. Duplicates are removed when settings are saved (F8).
2. **The remaining places go to the most watched candidates**, by **viewer-days in the 7 UK days before the run's UK day** (for a Sunday run, the previous Sunday to Saturday). Plays on the run's own day count next week. Episodes count towards their series. A pinned title isn't picked again.
3. **Order, first to last** (for watched titles; filler uses b–e):
   a. more viewer-days first;
   b. newest content first: a movie's date added; a series' newest available episode's date added;
   c. newer premiere date first; no premiere date goes after one that has it;
   d. title, A to Z, ignoring case;
   e. source Emby ID, ordinal, lowest first (so the order is always the same).
4. **Filler:** if fewer than 10 places are filled, the rest go to candidates nobody watched, ordered by 3b–3e. On the first run after install (no plays yet) every unpinned place is filler.
5. **Candidates:** movies and series in the chosen libraries (default: every movie and TV library apart from the Top Picks library). At step 1 the chosen library IDs are checked again: one that no longer exists or is no longer a movie or TV library is ignored and the report says so; if libraries were chosen and none of them is still usable, the run is refused. Logic itself drops anything in the Top Picks library or with a file under the Top Picks folder, whatever the settings say. A title appears once per Emby ID (the same film in two libraries is two titles). A movie is left out if its video file can't be found. A series is left out only if none of its episode files can be found; missing episodes are left out of its copy.
6. With fewer than 10 candidates the row is shorter. The run doesn't fail.
7. **Always 10.** The number of picks is fixed (D11).
8. **Offline drives (D25):** at F4 step 1, every folder of every chosen library, and of every pinned title's library, must be reachable (EmbyBridge's source-files adapter, D28). If any can't, the run is **refused** and the report names the folder.

## F3 – What each copy contains
**One naming rule for every folder and file the plugin makes** (Logic, `SafeName`):
- Remove `< > : " / \ | ? *` and control characters, turn runs of spaces into one space, then trim leading spaces and trailing dots and spaces.
- If the result is empty, or a name Windows reserves (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`, any case, with or without an extension), use the fallback given (always the item's Emby ID).
- Shorten to the length limit at a whole character, then trim again.
- **Lengths:** copy folder name at most 60 characters; episode file name (before the extension and language parts) at most 80. Every full path, including the 8-character temporary suffix `.tpp-tmp` (F4 step 7), must be at most 240 characters both in the Top Picks folder and in `staging\` (Logic checks the longer of the two). If a path is too long, the episode title part is shortened; if it still can't fit, that episode is left out and the report says so.
- Names are compared ignoring case when checking for clashes, as Windows does.

**Names:**
- **Copy folder:** `Title (Year)`, or `Title` with no year; fallback the source ID. If two picks get the same name, or the name is already used (ignoring case) by any folder in the Top Picks folder (managed or not, including a managed folder being deleted this run), the name is cut to 55 characters and ` - 2`, ` - 3` … added until free. The plugin never writes into a folder that isn't managed.
- **A title that stays keeps its folder name** for as long as it stays, even if its title or year changes, so Emby keeps the same item. A new name is only used when a title comes back after dropping out.
- **Movie files:** `<folder>.strm` and `<folder>.nfo`. The .strm points to the movie's main video file (for a movie with several versions, the one Emby plays by default).
- **Series:** `tvshow.nfo` in the copy folder, then one folder per season: `Season 1`, `Season 2` … (`Specials` for season 0). Episode files are `<Series> - S01E02 - <Episode title>` (`.strm` and `.nfo`), where `<Series>` is SafeName of the source series' name cut to 40 characters, and numbers are padded to at least 2 digits. With no episode title: `<Series> - S01E02`. A video file holding several episodes gets one .strm named with its first and last, `S01E01-E02`, and its managed entry lists every episode it holds. An episode with no season or episode number goes in `Specials` as `<Series> - S00E<Emby ID>`.
- **Subtitles:** for movies and episodes, external subtitle files (.srt, .ass, .ssa, .vtt, .sub) next to the source video are copied (read through the source-files adapter, D28), keeping the source's language and forced/default parts: `<copy file name>.en.forced.srt`.
- **The .strm content** is exactly the source file's full path and nothing else. If the path contains a line break, that item is left out and reported.

**.nfo files** are written with an XML writer (any text from Emby is escaped, never pasted in raw; characters XML doesn't allow at all are removed first), UTF-8:
- Movie: `title`, `originaltitle`, `year`, `premiered` (yyyy-MM-dd), `plot`, `mpaa` (age rating), `genre` (one per genre), `runtime` (minutes), `uniqueid` (each provider ID, e.g. imdb, tmdb), `dateadded`.
- Series (`tvshow.nfo`): `title`, `originaltitle`, `year`, `premiered`, `plot`, `mpaa`, `genre`, `uniqueid`, `dateadded`.
- Episode: `title`, `showtitle`, `season`, `episode`, `aired`, `plot`, `runtime`, `mpaa`, `dateadded`.
- `dateadded` is the rank date in **UTC**, `yyyy-MM-dd HH:mm:ss`, so the autumn clock change can't make two ranks share a time. Every episode of a series gets the series' rank date. The plugin also sets the date itself at F4 step 9; if Emby ever re-reads the .nfo as local time, every copy moves by the same amount, so the order still holds, and the order guard puts the exact values back.

**Artwork** (images come from EmbyBridge as bytes; Artwork never touches files):
- `fanart.jpg`, or `fanart.png` if the source backdrop is a PNG: the source's first backdrop, as-is.
- `logo.png`: the source logo, if there is one.
- `backdrops\theme.mp4`: the source theme video, if there is one, copied (opened through the source-files adapter) only when a copy is first created. An update keeps it as it is.
- `folder.png` and `thumb.png` (the same picture), drawn at **1000×562** (D6) with the embedded font **Montserrat ExtraBold** (SIL OFL, D15):
  - Background: the backdrop scaled to cover the whole picture, centred. No backdrop: the poster, scaled to the full height and centred on dark grey `#111111`. Neither: plain `#111111`.
  - A dark fade over the left 45%: 70% black at the left edge to clear at 45%.
  - The rank number in white. The font size is chosen so the digit "0" is 390 px tall (measured from the drawn glyph), the number starts 40 px from the left edge and is centred top to bottom. If the number is wider than 360 px ("10"), it is scaled down to 360 px wide, so it never reaches the logo box.
  - The logo fitted inside the box x 420–960, y 60–300, keeping its shape, centred in the box. No logo: the title in white in the same box, at most 2 lines, as large as fits between 72 px and 36 px, then cut with "…".
  - **If drawing fails** (an image that can't be read, or SkiaSharp can't load): the copy's `folder` and `thumb` pictures are the source backdrop (or, with no backdrop, the poster) saved as-is with their own extension (`folder.jpg` / `thumb.jpg` for a JPEG). With neither image, there is no folder picture. The report says drawing failed for that title.
  - Whatever is written, the copy ends up with only one `folder.*` and one `thumb.*`: an old one with another extension is removed (F4 step 7 "update").

## F4 – Each week's update, in order
Run carries out these steps. run.json's `step` names the step in progress, so a restart can carry on (F5). A run that refuses at step 1 changes nothing in the Top Picks folder.
1. **Create the run and refuse or go:** Store copies the previous run.json to run.previous.json (so a refused or failed run never hides the last report), then writes the new run.json (`runId`, run start, rank time, `trigger`, `step` = `guards`, status `running`, and `historyLost` / `stateReset` copied from state.json's pending flags, which are then cleared). Then the guards, in order: old-plugin guard, finding the Top Picks library (F9), damaged or missing managed.json (F9), chosen libraries still usable (F2 step 5), offline-drive check (F2 step 8). If any fails, status `refused`, the report says why, and nothing else is changed.
2. **Roll-up** the play log (F6). Safe to repeat.
3. **Back up** (F9) into `backups\YYYY-MM-DD-HHMMZ\` (the run start, UTC). If the backup fails, the run is refused (nothing has been changed yet).
4. **Choose** the 10 (F2).
5. **Plan:** compare the 10 with the managed list and write every operation into run.json before anything is changed: `delete` a drop-out, `update` a stayer, `add` a new title. Leftovers from an earlier stopped run are tidied first: an `incoming` entry whose folder is in the Top Picks folder becomes `active`; one whose folder isn't there is removed. A pick whose copy can't be planned at all (for example its every episode was left out) gets no operation and is listed in `skipped`.
6. **Prepare:** Store empties `staging\`. Run writes every new title's whole copy, and every stayer's whole new set of files (everything except `backdrops\theme.mp4`), into `staging\`, which Emby never sees.
7. **Swap**, one operation at a time, in the order **deletes, then updates, then adds**, each marked done in run.json when it finishes:
   - **delete:** delete its folder through the delete guard (F9), then remove its entry.
   - **update:** the same folder is kept. Each file from staging is written into the folder under a temporary name (`<name>.tpp-tmp`) and then renamed over the old file. Then every file in the folder that is not in the new set is deleted through the delete guard, except `backdrops\theme.mp4`; so the folder ends up exactly matching the new copy (an episode that has gone, an old `folder.jpg` next to a new `folder.png`, and any leftover `.tpp-tmp` file are all removed). Then the entry gets this week's rank, rank date and file list, `state` stays `active`.
   - **add:** write its entry to managed.json as `incoming`, move its folder from staging into the Top Picks folder (a move within the same drive, since both are under programdata), then mark it `active`. So a folder in the Top Picks folder is never unknown to the managed list.
   - Any operation that can't be completed (a guard check fails, the target of an add already exists, a file can't be written) is skipped with a `note` in run.json, and the run will end `incomplete`.
8. **Rescan** only the Top Picks library, and wait until every copy is in the library: for a movie, its copy item; for a series, the copy series and a copy episode for every .strm in its managed entry. Up to 15 minutes, or 30 minutes if any series' managed entry lists more than 200 .strm files. Each copy item's Emby ID is stored in managed.json as it is found.
9. **Set the order and reset played state:** for every copy item that has appeared, set its date added to its rank date (for a series, the series and every episode; if the first build chunk shows Emby orders series in Latest by "date last media added", that is set too, C34). Set every copy item to unplayed with no resume point for every user (D26). The same date is already in the .nfo (card 0001). Before any change, EmbyBridge checks that the item's path is inside the Top Picks folder, and refuses the change if not (F9).
10. **Refresh artwork** for each copy that has appeared, so the number on the picture matches. Then read each date added back and set it again if the refresh changed it.
11. **Check the row** (D22): ask Emby for the Latest Top Picks row as the apps do (the "latest items" request for the Top Picks library, 10 items, grouped by series), as the **check user**: the first user, by user ID (ordinal), who isn't an administrator, isn't disabled and can see the Top Picks library; if there is none, the first administrator the same way, and the report says so.
    - **Expected** = the picks whose copy is `active` and appeared at step 8, that the check user is allowed to see, in rank order. A pick with no copy (skipped) or a copy that hadn't appeared is left out of what is expected and listed in the report; so is a pick the check user isn't allowed to see (parental controls). Because copies are never played (step 9, F7), "hide watched from Latest" can't hide a pick.
    - Each returned item is matched by its Emby ID to the managed list. An episode (Emby sometimes returns one instead of its series) counts as its series. An item that matches no managed copy is reported by name and makes the check fail.
    - **passed** = the returned titles, in order, are exactly the expected picks and every pick was expected. **partial** = the returned titles are exactly the expected picks, in order, but some picks were left out because their copy was skipped or hadn't appeared. **failed** = anything else, including expected copies missing or out of order. **not run** = the run stopped before this step.
12. **Record:** first state.json's `lastSuccessfulRunUtc` (and each pin's last-known title), then run.json: status `finished` (or `incomplete`), `finishedUtc`, `step` = `done`. Release the lock. Run the order guard once. (If Emby stops between the two writes, resume repeats from step 8, which is harmless.)

**If the step 8 wait runs out:** steps 9–11 run for every copy that has appeared, the status is `incomplete`, and the order guard (F5) sets the dates of the rest when they appear. Copies that appear late are new items, so they are already unplayed, and their folder.png already carries the right number.

**If something goes wrong** (an error, not a restart) at any step after step 1: the run stops, the status is `failed`, and the report says which step and what failed in plain words; before step 7 it adds "nothing in the Top Picks folder was changed". No operation is undone. The lock is released. The order guard keeps the row ordered for the copies in place. The next run starts fresh (step 5 tidies leftovers).

**Errors never reach Emby:** every hook, timer, event handler and API action catches every error. In a run, an error makes the run `failed` as above; anywhere else it is logged (never with a user ID, F6) and that one action is dropped.

## F5 – Order guard and resume
- **Order guard:** 2 minutes after Emby starts, once after every run (successful or not), and 2 minutes after the last Emby library change to a Top Picks item (item added or updated in the Top Picks library: EmbyBridge waits for 2 quiet minutes, so a full scan triggers it once at the end). For each `active` entry in managed.json it compares the copy item's date added (the series and each episode) with its rank date and sets any that differ by more than 1 second. It finds copy items by the IDs in managed.json; an ID that is null or no longer exists is looked up by path (folder or .strm path) and stored; if nothing is found, that copy is skipped until next time. It only sets dates: it never resets played state, refreshes artwork or changes files. **It does nothing while a run holds the lock or a run is waiting to resume.** The exact Emby events are confirmed in the first EmbyBridge build chunk (C4, C32).
- **Resume (D36):** at +3 minutes after start-up, if a run is waiting to resume, Run takes the run lock (the one start allowed while a run is waiting) and adds 1 to run.json `resumeCount`. If `resumeCount` is now above 3 (Emby stopped during the same run three times), the run is not tried again: status `failed`, message "gave up after 3 restarts", lock released. Otherwise, by run.json `step`:
  - `guards` to `plan`: start again from step 1 with the same `runId`, run start and rank time (run.previous.json is not copied again). Each of these steps is safe to repeat: guards only read; roll-up repeats harmlessly; the backup is skipped if this run's backup folder is complete. If a guard now refuses, the status is `refused`.
  - `prepare`: run the guards again, then Store empties staging and step 6 starts again.
  - `swap`: run the guards again, then carry on from the first operation not marked done, using the files already in staging. Repeating is safe: a folder already gone is done; a new folder whose staging copy has gone and whose target exists is done; a stayer's files are written again. If an operation can't be finished (its staging folder is missing and the target isn't there), it is skipped with a note, and the run ends `incomplete`.
  - `rescan`, `dates`, `artwork`, `check`: run the guards again, then start again from step 8 (each is safe to repeat).
  - A guard that fails on resume at `prepare` or later makes the status `failed` with the guard's message.
  - Any error during resume is handled as any other run error (F4): status `failed`, lock released. So a run can never stay waiting to resume after its resume has been tried.
- What the row may show while a run is in progress: for a few minutes, a mix of last week's and this week's titles. When the run finishes, the row is complete and in order.

## F6 – Counting plays
- EmbyBridge listens for Emby's "playback started", "playback stopped" and "user data changed" events. It ignores trailers, extras and anything that isn't a movie or an episode.
- **Which title it counts for:** a play on a copy item counts for the source title. The copy is found in the managed list by its path (each managed .strm lists the source items it holds, data.md). A play on a source item counts for its movie or series, wherever it was started.
- **Runtime** of what was watched is always the **source** item's runtime (for a copy, the first source item its .strm holds), never the copy's.
- **Minutes watched in a sitting** = the stop position minus the start position (from the matching "playback started", held in memory only), in whole minutes rounded down, never below 0 or above the runtime. If the start wasn't seen (for example Emby restarted), the stop position is used. The in-memory start list holds at most 1,000 entries; entries older than 24 hours, and the oldest when it is full, are dropped.
- **Finished** = at "playback stopped", Emby reports the item as played for that user (Emby does this itself when playback passes its own "played" point).
- **Marked by hand** = a "user data changed" event that sets a movie or episode (source or copy) to played while that user has no playback of it in progress. It adds a sitting with 0 minutes and `markedPlayed` true, on the UK day it happened. Clearing played state adds nothing and removes nothing already recorded. The plugin's own changes to source items (F7) may show up here too; they can only add a user to a title they already watched that day, which counts once anyway.
- **Which users count:** every Emby user, administrators included.
- **What counts as watched:** a user counts for a title on a UK day if, that day, they finished an item in it or marked one played by hand, or their minutes on the title that day add up to at least 50% of the runtime, or 20 minutes when the runtime isn't known. For a series, the runtime is that of the episode watched, and each episode is judged on its own: one qualifying episode is enough. Two sittings of the same film at 30% each count. A sitting belongs to the UK day on which it stopped.
- **No viewing history (D21, D33):** who watched what is held only in plays.json's `today` section, so each person is counted once a day. **Roll-up** happens 1 minute after start-up, at 00:05 UK time every day, and at F4 step 2: every `today` entry from a UK day before the current one is turned into "title, day, number of viewers", and those entries (with their user IDs) are deleted, in one atomic write. A day is rolled up once: its entries are deleted in the same write that adds its counts. Daily counts more than 90 days before the current UK day are removed at every roll-up. Store keeps one lock around every change to plays.json, so the listener and a run never overwrite each other.
- Plays seen before start-up has finished wiring the plugin are dropped (logged at Debug without user).
- **Logging:** plays are never logged at Info level. The plugin never puts a user ID or user name into any log line or error message; Debug lines may name a title.

## F7 – Watched status goes to the real title (D26, D38)
- **Copies never look watched.** When a user's played state or resume point changes on a copy item (playback stopped, or marked played by hand), the plugin looks at the copy's new state for that user:
  - played → set every source item the copy's .strm holds to played for that user (Emby clears their resume point, as it always does when something is marked played);
  - not played but with a resume point above 0 → set that resume point on the source item for that user;
  - anything else (unplayed with no resume point) → **nothing is sent to the source.**
  Then the copy item is set back to unplayed with no resume point. The copy is matched by its path in the managed list, as in F6.
- **The source is only ever moved forward (D38):** F7 never sets a source item to unplayed and never clears or lowers its resume point. So the weekly reset of copies (F4 step 9), or anyone clearing a copy, can never wipe real watch history.
- So the real title shows as watched, and a half-watched film appears in Continue Watching as the real title. Starting the copy again from the row starts from the beginning: to carry on, use the real title or Continue Watching. Accepted by the owner (D26).
- Copies never take played state from their source.
- **Nothing chases itself:** F7 only reacts to changes on copy items and only writes to source items (plus resetting the copy to the one state that sends nothing), so its own changes can't start it again.
- **Known limitation, accepted by the owner:** copies appear in search next to the real titles, as they do today.

## F8 – Settings page and run report (Dashboard → Plugins → Top Picks Plus)
- **Administrators only.** Every action of the plugin's API (save settings, pin search, Run now, run status, read the report) takes the signed-in user from Emby's own authorisation of the request, never from a user ID in the request, and refuses anyone who isn't an Emby administrator with "Administrators only" and no data. Hiding the page is not relied on.
- **Libraries** to choose from (movie and TV libraries only). The Top Picks library is never offered.
- **Pins:** search for a movie or series, add it, drag to reorder, remove. The page marks a pin "missing" by asking the server when the page opens (live), and the report shows which were missing at the last run.
- **Saving checks every value** (EmbyBridge's API, on the server): library and pin IDs must be Emby IDs that exist and are a movie or TV library, or a movie or series in one; the Top Picks library and its items are refused; duplicate pins are removed; at most 20 pins. A bad value is refused with a plain message and nothing is saved. Good values are saved through Emby's plugin configuration (the Settings part's configuration class).
- **Run now** button (F1, D35), plus a "run in progress" indicator, read from the run-status API.
- **Last run report:** plugin version and Emby version; what started it and whether it was resumed; when it ran; status (finished / incomplete / failed / refused) and a plain-words message; each pick with rank, title, why it was picked (pinned / N viewer-days / filler) and whether its copy, date, artwork drawing and artwork refresh worked; skipped items and why; missing pins; ignored libraries; the row check result (passed / partial / failed / not run, which kind of user was used, what was left out of the expected list and why, and exactly what Emby returned if it didn't pass); total viewer-days in the 7-day window; whether popularity history was lost (damaged plays.json) or run state was reset (damaged state.json); a time-zone warning if needed. If the last run was `refused` or `failed`, the page also shows the run before it (run.previous.json). No user names or IDs appear anywhere.
- **Text from Emby** (titles, names, messages) is put on the page as plain text only, never as HTML.

## F9 – Safety
- **Finding the Top Picks library:** the Top Picks folder is `<programdata>\data\top-picks`. The run is refused if no library has it as a folder, if more than one does, if the library holding it has any other folder, if the Top Picks folder or the plugin data folder is itself a junction or symbolic link, or if the Top Picks folder is inside, or contains, any other library's folder, the plugin data folder or programdata itself (other than being under it). All of these comparisons use full resolved paths, ignoring case.
- **Before every write, move, delete or replace** in the Top Picks folder or `staging\`, Run checks (and skips and reports the item if any check fails):
  1. the target folder is on the managed list (for a file: it is inside a managed folder), or is inside `staging\`;
  2. a copy folder's name is a single name (no `\`, `/`, `..` or drive letter), and each part of a file's path inside the folder is a single name;
  3. its real location, with every shortcut resolved, is directly inside the Top Picks folder (for files: inside the managed folder), or inside `staging\`;
  4. neither it nor any folder on the way to it is a junction or symbolic link, and deleting never follows one: a link found inside a managed folder is removed as a link, never entered.
  **In practice:** the plugin deletes only folders on the managed list and the files inside them. If someone puts their own file inside a managed folder, it goes when that folder is deleted or updated. Unmanaged folders and loose files in the Top Picks folder are never touched. `staging\` can never be on the managed list (managed names are folders directly in the Top Picks folder).
- **Emby items the plugin changes:** date added, played state, resume points and artwork refresh are only ever changed on items whose path is inside the Top Picks folder (EmbyBridge checks before each change), with one exception: F7 sets played state and resume points on source items, only ever forward (D38).
- **Store's own deletes** (emptying `staging\`, pruning `backups\`, removing old damaged run files, deleting a damaged plays.json, removing leftover `*.tmp` files at start-up) are only ever inside the plugin data folder, with the same real-location and no-link checks, and backups are only pruned by their own name pattern (`YYYY-MM-DD-HHMMZ`).
- **Old-plugin guard:** if Emby has loaded a plugin whose assembly is `Emby.TopPicks`, every run is refused and the report says to uninstall it first. Nothing else is matched by name.
- **Damaged files (D37).** A file is **damaged** if it can't be read as JSON, has a `schemaVersion` the plugin doesn't know, or any field breaks data.md's rules (wrong type, value out of range, missing required field, a folder name that isn't a single name, a path that leaves its folder).
  - **managed.json** damaged: left exactly where it is and every run is refused until it is fixed. A **missing** managed.json while the Top Picks folder holds a copy-looking folder is treated the same way (Store reports the file missing; Run looks for copy-looking folders, since only Run reads the Top Picks folder). The report gives the fix, which is the same as installing: stop Emby, empty the top-picks folder (keeping any folder you made yourself), delete managed.json, start Emby, Run now.
  - **state.json** damaged: renamed to `state.json.damaged-YYYYMMDD-HHMMZ` and replaced by an empty one; the run carries on and the report says run state was reset (the missed-run rule may start an extra run, and missing pins show no title until the next run). It holds no user IDs.
  - **run.json** damaged: renamed to `run.json.damaged-YYYYMMDD-HHMMZ`; there is nothing to resume, and a fresh run can start. The newest 3 such files are kept. It holds no user IDs. (run.previous.json damaged: it is ignored and overwritten at the next run.)
  - **plays.json** damaged: **deleted at once**, because it may hold user IDs and can't be read to roll them up; counting starts again from empty, and the next run's report says the popularity history was lost. The daily counts can be put back by hand from the newest backup (stop Emby, copy its plays.json over, start Emby); backups never hold user IDs.
  - Damage is noticed whenever Store reads the file (a run, a roll-up, a play, the order guard), and handled the same way each time.
- **Backups:** at F4 step 3, plays.json **without its `today` section** (so no user IDs) and `TopPicksPlus.xml` (copied as a file, never read) are copied into `backups\YYYY-MM-DD-HHMMZ\`, then an empty `complete` file is written last; a folder without `complete` is unfinished and is not counted. Files that don't exist yet are skipped. The 8 newest complete folders are kept. To restore popularity counts or settings: stop Emby, copy the file back over the live one, start Emby. managed.json and state.json are not backed up: neither can be usefully restored (D37).
- **Personal data:** none is kept (D21, D33). Today's not-yet-counted plays hold user IDs until the next roll-up after their day ends. Nothing is sent anywhere; the plugin makes no network calls (no part may use `System.Net`).
- **Age ratings:** each copy's .nfo carries the source title's age rating, so Emby's parental controls apply to copies as to the originals.

## F10 – Staying working through Emby updates
- The build targets a fixed Emby plugin SDK version (D14).
- After every Emby server upgrade, the owner runs check C20. The plugin writes its version and the Emby version at the top of each run report.

## How the parts connect (for the builder)
- Emby creates the plugin's root class and the start-up entry point (both in the root `TopPicksPlus` namespace), plus EmbyBridge's scheduled task, event listeners and API service.
- The entry point receives Emby's services, builds EmbyBridge's adapters with them, builds the **paths** record (programdata, Top Picks folder, plugin data folder) and the **log** (both plain types defined in Logic), then builds Store, Artwork and Run, handing each what it needs. It then fills EmbyBridge's static `Hooks` registry with Run's hook implementations (the full list is in the EmbyBridge README) and a settings source (reading and saving the plugin's configuration). Emby-created EmbyBridge classes reach Run and the settings only through that registry. Until it is filled, events are dropped (F6) and API calls answer "starting up".
- Run uses Store, Artwork and EmbyBridge only through interfaces those parts define (`IStore`, `IArtwork`, the EmbyBridge adapter interfaces), so Run is tested with fakes.
- Run gets settings as a plain Logic record (through a hook argument or a settings-source interface defined in EmbyBridge) and never uses Settings; run state is in Store's state.json (D29).
- managed.json is only ever written by Run (the weekly steps and the order guard storing found IDs), through `IStore`, which holds one lock around each read-change-write of it.

## Before building (prerequisites, not open questions)
Read from the server before the chunk that needs it: the SkiaSharp version Emby loads (Artwork, D15), the .NET version Emby runs on (System.Text.Json, D30), and the exact names of the library-change and task events (EmbyBridge, F5). The first build chunk proves that a date added set by the plugin survives a full scan and a restart, and confirms which property is the Emby ID and how Latest orders series (C34).

## Not included (on purpose)
- Different picks for different users. It's one shared row, like today.
- Trailers or "coming soon" items.
- A different number of picks.
- Hiding the copies from search.
- Resuming a half-watched film from its copy (F7).
- Taking over the old plugin's folders, or importing from Playback Reporting (D34).
