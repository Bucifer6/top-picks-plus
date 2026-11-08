---
summary: Fresh-reviewer re-review of spec v3 (comprehension, builder readiness, security). Not passed: 8 Blockers. Every item is for spec v4 unless marked otherwise.
links: [../spec.md, ../data.md, ../checks.md, ../modules.md, ../security.md, 2026-11-08-scaffold-review.md]
---

# Spec v3 re-review, 2026-11-08

Reviewers: **Comp** (comprehension: 5 Blocker, 19 Should, 9 Nit), **Build** (builder readiness: 2 Blocker, 14 Should, 8 Nit), **Sec** (security: 1 Blocker, 11 Should, 5 Nit). Run at the same time on commit 1f81762. Stage 4 **not passed**; pull request 1 stays unmerged.

## Blockers (fix first in spec v4)
1. **Watched titles hidden from Latest (Build 1).** Emby's per-user "hide watched content from latest media" setting (reviewer fairly sure, unverified on 4.10.2) would remove picks from the row for anyone who has watched them, and F7 copies played state onto copies, and playing a copy marks it played. Row check would fail too. **Owner decision needed (asked 2026-11-08).**
2. **Run's walls forbid files the spec needs (Build 2).** Subtitles and theme video from the source, offline-drive check, takeover zip. Name the EmbyBridge adapter (or widen the wall with owner OK).
3. **Refused run vs count (Comp 1, Build 3).** "Nothing changes when refused" vs "count/prune on every run". Pick one and order the steps.
4. **Resume covers only the swap (Comp 2).** Define resume for every run.json step.
5. **No copy item IDs stored (Comp 3).** Row check, set dates and guard need each copy's Emby item (series and episodes); managed.json has only sourceId and paths.
6. **Takeover, one-off import and source-to-copy sync not in F4's steps (Comp 4, Build 4).** Place each in the step list and run.json `step`; what a refused first run does to adopted folders; the import's 7-day window.
7. **Rank time defined two ways (Comp 5, Build 18).** Run start vs start of choosing; can change the UK day and the window.
8. **Backups and renamed-aside plays.json keep user IDs for weeks or forever (Sec 1).** Breaks D21. Back up after counting or without `today`; limit how long `.damaged-*` plays files live.

## Should fix
**Running and resuming**
- Status for an error before the swap (Comp 6); `incoming`/`adopted` leftovers next run (Comp 23); "partial" when appeared copies are out of order, and `not run` (Comp 15).
- One lock for Run now, scheduled task, resume and missed run; Emby queues rather than refuses, so refuse in the plugin API; whether timer runs go through the task (Build 12).
- Which Emby signal means "a library scan finished" for the guard (Build 11).
- Who builds EmbyBridge adapters with Emby's services; when `Hooks` is filled; events before it is filled (Build 6, Comp 32).
- Settings: Run must save LastSuccessfulRunUtc / TakeoverDone / PlaybackReportingImported but may not use Settings; reading settings without the root type; a page save or restored XML resetting TakeoverDone would re-run takeover. Move run-state out of the settings file, e.g. a marker in the data folder (Comp 9, Build 7, Sec 6).
- Store needs TakeoverDone for "missing managed.json = damaged" (Comp 9).
- "Hands every part a paths record" conflicts with EmbyBridge's walls; where the start-up entry point lives (Comp 11).
- Single writer or lock for plays.json (listener vs run) (Build 20).

**Data and matching**
- Multi-episode .strm needs several sourceItemIds (Comp 8).
- Manual mark-played and unmark: entry shape and day (Comp 22, Build 5).
- Last-known titles for missing pins; live vs last-run "missing" mark (Comp 16).
- Ordinal comparisons for title and ID tie-breaks (Comp 17).
- `artworkDrawn` vs artwork refreshed (Comp 14).
- `dateadded` local time repeats at the autumn clock change: write UTC or an offset (Comp 18).
- Episode name parts: which series name, number padding, Emby-ID format (Comp 21).
- Grouped Latest may return a lone episode instead of its series (Build 15, unsure).
- Every copy "in the library" for a series = series and all episodes? (Build 24).

**Safety**
- Delete guard: Store's staging empty and backup pruning; staging can't be "managed"; rule 2 vs `Season 1\x.strm` paths (Comp 7, Sec 3, Sec 17).
- Check real location and "not a link" before every write and move, not only delete/replace (Sec 4).
- How the Top Picks folder is found (circular now); refuse if it overlaps a source library, programdata or the data folder (Sec 5).
- Refuse the run if any backup or the takeover zip fails; verify the zip; backup folders `YYYY-MM-DD-HHMM`; theme videos in an unmatched folder may be the only copy (Sec 7, Comp 28).
- Admin check via Emby's authorisation context, never a userId in the request; C26 covers all four actions (Sec 8).
- Metadata text on the settings page shown as plain text only (Sec 9).
- Validate saved settings: ID format, unknown IDs, pin cap, duplicates, Top Picks library (Sec 10).
- Offline drive: define "can be reached" (Comp 10).
- User IDs can outlive the day if nobody plays: roll up at start-up and just after midnight, or reword D21 (Sec 2).

**Build and tests**
- System.Text.Json on netstandard2.0: Emby's copy compile-only, ship, or other serializer (Build 8, Comp 12).
- SkiaSharp and SQLite versions are open prerequisites; SkiaSharp calls behind their own type so a load failure hits the fallback; Linux native assets for tests (Build 9).
- Windows-only rules (junctions, CON, `\`, path length) can't be tested on ubuntu-latest; netstandard2.0 has no link-target API: run tests on windows-latest or state the method (Build 10).
- Lock file and locked-mode restore (Sec 11). Checksum and upload the DLL before tests and Stryker run (Sec 12).
- Prove the plugin's own DateCreated save survives a full scan and restart in the first build chunk (Build 16).
- Missing checks: F3 .nfo fields/escaping/dateadded/.strm/lengths/subtitles/age rating; backups kept 8; failed status; partial and parental controls; missed run after 7 days; order guard at +2 min; tie-break chain; pin twice; >10 pins with missing; Top Picks files excluded; import; 20-minute rule; midnight and clock-change days; logging; F7 ignores own changes; missing library; damaged run.json/plays.json; delete-guard name checks (Build 13, Comp 24).
- C26, C17, C22, C14 "automatic": say what each test actually asserts and how Emby is faked (Build 14).

**Artwork**
- "10" at 390 px may hit the logo box; font size vs digit height (Comp 19).
- Fallback with no backdrop, and JPEG bytes in a .png name (Comp 20, Build 23).

## Nits (fold in)
Comp 25 (Store vs Run owning staging; settings backup owner), Comp 26 (240 limit on staging or final path; " - 2" past 60; case-insensitive clashes), Comp 27 (minutes rounding), Comp 29 (which non-admin user), Comp 30 ("movie or TV library"), Comp 31 (unmanaged folders in the row), Comp 33 (clock rule not enforced; scan command location), Build 17 + Comp 13 (EmbyBridge README step numbers stale), Build 19, Build 21, Build 22 (IsExternalInit, EmbeddedResource: build chunk), Sec 13 (gitleaks scans pushed range, not full history), Sec 14 (attestation, branch rules, scheduled audit), Sec 15 (.gitignore `*.damaged-*`, `*.zip`, `*.tmp`, `staging/`), Sec 16 (ban System.Net for Store, Artwork, Run, Settings).

## Lesson
Spec v3 fixed every v2 finding but added detail faster than the docs were cross-checked: each new rule (backups, resume, damaged files) created a contradiction elsewhere. Card 0005.
