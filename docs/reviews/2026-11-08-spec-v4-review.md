---
summary: Fresh-reviewer re-review of spec v4 (comprehension, builder readiness, security). Not passed: 7 Blockers (one found by two reviewers). For spec v5.
links: [../spec.md, ../data.md, ../checks.md, ../modules.md, ../security.md, 2026-11-08-spec-v3-review.md, 2026-11-08-spec-v3-handling.md]
---

# Spec v4 re-review, 2026-11-08

Reviewers: **Comp** (4 Blocker, 15 Should, 13 Nit), **Build** (1 Blocker, 17 Should, 8 Nit), **Sec** (2 Blocker, 12 Should, 7 Nit). Run at the same time on commit 84d57f2 (lock files were committed afterwards, dd8e2df, which settles Build 5 and Sec 13 in part). Stage 4 **not passed**; pull request 1 stays unmerged.

## Blockers
1. **"Waiting to resume" undefined; resume refuses itself (Comp 1).** Which run.json fields mean waiting; a failed run must not block every later run.
2. **No part may open the Playback Reporting database (Comp 2, Build 1).** SQLite in nobody's "may use"; table layout and timestamp zone unstated.
3. **Takeover zip needs matching that comes later, from a part Store can't use (Comp 3).** Exclusion list, order, zip check.
4. **Updating an adopted folder undefined (Comp 4).** Old files left beside new ones; adopted entry fields; long old names.
5. **Damaged plays.json can keep user IDs ~48 h, or forever under the "3 newest" rule (Sec 1).** D33, F9 and security.md disagree.
6. **The weekly unplayed reset could flow back and wipe real watch history (Sec 2).** F7 must only ever send played / resume > 0 to the source.

## Should fix (each item summarised from the reviewers' reports)
- Lock and triggers: API must reserve the lock before starting the task; how `trigger` is known; whether resume/missed run show in Scheduled Tasks; C22 through the API path (Build 11, 12; Comp 6).
- `refused` vs `failed` contradictions; refused run overwriting the last report (Comp 7). Step 14 write order (Comp 20).
- Order guard and null item IDs; guard after failed runs; what "finishes the job" includes (Comp 8). managed.json writers (Comp 9).
- Restore leaves orphan copies; backup timing; first-run backup with missing files (Comp 10, 11; Sec 6). Never overwrite the takeover zip; restored state re-running takeover (Sec 5).
- Play events: "finished" vs "by hand" vs Emby's auto-played; how own changes are ignored; daily add-or-replace and import double counting; import per-episode rule and timestamps (Comp 5, 12, 19; Build 14 runtime from .nfo).
- Names: clash with a managed folder being deleted; swap order; copy vs move from staging; stale `folder.*` after a successful draw; which Emby ID (GUID vs InternalId), `S00E<id>` (Comp 13, 14, 18, 26; Build 13).
- Row check expected list for picks with no copy (Comp 15). Report fields for import, lost history, takeover (Comp 16). Settings save path and full hook list (Comp 17).
- Safety: Top Picks / data folder themselves not links, resolved-path comparisons (Sec 3); every written Emby item must be under the Top Picks folder (Sec 4); stray `.tmp` cleanup (Sec 7); catch-all at every Emby/timer entry (Sec 8); XML-invalid characters (Sec 9); field-level validation of JSON (Sec 10); Run re-checks settings (Sec 11, Build 15).
- Tests: which A/T checks are really automatic and how (Build 2, Sec 12); meta-test that every A/T check has a test (Build 3); Stryker break threshold (Build 4); fail when lock files missing (Build 5, Sec 13); ban `Emby.*` namespaces (Build 6); types outside `TopPicksPlus.*` (Build 7); polyfills on netstandard2.0 (Build 8); test-time SkiaSharp/SQLite versions (Build 9); NoInlining for the SkiaSharp fallback (Build 10); Run must not use concrete EmbyBridge classes (Build 16); root namespace bans, Process, wider file bans (Sec 14); 13 features without checks (Build 17); C48 reworded to plugin lines only (Build 18).

## Nits
- Build 19 IStore zip needs an exclusion input. Build 20 state the Top Picks library's content type (refuse "movies"). Build 21 series "date last media added" may also drive Latest; confirm in C34. Build 22 backup names collide in the autumn repeated hour: use UTC or runId. Build 23 count the temp-file suffix in the 240 limit. Build 24 fixtures for root-type, SkiaSharp and Logic file/Task bans. Build 25 concurrency group: add event name, don't cancel main. Build 26 junctions in tests need `mklink /J` or P/Invoke.
- Sec 15 zip checked by name and size only; must not follow junctions. Sec 16 dedupe by-hand marks; cap the in-memory start map. Sec 17 scrub user IDs from logged exceptions. Sec 18 machine backups/shadow copies capture `today` (tell the owner). Sec 19 floating toolchain (8.0.x, windows-latest, gitleaks binary, Stryker); audit after tests; audit checks compile-only versions. Sec 20 pre-commit check not a script; main unprotected while D27 lets Claude merge; .sha256 from the same pipeline. Sec 21 owner-made folders with .strm + .nfo get adopted and deleted: say so in install notes.
- Comp 20 step 14 write order. Comp 21 two adopted folders for one source. Comp 22 date bases for zip, backup and damaged-file names. Comp 23 two different "managed-looking folder" tests. Comp 24 Store reading TopPicksPlus.xml vs Settings README; EmbyBridge reading programdata images and the DB. Comp 25 "source title" tied to N:\ and K:\. Comp 26 200-episode threshold basis; multi-episode item count; `S00E<id>`. Comp 27 same film in two libraries. Comp 28 chosen library deleted or changed type. Comp 29 Run "interfaces only" unenforced; Artwork file ban misses FileStream/FileInfo. Comp 30 which skips make a run `incomplete`. Comp 31 D29 names vs data.md; `pinTitles` has no decision. Comp 32 `today.itemId` single ID vs multi-episode files.

## Lesson
Same as card 0005, second time: v4 fixed every v3 item but its new rules (resume lock, zip exclusions, damaged-plays deletion) clashed with other parts again. Card 0005 `seen` raised to 2: a promotion candidate.
