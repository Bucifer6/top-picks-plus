---
summary: Plain-English checks the owner can try; each maps to a feature in spec.md and gets an automatic test where possible.
links: [spec.md]
---

# Checks

| ID | Feature | Check |
|----|---------|-------|
| C1 | F1 | In Dashboard → Scheduled Tasks, "Refresh Top Picks" has a weekly trigger, Sunday 05:00, and "Run now" works. |
| C2 | F4 | With last week's folders still in top-picks, run the task. The row shows 1 to 10 left to right, and each picture's number matches its place. (This is today's bug.) |
| C3 | F4 | Run the task twice in a row without deleting anything. The order is 1–10 both times. |
| C4 | F4/F5 | After a run, do a full library scan, then restart Emby, then look again the next day. The order is still 1–10 each time. |
| C5 | F4 | A week where series are at ranks 1, 5 and 10 and movies are in between: the row is still 1–10. Then add a new episode to the series at rank 10 in its real library, run again with the same picks: it stays at 10. |
| C6 | F2 | With no pins, a run fills all 10 places by itself from what was watched. Nothing is done by hand. |
| C7 | F2 | Pin 3 titles in a chosen order. After a run they are 1, 2, 3 in that order, and 4–10 are the most watched. |
| C8 | F2 | Pin 10 titles. The row is exactly those 10, in your order. |
| C9 | F2 | Pin a title, then remove it from its real library. The next run skips it, the others move up, and the settings page shows the pin as "missing". |
| C10 | F2/F6 | One user watches 6 episodes of a series in one evening; another user watches one film on 3 different days. The report shows the series with 1 viewer-day and the film with 3. |
| C11 | F6 | Watch a title from the Top Picks row past halfway. The report counts it for the real title. |
| C12 | F6 | Watch a film for 30%, stop, come back the same day and watch another 30%. It counts. Watching for 5 minutes doesn't. |
| C13 | F2 | A week with only 4 watched titles: places 5–10 are the newest additions, and the run finishes normally. |
| C14 | F7 | Finish a film from the Top Picks row. The real film in your Movies library now shows as watched for you. Stop a film half-way in the row, then open the real one: it offers to resume. |
| C15 | F4 | A title that drops out of the 10 disappears from the row, and its folder is gone from top-picks. A title that stays keeps your watched tick on its copy. |
| C16 | F3 | Playing a movie or an episode from the row plays the real file, with its subtitles. A series opens with its seasons and episodes. |
| C17 | F3 | A title with no logo gets artwork with its name in large text and the rank number. |
| C18 | F9 | Put a test folder in top-picks with a text file in it, after the plugin is installed. After two runs, it is still there. Separately, an old-plugin folder for a title that's no longer a pick is removed on the first run. |
| C19 | F9 | With the old Top Picks plugin still installed, the task refuses to run and the report says to uninstall it. |
| C20 | F10 | After an Emby upgrade, Run now: the run finishes, the row is in order, and the report shows the new Emby version. |
| C21 | F5 | Restart Emby in the middle of a run. Within 15 minutes of it coming back, the row is complete and in order, and the report says the run finished. |
| C22 | F1 | Press Run now twice quickly. The second is turned down with "a run is already in progress". |
| C23 | F8 | Settings: add, reorder and remove pins; choose libraries. The Top Picks library is not offered. The report shows rank, reason and row-check result for each title. |
| C24 | F6 | A play from 91 days ago is no longer in the log after a run. |
| C25 | F2 | Choose only your Movies library: the next run has no series. |
