# Handover

## Job just finished
Spec gaps: owner answered Q1–Q4 (D20–D23) and the library-block question (D42); Claude settled the eleven technical gaps (D24–D41) from facts on the owner's Emby; checks now C1–C50. Two fresh reviews: the first found 2 Blockers (fixed), the re-review found none (its 8 should-fixes are fixed too).

## Where it stopped
Stage 4 complete, spec v3. Branch **spec/gaps-settled**, pull request open (see GitHub), NOT merged: the owner merges it. Still no plugin code. The fixes to the re-review were not reviewed again (no Blockers were involved).

## What is still owed (smallest first)
1. Owner merges the spec/gaps-settled pull request. The old branch **spec/gaps** (one commit, d3c7418, from another session, an earlier D20–D23 wording) is superseded: don't merge it; the owner can delete it.
2. Stage 5 readiness: a fresh reviewer with only the docs and scaffold restates the plan, lists what it would guess, and scores it out of 100. Build only above 90 and once the owner agrees the restatement. If it scores low, fix the docs, not the score.
3. Stage 6 build in chunks. The first Logic chunk adds Stryker to CI in the same pull request (D18).

## Watch out for
- Two things are deliberately unproved until their build chunk: whether the Emby SDK package provides SQLitePCL.pretty (D25; if not, only the "last played" fallback is used), and the old plugin's ID 672630FC-… (D33; the assembly name Emby.TopPicks also matches).
- Deletes: every delete must pass D37's containment checks (cards 0004, 0005 explain why). Never key the managed list by sourceId: it is keyed by folder.
- device_bash on the owner's PC fails (hypervisor off). Listing works; staging works only up to 7 folder levels below C:\ (files inside top-picks copies can't be staged).
- NuGet is blocked in the cloud workspace, so nothing compiles here; GitHub Actions only.
- The shallow clone only fetches main: fetch other branches with an explicit refspec.
- Wall changes (src/TopPicksPlus/README.md table, BoundaryTests.cs, Canary.cs) need the owner's approval.

## Waiting on the owner
- Merge the spec/gaps-settled pull request; delete branch spec/gaps if wanted.
- Then: agree (or correct) the Stage 5 restatement.

## Next job
Stage 5 readiness review. Read first: CLAUDE.md, docs/spec.md, docs/data.md, docs/checks.md, docs/decisions.md, the folder READMEs.

## Cards that applied
0001 (rank dates set on the item). New this job: 0004 (delete containment), 0005 (series .strm in Season folders).
