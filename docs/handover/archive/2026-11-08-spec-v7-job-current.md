# Handover

## Job just finished
Spec v6 written (D39 applied: the plugin never changes watched state; v5 Blockers 1, 2, 3, 6 fixed; Should-fix and Nits handled; D40–D42; checks C63–C75), then re-reviewed by three fresh agents: **not passed, 3 Blockers** (docs/reviews/2026-11-08-spec-v6-review.md).

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged** (pushed). Spec v6 is commit 04e23c4; this handover, the v6 review and card updates are the commit after.
- main is still docs only (dbf3277). Always read this branch's handover (card 0006).
- No plugin code exists anywhere, on purpose.
- How v5 findings were handled: docs/reviews/2026-11-08-spec-v5-handling.md.

## What is still owed (smallest first)
1. ~~Owner proof test~~ done (D43).
2. Spec v7, in this order:
   a. Fix Blocker 2 (give-up count off by one: F5, D36, data.md `resumeCount`, C55 must agree) and Blocker 3 (rule for a skipped delete or update's managed entry so the order guard can't set stale dates). Blocker 1 is settled by D43: record it as handled.
   b. Then Should-fix, then Nits, from the v6 review. Write `docs/reviews/2026-11-08-spec-v6-handling.md`.
   c. Apply R1 to every new or changed rule, **and** re-read every old rule that mentions anything a change touched (card 0005: removing F7 changed what F6 "finished" means; v6 Comp 6).
3. Re-run the three goldfish on spec v7. If none finds a Blocker, merge pull request 1 (D27, after build and tests pass). Stage 4 then passes.
4. Stage 5 readiness goldfish (score above 90, owner agrees with the restatement); Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check in docs/security.md "Secret scan" on the staged changes. Nothing printed (exit 1) means clean. Don't try to download gitleaks.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31). No .NET SDK in the workspace.
- Two v6 Should-fix items touch walls or test layers: an EmbyBridge test layer (Build 1), and which part holds the link-resolving helper on netstandard2.0 (Build 4, Comp 11). A new "may use" link or test layer needs the owner's OK, with matching edits to READMEs, docs/modules.md and BoundaryTests.cs.
- D39 knock-on still open: with shared state, "finished" in F6 must mean "became played during this playback" (Comp 6), or C12 breaks.
- The Sec 2 suggestion (back up Emby's own database before install and each release) is new: it touches the owner's server, so ask before writing it into releases.md.
- SkiaSharp (D15) and System.Text.Json (D30) are compiled against but not shipped; versions must be read from the server before their build chunks.
- The old plugin (Emby.TopPicks.dll) is still installed; it is uninstalled at release (Stage 8) using docs/releases.md's steps.
- The workspace clone is shallow. To track `scaffold`, add it to remote.origin.fetch first.
- device_bash on the owner's PC is broken; listing and staging files works. Emby's HTTP port is 48096 locally, 8096 publicly behind Caddy.

## Waiting on the owner
- **Answered 2026-11-08 10:52 (D43):** the proof test passed on all three: copy → real works, removing copies keeps real watch history, episodes share both ways. **v6 Blocker 1 is settled; D39 and D42 stand.** Nothing more needed from the owner for it. In spec v7, record it in the handling file and note C14 stays the release check.
- Still to ask when spec v7 reaches it: whether to add "back up Emby's own .db files before install and each release" to releases.md (v6 Sec 2). The owner already did this once for the proof test.

## Next job
Spec v7: record D43 (Blocker 1 settled), fix v6 Blockers 2 and 3, then Should-fix and Nits, then the goldfish re-review. Read first: CLAUDE.md (R1), docs/reviews/2026-11-08-spec-v6-review.md, docs/decisions.md (D36, D39–D42), docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, the part READMEs.

## Cards that applied
0001 (order through Emby), 0002 (ban fixtures), 0005 (R1; now seen 3, new evidence), 0006 (handover on the unmerged branch), 0008 (real-server proof; now seen 2). No new card this job.
