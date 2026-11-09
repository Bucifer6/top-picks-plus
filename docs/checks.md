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
| C21 | F5 | Restart Emby in the middle of a run. Within 30 minutes of it coming back (45 if a pick is a series with over 200 episodes; the resume starts at 2 minutes, D90, and the rescan wait alone can take 15 or 30), the row is complete and in order, and the report says the run finished. |
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
| C43 | F8 | (Automatic) When the artwork drawing fails, Artwork returns "failed" instead of throwing (chunk 3); the copy then gets the plain backdrop and `artworkFailed` is set for that pick (Logic plan and Storage, chunks 1–2); the report shows it (chunk 5, seen on go-live day). |
| C44 | F1/F9 | [Amended by D94.] With the old plugin installed, restart Emby: nothing runs (no resume, no order guard, no missed run), nothing is written, and the report says to uninstall it. (An unfinished run would wait unchanged: C96.) |
| C45 | F9 | (Automatic) A managed entry whose folder is empty, ".", "..", a path, or a junction pointing elsewhere is never deleted; the report names it. |
| C46 | F9 | (Automatic) An old-plugin series folder (tvshow.nfo at the top, .strm files only in Season folders) is adopted at takeover. |
| C47 | F4 | (Automatic) A run after a failed run finishes the old run's "leaving" deletes and drops its stray "incoming" copies; nothing is orphaned. |
| C48 | F9 | [Amended by D93.] (Automatic, Storage) Takeover run a second time (e.g. `takeoverDone` lost from state.json) adopts nothing already on the managed list; takeover saves managed.json before state.json, so a stop between the two leaves `takeoverDone` false and a repeat adopts nothing twice. state.json is its own file: saving it changes no other file, and it holds only the plugin's flags (`version`, `takeoverDone`, `playbackReportingImported`, `listeningSinceUtc`), none of the owner's settings. That the settings page can't change the flags (PluginConfiguration has no flag fields; Settings never writes state.json) is proved by code review. |
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
| C62 | F4 | [Amended by D76.] (Proof plugin part (c), before chunk 4 is kept, D50, D67, D76) In a throwaway test library, the proof plugin writes one .nfo dated in summer time and one in winter time: after a metadata refresh, Emby shows the same date added. Plus (Automatic, chunk 1) a run on a clock-change Sunday writes each pick's local time with its own offset. |
| C63 | F9 | (Automatic) With the old plugin loaded , no plays are logged and no watched state is copied (D51). |
| C64 | Boundary | (Automatic) Logic may use names that only start like a forbidden one (e.g. `System.IObservable<T>`): the boundary check doesn't fail on them. |
| C65 | F3 | Before the first artwork chunk is kept, look at three sample pictures (with a logo, without a logo, rank 10): they look like today's row in docs/reference/artwork/ (D62). |
| C66 | F10 | Go-live: Top Picks Plus shows in Dashboard → Plugins on Emby 4.10.2 with its version (D14). |
| C67 | F8 | [Amended by D79.] (Go-live, plus code review) Signed in as a non-administrator, you can't open the settings page or use Run now or the report. |
| C68 | F8 | (Automatic) The settings page refuses an 11th pin, a duplicate pin, and an episode or collection (D61, D74). |
| C69 | F2 | (Automatic) Ties go to the newest content, then the newer premiere date; a pin that is also most watched appears once; a movie with no file is skipped; fewer than 10 candidates gives a shorter row. |
| C70 | F3 | (Automatic) Subtitles keep their full suffix (`.en.forced.srt`), `.idx` goes with `.sub`, theme songs are copied the first week only, episode thumbs and season posters are copied (D55, D70). |
| C71 | F6 | (Automatic) A play on an old-plugin copy not yet on the managed list counts for the real title; imported rows for items that no longer exist are ignored and counted (D64). |
| C72 | F9 | [Amended by D93, D94.] (Automatic) Any of the four data files (managed.json, plays.json, run.json, state.json) with a newer version makes runs refuse with "data newer"; nothing is renamed, deleted or written (not even `lastRefusal`), and a waiting run in run.json is left exactly as it was (D72, D85, D93, D94). |
| C73 | F1 | [Amended by D94.] (Automatic) A Sunday trigger while an unfinished run waits resumes it with the same picks; if a guard refuses at that resume, the run is not failed and keeps waiting, unchanged (D68, D94; details C95). |
| C74 | F9 | (Automatic) The old-plugin guard never matches Top Picks Plus's own ID or assembly (D33). |
| C75 | F6 | (Automatic) Only movie and episode plays are logged, dated by the UK day of the stop (D70). |
| C76 | F10 | (Proof plugin, before chunk 4 is kept, D76, D77) Install Top Picks Proof next to the old plugin and restart: it shows in Dashboard → Plugins on 4.10.2. Its page says whether Playback Reporting's DateCreated is UTC or local for a play you name, and what the IDs look like; for the test library it shows the two dates Emby read back. You copy the page into the chat, then uninstall it, delete the test library and the `top-picks-proof` folder. The Top Picks row is unchanged throughout. |
| C77 | F1/F5 | [Amended by D94.] (Automatic) An error, then its resume, adds exactly 1 to `resumeCount`; after the first attempt and 3 resumes all stop short, the run is failed; a restart counts as a resume the same way; a resume refused by a guard adds nothing (D78, D94). |
| C78 | F6/F9 | [Amended by D93.] (Automatic, Storage) A run stopped during import, takeover or leftovers and resumed: nothing is imported twice, no folder is adopted twice, and `playbackReportingImported` and `takeoverDone` end true in state.json. In particular, stopped after plays.json was saved (rows plus an import marker for this run) but before state.json: the resume finds the marker, imports nothing, and only sets `playbackReportingImported` (D80, D93). |
| C79 | F8 | (Automatic) run.json saves and loads every report field in D81 (episodes skipped, imported rows ignored, refused deletes, unmanaged folders, row-check fallback and extras, fail reason, refusal detail, notices). |
| C80 | F9 | (Automatic) A managed entry whose `items` or `files` hold an empty path, a full path, `..`, a forbidden character, or a path through a junction: that file is never deleted or written, and the report names it (D82). |
| C81 | F4 | (Automatic) An unmanaged folder or an unranked leftover in the row doesn't fail the row check; it is listed as an extra; a wrong order among the ranked copies still fails (D83). |
| C82 | F4 | (Automatic) A run whose rescan wait ran out ends "incomplete" even if its row check fails, with no row-check retry (D84). |
| C83 | F9 | [Amended by D93, D94.] (Automatic) With plays.json at a newer version, play logging, watched sync and the order guard stop and none of the four data files is written, `lastRefusal` included (D85, D93, D94). |
| C84 | F7 | Mark a copy watched by hand in the row: the real title shows watched for you. Mark it unwatched: the real title is unwatched again (D55). |
| C85 | F9 | Put a folder with only a .strm in top-picks (no .nfo, made after takeover). After Run now the report names it as an unmanaged folder, and the row check still passes (D73, D83). Then delete it by hand. |
| C86 | Boundary | (Automatic) A Logic type using `TopPicksPlus.PluginConfiguration` is caught as crossing a wall by its own entry, not by `TopPicksPlus.Plugin`; a nested type inside a forbidden type is caught (D89). |
| C87 | F9 | Turn on an .nfo metadata saver on the Top Picks library and Run now: it refuses and the report says which setting to turn off. Turn it off: the next run works (D53). |
| C88 | F2 | (Automatic) A series with some episode files missing is a candidate and its copy leaves those episodes out; a series pin with no episode file found is "missing"; a pin with one found is used (D61). |
| C89 | F3 | A movie with two versions becomes a pick: playing its copy plays the version Emby plays by default for the real movie. |
| C90 | F8 | The report shows the number of plays recorded in the last 7 days, and it goes up after you watch something past halfway and Run now again. |
| C91 | F6 | [Amended by D93.] (Automatic) Storage half: `listeningSinceUtc` is saved in state.json once and a later save leaves it unchanged; it isn't saved while state.json is damaged. Logic half: it isn't saved while the old plugin is loaded; the import takes plays up to `listeningSinceUtc`, including earlier the same day; next week's window counts those run-day plays once (D91, D93). |
| C92 | F1 | (Automatic) With run.json damaged and `takeoverDone` true (state.json), start-up starts the missed run 10 minutes later; with `TakeoverDone` false, it doesn't (D90). |
| C93 | F3 | (Automatic) .strm text is the path alone (UTF-8, no byte-order mark, no line ending); provider IDs are `<uniqueid type="imdb">`; a theme keeps its own extension; a subtitle removed from the source is removed from a staying copy (D90). |

| C94 | F9 | (Automatic) With state.json unreadable, every run refuses with "state damaged"; state.json is not renamed, recreated or rewritten, nothing is deleted, `listeningSinceUtc` isn't saved, and the refusal's detail says to restore state.json from a backup (deleting it would redo the takeover and the import). A missing state.json is not damaged: all three flags start unset (D93). |
| C95 | F1/F5 | (Automatic) An unfinished run waits at step "swap" with `resumeCount` 1. At the resume a guard refuses (e.g. library missing): run.json's `status`, `step`, `resumeCount` and picks are unchanged and only `lastRefusal` is written. Once the library is back, the next trigger (Sunday, Run now or the start-up resume) resumes from "swap" with the same picks and rank time, and `resumeCount` becomes 2. With "data newer" instead, nothing at all is written (D94). |
| C96 | F1/F9 | (Automatic) Emby starts with an unfinished run while the old plugin is loaded: nothing happens and nothing is written; the run waits. At the next start-up without the old plugin, the run resumes at 2 minutes and the wait has not changed `resumeCount` (only the resume itself adds 1) (D94). |
| C97 | F1/F5 | (Automatic) A problem first found in the middle of a step after the guards passed (e.g. the library vanishes during the rescan) is an error, not a refusal: a resume is planned 5 minutes later; if that resume's guards then refuse, the run waits unchanged (C95); the run ends `failed` with `failReason` = error plus the message only when its resumes run out, never with a refusal reason (D94). |
| C98 | F9 | (Automatic, Storage) state.json unreadable, plays.json with an import marker and managed.json with entries: the run renames it, rebuilds it with import and takeover done and listening-since the earliest play, adds a notice, and carries on. With managed.json also unreadable it refuses `stateDamaged` (D95). |
| C99 | F6 | (Automatic, Storage) plays.json holds an import marker from an earlier failed run and state.json says not imported: the new run imports nothing and sets the flag (D96). |
| C100 | F9 | (Automatic, Storage) managed.json deleted after takeover: the next run adopts the copy folders again and nothing in top-picks is left unmanaged (D95). |

## Which chunk proves which check (D69)
- **Chunk 1 – Logic:** C6, C7, C8, C10, C12, C13, C24, C33, C34, C35, C51, C52, C53, C54, C55, C56, C57, C58, C59, C60, C63, C64, C69, C71, C73, C74, C75, C77, C81, C82, C86, C88, the Logic half of C91, C92, C94 (the refusal), C95, C96, C97 (the run and resume rules, D94), the name rules of C80, the plan half of C43, the .strm/.nfo text of C93, and the Logic halves of C26, C28, C29, C61, C62 (D79).
- **Chunk 2 – Storage:** C36, C37, C40, C45, C46, C47, C48, C49, C50, C70, C72, C78, C79, C80, C83, the Storage half of C91, C94 (state.json left alone), C95 (only `lastRefusal` written; nothing for data newer), the file half of C43 and C93. C98, C99, C100.
- **Chunk 3 – Artwork:** C43 (drawing returns "failed"), C65 (samples kept as a CI artifact, D87).
- **Chunk 4 – EmbyLink:** starts with the proof plugin, whose results (C76, C62 test library) are in before chunk 4 is kept (D76); then only wiring, proved by code review and the go-live checks (D73, D79).
- **Chunk 5 – Settings and report:** C68; shows the D81 fields (seen on go-live day).
- **Go-live day, on the server (D63, D86):** C1–C5, C9, C11, C14–C23, C25, C27, C30–C32, C38, C39, C41, C42, C44, C61, C66, C67, C84, C85, C87, C89, C90. Order: C44 and C19 first, with both plugins installed and the old one's triggers removed; C18's "Keep me" folder is made before that (D86). C9 means pinning a title and removing it from its real library, and C5 means adding an episode to a series that is a pick, so they use the real libraries on purpose.
