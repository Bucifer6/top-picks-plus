---
summary: What Top Picks Plus does, in plain English. Every feature has a check in checks.md. Version 6: readiness fixes (D43–D91).
links: [decisions.md, checks.md, data.md]
---

# Top Picks Plus – spec (v6, 2026-11-08)

**Purpose:** Every week, fill the "Latest Top Picks" row on the Emby home screen with the 10 titles people are watching most, numbered 1–10 in the right order every time, with no manual step. The owner can pin titles if they want to.

**Replaces:** the old third-party Emby.TopPicks plugin, which is abandoned and closed source. The old plugin orders the row by writing a "date added" into each title's .nfo file. Emby only reads that date the first time it sees a title, so when last week's folders are still there the order breaks.

**Rigour level:** 1 – Personal: a home Emby server with a handful of users. The plugin keeps 90 days of who watched what, stored only on the server and visible only to administrators (see F9).

## Words used here
- **Source title:** the real movie or series in the owner's normal libraries (N:\Movies, K:\Series).
- **Copy:** the Top Picks version of a source title: a folder of .strm, .nfo and image files in the Top Picks folder. A .strm file is a one-line text file holding the path of the real video file. Emby plays the real file through it.
- **Top Picks folder:** `C:\Users\Admin\AppData\Roaming\Emby-Server\programdata\data\top-picks`.
- **Top Picks library:** the existing Emby library whose folder is the Top Picks folder. Its "Latest" home-screen row is the "Latest Top Picks" row. The plugin finds the library by its folder path. It doesn't create or delete libraries.
- **Date added:** Emby's own record of when an item was added (DateCreated). The Latest row shows the newest date added first.
- **Rank time:** the time this week's run started (UTC, to the whole minute), fixed for the whole run, including after a restart (D27). Pick 1 gets the rank time as its date added, pick 2 gets one hour earlier, and so on down to pick 10 at nine hours earlier.
- **Managed list:** the plugin's record of which copy folders it manages, keyed by **folder name** (an adopted folder may have no source title, and two old folders may point to the same one; see data.md).

## F1 – When it runs
- Emby scheduled task **"Refresh Top Picks"**, every **Sunday at 05:00 on the server's clock**. The server is set to UK time, so the run follows the clocks when they change.
- Can also be run with "Run now", in Emby's Scheduled Tasks or on the plugin's settings page.
- **Only one run at a time.** If a run is asked for while one is going, the request is turned down and the settings page says a run is already in progress. If a run hits an error while Emby keeps going, it is resumed 5 minutes later: the first attempt plus up to 3 resumes, then it is marked failed. The resume counter goes up once each time a resume starts, never on the error itself (D44, D68, D78). A scheduled or Run now request while an unfinished run is waiting resumes that run instead (D68).
- **Missed run:** if Emby starts up and the last successful run (finished or incomplete, D31) was more than 7 days ago, the plugin runs once, 10 minutes after start-up, unless an unfinished run was resumed. Right after first install there is no last run, so it waits for Sunday or Run now (D41). If the run record was damaged but the plugin has run before, it counts as a missed run (D90).
- **Start-up order (D35):** old-plugin guard first (if the old plugin is loaded, nothing else happens: no runs, no play logging, no watched sync, D51); then resume an unfinished run at 2 minutes (D90); otherwise the order guard at 2 minutes; then the missed-run rule at 10 minutes.

## F2 – Choosing the 10 (fully automatic)
The list is built automatically every week. **No manual step is ever needed.** Pins are optional.
1. **Pins (optional) first**, in the owner's order. A pin counts even if it is outside the chosen libraries (D23). A pin whose source title has gone from the library, or whose video file can't be found, is skipped. The rest move up, and the pin shows as "missing" on the settings page and in the run report. Only whole movies and series can be pinned, at most 10; a series pin needs at least one episode file that can be found (D61).
2. **The remaining places go to the most watched source titles**, scored as **viewer-days in the last 7 days** (the 7 full UK calendar days before the run day; for the Sunday run, the previous Sunday to Saturday, D21): each user who watched a title on a given UK calendar day adds 1. Watching 10 episodes of a series in one evening counts as 1 for that user that day. The same user on three different days counts as 3. Episodes count towards their series.
3. **Ties:** the title whose newest content was added most recently wins: for a movie, its date added; for a series, its newest episode's date added. If that's also tied, the newer premiere date wins; if still tied, the lower source item ID (D35).
4. **Filler:** if fewer than 10 titles were watched, the empty places go to the most recently added source titles (using the same "newest content" rule).
5. **Candidates:** movies and series in the chosen libraries (default: every movie and TV library apart from the Top Picks library). A title appears only once. A movie is skipped if its video file can't be found. A series is skipped only if none of its episode files can be found, and missing episodes are left out.
6. With fewer than 10 candidates the row is shorter. The run doesn't fail.
7. **Always 10.** The number of picks is fixed and can't be changed in settings.

## F3 – What each copy contains
- **Folder name:** `Title (Year)`. Characters Windows doesn't allow in folder names, and trailing dots or spaces, are removed. With no year it's just `Title`. If two picks would end up with the same folder name, the second gets ` - 2` added. The managed list links each folder to its source title's ID, so renamed titles never leave orphan folders behind.
- **Movie:** `<folder>.strm` pointing to the movie's main video file (for a movie with several versions, the one Emby plays by default) and `<folder>.nfo` holding the source title's metadata, including its age rating and a `<dateadded>` equal to its rank date. External subtitle files (.srt, .ass, .ssa, .vtt, .sub) of that version are copied alongside with matching names. The .nfo fields are listed in D30.
- **Series:** `tvshow.nfo`, then `Season N` folders (`Specials` for season 0) with a .strm and .nfo for every available episode, named `Series - SxxEyy - Episode title`, with each episode's external subtitles copied the same way as a movie's. Multi-episode files, missing numbers, title cleaning and long paths: D49. The .nfo `<dateadded>` format: D50.
- **Series images:** each episode's thumb and each season's poster are copied too (D70).
- **Subtitles and themes:** the whole subtitle suffix is kept (`.en.forced.srt`), `.idx` goes with its `.sub`, and subtitles are refreshed every run; theme songs are handled like theme videos (D55).
- **New copies:** a folder name that clashes with any folder already in top-picks gets ` - 2`; Windows reserved names get `_` added (D30).
- **Staying copies** keep their folder name even if the source title is renamed. In a staying series, new episodes are added and episode files the plugin made that are no longer available are deleted; nothing else in the folder is touched (D30).
- **Artwork:** `fanart.jpg` (the source backdrop), `logo.png` (the source logo, if there is one) and the theme video (`backdrops\theme.<ext>`, keeping the source's file type, if the source has one, D90). Theme videos are copied only when a copy is first created, not every week. `folder.png` and `thumb.png` are drawn by the plugin at 1000×562: the backdrop, darkened on the left, a large white rank number (1–10) on the left, and the logo over the picture. Exact layout and font: D62, with today's pictures in docs/reference/artwork/. With no logo, the title is written in large white text. With no backdrop, the poster is used on a dark background.

## F4 – Each week's update, in order
0. **Before anything else (D80):** the guards (a refusal stops here and is recorded), then the one-off Playback Reporting import (first run only), then the takeover of old-plugin folders (first run only). Each of these is a saved step, so a restart part-way repeats it safely.
1. **Finish leftovers** from an earlier run (D39), then **prepare** this week's copies in a staging folder outside every library: `programdata\plugins\TopPicksPlus\staging`. Emby never sees half-written files there. The 10 picks and the rank time are fixed now and saved in the run record.
2. **Swap in:**
   (Anything an earlier run left half-done was already finished before step 1, D39.) In this order, so a restart at any moment is safe (D26, D40):
   - Copies of titles that **drop out** are deleted (marked "leaving" on the managed list first, removed from it after).
   - Copies of titles that **stay** are updated in place, one file at a time: the same folder, with the .strm and .nfo rewritten and the artwork replaced. The folder isn't deleted and recreated, so Emby keeps the same item and the user's watched state on the copy.
   - **New** titles are added to the managed list first, then their folders are moved in from staging.
   - An old-plugin folder that is still a pick is treated as a drop-out plus a new copy, never updated in place (D40).
3. **Rescan** only the Top Picks library, and wait until every copy is in the library: up to 15 minutes, or 30 minutes if a series has more than 200 episodes.
4. **Set the order:** for each copy, set the date added directly in Emby to its rank date. For a series, the series and every one of its episodes get the rank date, so a show can never jump up the row because one episode was added later. The same date is already in the .nfo (step 1), so a later refresh that re-reads the .nfo can't undo it.
5. **Refresh artwork** for each copy so the number on the picture matches. Then read each date added back and set it again if the refresh changed it.
6. **Check the row** the way a user sees it: ask Emby for the Latest Top Picks row as the row-check user sees it (a setting; by default the first non-administrator user with no restrictions, D20), through the same request the apps use. Only ranked copies are compared: the check passes if the ranked copies in the row are exactly the ranked copies that user may see, in rank order (1–10 for an unrestricted user). Anything else in the row (a folder the plugin doesn't manage, a drop-out it couldn't delete) is listed in the report as an extra and doesn't fail the check (D83).
7. **Record** the result in the run report (F8). If the row check fails, steps 4–5 are done once more and it is checked again; if it still fails, the run still counts as finished and the report shows the failed check (D45).

**If the step 3 wait runs out:** steps 4–6 still run for every copy that has appeared, the run is marked "incomplete" in the report (even if the row check fails, and with no row-check retry, D84), and the order guard (F5) finishes the job when the rest appear.

## F5 – Order guard (keeps the order right between runs)
- After every library scan, and 2 minutes after Emby starts up (not while a run is in progress; the run sets the dates itself, D26), the plugin compares each ranked copy's date added (for a series, the series and every episode, D52) with its rank date and resets any that differ. It pauses only while a run is actually working (D44). Copies with no rank (adopted or leftover ones about to drop out) are skipped. This covers Emby restarting mid-run, full library scans, and metadata refreshes.
- **Unfinished run:** if Emby restarts during a run, the run carries on from where it stopped when Emby starts again, with the same picks and rank time. It doesn't roll back. Each step is safe to repeat (D26). At most 3 resumes are tried; after that the run is marked failed (D39, D78).
- What the row may show while a run is in progress: for a few minutes, a mix of last week's and this week's titles. When the run finishes, the row is complete and in order.

## F6 – Counting plays
- The plugin listens for Emby's "playback stopped" event on movies and episodes (nothing else is logged; the day is the UK date of the stop, D70). For each one it notes the user, the source title, the day (UK date), how many minutes were watched (stop position minus start position, D29), and whether Emby marked the item as played.
- **A play on a copy counts towards its source title.** The plugin looks the copy up in the managed list, so this week's picks get credit when people watch them from the row. A copy not on the list (an old-plugin copy before takeover) is traced to its source through its .strm file (D64).
- **What counts as watched:** a user counts for a title on a day if, that day, Emby marked something in the title as played, or their watching time on the title adds up to at least 50% of the runtime, or at least 20 minutes when the runtime isn't known. Two sittings of 30% each count. For a series this is per episode: minutes of different episodes don't add up (D22).
- Plays are kept for **90 days** in the plugin's own data file. Older ones are deleted every time it runs.
- **First run:** if the Playback Reporting plugin's history can be read, the last 7 days are imported from it, once only, so the first list isn't empty. If it can't be read, the first week uses each user's "last played" dates from Emby instead (D25, D47). Only plays from before the plugin started listening (the first start with the old plugin gone) are imported, so nothing counts twice (D46, D91); on go-live day, Run now is pressed straight after install so the whole week is imported (D63, D64). The report says which was used.
- The plugin keeps its own record, rather than reading Playback Reporting each week, so it keeps working if that third-party plugin breaks or is removed (decision D9).

## F7 – Watched status follows the real title
- When a user watches a copy and Emby marks it as played, the plugin marks the matching source movie or episode as played for that user too. When a user stops part-way, the resume point is copied to the source item. Marking a copy watched or unwatched by hand is copied to the source too (D55).
- When a copy is created or updated, it takes on each user's played state from the source title, so a copy someone has already seen shows the tick.
- **Known limitations, accepted by the owner:** the copies appear in search next to the real titles, as they do today; and a user blocked from a whole library still sees that library's titles in the Top Picks row (age ratings still apply, D42).

## F8 – Settings page and run report (Dashboard → Plugins → Top Picks Plus; administrators only)
- **Libraries** to choose from. The Top Picks library is never offered.
- **Row-check user** (D20).
- **Pins:** search for a title, add it, drag to reorder, remove. Pins that can't be used are marked "missing".
- **Run now** button, plus a "run in progress" indicator.
- **Last run report:** when it ran, finished / incomplete / failed, and each of the 10 titles with its rank, why it was picked (pinned / X viewer-days / filler) and whether its copy and date were applied. The row check result shows exactly what Emby returned if it didn't pass. Also shown: the number of plays recorded in the last 7 days, missing pins, any artwork that failed, and the last refused run with its reason and what to change (D31). Plus, when they happen: episodes left out, imported plays ignored, deletes refused, unmanaged folders, the row-check user falling back to an administrator, why a run failed, and damaged files that were set aside (D81).

## F9 – Safety
- **Deletes only what it manages:** it deletes only folders on the managed list (the whole folder, including anything inside), only direct children of the Top Picks folder after the safety checks in D37, and inside a staying copy only the files it wrote, after checking each file's stored path stays inside that copy (D82). Nothing in the Top Picks folder that isn't on the managed list is ever touched; staging is the plugin's own scratch folder (D60).
- **First-run takeover:** before the first run, folders already in the Top Picks folder that contain, counting their subfolders, at least one .strm file and one .nfo file (D38) are taken to be the old plugin's and added to the managed list, linked to their source title through the path in their .strm file (D28). Anything else, including loose files in the Top Picks folder, is left alone forever.
- **Old plugin guard:** if the old plugin is loaded in Emby (exactly: assembly `Emby.TopPicks` or plugin ID `672630FC-6FB8-4207-9C7B-AB414CD85B53`, D33), every run refuses to start and the report says to uninstall it first.
- **Newer data:** if a data file was written by a newer version, runs refuse and the plugin stops logging plays, syncing watched state and guarding the order, and writes none of its data files, so an older version can't spoil newer data (D72, D85).
- **Damaged data:** if the managed list can't be read, every run refuses and nothing is deleted. A damaged play log or run record is set aside (renamed) and a fresh one started; the report says so (D32).
- **Missing library:** if no library uses the Top Picks folder, the run refuses to start and says so.
- **Library setup (D53):** the Top Picks library must have its .nfo metadata reader on and no .nfo metadata saver on. Otherwise the run refuses (`librarySetup`) and the report says which setting to change.
- **Personal data:** the play log (user, title, day, minutes) stays in the plugin's data folder on the server, is never sent anywhere, and is shown only on the administrator settings page.
- **Age ratings:** each copy's .nfo carries the source title's age rating, so Emby's parental controls apply to copies just as they do to the originals.

## F10 – Staying working through Emby updates
- The build targets a fixed Emby plugin SDK version, recorded in the repo.
- After every Emby server upgrade, the owner runs check C20 ("Run now", then look at the row). The plugin also writes its version and the Emby version at the top of each run report.

## Proof plugin (D76, D77)
Before chunk 4 is kept, the owner installs a tiny separate plugin, "Top Picks Proof" (never Top Picks Plus), next to the old plugin. It proves a plugin built this way loads in Emby 4.10.2, reads Playback Reporting's history read-only to find out whether its times are UTC or local, and checks in a throwaway test library how Emby reads .nfo dates in summer and winter time. It never touches the Top Picks folder or the row and keeps no data. The owner copies its results into the chat, then uninstalls it.

## Going live (D63, D86)
Nothing of Top Picks Plus goes on the server until every part is built. On go-live day: remove the old plugin's task triggers; make the "Keep me" test folder; install Top Picks Plus with the old plugin still installed, restart, and check Top Picks Plus does nothing and asks for the old one to go (C44, C19); uninstall the old plugin, restart, press Run now, then try the other checks. To undo it that day, uninstall Top Picks Plus, put the old plugin back and restore its Sunday trigger.

## Known limits (accepted)
- Uninstalling Top Picks Plus leaves the copies and its data where they are (D73).
- A media folder in top-picks that the plugin doesn't manage shows in the row; the report names it (D73).
- The same film in two libraries (say Movies and a 4K library) counts as two titles (D55).
- A title removed and added again in Emby gets a new ID, so its pin shows "missing" and is added again (D48).
- Pins from the old plugin are re-entered by hand.
- "UK time" in this spec means the server's own time zone, which is set to UK time (D90).

## Not included (on purpose)
- Different picks for different users. It's one shared row, like today.
- Trailers or "coming soon" items.
- A different number of picks.
- Hiding the copies from search.
