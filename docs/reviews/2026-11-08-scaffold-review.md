---
summary: Stage 4 docs-and-scaffold review (comprehension + builder readiness), and how each finding was handled. Open items here must be settled before Stage 5.
links: [../spec.md, ../data.md, ../checks.md, ../decisions.md, ../../src/TopPicksPlus/README.md]
---

# Scaffold review, 2026-11-08

Two fresh reviewers (comprehension: 37 findings, 2 Blockers; builder readiness: 18 findings, no Blockers). Numbers below are C# (comprehension) and B# (builder).

## Fixed in the scaffold (this job)
- DLL version overwritten as 0.0.0 by the test step (C34, B1) → one versioned build, `dotnet test --no-build`, plus a step that fails if the DLL lacks the version. Card 0002.
- gitleaks on pull requests needs `pull-requests: read` (B2) → added, PR comments off.
- Project root not enforced (C1, B8) → test: only Plugin and PluginConfiguration may live there; README table has a root row (C4).
- SkiaSharp not ruled for EmbyLink/Settings (C2) → forbidden in README and test.
- Logic "no files / clock / time zone" not enforced (C3, B8) → System.IO, System.Net, TimeZoneInfo and common third-party namespaces now blocked; the clock rule is stated as review-only.
- Polyfill and Properties/ had nowhere to go (C5, B12) → one root `Polyfills.cs` may declare System.Runtime.CompilerServices; no Properties/ folder (csproj instead).
- Rules can't be shown to fail with no code (B8) → canary tests with deliberate wall-breakers.
- Stryker said to run in CI but doesn't (C35, B9) → READMEs now say the first Logic chunk adds it in the same PR with a break-at threshold.
- Tag safety (B14) → tag must look like v1.2.3 and be on main before a Release.
- Audit only covered the test graph (B13) → plugin project audited too; policy written in build.yml.
- Missing "project root" in Storage/Artwork must-not-use (C6), EmbyLink .nfo order vs F4 (C19, B15), C7→C10 example (B17), decisions.md header (C37), Logic owns record types (C28) → fixed.

## Skipped
- CI runs twice per PR (B14 part) — harmless; D16 wants every branch built.
- Tag builds still upload a 3-day artifact (B14 part) — harmless.
- B18 SDK version unverifiable offline — CI restored it fine (run 42473587570).

## Owed: next job (spec and data gaps, before Stage 5)
Technical, for Claude to settle (owner has delegated these) and record as decisions or spec wording:
1. JSON library within the one-DLL rule (C7 Blocker, B3) — find what Emby 4.10.2 already loads (e.g. System.Text.Json / Newtonsoft in its system folder) and compile against it without shipping, as D15 does for SkiaSharp; same question for reading Playback Reporting's history (C26).
2. Safe resume of an interrupted run (C8 Blocker): what run.json records during the swap, whether staging is kept on resume, the order of managed.json updates vs folder moves and deletes; order guard paused during a run and when new rank dates are written (C9); which time is the rank time and whether a resume keeps it (C10).
3. Mappings: adopted old-plugin folders → source titles (via the .strm target path), and what happens when one can't be matched (C15, B4); copy episodes → source episodes for F6/F7 (C14, B4).
4. Minutes watched: where the start position comes from (C13).
5. Folder rules: names for staying copies, clashes with unmanaged folders (C16); adding/removing episodes inside a staying series (C17, B16); .nfo fields and who builds file contents (C20); stale files in adopted folders (B16).
6. run.json gaps: missing pins, artwork failure, plays-in-7-days, the "record" step, fixed list of reasons, how refusals are recorded without overwriting a running report (C22, C23, B11); one source of truth for "last successful run" and whether incomplete counts (C25).
7. Damaged data files: run refuses if managed.json is unreadable (C27).
8. Old-plugin guard by exact assembly name/GUID, and this plugin's fixed GUID (C24).
9. SkiaSharp version and font licence (C29, B10); Storage unit tests for atomic save (C36).
10. Final tie-break by ID so order is fixed (B6); start-up order of guard / resume / missed run (C31); Top Picks folder path built from Emby's data path, case-insensitive (C32); staging lives under the plugin data folder and is the only place outside top-picks it may delete (C33).
11. Checks to add (B7): missed run, Playback Reporting import + fallback, missing library refusal, incomplete path, age ratings on copies (a restricted user doesn't see an over-age title); make C24 an automatic test; tighten C18; subtitles for episodes too or narrow C16 (C18 finding).

Behaviour questions for the owner (one at a time, with a recommendation):
- Q1. Which user the row check looks through, given parental controls and "hide watched" settings (C21, B5). Recommend: a setting to pick the check user, defaulting to the first non-admin user with no restrictions; a short row of N titles passes if it reads 1..N.
- Q2. "Last 7 days" at a Sunday 05:00 run (C11, B6). Recommend: the 7 full UK days before the run day (Sunday to Saturday); plays are kept for 90 full days.
- Q3. For a series, is the 50% rule per episode? (C12, B6). Recommend: per episode; two sittings of the same episode add up, different episodes don't.
- Q4. Must a pin be in the chosen libraries? (C30, B6). Recommend: no, a pin always counts if its file exists.

## Fix review (third reviewer, on commit 51bce01): safe to merge; three should-fixes done before merging
- Canary for "uses another part" didn't use the real rule (1) → a canary for every forbidden entry of every part, checked against the real lists, and a test that fails if lists and canaries drift apart.
- Types outside any namespace or extra types in Polyfills.cs escaped every wall (2) → one test: every plugin type must be in a part, or be Plugin, PluginConfiguration or IsExternalInit.
- README promised "no other packages" for Logic (3) → names the three blocked packages; the rest is review-only, said plainly.
- Version check was a substring match (4) → whole-value match, reading the DLL's version resource.
- Logic README must-not-use list and System.IO.Path note (7, 9) → added. Nits 5, 6, 8 → no change (correct as is, or covered by "reviewed by hand").
