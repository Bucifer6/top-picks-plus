---
summary: What Top Picks Plus does, in plain English. Every feature has a check in checks.md. Version 2, after the challenge round.
links: [decisions.md, checks.md, data.md]
---

# Top Picks Plus – spec (v2, 2026-11-08)

**Purpose:** Every week, fill the "Latest Top Picks" row on the Emby home screen with the 10 titles people are watching most, numbered 1–10 in the right order every time, with no manual step. The owner can pin titles if they want to.

**Replaces:** the old third-party Emby.TopPicks plugin, which is abandoned and closed source. The old plugin orders the row by writing a "date added" into each title's .nfo file. Emby only reads that date the first time it sees a title, so when last week's folders are still there the order breaks.

**Rigour level:** 1 – Personal: a home Emby server with a handful of users. The plugin keeps 90 days of who watched what, stored only on the server and visible only to administrators (see F9).

## Words used here
- **Source title:** the real movie or series in the owner's normal libraries (N:\Movies, K:\Series).
- **Copy:** the Top Picks version of a source title: a folder of .strm, .nfo and image files in the Top Picks folder. A .strm file is a one-line text file holding the path of the real video file. Emby plays the real file through it.
- **Top Picks folder:** `C:\Users\Admin\AppData\Roaming\Emby-Server\programdata\data\top-picks`.
- **Top Picks library:** the existing Emby library whose folder is the Top Picks folder. Its "Latest" home-screen row is the "Latest Top Picks" row. The plugin finds the library by its folder path. It doesn't create or delete libraries.
- **Date added:** Emby's own record of when an item was added (DateCreated). The Latest row shows the newest date added first.
- **Rank time:** the time of this week's run. Pick 1 gets the rank time as its date added, pick 2 gets one hour earlier, and so on down to pick 10 at nine hours earlier.
- **Managed list:** the plugin's record of which copies it made, keyed by the source title's Emby ID (see data.md).

## F1 – When it runs
- Emby scheduled task **"Refresh Top Picks"**, every **Sunday at 05:00 on the server's clock**. The server is set to UK time, so the run follows the clocks when they change.
- Can also be run with "Run now", in Emby's Scheduled Tasks or on the plugin's settings page.
- **Only one run at a time.** If a run is asked for while one is going, the request is turned down and the settings page says a run is already in progress.
- **Missed run:** if Emby starts up and the last successful run was more than 7 days ago, the plugin runs once, 10 minutes after start-up.

## F2 – Choosing the 10 (fully automatic)
The list is built automatically every week. **No manual step is ever needed.** Pins are optional.
1. **Pins (optional) first**, in the owner's order. A pin whose source title has gone from the library, or whose video file can't be found, is skipped. The rest move up, and the pin shows as "missing" on the settings page and in the run report. Only the first 10 valid pins are used.
2. **The remaining places go to the most watched source titles**, scored as **viewer-days in the last 7 days**: each user who watched a title on a given UK calendar day adds 1. Watching 10 episodes of a series in one evening counts as 1 for that user that day. The same user on three different days counts as 3. Episodes count towards their series.
3. **Ties:** the title whose newest content was added most recently wins: for a movie, its date added; for a series, its newest episode's date added. If that's also tied, the newer premiere date wins.
4. **Filler:** if fewer than 10 titles were watched, the empty places go to the most recently added source titles (using the same "newest content" rule).
5. **Candidates:** movies and series in the chosen libraries (default: every movie and TV library apart from the Top Picks library). A title appears only once. A movie is skipped if its video file can't be found. A series is skipped only if none of its episode files can be found, and missing episodes are left out.
6. With fewer than 10 candidates the row is shorter. The run doesn't fail.
7. **Always 10.** The number of picks is fixed and can't be changed in settings.

## F3 – What each copy contains
- **Folder name:** `Title (Year)`. Characters Windows doesn't allow in folder names, and trailing dots or spaces, are removed. With no year it's just `Title`. If two picks would end up with the same folder name, the second gets ` - 2` added. The managed list links each folder to its source title's ID, so renamed titles never leave orphan folders behind.
- **Movie:** `<folder>.strm` pointing to the movie's main video file (for a movie with several versions, the one Emby plays by default) and `<folder>.nfo` holding the source title's metadata, including its age rating and a `<dateadded>` equal to its rank date. External subtitle files (.srt, .ass, .ssa, .vtt, .sub) of that version are copied alongside with matching names.
- **Series:** `tvshow.nfo`, then `Season N` folders with a .strm and .nfo for every available episode, named `Series - SxxEyy - Episode title`.
- **Artwork:** `fanart.jpg` (the source backdrop), `logo.png` (the source logo, if there is one) and the theme video (`backdrops\theme.mp4`, if the source has one). Theme videos are copied only when a copy is first created, not every week. `folder.png` and `thumb.png` are drawn by the plugin at 1000×562: the backdrop, darkened on the left, a large white rank number (1–10) on the left, and the logo over the picture. With no logo, the title is written in large white text. With no backdrop, the poster is used on a dark background.

## F4 – Each week's update, in order
1. **Prepare** this week's copies in a staging folder outside every library: `programdata\plugins\TopPicksPlus\staging`. Emby never sees half-written files there.
2. **Swap in:**
   - Copies of titles that **drop out** are deleted.
   - Copies of titles that **stay** are updated in place: the same folder, with the .strm and .nfo rewritten and the artwork replaced. The folder isn't deleted and recreated, so Emby keeps the same item and the user's watched state on the copy.
   - **New** titles' folders are moved in from staging.
3. **Rescan** only the Top Picks library, and wait until every copy is in the library: up to 15 minutes, or 30 minutes if a series has more than 200 episodes.
4. **Set the order:** for each copy, set the date added directly in Emby to its rank date. For a series, the series and every one of its episodes get the rank date, so a show can never jump up the row because one episode was added later. The same date is already in the .nfo (step 1), so a later refresh that re-reads the .nfo can't undo it.
5. **Refresh artwork** for each copy so the number on the picture matches. Then read each date added back and set it again if the refresh changed it.
6. **Check the row** the way a user sees it: ask Emby for the Latest Top Picks row as one non-administrator user would see it, through the same request the apps use. The check passes only if the row reads 1–10 in order.
7. **Record** the result in the run report (F8).

**If the step 3 wait runs out:** steps 4–6 still run for every copy that has appeared, the run is marked "incomplete" in the report, and the order guard (F5) finishes the job when the rest appear.

## F5 – Order guard (keeps the order right between runs)
- After every library scan, and 2 minutes after Emby starts up, the plugin compares each current copy's date added with its rank date and resets any that differ. This covers Emby restarting mid-run, full library scans, and metadata refreshes.
- **Unfinished run:** if Emby restarts during a run, the run carries on from where it stopped when Emby starts again. It doesn't roll back.
- What the row may show while a run is in progress: for a few minutes, a mix of last week's and this week's titles. When the run finishes, the row is complete and in order.

## F6 – Counting plays
- The plugin listens for Emby's "playback stopped" event on every item. For each one it notes the user, the source title, the day (UK date), how many minutes were watched, and whether Emby marked the item as played.
- **A play on a copy counts towards its source title.** The plugin looks the copy up in the managed list, so this week's picks get credit when people watch them from the row.
- **What counts as watched:** a user counts for a title on a day if, that day, Emby marked something in the title as played, or their watching time on the title adds up to at least 50% of the runtime, or at least 20 minutes when the runtime isn't known. Two sittings of 30% each count.
- Plays are kept for **90 days** in the plugin's own data file. Older ones are deleted every time it runs.
- **First run:** if the Playback Reporting plugin's history can be read, the last 7 days are imported from it, once only, so the first list isn't empty. If it can't be read, the plugin carries on, and the report says the first list mostly used recently added titles.
- The plugin keeps its own record, rather than reading Playback Reporting each week, so it keeps working if that third-party plugin breaks or is removed (decision D9).

## F7 – Watched status follows the real title
- When a user watches a copy and Emby marks it as played, the plugin marks the matching source movie or episode as played for that user too. When a user stops part-way, the resume point is copied to the source item.
- When a copy is created or updated, it takes on each user's played state from the source title, so a copy someone has already seen shows the tick.
- **Known limitation, accepted by the owner:** the copies appear in search next to the real titles, as they do today.

## F8 – Settings page and run report (Dashboard → Plugins → Top Picks Plus; administrators only)
- **Libraries** to choose from. The Top Picks library is never offered.
- **Pins:** search for a title, add it, drag to reorder, remove. Pins that can't be used are marked "missing".
- **Run now** button, plus a "run in progress" indicator.
- **Last run report:** when it ran, finished / incomplete / failed, and each of the 10 titles with its rank, why it was picked (pinned / X viewer-days / filler) and whether its copy and date were applied. The row check result shows exactly what Emby returned if it didn't pass. Also shown: the number of plays recorded in the last 7 days.

## F9 – Safety
- **Deletes only what it made:** it deletes only folders on the managed list, only inside the Top Picks folder, and never a file or folder it didn't create.
- **First-run takeover:** before the first run, folders already in the Top Picks folder that contain at least one .strm file and one .nfo file are taken to be the old plugin's and added to the managed list. Anything else, including loose files in the Top Picks folder, is left alone forever.
- **Old plugin guard:** if a plugin named "Top Picks" (the old Emby.TopPicks) is loaded in Emby, every run refuses to start and the report says to uninstall it first.
- **Missing library:** if no library uses the Top Picks folder, the run refuses to start and says so.
- **Personal data:** the play log (user, title, day, minutes) stays in the plugin's data folder on the server, is never sent anywhere, and is shown only on the administrator settings page.
- **Age ratings:** each copy's .nfo carries the source title's age rating, so Emby's parental controls apply to copies just as they do to the originals.

## F10 – Staying working through Emby updates
- The build targets a fixed Emby plugin SDK version, recorded in the repo.
- After every Emby server upgrade, the owner runs check C20 ("Run now", then look at the row). The plugin also writes its version and the Emby version at the top of each run report.

## Not included (on purpose)
- Different picks for different users. It's one shared row, like today.
- Trailers or "coming soon" items.
- A different number of picks.
- Hiding the copies from search.
