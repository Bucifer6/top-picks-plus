# Handover

## Job just finished
Stage 4 scaffold: folders with READMEs, build workflow, test project, boundary check with canaries, and three fresh reviews (docs/reviews/2026-11-08-scaffold-review.md).

## Where it stopped
Stage 4 done apart from the spec gaps the review found. Branch stage4/scaffold, pull request #2, merged into main by Claude after all checks passed (see the last commit on main). Still no plugin code.

## What is still owed (smallest first)
1. Ask the owner the four behaviour questions Q1–Q4 in the scaffold review, one at a time, each with its recommendation; record the answers as decisions.
2. Settle the eleven technical gaps listed under "Owed: next job" in the scaffold review (two were Blockers: the JSON library within the one-DLL rule, and safe resume of an interrupted run). Record each as a decision or as spec/data wording, and add the missing checks.
3. Stage 5 readiness: a fresh reviewer restates the plan and scores it; it must score above 90 and the owner must agree the restatement.
4. Stage 6 build in chunks the owner can check. The first Logic chunk must add Stryker mutation testing to CI in the same pull request. Then Stage 7 review, and Stage 8 release (the old plugin is removed at the same time).

## Watch out for
- Item 2's JSON question needs facts from the owner's PC: which JSON and SQLite DLLs Emby 4.10.2 already loads (look in Emby's system folder through the device bridge). Don't guess.
- Old plugin (Emby.TopPicks.dll) is still installed; F9's guard should match it exactly (by assembly name or ID, not "contains Top Picks"), or it would block Top Picks Plus too.
- device_bash on the owner's PC is broken; listing and staging files works.
- NuGet is blocked in the cloud workspace, so nothing compiles here; GitHub Actions only.
- The shallow clone only fetches main: to track a new branch, add a fetch refspec for it.
- Any change to the walls (src/TopPicksPlus/README.md table, BoundaryTests.cs, Canary.cs) needs the owner's approval; adding a forbidden entry needs a matching canary or the build fails.

## Waiting on the owner
Q1–Q4 in docs/reviews/2026-11-08-scaffold-review.md (row-check user, the 7-day window, series 50% rule, pins outside chosen libraries).

## Next job
Spec gaps: ask Q1–Q4, settle the eleven technical gaps, add the missing checks. Read first: CLAUDE.md, docs/reviews/2026-11-08-scaffold-review.md, docs/spec.md, docs/data.md, docs/checks.md, docs/decisions.md.

## Cards that applied
0001 (date set directly on the item; EmbyLink README). New this job: 0002, 0003.
