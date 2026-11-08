# Handover

## Job just finished
Stage 4 scaffold built and reviewed by three fresh agents ("goldfish": reviewers given only the files, no chat history). Scaffold fixes done; spec fixes are the next job.

## Where it stopped
- Stage 4 (Scaffold), not yet passed: the review found spec gaps, so the re-review is still owed.
- Branch `scaffold`, last commit 9ba0e58, build green (15 tests). Pull request 1 is open and **not merged** (D19: merge only after a passing fresh review).
- main is still docs only (commit dbf3277). No plugin code exists anywhere, on purpose.

## What is still owed (smallest first)
1. Owner to delete the throwaway branch `probe/boundary-bites` on GitHub (Claude was not allowed to delete it). It is harmless.
2. Spec v3: work through every "Fix in the next job" item in docs/reviews/2026-11-08-scaffold-review.md: 8 Blockers first, then the Should-fix list. Update spec.md, data.md, checks.md and the READMEs to match.
3. Ask the owner the two questions below, one at a time, and write the answers into the docs.
4. Re-run the three goldfish (comprehension, builder readiness, security) on the updated docs and scaffold. Merge pull request 1 only if they find no Blockers. Stage 4 then passes.
5. Stage 5 readiness goldfish: score must be above 90 and the owner must agree with its restatement.
6. Stage 6 build in chunks the owner can check, Stage 7 review, Stage 8 release (old plugin removed at the same time).

## Watch out for
- **Before every commit**, run the pattern check on staged changes (security.md): `git diff --cached -U0 | grep -E '^\+' | grep -nEi '(api[_-]?key|secret|token|passw(or)?d)\s*[:=]\s*["'"'"']?[A-Za-z0-9/+_\-]{12,}|-----BEGIN [A-Z ]*PRIVATE KEY|ghp_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|xox[baprs]-'`. Nothing printed means clean. Downloading and running gitleaks here was refused; don't try again.
- **Nothing is compiled here or on the owner's PC.** The cloud workspace can't reach NuGet, and D5 says the PC never builds. Only GitHub Actions builds (D16: every branch and pull request; a v1.2.3 tag on main makes a Release with the DLL and checksum).
- Tests are xUnit plus Stryker mutation testing on Logic (D18). Stryker is skipped until Logic has code.
- Walls between parts are enforced by tests/TopPicksPlus.Tests/BoundaryTests.cs. Changing a wall needs the owner's OK and matching edits to the READMEs, docs/modules.md and that file.
- The workspace clone is shallow; to track a branch, add it to remote.origin.fetch first.
- Old plugin (Emby.TopPicks.dll) is still installed. It is uninstalled at release (Stage 8), at the same moment Top Picks Plus goes in (F9 guard). Its DLL and settings are backed up on the Desktop (D17).
- device_bash on the owner's PC is broken; listing and staging files works. The SkiaSharp version Emby loads has to be read from the server's system folder before the Artwork chunk.
- Emby's HTTP port is 48096 locally, public 8096 behind Caddy.

## Waiting on the owner
1. Should the people using the server be told their viewing is logged (90 days, admin-only)?
2. Which non-administrator user should the weekly row check use?
Technical choices are delegated to Claude, but anything hard to undo still goes to the owner.

## Next job
Spec v3 from the review file, the two owner questions, then the goldfish re-review. Read first: CLAUDE.md, docs/reviews/2026-11-08-scaffold-review.md, docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md.

## Cards that applied
0001 (ordering via Emby, not the .nfo). New this job: 0002, 0003, 0004.
