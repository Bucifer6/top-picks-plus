# Handover

## Job just finished
Stage 5 readiness, first pass: two fresh readiness reviews (72, then 71) and two rounds of doc fixes, D43–D75, checks C51–C75, spec v5. Not yet re-scored after the second round.

## Where it stopped
Stage 5, not passed. Branch **spec/readiness-fixes** (pull request open, not merged: the owner merges). Still no plugin code. Review reports: docs/reviews/2026-11-08-readiness-1.md and -2.md.

## What is still owed (smallest first)
1. Owner merges the spec/readiness-fixes pull request.
2. A fresh readiness reviewer re-scores the docs (same brief as readiness-2: docs, scaffold, reference pictures, cards; not docs/handover/). Build only above 90 **and** once the owner agrees its restatement. If below, fix the docs, not the score, and re-score.
3. Stage 6: build in the five chunks of D69, each on its own branch with a fresh review. Nothing goes on the server until all five are merged (D63).

## Watch out for
- **Go-live is all at once (D63, owner).** No early release, because the old plugin's copies and the first-week import break in an overlap (card 0006). D58 is replaced and D59 withdrawn.
- **Storage does every file change (D43, owner)**, because that is the only place the delete and swap safety rules can have automatic tests; EmbyLink only talks to Emby. CI gets a Windows job so junction tests run.
- **Older decisions are marked "[Amended]" or "[Replaced]" at the start of their row** (D25, D26, D30, D31, D34, D50, D56–D59). Read the newer one, because a builder reading top-down meets the old rule first (card 0007).
- **Owner's other choices this job:** pins are movies and series only, at most 10 (D61); artwork layout measured from the owner's screenshots in docs/reference/artwork/ (D62); wall check matches whole names only (D75).
- The managed list is keyed by folder, never by sourceId, because an adopted folder may have no source and two old folders can share one (cards 0004, 0005, 0008).
- Still unproved until their chunk: SQLitePCL.pretty in the Emby SDK package (D25), the old plugin's ID (D33), and whether SkiaSharp 2.88.3 is flagged by the audit (D65 adds a named exception either way).
- This chat could not reach the owner's PC (no file or computer-use tools). Ask the owner to attach files instead.
- NuGet is blocked in the cloud workspace, so nothing compiles here; GitHub Actions only. The shallow clone only fetches main: fetch other branches with an explicit refspec.

## Waiting on the owner
- Merge the spec/readiness-fixes pull request.
- Then agree (or correct) the restatement from the next re-score.

## Next job
Stage 5 re-score. Read first: CLAUDE.md, docs/decisions.md (from D43), docs/spec.md, docs/checks.md, docs/reviews/2026-11-08-readiness-2.md.

## Cards that applied
0001–0005. New this job: 0006 (early release vs old system), 0007 (mark amended decisions), 0008 (handover reasons).
