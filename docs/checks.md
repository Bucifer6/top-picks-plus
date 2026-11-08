---
summary: Plain-English checks the owner can try; each maps to a feature in spec.md and gets an automatic test where possible (A = automatic test of the rule as well as the owner's check; T = automatic test only). Version 5.
links: [spec.md, data.md]
---

# Checks

| ID | Feature | Check | Auto | Test layer |
|----|---------|-------|------|------------|
| C1 | F1 | In Dashboard → Scheduled Tasks, "Refresh Top Picks" has a weekly trigger, Sunday 05:00, and its Run button starts a run that shows as running there. Pressing Run now on the settings page starts a run and the page shows "run in progress" until it ends. |  |  |
| C2 | F4 | With last week's folders still in top-picks, run the task. The row shows 1 to 10 left to right, and each picture's number matches its place. (This is today's bug.) |  |  |
| C3 | F4 | Run the task twice in a row without deleting anything. The order is 1–10 both times. |  |  |
| C4 | F4/F5 | After a run, do a full library scan, then restart Emby, then look again the next day. The order is still 1–10 each time. |  |  |
| C5 | F4 | A week where series are at ranks 1, 5 and 10 and movies are in between: the row is still 1–10. Then add a new episode to the series at rank 10 in its real library, run again with the same picks: it stays at 10. |  |  |
| C6 | F2 | With no pins, a run fills all 10 places by itself from what was watched. Nothing is done by hand. | A | L |
| C7 | F2 | Pin 3 titles in a chosen order. After a run they are 1, 2, 3 in that order, and 4–10 are the most watched. | A | L |
| C8 | F2 | Pin 10 titles. The row is exactly those 10, in your order. | A | L |
| C9 | F2 | Pin a title, then remove it from its real library. The next run skips it, the others move up, and the settings page and report show the pin as "missing". | A | L, R |
| C10 | F2/F6 | One user watches 6 episodes of a series in one evening; another user watches one film on 3 different days. After the next run, the report shows the series with 1 viewer-day and the film with 3, and no user names. | A | L |
| C11 | F6 | In a quiet week, watch a title from the Top Picks row past halfway. After the next run, that title's reason in the report is "N viewer-days" (not "filler"), counted for the real title. | A | L, R |
| C12 | F6 | Watch a film for 30%, stop, come back the same day and watch another 30%: after the next run it has a viewer-day. A film watched for 5 minutes only has none (it shows as "filler" if it is picked at all). | A | L |
| C13 | F2 | A week with only 4 watched titles: places 5–10 are the newest additions, and the run finishes normally. | A | L |
| C14 | F7 | Finish a film from the Top Picks row: the real film in your Movies library now shows as watched for you, and the copy in the row still shows unwatched. Stop a film half-way in the row: the real film appears in Continue Watching and offers to resume; the copy doesn't. Mark an episode in the row as watched by hand: the real episode is watched and the copy isn't. After the next weekly run (which resets every copy to unwatched), the real film still shows as watched and the half-watched one still offers to resume. | A | R |
| C15 | F4 | A title that drops out of the 10 disappears from the row, and its folder is gone from top-picks. A title that stays keeps the same folder name. With "hide watched content from Latest" turned on for your user, after watching a pick it is still in the row. | A | R |
| C16 | F3 | Playing a movie or an episode from the row plays the real file, with its subtitles. A series opens with its seasons and episodes. |  |  |
| C17 | F3 | A title with no logo gets artwork with its name in large text and the rank number. A title with no backdrop gets its poster on dark grey with the number. | A | Art |
| C18 | F9 | Put a test folder in top-picks with a text file in it, after the plugin is installed. After two runs, it is still there, untouched. | A | R |
| C19 | F9 | With the old Top Picks plugin still installed, the task is refused and the report says to uninstall it. |  |  |
| C20 | F10 | After an Emby upgrade, Run now: the run finishes, the row is in order, and the report shows the new Emby version. |  |  |
| C21 | F5 | Restart Emby in the middle of a run. Within 20 minutes of it coming back (35 if a series has over 200 episodes), the row is complete and in order, and the report says the run finished or was incomplete, not failed. | A | R |
| C22 | F1 | Press Run now twice quickly. The second is turned down with "a run is already in progress". The same while the task is running from Scheduled Tasks. | A | R |
| C23 | F8 | Settings: add, reorder and remove pins; choose libraries. The Top Picks library is not offered. The report shows rank, title, reason, and the row-check result. |  |  |
| C24 | F6 | A daily count from 91 days ago is no longer in plays.json after a run. | A | S |
| C25 | F2 | Choose only your Movies library: the next run has no series. | A | L |
| C26 | F8 | Signed in as a non-administrator, open the plugin's report address (given in the release notes) in a browser: it says "Administrators only" and shows nothing else. Saving settings, searching pins and Run now from that account are refused the same way. |  |  |
| C27 | F9 | Inside the copy folder of a title that is about to drop out, make a junction pointing at a test folder of your own with a file in it. After the run, the copy folder is gone, and your test folder and its file are untouched. | A | R |
| C28 | F3 | A title whose name is only characters Windows doesn't allow (or is called "CON") gets a folder named by its Emby ID and plays normally. A title whose name matches a folder you made yourself in top-picks gets " - 2" and your folder is untouched. | A | L, R |
| C29 | F2 | Unplug or disconnect the drive of a chosen library and run the task: it is refused, the report names the folder, and the row is unchanged. | A | R |
| C30 | F9 | Stop Emby, damage managed.json (type a few letters at the start), start Emby and run: the run is refused, nothing in top-picks changes, and the report gives the fix (empty top-picks, delete managed.json). Do the fix: the next run finishes and the row is 1–10. Damage state.json instead: the run carries on and the report says run state was reset. | A | S, R |
| C31 | F1 | On first install (no run yet), restart Emby: about 10 minutes later a run starts by itself. | A | R |
| C32 | F5 | Start a full library scan while a run is in its rescan step: the run still finishes and the row is 1–10. | A | R |
| C33 | F6 | Another user watches an episode of a series on the run's own day (Sunday morning, before 05:00): it counts in next week's run, not this one. | A | L |
| C34 | F4 | (First build chunk) Set a copy's date added with the plugin, then run a full library scan and restart Emby: the date is unchanged. |  |  |
| C35 | F3 | Open a copy's .nfo files: title, year, age rating, genres, plot and date added are there; a title with `&` or `<` in it shows correctly in Emby; the .strm holds only the real file's path; subtitles sit next to the .strm with their language part. | A | L, R |
| C36 | F3 | A title or episode name long enough to break the 240-character limit is shortened (or that episode left out and reported), and the run still finishes. | T | L |
| C37 | F9 | After 9 runs, `backups\` holds exactly 8 dated folders, each with a `complete` file, and none of the plays.json copies contains user IDs. | A | S |
| C38 | F4 | Make a step fail on purpose (for example lock the Top Picks folder with a file open in another program) and run: the status is "failed", the report names the step, and the next run finishes. | T | R |
| C39 | F4 | A pick that a non-admin check user isn't allowed to see (parental controls): the row check leaves it out and says so, and still passes. If copies hadn't all appeared in time, the check says "partial". | T | R |
| C40 | F1 | With the last successful run 8 days ago, restart Emby: about 10 minutes later a run starts by itself. With it 6 days ago, nothing starts. | T | R |
| C41 | F5 | Change a copy's date added by hand in the dashboard, then restart Emby: within 3 minutes it is back to its rank date. | A | R |
| C42 | F2 | Two titles with the same viewer-days are ordered by newest content, then premiere date, then title, then ID, the same way every run. | T | L |
| C43 | F2/F8 | Pinning the same title twice keeps one pin. With 12 pins of which 1 is missing, the row is the first 10 usable pins in order. | A | L |
| C44 | F2 | A file under the Top Picks folder is never a candidate, even if the Top Picks library is chosen by mistake. | T | L |
| C45 | F2/F4 | First install (D34): with the old plugin uninstalled and top-picks emptied by hand, restart Emby. About 10 minutes later a run starts by itself and the row shows your pins, then the newest titles, each marked "filler" in the report. | A | R |
| C46 | F6 | A title with no known runtime counts after 20 minutes in a day, not after 19. | T | L |
| C47 | F6 | A film stopped at 00:10 counts for the day it was stopped. On the autumn and spring clock-change days, days and the 7-day window still come out right. | T | L |
| C48 | F6 | After a day of watching, the plugin's own lines in Emby's log (those starting with its name) mention no plays at the normal level, and its Debug lines name no user. |  |  |
| C49 | F7 | The weekly reset of copies to unwatched, and clearing played state on a copy by hand, never set the real title to unwatched and never clear or lower its resume point (D38). | T | R |
| C50 | F9 | Remove the Top Picks library, or add a second folder to it, and run: the run is refused and says why. | A | L |
| C51 | F9 | Damage run.json: the next run carries on and the damaged file is renamed aside (3 newest kept). Damage plays.json: it is deleted straight away, the next report says the popularity history was lost, and no user IDs remain anywhere in the plugin data folder. | T | S |
| C52 | F9 | The write and delete checks refuse a managed name with `..`, `\` or a drive letter in it, and a file path that leaves its folder. | T | L, R |
| C53 | F6 | Between midnight and 00:05, and with nobody watching for a week, no user IDs are left in plays.json from before today after 00:05. | T | S |
| C54 | F8 | A pin search result or title containing `<b>` shows the text `<b>` on the settings page, not bold. Saving an unknown ID, the Top Picks library or 21 pins is refused with a message. | A | L |
| C55 | F1/F5 | Make a run fail (as in C38): Run now works straight away; a failed run never blocks later runs. Restart Emby during the same run three times: it ends "failed" with "gave up after 3 restarts", and Run now then works. | T | R |
| C56 | F1/F5 | Restart Emby during a run and press Run now before the run carries on (within 3 minutes): it is turned down with "a run is already in progress", and the run then resumes by itself. | T | R |
| C57 | F8 | After a refused run (for example with a drive unplugged, C29), the report shows the refusal and, below it, the report of the run before. | A | R |
| C58 | F4 | A series in the row loses an episode from its real library: after the next run its old episode files are gone from its copy folder. A title whose artwork fell back to folder.jpg one week and was drawn the next has only folder.png and thumb.png. | T | R |
| C59 | F9 | The plugin refuses to change the date, played state or artwork of any item whose path is outside top-picks (only F7 touches real titles, and only forward). | T | L |
| C60 | F2 | Delete a library you had chosen: the next run ignores it and the report says so. With every chosen library deleted, the run is refused. | A | L, R |
| C61 | F6 | A play on a copy is judged against the real title's runtime, not the copy's. | T | L, R |
| C62 | F9 | A managed.json with a value outside its rules (for example rank 11, or a folder name with `..`) counts as damaged and the run is refused. | T | S |

## How the automatic tests work (D18, D31)
- **Test layer** says where the automatic test lives: **L** = Logic (plain records in, answer out, xUnit, with Stryker mutation testing in CI; the build fails below 80% of mutants caught); **S** = Store, with real files in a temporary folder on Windows; **R** = Run, with real files in a temporary folder and fake EmbyBridge adapters (in-memory lists of items, users, libraries, dates and played state) and a fake clock and time zone; **Art** = Artwork, drawing real PNGs with SkiaSharp at the version Emby loads (read from the server first, D15) and checking pixels at fixed points.
- An A check's automatic test proves the rule; the owner's check proves Emby really behaves that way. A check with no Auto mark is the owner's only, because it needs the real server (EmbyBridge talks to Emby; its tests only check the plain-record conversions).
- Junctions in tests are made with `cmd /c mklink /J` on the Windows build machine. Run's lock (C22, C56) is tested by starting 10 runs at once through the start-run hook and checking exactly one goes ahead.
- Every test that covers a check is tagged with its check ID (`[Trait("Check", "C10")]`). A meta-test fails the build if any check marked A or T has no test tagged with its ID, once its feature is built (the meta-test reads this table and `tests/TopPicksPlus.Tests/built-features.txt`, one feature ID per line, added to in the same pull request that builds the feature, so it can't pass by skipping a check), so results are reported per check and none is forgotten.
