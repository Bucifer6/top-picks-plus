---
summary: Plain-English checks the owner can try; each maps to a feature in spec.md and gets an automatic test where possible.
links: [spec.md, decisions.md]
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
| C16 | F3 | Playing a movie or an episode from the row plays the real file, with its subtitles (episodes too). A series opens with its seasons and episodes. |
| C17 | F3 | A title with no logo gets artwork with its name in large text and the rank number. |
| C18 | F9 | Before the first run, create a folder "Keep me" in top-picks holding only note.txt. After two runs, the folder and note.txt are still there, unchanged. Separately, an old-plugin folder for a title that's no longer a pick is removed on the first run, and one that is still a pick is replaced by a fresh copy. |
| C19 | F9 | With the old Top Picks plugin still installed, the task refuses to run and the report says to uninstall it. |
| C20 | F10 | After an Emby upgrade, Run now: the run finishes, the row is in order, and the report shows the new Emby version. |
| C21 | F5 | Restart Emby in the middle of a run. Within 15 minutes of it coming back (30 if a pick is a series with over 200 episodes), the row is complete and in order, and the report says the run finished. |
| C22 | F1 | Press Run now twice quickly. The second is turned down with "a run is already in progress". |
| C23 | F8 | Settings: add, reorder and remove pins; choose libraries and the row-check user. The Top Picks library is not offered. The report shows rank, reason and row-check result for each title. |
| C24 | F6 | A play from 91 days ago is no longer in the log after a run. (Also an automatic test.) |
| C25 | F2 | Choose only your Movies library: the next run has no series. |
| C26 | F1 | Stop Emby for over a week (or set the last successful run back 8 days on a test copy), start it: about 10 minutes later a run happens by itself. |
| C27 | F6 | First run after install: the report says the first week came from Playback Reporting, and titles watched last week are in the list. If Playback Reporting can't be read, it says "last played" was used instead. |
| C28 | F9 | Remove the Top Picks library (on a test server) and Run now: it refuses and says the library is missing. Nothing is deleted. |
| C29 | F4 | A run whose rescan wait runs out is marked "incomplete"; once the rest of the copies appear, the order guard puts them in order without another run. |
| C30 | F9 | A user whose parental control blocks a rating doesn't see a Top Picks copy of a title with that rating, just as they don't see the original. |
| C31 | F4 | With the row-check user left empty, the report names the user it used (not an admin). With a restricted user chosen, a row missing their blocked titles still passes if the rest are in order. |
| C32 | F2 | Choose only Movies, and pin a title from Kids: the pin is still used. |
| C33 | F2 | (Automatic) The 7-day window for a Sunday run is the previous Sunday to Saturday; a mid-week run uses the 7 days before it. |
| C34 | F6 | (Automatic) 30% of one episode plus 30% of another on the same day doesn't count; two 30% sittings of the same episode do. |
| C35 | F2 | (Automatic) Titles tied on everything come out in the same order every time (lowest ID as a number first, so 99 before 100). |
| C36 | F5 | (Automatic, plus C21) Stopping a run at any step and resuming it ends with the same 10 copies, the same rank dates, no folder in top-picks missing from the managed list, and nothing unmanaged deleted. |
| C37 | F9 | (Automatic) If managed.json is damaged, a run refuses with "data damaged" and deletes nothing; a damaged plays.json is renamed and a new one started. |
| C38 | F8 | Press Run now while a run is going: the running report is not replaced; the refusal shows separately. |
| C39 | F3 | A series that stays a pick gains a new episode in its real library: the next run adds it to the copy. An episode removed from the real library disappears from the copy. Any file you put in the copy folder yourself is left alone while the title stays. |
| C40 | F9 | (Automatic) A damaged run.json is renamed and the next run starts normally. |
| C41 | F7 | A title you've already watched becomes a new pick: its copy shows your watched tick straight away. |
| C42 | F3 | A title with a theme video that stays a pick for two weeks: the theme video is copied the first week only (its file date doesn't change). |
| C43 | F8 | (Automatic) When the artwork drawing fails, the copy gets the plain backdrop and the report marks that pick "artwork failed". |
| C44 | F1/F9 | With the old plugin installed, restart Emby: nothing runs (no resume, no order guard, no missed run) and the report says to uninstall it. |
| C45 | F9 | (Automatic) A managed entry whose folder is empty, ".", "..", a path, or a junction pointing elsewhere is never deleted; the report names it. |
| C46 | F9 | (Automatic) An old-plugin series folder (tvshow.nfo at the top, .strm files only in Season folders) is adopted at takeover. |
| C47 | F4 | (Automatic) A run after a failed run finishes the old run's "leaving" deletes and drops its stray "incoming" copies; nothing is orphaned. |
