# Handover

## Job just finished
Spec v4 written (all spec v3 review items handled), re-reviewed by three fresh agents: **not passed, 7 Blockers**. Owner then chose to cut the automatic takeover and the Playback Reporting import (D34).

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v4 is commit 84d57f2; lock files dd8e2df; this handover is the commit after it. Build passes on Windows.
- main is still docs only (dbf3277) and its handover is three jobs old. Always read this branch's handover (card 0006).
- No plugin code exists anywhere, on purpose.
- **spec.md, data.md and checks.md still describe the takeover and import.** D34 removes them, but the docs have not been edited yet. That is the first part of the next job.

## What is still owed (smallest first)
1. Owner: delete the throwaway branch `probe/boundary-bites` on GitHub (Claude can't). Harmless.
2. Owner: decide whether card 0005 (seen twice) becomes a standing rule. Proposed wording: "ALWAYS, after any spec change, check every new or changed rule against the F4 step list, run.json `step`/`status`, data.md and the part walls before asking for a review." Needs the owner's explicit OK.
3. Spec v5, in this order:
   a. Apply D34: remove takeover (F9), import (F6, F4 steps 4–5), the zip, `takeoverDone`/`importTried`, SQLite (D24) from spec, data, checks (C18 second half, C45), modules, READMEs, security.md; renumber F4 steps and run.json `step`. Add the install steps (empty top-picks by hand) for the release notes.
   b. Mark which v4 review findings D34 has removed (Blockers 2, 3, 4 and their Should/Nit items) in a handling file.
   c. Fix the remaining Blockers 1 ("waiting to resume" undefined), 5 (damaged plays.json keeps user IDs too long) and 6 (the weekly unplayed reset could flow back to real titles) from docs/reviews/2026-11-08-spec-v4-review.md, then Should-fix, then Nits. Apply card 0005 to every new rule.
4. Re-run the three goldfish (comprehension, builder readiness, security) on spec v5. If none finds a Blocker, merge pull request 1 (D27 lets Claude merge without asking). Stage 4 then passes.
5. Stage 5 readiness goldfish (score above 90, owner agrees with the restatement); Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check on staged changes: `git diff --cached -U0 | grep -E '^\+' | grep -nEi '(api[_-]?key|secret|token|passw(or)?d)\s*[:=]\s*["'"'"']?[A-Za-z0-9/+_\-]{12,}|-----BEGIN [A-Z ]*PRIVATE KEY|ghp_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|xox[baprs]-'`. Nothing printed (exit 1) means clean. Don't try to download gitleaks.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), now on Windows (D31).
- **CI artifacts can't be downloaded here; job logs can** (card 0007). build.yml still prints lock files to the log when they are missing; that fallback should now become a build failure (review Build 5).
- The workspace clone is shallow. To track `scaffold`, add it to remote.origin.fetch first.
- Wall changes need the owner's OK, with matching edits to the READMEs, docs/modules.md and tests/TopPicksPlus.Tests/BoundaryTests.cs. D28 (owner) and D29 already changed walls this job.
- SkiaSharp (D15) and System.Text.Json (D30) are compiled against but not shipped; their versions must be read from the server before their build chunks (spec "Before building").
- The old plugin (Emby.TopPicks.dll) is still installed; it is uninstalled at release (Stage 8), when the owner also empties top-picks (D34).
- device_bash on the owner's PC is broken; listing and staging files works. Emby's HTTP port is 48096 locally, 8096 publicly behind Caddy.

## Waiting on the owner
- Item 2 above (card 0005 as a standing rule).
- Nothing else; technical decisions are delegated, anything hard to undo still goes to the owner.

## Next job
Spec v5: apply D34, then fix the remaining v4 review Blockers and the rest, then the goldfish re-review. Read first: CLAUDE.md, docs/decisions.md (D26–D34), docs/reviews/2026-11-08-spec-v4-review.md, docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, the part READMEs.

## Cards that applied
0001 (order through Emby), 0002 (wall test must prove it can fail), 0005 (cross-check every new rule; seen 2), 0006 (handover on the unmerged branch). New this job: 0007.

## Quiz result (spec v5 job, 2026-11-08)
Marked by a fresh reviewer: 6 right, 1 partly right (Q5 left out why artifacts fail, blob storage, and the log-decoding steps). No answer wrong, so the job went ahead.
