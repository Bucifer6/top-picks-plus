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
| C6 | F2 | (Automatic too) With no pins, a run fills all 10 places by itself from what was watched. Nothing is done by hand. |
| C7 | F2 | (Automatic too) Pin 3 titles in a chosen order. After a run they are 1, 2, 3 in that order, and 4–10 are the most watched. |
| C8 | F2 | (Automatic too) Pin 10 titles. The row is exactly those 10, in your order. |
| C9 | F2 | Pin a title, then remove it from its real library. The next run skips it, the others move up, and the settings page shows the pin as "missing". |
| C10 | F2/F6 | One user watches 6 episodes of a series in one evening; another user watches one film on 3 different days. The report shows the series with 1 viewer-day and the film with 3. |
| C11 | F6 | Watch a title from the Top Picks row past halfway. The report counts it for the real title. |
| C12 | F6 | Watch a film for 30%, stop, come back the same day and watch another 30%. It counts. Watching for 5 minutes doesn't. |
| C13 | F2 | (Automatic too) A week with only 4 watched titles: places 5–10 are the newest additions, and the run finishes normally. |
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
| C26 | F1 | (Automatic, D54) With the last successful run more than 7 days ago, start-up starts a run 10 minutes later; with a resumed run, it doesn't. |
| C27 | F6 | Go-live day, Run now straight after install: the report says the first week came from Playback Reporting, and titles watched last week (including from the old Top Picks row) are in the list. If Playback Reporting can't be read, it says "last played" was used instead (D63, D64). |
| C28 | F9 | (Automatic, D54) With no library using the Top Picks folder, a run refuses with "library missing" and deletes nothing. |
| C29 | F4 | (Automatic, D54) A run whose rescan wait runs out is marked "incomplete", still counts as successful, and the order guard later sets the dates of copies that appear. |
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
| C48 | F9 | (Automatic) Takeover run a second time (e.g. TakeoverDone lost) adopts nothing already on the managed list; saving the settings page doesn't change TakeoverDone. |
| C49 | F9 | (Automatic) A staging folder that is a junction is not emptied; a junction inside a dropped copy is removed as a link and its target is untouched. |
| C50 | F9 | (Automatic) An old-plugin folder that is still a pick: the new copy takes the plain name; if the old folder can't be deleted, the new copy gets " - 2" and nothing is merged. |
| C51 | F3 | (Automatic) Folder and file names: forbidden characters and trailing dots or spaces removed; no year gives `Title`; clashes (ignoring case) get ` - 2`, ` - 3`; reserved names get `_`; specials go in `Specials` as `S00Eyy`; a double episode is `SxxEyy-Ezz`; an episode with no number is left out and counted; no path over 240 characters (D49). |
| C52 | F3 | (Automatic) Each .nfo holds the fields in D30, including the age rating, with `&` and `<` in a title written safely. |
| C53 | F4 | (Automatic) Rank dates: pick n is the rank time minus (n−1) hours, the rank time cut to the whole minute, and a resume keeps them. |
| C54 | F6 | (Automatic) Minutes watched never go below 0 or above the runtime; a play whose start wasn't seen counts 0 minutes; with no runtime, 20 minutes counts; "marked played" counts on its own (D29). |
| C55 | F1 | (Automatic) Right after first install, with no last run, start-up doesn't start a run (D41). |
| C56 | F5 | (Automatic) The order guard skips unranked copies, and resets a ranked series' episodes as well as the series (D52). |
| C57 | F4 | (Automatic) With no unrestricted user, the row check uses the first administrator and the report says so (D20). |
| C58 | F6 | (Automatic) Plays already in the log are not imported again from Playback Reporting or the "last played" fallback; each fallback title counts as one viewer-day (D46, D47). |
| C59 | F1/F5 | (Automatic) An error mid-run is retried 5 minutes later, at most 3 times, then the run is failed; the order guard runs while no run is working, even if run.json says "running" (D44). |
| C60 | F4 | (Automatic) A row check that fails is retried once after the dates are set again; if it still fails, the run is finished and the report shows the failed check (D45). |
| C61 | F9 | Turn off the .nfo reader on the Top Picks library and Run now: it refuses and the report says to turn it back on. Turn it on again: the next run works (D53). |
| C62 | F4 | (Chunk 4, before go-live, D50, D67) In a throwaway test library, one .nfo dated in summer time and one in winter time: after a metadata refresh, Emby shows the same date added. Plus (Automatic) a run on a clock-change Sunday writes each pick's local time with its own offset. |
| C63 | F9 | (Automatic) With the old plugin loaded , no plays are logged and no watched state is copied (D51). |
| C64 | Boundary | (Automatic) Logic may use names that only start like a forbidden one (e.g. `System.IObservable<T>`): the boundary check doesn't fail on them. |
| C65 | F3 | Before the first artwork chunk is kept, look at three sample pictures (with a logo, without a logo, rank 10): they look like today's row in docs/reference/artwork/ (D62). |
| C66 | F10 | Go-live: Top Picks Plus shows in Dashboard → Plugins on Emby 4.10.2 with its version (D14). |
| C67 | F8 | (Automatic) A non-administrator can't open the settings page or use Run now or the report. |
| C68 | F8 | (Automatic) The settings page refuses an 11th pin, a duplicate pin, and an episode or collection (D61, D74). |
| C69 | F2 | (Automatic) Ties go to the newest content, then the newer premiere date; a pin that is also most watched appears once; a movie with no file is skipped; fewer than 10 candidates gives a shorter row. |
| C70 | F3 | (Automatic) Subtitles keep their full suffix (`.en.forced.srt`), `.idx` goes with `.sub`, theme songs are copied the first week only, episode thumbs and season posters are copied (D55, D70). |
| C71 | F6 | (Automatic) A play on an old-plugin copy not yet on the managed list counts for the real title; imported rows for items that no longer exist are ignored and counted (D64). |
| C72 | F9 | (Automatic) A data file with a newer version makes runs refuse with "data newer" and nothing is renamed or deleted (D72). |
| C73 | F1 | (Automatic) A Sunday trigger while an unfinished run waits resumes it with the same picks; a refusal found mid-run fails the run straight away with no retry (D68). |
| C74 | F9 | (Automatic) The old-plugin guard never matches Top Picks Plus's own ID or assembly (D33). |
| C75 | F6 | (Automatic) Only movie and episode plays are logged, dated by the UK day of the stop (D70). |

## Which chunk proves which check (D69)
- **Chunk 1 – Logic:** C6, C7, C8, C10, C12, C13, C24, C33, C34, C35, C51, C52, C53, C54, C55, C56, C59, C60, C64, C69, C73, C75, and the Logic halves of C26, C28, C29, C58, C61, C62.
- **Chunk 2 – Storage:** C36, C37, C40, C45, C46, C47, C48, C49, C50, C70, C72.
- **Chunk 3 – Artwork:** C43, C65.
- **Chunk 4 – EmbyLink:** C57, C58, C62 (test library), C63, C71, C74, and the wiring for everything below.
- **Chunk 5 – Settings and report:** C67, C68.
- **Go-live day, on the server (D63):** C1–C5, C9, C11, C14–C23, C25, C27, C30–C32, C38, C39, C41, C42, C44, C61, C66. C9 means pinning a title and removing it from its real library, and C5 means adding an episode to a series that is a pick, so they use the real libraries on purpose.
