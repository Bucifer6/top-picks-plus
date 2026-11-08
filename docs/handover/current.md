# Handover

## Job just finished
Spec v7 written (v6 Blockers 2 and 3 fixed, D43 recorded, Should-fix and Nits handled, D44–D48), then re-reviewed by three fresh agents: **not passed, 2 Blockers** (docs/reviews/2026-11-08-spec-v7-review.md).

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v7 is commit 20588d0; this handover, the v7 review and card updates are the commit after.
- main is still docs only. Always read this branch's handover (card 0006).
- No plugin code exists anywhere, on purpose.
- How v6 findings were handled: docs/reviews/2026-11-08-spec-v6-handling.md.

## What is still owed (smallest first)
1. Spec v8, in this order:
   a. Fix Blocker 1 (a link *inside* an operation's folder passes the pre-check and is unlinked, never entered; only a link *on the way to* it fails; C27 must still pass) and Blocker 2 (a half-updated series must not sort into the row: e.g. the order guard sets every item of a stale copy to one fixed old date).
   b. Then Should-fix, then Nits, from the v7 review. Write `docs/reviews/2026-11-08-spec-v7-handling.md`.
   c. Apply R1, plus card 0005's new step: walk one concrete case (a delete of a folder with a junction inside, a brand-new add) through every older rule a new pre-check, lock or wait now runs around.
2. Re-run the three goldfish on spec v8. If none finds a Blocker, merge pull request 1 (D27, after build and tests pass). Stage 4 then passes.
3. Stage 5 readiness goldfish; Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check in docs/security.md "Secret scan" on the staged changes. Nothing printed (exit 1) means clean.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31).
- **The spec is growing faster than it settles.** Each review's fixes add rules that clash with older ones (card 0005, seen 4). In v8 prefer removing a rule to adding one, and where a simpler design exists (for a stale copy: just set its dates to one fixed old value), take it.
- Owed to build chunks, not v8: NSubstitute and Mono.Cecil in the test csproj with locked versions (D47; say in v8 which chunk adds them), the ban fixtures and the two empty-passing wall tests (v6 Build 11, card 0002), the meta-test and built-features.txt, SkiaSharp and System.Text.Json versions read from the server.
- D47 (EmbyBridge test layer B) and D48 (Emby database safety copy) are **owner decisions**; v7 review Sec 11–12 only refine how D48 is done (copy `*.db*`, restore tried before the first install), which is in the spirit of D48, but tell the owner.
- D40 says "up to 2 minutes": mark it replaced by D45 in v8.
- The old plugin (Emby.TopPicks.dll) is still installed; it is uninstalled at release using docs/releases.md.
- The workspace clone is shallow: run `git config remote.origin.fetch '+refs/heads/*:refs/remotes/origin/*'` before tracking `scaffold`.
- device_bash on the owner's PC is broken; listing and staging files works.

## Waiting on the owner
- **A second short proof test (v7 Comp 10, card 0008):** D43 proved deleting *film* copies keeps watch history, not *episode* copies. On the old plugin: take the Emby safety copy (D48), note an episode watched and one half-watched in a series in the row, move that series' copy folder out of top-picks, scan, check the real episodes are still watched and resumable, then put it back and scan. Ask this when v8 reaches F7.
- **Two Emby settings on the Top Picks library (v7 Sec 9):** ask the owner to check that "save metadata as .nfo" (and "save user watch data") and "save artwork into media folders" are off for the Top Picks library, and record the answer.
- **Proposed change to R1** (card 0005, seen 4): add "and walk one concrete case through every older rule that a new pre-check, lock or wait runs around". Needs the owner's OK before it goes into CLAUDE.md.

## Next job
Spec v8: fix v7 Blockers 1 and 2, then Should-fix and Nits, then the goldfish re-review. Read first: CLAUDE.md (R1), docs/reviews/2026-11-08-spec-v7-review.md, docs/decisions.md (D36, D40, D44–D48), docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, the part READMEs.

## Cards that applied
0001 (order through Emby), 0002 (ban fixtures), 0005 (R1; now seen 4), 0006 (handover on the unmerged branch), 0008 (real-server proof; now seen 3). No new card this job.
