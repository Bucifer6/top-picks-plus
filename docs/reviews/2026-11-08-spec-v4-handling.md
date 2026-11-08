---
summary: How each spec v4 re-review finding was handled in spec v5 (Removed by D34 / Fix / Contest / Ask the owner / Skip).
links: [2026-11-08-spec-v4-review.md, ../spec.md, ../data.md, ../checks.md, ../modules.md, ../decisions.md, ../security.md]
---

# Handling of the spec v4 re-review (spec v5, 2026-11-08)

Nothing was contested. One item went to the owner before this job (takeover and import → D34). The rest were technical and decided by Claude (D35–D38). Standing rule R1 (owner-approved) was applied to every new rule: each was checked against the F4 step list, run.json `step`/`status`, data.md and the part walls.

## Removed by D34 (owner: no takeover, no import)
- **Blocker 2** (no part may open the Playback Reporting database), **Blocker 3** (takeover zip ordering and walls), **Blocker 4** (updating an adopted folder): the features are gone. Spec F4 steps 4–5, F6 import, F9 takeover, data.md `takeoverDone`/`importTried`/`adopted`/`Unknown`, the zip, D24's SQLite, C18's second half and C45's import wording are removed; F4 renumbered (12 steps) and run.json `step` values updated.
- Should-fix and Nits that only concerned them: import double counting and per-episode/timestamp rules (Comp 5, 19 in part), never overwrite the takeover zip and restored state re-running takeover (Sec 5), test-time SQLite version (Build 9 in part), Build 19 zip exclusion input, Sec 15 zip checks, Sec 21 owner-made folders adopted, Comp 21 two adopted folders, Comp 23 two "managed-looking" tests (now one definition: "copy-looking folder"), Comp 24's database half.

## Blockers still open after D34
1. **"Waiting to resume" undefined (Comp 1) — Fix, D36.** Defined in spec "Words used here" and data.md `status`; only resume may start while waiting; `resumeCount` gives up after 3; any error ends the run `failed` and releases the lock. C55, C56.
5. **Damaged plays.json kept user IDs (Sec 1) — Fix, D37.** Deleted at once; `historyLostPending` in state.json carries the news to the next report; security.md and F9 agree. C51.
6. **Weekly unplayed reset could wipe real history (Sec 2) — Fix, D38.** F7 only ever sends played or a resume point above 0 to the source. C49, C14.

## Should fix
- Lock and triggers (Build 11, 12; Comp 6) — **Fix, D35.** Run now calls Run directly and takes the lock in the same call; `trigger` values `task` / `run now` / `missed run`; resume and missed run don't show in Scheduled Tasks; C1, C22 reworded.
- `refused` vs `failed`; refused run hiding the last report (Comp 7) — **Fix.** One definition list in spec "Words used here"; run.previous.json shown when the latest run was refused or failed (F8, C57).
- Step 14 write order (Comp 20) — **Fix.** Step 12: state.json first, then run.json; a stop between them repeats from step 8.
- Order guard null IDs, after failed runs, "finishes the job" (Comp 8) — **Fix.** F5: null IDs looked up by path, runs after every run, sets dates only; F4 says late copies need nothing else.
- managed.json writers (Comp 9) — **Fix.** Only Run, through IStore, under a lock (spec "How the parts connect", data.md, Store README).
- Restore orphans, backup timing, first-run backup with missing files (Comp 10, 11; Sec 6) — **Fix, D37.** managed.json and state.json no longer backed up; the fix for a damaged managed.json is the install steps; backups skip missing files and are marked `complete` last.
- Play events: finished vs by hand vs auto-played; own changes; daily add-or-replace (Comp 5, 12, 19; Build 14) — **Fix.** F6 defines finished and marked by hand; runtime always from the source; one atomic roll-up per day; F7 can't chase itself by construction.
- Names (Comp 13, 14, 18, 26; Build 13) — **Fix.** New names avoid every existing folder including ones being deleted; swap order deletes → updates → adds; move from staging; update makes the folder match exactly (removes stale `folder.*`); Emby ID defined (confirmed in the first chunk); 200-episode threshold counts .strm files.
- Row check for picks with no copy (Comp 15) — **Fix.** `expected` vs `leftOut` with reasons; passed/partial redefined.
- Report fields (Comp 16) — **Fix.** `historyLost`, `stateReset`, `ignoredLibraries`, `leftOut`.
- Settings save path, hook list (Comp 17) — **Fix.** F8 and the EmbyBridge README list the API, hooks and settings source.
- Safety: Sec 3 (links, resolved paths), Sec 4 (items under the Top Picks folder), Sec 7 (stray temp files), Sec 8 (catch-all), Sec 9 (XML-invalid characters), Sec 10 (field-level validation), Sec 11 / Build 15 (Run re-checks libraries and pins) — **Fix** in F2, F3, F4, F9, data.md. C59, C60, C62.
- Tests: Build 2 / Sec 12 (which checks are automatic and how) — **Fix**, checks.md "Test layer" column; C26 is now owner-only (its code needs the real server). Build 3 (meta-test) — **Fix**, checks.md; starts with the first built feature. Build 4 (Stryker break threshold) — **already in place** (stryker-config.json break 80), now stated in checks.md. Build 5 / Sec 13 (missing lock files) — **Fix**, build.yml fails. Build 6 (`Emby.*`), Build 7 (types outside `TopPicksPlus.*`), Sec 14 (Process, wider file bans) — **Fix**, BoundaryTests.cs, modules.md, READMEs. Build 8 (polyfills) and Build 10 (NoInlining) — **Fix**, Artwork README. Build 16 (Run uses interfaces only) — **Fix**, Run README and modules.md; checked at the Stage 7 review (the tool can't). Build 17 (features without checks) — **Fix** for every rule added in v5 (C55–C62); the re-review is asked to list any feature still without a check. Build 18 (C48) — **Fix.** Build 9's SkiaSharp half — **Fix**, checks.md (tests use the version read from the server).

## Nits
- Build 20 (Top Picks library content type) — **Skip:** the first build chunk sees the real library; nothing depends on it yet.
- Build 21 (series "date last media added") — **Fix**, F4 step 9 and C34.
- Build 22 (autumn-hour name clash) — **Fix**, UTC names with `Z`.
- Build 23 (temp suffix in the 240 limit) — **Fix**, F3.
- Build 24 (fixtures for each ban) — **Fix**, BoundaryFixtures.cs.
- Build 25 (concurrency group) — **Fix**, build.yml.
- Build 26 (junctions in tests) — **Fix**, checks.md.
- Sec 16 (in-memory start map cap) — **Fix**, F6; by-hand dedupe is not needed because a viewer-day counts once.
- Sec 17 (user IDs in logged exceptions) — **Fix**, F6 "Logging".
- Sec 18 (machine backups capture `today`) — **Fix**, security.md note for the owner.
- Sec 19 (floating toolchain; audit order) — **Fix**, listed in security.md as owed before release.
- Sec 20 (pre-commit not a script; main unprotected; .sha256 from the same pipeline) — **Skip** for now: the pre-commit command is in every handover; main protection is owed before release (security.md); a matching checksum from the same pipeline is accepted at level 1.
- Comp 22 (date bases) — **Fix**, data.md common rules. Comp 24 (Store reading the XML) — **Fix**, Store copies it as a file only. Comp 25 — **Fix**, "for example". Comp 26 — **Fix**, F4 step 8 counts .strm files; `S00E<id>` kept. Comp 27 (same film in two libraries) — **Fix**, F2 step 5 says they are two titles. Comp 28 (chosen library deleted) — **Fix**, F2 step 5, C60. Comp 29 — **Fix** (interfaces-only rule stated; Artwork file bans widened). Comp 30 (which skips make a run incomplete) — **Fix**, spec "Words used here" and F4 step 7. Comp 31 — **Fix**, D29 annotated; `pinTitles` lives in data.md. Comp 32 — **Fix**, data.md `today.itemId`.

## Lesson
No new card. Card 0005 was promoted to standing rule R1 at the start of this job.
