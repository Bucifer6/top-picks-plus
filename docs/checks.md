---
summary: Plain-English checks the owner can try; each maps to a feature in spec.md and gets an automatic test where possible (A = automatic test of the rule as well as the owner's check).
links: [spec.md, data.md]
---

# Checks

| ID | Feature | Check | Auto |
|----|---------|-------|------|
| C1 | F1 | In Dashboard → Scheduled Tasks, "Refresh Top Picks" has a weekly trigger, Sunday 05:00. Pressing Run now on the settings page starts that task and it shows as running there. | |
| C2 | F4 | With last week's folders still in top-picks, run the task. The row shows 1 to 10 left to right, and each picture's number matches its place. (This is today's bug.) | |
| C3 | F4 | Run the task twice in a row without deleting anything. The order is 1–10 both times. | |
| C4 | F4/F5 | After a run, do a full library scan, then restart Emby, then look again the next day. The order is still 1–10 each time. | |
| C5 | F4 | A week where series are at ranks 1, 5 and 10 and movies are in between: the row is still 1–10. Then add a new episode to the series at rank 10 in its real library, run again with the same picks: it stays at 10. | |
| C6 | F2 | With no pins, a run fills all 10 places by itself from what was watched. Nothing is done by hand. | A |
| C7 | F2 | Pin 3 titles in a chosen order. After a run they are 1, 2, 3 in that order, and 4–10 are the most watched. | A |
| C8 | F2 | Pin 10 titles. The row is exactly those 10, in your order. | A |
| C9 | F2 | Pin a title, then remove it from its real library. The next run skips it, the others move up, and the settings page and report show the pin as "missing". | A |
| C10 | F2/F6 | One user watches 6 episodes of a series in one evening; another user watches one film on 3 different days. After the next run, the report shows the series with 1 viewer-day and the film with 3, and no user names. | A |
| C11 | F6 | In a quiet week, watch a title from the Top Picks row past halfway. After the next run, that title's reason in the report is "N viewer-days" (not "filler"), counted for the real title. | A |
| C12 | F6 | Watch a film for 30%, stop, come back the same day and watch another 30%: after the next run it has a viewer-day. A film watched for 5 minutes only has none (it shows as "filler" if it is picked at all). | A |
| C13 | F2 | A week with only 4 watched titles: places 5–10 are the newest additions, and the run finishes normally. | A |
| C14 | F7 | Finish a film from the Top Picks row: the real film in your Movies library now shows as watched for you. Stop a film half-way in the row, then open the real one: it offers to resume. Mark an episode in the row as unwatched: the real episode is unwatched too. | A |
| C15 | F4 | A title that drops out of the 10 disappears from the row, and its folder is gone from top-picks. A title that stays keeps your watched tick on its copy and keeps the same folder name. | A |
| C16 | F3 | Playing a movie or an episode from the row plays the real file, with its subtitles. A series opens with its seasons and episodes. | |
| C17 | F3 | A title with no logo gets artwork with its name in large text and the rank number. A title with no backdrop gets its poster on dark grey with the number. | A |
| C18 | F9 | Put a test folder in top-picks with a text file in it, after the plugin is installed. After two runs, it is still there. Separately, an old-plugin folder for a title that's no longer a pick is removed on the first run, and `backups\takeover-….zip` exists. | A |
| C19 | F9 | With the old Top Picks plugin still installed, the task is refused and the report says to uninstall it. | |
| C20 | F10 | After an Emby upgrade, Run now: the run finishes, the row is in order, and the report shows the new Emby version. | |
| C21 | F5 | Restart Emby in the middle of a run. Within 20 minutes of it coming back (35 if a series has over 200 episodes), the row is complete and in order, and the report says the run finished or was incomplete, not failed. | A |
| C22 | F1 | Press Run now twice quickly. The second is turned down with "a run is already in progress". | A |
| C23 | F8 | Settings: add, reorder and remove pins; choose libraries. The Top Picks library is not offered. The report shows rank, title, reason, and the row-check result. | |
| C24 | F6 | A daily count from 91 days ago is no longer in plays.json after a run. | A |
| C25 | F2 | Choose only your Movies library: the next run has no series. | A |
| C26 | F8 | Signed in as a non-administrator, open the plugin's report address (given in the release notes) in a browser: it says "Administrators only" and shows nothing else. Pressing Run now from that account is refused too. | A |
| C27 | F9 | Inside the copy folder of a title that is about to drop out, make a junction pointing at a test folder of your own with a file in it. After the run, the copy folder is gone, and your test folder and its file are untouched. | A |
| C28 | F3 | A title whose name is only characters Windows doesn't allow (or is called "CON") gets a folder named by its Emby ID and plays normally. A title whose name matches a folder you made yourself in top-picks gets " - 2" and your folder is untouched. | A |
| C29 | F2 | Unplug or disconnect the drive of a chosen library and run the task: it is refused, the report names the folder, and the row is unchanged. | A |
| C30 | F9 | Stop Emby, damage managed.json (type a few letters at the start), start Emby and run: the run is refused, nothing in top-picks changes, and the report says how to restore from backups. Restore it as described: the next run finishes. | A |
| C31 | F1 | On first install (no run yet), restart Emby: about 10 minutes later a run starts by itself. | A |
| C32 | F5 | Start a full library scan while a run is in its rescan step: the run still finishes and the row is 1–10. | A |
| C33 | F6 | Another user watches an episode of a series on the run's own day (Sunday morning, before 05:00): it counts in next week's run, not this one. | A |
