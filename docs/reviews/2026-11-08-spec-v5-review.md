---
summary: Fresh-reviewer re-review of spec v5 (comprehension, builder readiness, security). Not passed: 6 Blockers (one found by two reviewers). For spec v6.
links: [../spec.md, ../data.md, ../checks.md, ../modules.md, ../security.md, 2026-11-08-spec-v4-handling.md]
---

# Spec v5 re-review, 2026-11-08

Reviewers: **Comp** (4 Blocker, 14 Should, 11 Nit), **Build** (1 Blocker, 10 Should, 7 Nit), **Sec** (2 Blocker, 4 Should, 5 Nit). Run at the same time on commit 01bb2dd. Build passed on GitHub (Windows) with the tightened boundary checks. Stage 4 **not passed**; pull request 1 stays unmerged.

The D34 cut worked: no finding concerns takeover or import, and the Blockers are fewer and narrower. But two are new kinds of risk (Sec 1, Sec 2).

## Blockers
1. **Row-check verdict contradicts C39 (Comp 1, Build 1).** A pick hidden by parental controls is neither "passed" nor "partial", so it falls to "failed"; C39 says it passes. Fix: parental controls → still passed; no copy / not appeared → partial.
2. **Resume at `swap` lacks the data to write managed entries (Comp 2).** run.json operations hold only kind/folder/sourceId; an add or update needs rank, rank date and the file list with sourceItemIds, which staging's .strm files don't hold. Fix: store the full planned entry in each operation.
3. **Resume "from step 1" may rewrite run.json (Comp 3).** That would reset `resumeCount` (give-up never triggers) and lose `historyLost`/`stateReset`. Fix: resume keeps run.json and never re-copies the pending flags.
4. **Forward-only resume point not fully defined (Comp 4, Sec 8).** Multi-episode .strm; source already higher; source already played. Fix: set only if higher than the source's, only on the first episode, nothing if the source is played.
5. **Emby may share watched state between a copy and its source by provider ID (Sec 1).** Copies' .nfo carry `uniqueid` (IMDb/TMDb/TVDb). Emby community reports say user data is keyed by provider ID, so items sharing one share played state. If true on 4.10.2, the weekly reset (F4 step 9) and F7's reset would unwatch the real title every week. Fake-adapter tests (C49, C59) can't catch this. **Needs a proof test on the real server before the design can be fixed** (owner).
6. **A restored plays.json backup deletes itself (Sec 2).** Backups leave out `today`, but data.md says every field is required, so the restored file is "damaged" and deleted. No check tries a restore. Fix: back up with `"today": []`; add a restore check.

## Should fix (summarised)
- Statuses: `failed` vs `refused` for a guard failing at `prepare` (Comp 5); which skips make `incomplete` (Comp 6); C38 expects `failed` but step 7 says `incomplete` (Comp 16).
- Swap: a managed folder missing from disk in a normal run (Comp 7); a resumed add left `incoming` (Comp 8).
- Walls: no part may check the plugin data folder is not a link, or resolve other libraries' folders (Comp 9, Build 9); "video file can be found" has no adapter (Comp 10, Build 8); no API action for the live missing-pin mark (Comp 11).
- `historyLost` when damage is found during this run (Comp 12); damaged managed.json for F6/F7/order guard (Comp 14); a plays.json read failure (I/O, locked by antivirus) must not count as damage and wipe history (Sec 3).
- F7 can trigger itself: reset only if not already unplayed with no resume point (Comp 13, Build 6). F7 events lost (before wiring, restart, caught error) lose the watch for good: push copy state forward before the weekly reset (Sec 6).
- Order guard doesn't take the lock and can overlap a run start (Comp 15); guard ignores "date last media added" (Build 10).
- run.previous.json: two failed runs in a row hide the last good report (Comp 17). Release exit checks leave out C26, C29–C31, C34, C45, C48 (Comp 18).
- Tests: C59's real guard is in EmbyBridge, never exercised (Build 2); C54's page half can't be tested at L (Build 3); the meta-test and built-features.txt don't exist yet and the naming conventions disagree (Build 4); rules with no check (Build 5: F9 link/overlap guards, missing managed.json, incoming tidy, failed backup, multi-episode/Specials names, line break, renamed stayer, admin fallback, errors outside a run); SkiaSharp in the test project for the Art layer and boundary reflection (Build 11).
- Logging: exception text on user paths may carry a user ID; log type and step only (Sec 4). C26 in a plain browser tab proves nothing; use a real non-admin token and add an auth-gate test (Sec 5).

## Nits
Comp 19–29, Build 12–18, Sec 7, 9, 10, 11: step values named in F4; missing-file defaults; prune state.json.damaged files; staging layout; step 1 field list; "run start" wording; owner of the playback-started list; run status while waiting; tests README out of date; CLAUDE.md counts; releases.md step 3 wording; NetArchTest prefix matching also bans FileNotFoundException etc.; modules.md omits Logic's Task ban; C10/C24/C53 layers; release notes text; start-up temp-file cleanup hook; smaller unchecked rules; timing wording in security.md; F7 bullet wording; pre-commit command only in the handover; settings that bypass the save checks; admin check before "starting up".

## Lesson
Card 0005 / R1 kept the new rules consistent with the step list and walls far better than v4 (no wall-crossing Blockers from v5's own additions apart from Comp 9). The two new Blockers of note come from outside the docs: Emby's real behaviour (Sec 1) and a field rule meeting a restore path (Sec 2). Possible card: a fake adapter can't prove a rule that depends on how Emby stores data; such rules need an owner proof test before building.
