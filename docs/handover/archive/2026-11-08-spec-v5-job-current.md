# Handover

## Job just finished
Spec v5 written (D34 applied; v4 Blockers 1, 5, 6 and the Should-fix list handled, D35–D38), scaffold tightened, re-reviewed by three fresh agents: **not passed, 6 Blockers**. Owner approved standing rule R1, promoted from card 0005: after any spec change, check every new or changed rule against the F4 step list, run.json `step`/`status`, data.md and the part walls before asking for a review.

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v5 is commit 64a2216; scaffold fixes 96682d3; handling file 01bb2dd; this handover and the v5 review are the commit after. Build passes on Windows (run 42469177368).
- main is still docs only (dbf3277). Always read this branch's handover (card 0006).
- No plugin code exists anywhere, on purpose.
- Review: docs/reviews/2026-11-08-spec-v5-review.md. How v4 findings were handled: docs/reviews/2026-11-08-spec-v4-handling.md.

## What is still owed (smallest first)
1. CLAUDE.md, tests README and releases.md small fixes listed as Nits in the v5 review (counts already updated in CLAUDE.md).
2. **Owner proof test (Blocker 5, card 0008):** does Emby 4.10.2 share watched state between a Top Picks copy and its real title? See "Waiting on the owner". Its answer decides how F7 and F4 step 9 are rewritten (keep `uniqueid` in copies and drop the sync, or drop `uniqueid` and keep the sync).
3. Spec v6, in this order:
   a. Fix Blockers 1 (row-check verdict vs C39), 2 (store the full planned entry in each run.json operation), 3 (resume keeps run.json; pending flags not re-copied), 4 (resume point only if higher, first episode only, nothing if source played), 6 (back up plays.json with `"today": []`; add a restore check). Then apply D39 (owner chose shared watched state), which settles Blockers 4 and 5.
   b. Then Should-fix, then Nits, from the v5 review. Write a handling file `docs/reviews/2026-11-08-spec-v5-handling.md`.
   c. Apply R1 to every new or changed rule.
4. Re-run the three goldfish (comprehension, builder readiness, security) on spec v6. If none finds a Blocker, merge pull request 1 (D27). Stage 4 then passes.
5. Stage 5 readiness goldfish (score above 90, owner agrees with the restatement); Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check on staged changes: `git diff --cached -U0 | grep -E '^\+' | grep -nEi '(api[_-]?key|secret|token|passw(or)?d)\s*[:=]\s*["'"'"']?[A-Za-z0-9/+_\-]{12,}|-----BEGIN [A-Z ]*PRIVATE KEY|ghp_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|xox[baprs]-'`. Nothing printed (exit 1) means clean. Don't try to download gitleaks. (v5 review Sec 9: move this command into security.md.)
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31). There is no .NET SDK in the workspace.
- build.yml now **fails** when a lock file is missing; the old "print lock files to the log" fallback is gone. Card 0007's method still works for any other small file.
- The boundary check now also bans `Emby.*`, `System.Diagnostics.Process` and file types for Logic and Artwork. NetArchTest matches by name prefix, so `System.IO.File` also bans `FileNotFoundException` (v5 review Build 12): Artwork must catch `Exception`, or the ban must be made exact.
- The workspace clone is shallow. To track `scaffold`, add it to remote.origin.fetch first.
- Wall changes need the owner's OK, with matching edits to the READMEs, docs/modules.md and BoundaryTests.cs. v5 review Comp 9 / Build 9 (who checks the plugin data folder isn't a link) will need one.
- SkiaSharp (D15) and System.Text.Json (D30) are compiled against but not shipped; versions must be read from the server before their build chunks.
- The old plugin (Emby.TopPicks.dll) is still installed; it is uninstalled at release (Stage 8) using the first-install steps now in docs/releases.md.
- device_bash on the owner's PC is broken; listing and staging files works. Emby's HTTP port is 48096 locally, 8096 publicly behind Caddy.

## Waiting on the owner
- **Answered (quick look), 2026-11-08:** films already watched show as watched in today's row, so Emby shares watched state between copy and real title (CLAUDE.md Facts). Owner then chose **D39: share it, like today**. Spec v6 removes F7 and the played-state reset in F4 step 9; Blocker 4 (resume-point rule) goes with them. Update C14, C15, C49, C59, P2, the EmbyBridge/Run/Logic READMEs and modules.md to match (R1).
- Original question, kept for the record: Quick look first: pick a film in today's Latest Top Picks row that you have watched in your Movies library. Does its copy in the row show as watched too? Then, to be sure, on one test film: mark the copy in Top Picks unwatched and see whether the real film also turns unwatched (mark it watched again afterwards).

## Next job
Spec v6: get the owner's proof-test answer, fix the v5 review Blockers, then Should-fix and Nits, then the goldfish re-review. Read first: CLAUDE.md (R1), docs/reviews/2026-11-08-spec-v5-review.md, docs/decisions.md (D26, D34–D38), docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, the part READMEs.

## Cards that applied
0001 (order through Emby), 0002 (each ban has a self-test fixture), 0005 (now R1), 0006 (handover on the unmerged branch), 0007 (read CI logs, not artifacts). New this job: 0008.
