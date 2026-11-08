# Handover

## Job just finished
Stage 5 passed (new gate, D92) and chunk 3 (Artwork) built, reviewed (keep) and approved by the owner.

## Where it stopped
Stage 6 Build. Two pull requests, both for the owner to merge, in this order:
1. #5, branch spec/readiness-fixes-2: all plan docs D76–D96, docs/backlog.md, reviews 3–4 and fix-check 4.
2. The Artwork pull request, branch build/chunk3-artwork (built on top of #5, last commit is the handover commit). Code in src/TopPicksPlus/Artwork, tests in tests/TopPicksPlus.Tests/Artwork. CI green (run 42504738112, 105 tests).
Still nothing on the owner's server.

## What is still owed (smallest first)
1. Owner merges #5, then the Artwork pull request.
2. Chunk 1 (Logic): build on a new branch from main once both are merged, with a fresh review. It also adds the Windows test job, global.json and Stryker (D87), which Artwork left for it on purpose.
3. Then chunk 2 (Storage), the proof plugin, chunk 4 (EmbyLink), chunk 5 (Settings), in that order (D92). Nothing goes on the server until all are merged (D63); the proof plugin is the one exception (D76).

## Watch out for
- **The Stage 5 gate is no longer a score (D92, owner).** Reviews stop when no Blockers remain; everything smaller goes to docs/backlog.md for its chunk. Re-reviews check only the fixes. Whole-plan scores sat at 72–76 for four rounds (card 0009), so never restart a full readiness round.
- **Each builder must read the open backlog rows for its chunk first** and fix or decide them (D92). Logic has the most (B1–B3, B5, B6, B8, B9, B12, B16, B22, B24, B28, B35).
- **No backup (D95, owner).** The plugin rebuilds a damaged state.json from its other files; a damaged managed.json is deleted by the owner and the copies are adopted again. Don't add "restore from backup" anywhere.
- **Any import marker means the import is done (D96),** whichever run wrote it, so viewer-days can't double.
- **Older decisions are marked "[Amended]"/"[Replaced]" at the start of their row** (card 0007, seen twice). Read the newer one.
- **This workspace can't download GitHub Actions artifacts (403).** To show the owner pictures, the files in the session scratchpad came from a CI log print; for new samples, print them into the CI log temporarily, then revert.
- **Commit as Claude** (noreply@anthropic.com) so GitHub shows commits as verified; a stop hook checks this.
- NuGet is blocked here; GitHub Actions is the only build. The shallow clone fetches only main: fetch other branches with an explicit refspec.

## Waiting on the owner
- Merge #5, then the Artwork pull request.

## Next job
Build chunk 1 (Logic). Read first: CLAUDE.md, docs/decisions.md (D43 onward, especially D69, D78, D80, D87, D88, D92–D96), docs/checks.md chunk list, docs/backlog.md Logic rows, src/TopPicksPlus/Logic/README.md.

## Cards that applied
0001–0009. New this job: 0009 (readiness score plateaus), 0010 (Artwork build). 0007 seen again.
