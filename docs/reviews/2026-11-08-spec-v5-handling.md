---
summary: How each spec v5 re-review finding was handled in spec v6 (Removed by D39 / Fix / Contest / Ask the owner / Skip).
links: [2026-11-08-spec-v5-review.md, ../spec.md, ../data.md, ../checks.md, ../modules.md, ../decisions.md, ../security.md]
---

# Handling of the spec v5 re-review (spec v6, 2026-11-08)

Nothing was contested. One item went to the owner (Blocker 5: the proof test, then D39). The rest were technical and decided by Claude (D40–D42) or are plain fixes. R1 was applied: every new or changed rule was checked against the F4 step list, run.json `step`/`status`, data.md and the part walls. No new "may use" link between parts was needed; new front-door actions sit inside existing walls (modules.md).

## Removed or settled by D39 (owner: copies share watched state, plugin never changes it)
- **Blocker 4** (forward-only resume point) and **Blocker 5** (shared watched state via provider IDs): F7's sync and F4 step 9's played-state reset are gone. F7 now says Emby shares state through `uniqueid`; the plugin has no action that changes played state (EmbyBridge README, F9). D42 adds `uniqueid` to episode .nfo files so episodes share too; C14 proves it on the server. C14, C15, C49, C59, P2 rewritten.
- Should-fix that only concerned F7: Comp 13 / Build 6 (F7 triggering itself), Sec 6 (F7 events lost). Nit: F7 bullet wording.

## Blockers
1. **Row-check verdict vs C39 (Comp 1, Build 1) — Fix.** F4 step 11: passed = exact match and only `parental controls` left out; partial = exact match with `no copy` / `not appeared` left out; failed = anything else. C39 now covers all three and the admin fallback. With D39, the request must not leave out watched items (C34 confirms).
2. **Resume at `swap` lacks data (Comp 2) — Fix.** Each `update`/`add` operation holds the full planned managed entry (`entry`, data.md). Swap and resumed swap use it; update keeps found item IDs. C65.
3. **Resume rewrote run.json (Comp 3) — Fix.** Resume never creates run.json again: every field kept, run.previous.json untouched; pending flags only ever add (spec "Pending flags"). C71.
6. **Backup restore deleted itself (Sec 2) — Fix.** Backups hold plays.json with `"today": []`; restore check C63 (automatic, and the owner tries one before release).

## Should fix
- Statuses (Comp 5, 6, 16) — **Fix.** `refused` now covers guards failing on resume up to `prepare`; `incomplete` defined exactly (spec "Words used here"); C38 no longer uses a swap skip. C72.
- Swap (Comp 7, 8) — **Fix.** Plan tidy removes entries whose folder is gone; a resumed add already moved is marked `active`. C65.
- Walls (Comp 9, Build 9; Comp 10, Build 8; Comp 11) — **Fix.** F9 "Who does what" assigns link and resolved-path checks to Run, Store and EmbyBridge's source-files adapter, comparisons to Logic; the adapter gets "file exists" and "resolved path"; the API gets "check pins".
- `historyLost` found during a run (Comp 12) — **Fix.** Pending flags carried at steps 1 and 12. Damaged managed.json for plays and the order guard (Comp 14) — **Fix**, F5, F6. Read failure isn't damage (Sec 3) — **Fix**, F9, data.md. C74.
- Order guard and the lock (Comp 15) — **Fix, D40.** C73. "Date last media added" in the guard (Build 10) — **Fix**, F5.
- run.previous.json (Comp 17) — **Fix, D41.** C57. Release exit checks (Comp 18) — **Fix**, releases.md.
- Tests: C59 guard in EmbyBridge (Build 2) — **Fix**, the rule is in Logic (L), Stage 7 review checks every call uses it. C54 page half (Build 3) — **Fix**, save rules at L, display owner-only. Meta-test and naming (Build 4) — **Fix**, checks.md and tests README: the Trait tag counts; both files come with the first build chunk. Rules without checks (Build 5) — **Fix**, C50 extended, C51 extended, C63–C75. SkiaSharp in the test project (Build 11) — **Fix**, checks.md, tests README.
- Logging (Sec 4) — **Fix**, F4 "What an error log line holds", C69. C26 (Sec 5) — **Fix**, uses a real non-admin sign-in or token; auth rule test C70.

## Nits
- Fixed: step values named in F4; missing-file defaults (data.md); state.json.damaged pruning (3 kept); staging layout; owner of the playback-started list (Run); run status while waiting (F8); tests README; CLAUDE.md counts; releases.md step 3 wording; NetArchTest prefix (Artwork catches `Exception`, ban kept, modules.md and Artwork README); Logic's Task ban in modules.md; C10/C24/C53 layers; start-up temp-file cleanup (root asks Store); pre-commit command moved into security.md; settings that bypass the save checks (F2 step 1, C75); admin check before "starting up" (F8).
- Skipped: release notes text (written at Stage 8); timing wording in security.md (no rule changes); smaller unchecked rules not listed by name in the review summary (the re-review is asked to name any rule still without a check).
