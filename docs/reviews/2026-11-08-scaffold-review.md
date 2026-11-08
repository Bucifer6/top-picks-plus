---
summary: Stage 4 goldfish review of docs and scaffold (comprehension, builder readiness, security), and how each finding was handled.
links: [../spec.md, ../data.md, ../modules.md, ../security.md, ../checks.md]
---

# Scaffold review, 2026-11-08

Three fresh reviewers, run at the same time: **Comp** (comprehension, 41 findings), **Build** (builder readiness, 32), **Sec** (security, 18). Numbers below are theirs, e.g. Comp 2 = comprehension finding 2.

## Fixed in this job (scaffold)
| Finding | Fix |
|---|---|
| Comp 1, Build 1 (Blocker): no part allowed to use the settings Run needs | EmbyBridge may now use Settings' configuration class and hands Run a plain record (READMEs, modules.md, BoundaryTests) |
| Build 10a: wall test can't fail with no code | Self-test `The_check_catches_a_real_wall_break` against fixtures in the test assembly; also proved by a deliberately broken branch (run failed on 2 tests) |
| Build 10b, Build 6 (part), Comp 33: root namespace is a backdoor | Parts may not use any root type (enforced) |
| Comp 27, Build 31: resource or IDE folders would fail the folder test | Resources live inside their part's folder (READMEs); dot-folders ignored |
| Build 4: release DLL loses its version (test rebuilt it) | Build once with the version, test with `--no-build` |
| Build 9, Comp 38: Stryker silently skipped for Logic subfolders; report lost | Recursive check; report kept 3 days |
| Build 21, Comp 39: CI builds all 0.0.0.0 | CI builds are 0.0.<run number> |
| Sec 7: actions on movable tags | Pinned to full commit IDs (latest of the tested major versions) |
| Sec 8, Build 20 (part): any v* tag releases; no checksum | Release only if the tagged commit is on main; SHA-256 attached |
| Sec 9 (part): gitleaks may 403 on pull requests | `pull-requests: read` added to the build job |
| Sec 14: tag name pasted into the shell | Passed through env and checked against v1.2.3 |
| Sec 15: audit matches English text | JSON output counted |
| Sec 9: no scan before commit | Pattern check on staged changes before every commit (security.md); gitleaks still runs on every push. Full gitleaks can't run in the cloud workspace |
| Sec 13: .gitignore gaps | Added .env.*, *.pem, *.p12, *.snk, *.log, plays/managed/run.json |

## Fix in the next job (spec v3) – Blockers
1. **Safe deletes (Sec 1, Sec 5, Sec 4, Comp 16):** a written path guard before any delete (single name segment, canonical parent is exactly the Top Picks folder or staging, no junctions/symlinks, refuse and report); rewrite F9's "never a file it didn't create" as what is actually enforced; back up the Top Picks folder once before the first takeover.
2. **Untrusted names and .nfo (Sec 2, Comp 13, 14, 15, Build 24):** one naming rule for every generated folder and file (empty or reserved name falls back to the source ID; episode and subtitle names; Season numbering, specials, path length); clashes with unmanaged folders; a stayer keeps its folder name; .nfo written with an XML writer and the fields listed (Build 18).
3. **Admin-only API (Sec 3, Build 14):** every settings API action checks for an administrator on the server; add a check.
4. **Takeover mapping (Comp 2, Build 2):** how an old folder is matched to its source title (e.g. the .strm target path), what happens with no match, subfolders counted for series.
5. **Episode mapping (Comp 3, Build 15):** how a copy episode maps to its source episode for F7 and plays.
6. **Guard vs run (Comp 4, Build 5):** the order guard stands down while a run is in progress.
7. **Resume (Comp 5, Build 11):** order of folder moves vs managed.json writes; rank time stored in run.json; staging not emptied on resume.
8. **Damaged files (Sec 6, Comp 22, Build 7):** damaged managed.json or run.json stops the run with a message; damaged plays.json is renamed aside and reported; Top Picks library excluded from candidates in Logic, not just the page.

## Fix in the next job (spec v3) – Should fix
- Old plugin identified exactly (assembly name or GUID), not by "Top Picks" in the name (Comp 6).
- Time rules: UK day via an explicit zone passed into Logic; exact 7-day window; 90-day cut-off by day; plays crossing midnight (Comp 7, Build 16).
- "Minutes watched" defined (position vs time watched; resumed sittings); which users count (Comp 8, Build 15).
- Final tie-break key, nulls, filler order (Comp 9, Build 16).
- Pins: which libraries, which item types; series pin "missing" rule (Comp 10, Build 27).
- Offline drive safeguard (Comp 11): recommend refusing the run if a chosen library's files can't be reached.
- Subtitles for episodes: F3 vs C16 (Comp 12).
- Row check: which user, which internal query, matching by ID, pass rule with fewer than 10 or incomplete, parental controls (Comp 17, Build 12, Sec 12).
- Date format, precision and tolerance; rank time fixed on resume (Comp 18).
- Missed-run rule: what counts as successful, first install, start-up order (Comp 19).
- run.json fields match the report (missing pins, title names, plays count, status list, failed vs incomplete) (Comp 20).
- Field types and formats in data.md; schema version (Comp 21, Build 28).
- Playback Reporting import: how it is read and mapped (Comp 23, Build 8).
- Paths, logging and timers: who supplies programdata paths and a logger to parts that can't use Emby; who owns the start-up delays (Comp 24).
- Artwork inputs, fallback with no images, font named, layout numbers or a reference image, backdrop format (Comp 25).
- Run now: starts the Emby scheduled task (Comp 28).
- Watched sync triggers and direction (Comp 29, Build 26).
- Missing checks (Comp 30, Build 13); C21's 15 minutes vs 30-minute wait (Build 23); C11/C12 counted vs filler (Build 29).
- Hook wiring: how Emby-created classes reach Run (static plugin instance) (Build 6).
- Interfaces around Artwork and Store so Run tests use fakes (Build 3).
- Plays pruned whatever the run status; no per-play Info logging (Sec 11).
- Backups: plays.json, managed.json, settings; where, how long, restore steps (Sec 10).
- Data folder under `programdata\plugins` is safe from Emby's plugin scan (Build 25).

## Fix when the code needs it (build chunks)
- SkiaSharp: record the version Emby 4.10.2 loads (read from the server), reference compile-only, native assets for tests (Build 3, Comp 26, Sec 16).
- netstandard2.0 needs: System.Text.Json reference, IsExternalInit polyfill at the src root, `File.Replace` for atomic writes, EmbeddedResource items (Build 17).
- Lock file and package source mapping (Sec 16).
- Fixed plugin GUID (Comp 40).
- Native image decode guard (Sec 17).
- Accepting a known vulnerable transitive package from the Emby SDK, if it ever happens (Build 19).

## Asked the owner (answered 2026-11-08)
- Sec 18 (users not told viewing is logged): owner wants no viewing history at all. D21: user IDs are dropped at the end of each day; only viewer counts are kept.
- Build 12 / Comp 17 / Sec 12 (which user for the row check): D22. Remaining row-check details (query, matching, pass rule) stay in spec v3.

## Skipped (Nits, recorded)
- Comp 31 (data.md intro wording) – fold into spec v3 edit. Comp 32, 35, 36, 37, 41 – fold into spec v3 edit.
- Comp 34, Build 22 (Logic must not read the clock; not enforceable by namespace) – relies on review; NetArchTest can't see member calls.
- Build 30 (Run using concrete EmbyBridge classes) – reviewer check in Stage 7.
- Comp 4/5 duplicates of Build 5/11 – merged above.

## Handled in spec v3 (next job, same day)
All 8 Blockers and every Should-fix item above were worked into spec.md v3, data.md, checks.md (C26–C33 added; C11, C12, C21 reworded), modules.md, security.md and the part READMEs. Three choices were recorded as decisions by Claude: D23 (data folder moved to `programdata\data\TopPicksPlus\`, answers Build 25), D24 (Playback Reporting read through Emby's own SQLite, answers Comp 23 / Build 8), D25 (unreachable library folder refuses the run, answers Comp 11). Nits Comp 31, 32, 35, 36, 37, 41 folded into the rewrite. Result of the re-review: docs/reviews/2026-11-08-spec-v3-review.md.
