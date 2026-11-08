---
summary: What the new Top Picks plugin does, in plain English. Every feature has a check in checks.md.
links: [decisions.md, checks.md, data.md]
---

# Top Picks (replacement) – spec

**Purpose:** Each week, put the 10 best titles to watch right now in the "Latest Top Picks" row on every Emby home screen, numbered 1–10 in the right order, every time.

**Replaces:** the old third-party Emby.TopPicks plugin (abandoned, closed source). Its order breaks when last week's files are still there, because Emby only reads the "date added" in an .nfo the first time it sees a title.

**Rigour level:** 1 – Personal (home Emby server, a handful of users, no logins or personal data handled by the plugin).

## F1 – When it runs
- A scheduled task in Emby, **"Refresh Top Picks"**, runs every **Sunday at 05:00** (UK time).
- The same task can be run any time with Emby's "Run now" button, or from the plugin's settings page.

## F2 – How the 10 titles are chosen
1. **Pinned titles first**, in the order the owner set. Pins always make the list (if there are more than 10 pins, only the first 10 are used).
2. **The remaining places are filled automatically**, by the ranking below. Candidates are movies and series from the chosen libraries (default: all movie and TV libraries, never the Top Picks library itself).
3. **Ranking = most watched** – plays across all users in the **last 7 days**, most plays first. For a series, every episode play counts towards the series. One person watching the same thing several times in a day counts once.
   - **Ties** go to the more recently added title, then the newer release.
   - **Filler:** if fewer than 10 titles were watched that week, the empty places go to the most recently added titles.
   - Pinning all 10 places gives a fully manual list, in exactly the owner's order.
4. Each title appears only once. Titles whose file can't be found are skipped.
5. If there aren't enough candidates, the row has fewer than 10 items. It never fails.

## F3 – What it writes
In the Top Picks folder (`C:\Users\Admin\AppData\Roaming\Emby-Server\programdata\data\top-picks`), one folder per title, named `Title (Year)`, matching the old plugin:
- **Movie:** `Title (Year).strm` (one line: the path of the real file), `Title (Year).nfo` (the title's metadata), subtitles (.srt) copied alongside, `fanart.jpg`, `logo.png`, and the theme video if one exists.
- **Series:** `tvshow.nfo`, then `Season N` folders with a .strm and .nfo for every episode, plus the same images.
- **Artwork:** `folder.png` and `thumb.png`, 1000×562: the title's backdrop, a large white rank number (1–10) on the left, and the title's logo over the picture (the title in large text if there's no logo).

## F4 – Clearing out last week
- Folders for titles that are no longer in the 10 are deleted.
- Folders for titles that stay are rewritten in full, with their new rank and artwork.
- The plugin only ever deletes inside the Top Picks folder, and only folders it manages (see F8).

## F5 – Getting the order right (the main fix)
After writing the files, the plugin:
1. Asks Emby to rescan just the Top Picks folder, and waits until all of this week's titles are in the library (up to 10 minutes).
2. Sets each title's **date added** directly in Emby: pick 1 = the time of the run, pick 2 = one hour earlier, and so on. It doesn't rely on Emby re-reading the .nfo.
3. Tells Emby to reload each title's artwork, so the number on the picture matches its position.
4. Reads the row back from Emby and checks the order is 1–10. Any mismatch is shown in the run report and the Emby log.

## F6 – Counting plays
- From the day it's installed, the plugin notes every playback that ends on the server: which title (and which series, for an episode), which user, and when.
- A playback counts as a play if it reached **at least 50%** or the item was marked as played.
- Plays are kept for 90 days, in the plugin's own data file. Older ones are removed.
- **First run:** if the Playback Reporting plugin's history is present, the last 7 days are read from it so the first list isn't empty. If it can't be read, the plugin carries on with what it has.

## F7 – Settings page (Emby dashboard → Plugins → Top Picks)
- Number of picks (default 10).
- Libraries to choose from.
- Pins: search for a title, add it, drag to reorder, remove.
- "Run now" button.
- **Last run report:** when it ran, the 10 titles with the reason each was picked (pinned / number of plays / filler), and whether the order check passed.

## F8 – Safety
- The plugin keeps a list of the folders it created. It deletes only folders on that list, and never anything outside the Top Picks folder.
- **First run takeover:** the folders the old plugin left behind are adopted once, so they can be cleared out properly.
- If the old Emby.TopPicks plugin is still installed, the task refuses to run and says why. Two plugins writing to the same folder would undo each other.
- If anything fails part-way, last week's row is left as it was rather than half-written. The new files are prepared first, then swapped in.

## Not included (on purpose)
- Different picks for different users. It's one shared row, like today.
- Trailers or "coming soon" items.
