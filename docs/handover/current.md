# Handover

## Job just finished
Spec v3 written from the scaffold review, then re-reviewed by three fresh agents ("goldfish": reviewers given only the files). Not passed: 8 Blockers.

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged** (D19: merge only after a fresh review with no Blockers). Spec v3 is commit 1f81762; this handover is the commit after it.
- main is still docs only (commit dbf3277) and its handover is two jobs old. Always read this branch's handover, not main's (card 0006).
- No plugin code exists anywhere, on purpose.

## What is still owed (smallest first)
1. Owner: delete the throwaway branch `probe/boundary-bites` on GitHub (Claude can't). Harmless.
2. (done: owner chose "copies never look watched", D26.)
3. Spec v4: fix everything in docs/reviews/2026-11-08-spec-v3-review.md. The 8 Blockers come first (Blocker 1 needs the owner's answer), then the Should-fix list, then the Nits. Follow card 0005 for every new rule: check it against the F4 step list, run.json `step`, data.md and the part's walls in the same edit.
4. Any fix that changes a wall (a part's "may use" / "must not use", e.g. Blocker 2) needs the owner's OK first, with matching edits to the READMEs, docs/modules.md and tests/TopPicksPlus.Tests/BoundaryTests.cs.
5. Re-run the three goldfish (comprehension, builder readiness, security) on spec v4. Merge pull request 1 only if none finds a Blocker. Stage 4 then passes.
6. Stage 5 readiness goldfish: the score must be above 90 and the owner must agree with its restatement.
7. Stage 6 build in chunks, Stage 7 review, Stage 8 release (the old plugin is removed at the same time).

## Watch out for
- **Before every commit**, run the pattern check on staged changes (security.md): `git diff --cached -U0 | grep -E '^\+' | grep -nEi '(api[_-]?key|secret|token|passw(or)?d)\s*[:=]\s*["'"'"']?[A-Za-z0-9/+_\-]{12,}|-----BEGIN [A-Z ]*PRIVATE KEY|ghp_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|xox[baprs]-'`. Nothing printed means clean. Don't try to download gitleaks here; it was refused.
- **Nothing is compiled here or on the owner's PC.** The cloud workspace can't reach NuGet, and D5 says the PC never builds. Only GitHub Actions builds (D16).
- The workspace clone is shallow. To track this branch, add it to remote.origin.fetch first.
- Spec v3 added decisions D23 (data folder `programdata\data\TopPicksPlus\`), D24 (Playback Reporting read through Emby's own SQLite, compiled against, not shipped) and D25 (an unreachable library folder refuses the run). All three were Claude's calls under the owner's delegation.
- SkiaSharp (D15) and Emby's SQLite library (D24) are compiled against but **not shipped**: the plugin uses Emby's own copies. So both versions must be read from the server before their build chunks. The same question is now open for System.Text.Json (review: Build 8).
- The old plugin (Emby.TopPicks.dll) is still installed. It is uninstalled at release (Stage 8), at the same moment Top Picks Plus goes in. Its DLL and settings are backed up on the Desktop (D17).
- device_bash on the owner's PC is broken; listing and staging files works.
- Emby's HTTP port is 48096 locally, 8096 publicly behind Caddy.

## Waiting on the owner
- Nothing on watched titles: answered 2026-11-08 as D26 (copies never look watched). Spec v4 must apply it to F7, C14, C15 and the row check.
- Possibly: whether card 0006 should become a standing rule (needs the owner's OK).

## Next job
Spec v4: apply D26, then fix the spec v3 review file item by item, then the goldfish re-review. Read first: CLAUDE.md, docs/reviews/2026-11-08-spec-v3-review.md, docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, the part READMEs.

## Cards that applied
0001 (order through Emby, not the .nfo), 0002 (wall test must prove it can fail), 0004 (scaffold record). New this job: 0005, 0006.
