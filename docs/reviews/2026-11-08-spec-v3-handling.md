---
summary: How each spec v3 re-review finding was handled in spec v4 (Fix / Contest / Ask the owner / Skip).
links: [2026-11-08-spec-v3-review.md, ../spec.md, ../data.md, ../checks.md, ../modules.md, ../decisions.md]
---

# Handling of the spec v3 re-review (spec v4, 2026-11-08)

Nothing was contested. Two items went to the owner (Blocker 1 → D26, Blocker 2 → D28).

## Blockers
1. Watched titles hidden from Latest — **Ask the owner → D26.** Applied in F7, F4 step 11 and 13, C14, C15.
2. Run's walls — **Ask the owner → D28.** EmbyBridge source-files adapter; Store makes the zip. READMEs, modules.md, F2 step 8, F3, F9 updated.
3. Refused run vs count — **Fix.** Guards are step 1 and a refused run changes nothing; roll-up is step 2 and also runs on timers (D33).
4. Resume per step — **Fix.** F5 lists every `step` value.
5. Copy item IDs — **Fix.** managed.json `itemId` per copy and per file; F4 step 10; F5.
6. Takeover, import, sync in the steps — **Fix.** F4 steps 4, 5, 11; run.json `step`; import window = 7 UK days before the run's UK day; takeover happens after the guards, so a refused first run adopts nothing.
7. Rank time — **Fix.** One definition: run start rounded down, set when run.json is created.
8. User IDs in backups and damaged files — **Fix.** Backups leave out `today`; damaged plays file deleted after its day; D33.

## Should fix — all **Fix**
Running and resuming: statuses and leftovers (F4 "If something goes wrong", step 7, step 13 results); one lock (D32, F1); scan signal (F5, confirmed in the first EmbyBridge chunk); wiring (spec "How the parts connect"); run state out of settings (D29, state.json); paths and entry point (root entry point); plays.json lock (F6, data.md).
Data and matching: `sourceItemIds`; manual marks (F6); `pinTitles` and live "missing" (F8, state.json); ordinal comparisons ("Words used here"); `artworkDrawn` vs `artworkRefreshed`; `dateadded` in UTC (F3); episode name parts (F3); lone episode in Latest (F4 step 13); "every copy in the library" (F4 step 10).
Safety: delete guard covers staging, backups, path parts (F9); checks before every write and move (F9); finding the Top Picks folder (F9, fixed path); backups and zip refuse on failure, zip checked, `YYYY-MM-DD-HHMM`, theme videos (F9); admin from Emby's authorisation (F8, C26); plain text on the page (F8, C54); settings validation (F8); "can be reached" defined; D33.
Build and tests: D30; prerequisites listed (spec "Before building"), SkiaSharp behind one type (Artwork README); D31 (Windows, lock files, checksum first); C34 proof; missing checks C35–C54; "How the automatic tests work" in checks.md.
Artwork: number size and width limit; fallback names and no-image case (F3).

## Nits
- **Fix:** Comp 25 (Store owns staging emptying and the settings copy), Comp 26, Comp 27, Comp 29, Comp 30, Comp 31, Comp 33 (time-zone warning; scan is EmbyBridge's), Build 17 + Comp 13 (EmbyBridge README rewritten), Sec 13 (security.md wording), Sec 15 (.gitignore), Sec 16 (System.Net banned for every part, boundary self-test).
- **Fix (partly):** Sec 14 — weekly scheduled audit added; branch protection is an owner setting owed before release (security.md); attestation skipped at level 1.
- **Skip:** Build 19 and Build 21 — the review file records no detail for them, so there is nothing specific to change; the re-review will raise them again if they still apply. Build 22 (IsExternalInit, EmbeddedResource) — belongs to the first build chunk, not the spec.
