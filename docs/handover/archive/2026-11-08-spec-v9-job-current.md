# Handover

## Job just finished
Spec v8 written (v7 Blockers fixed mostly by removing rules: D51 guard one entry at a time, D52 no link resolving, D53 stale date; owner proofs D49, D50; D54), then re-reviewed by three fresh agents: **not passed, 2 narrow Blockers** (docs/reviews/2026-11-08-spec-v8-review.md).

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v8 is commit c1fab5f; this handover and the v8 review are the commit after.
- main holds pull request 2, an older parallel scaffold (spec v2, D1–D19, five parts: Storage, EmbyLink) the owner merged at 11:33 on 2026-11-08 from another session. **Owner's choice 2026-11-08: pull request 1 is the real line.** main was merged into this branch keeping this branch's files (its handover is in archive/2026-11-08-pr2-parallel-scaffold-current.md), so pull request 1 merges cleanly and replaces it. Never work from `stage4/scaffold`. Always read this branch's handover (card 0006).
- No plugin code exists anywhere, on purpose.
- How v7 findings were handled: docs/reviews/2026-11-08-spec-v7-handling.md.

## What is still owed (smallest first)
1. Spec v9, in this order:
   a. **Blocker 2 (C39):** C39 calls a missing pick both `partial` and `failed`. Fix: remove "missing an expected pick" and "an extra item" from its `failed` list, matching spec F4 step 11.
   b. **Blocker 1 (update and links):** today an update writes before removing links inside, so a junction named like a new folder skips the update, and a file symlink with a new file's name could be written through. Fix: an update first sweeps its folder removing every link as a link, before any write; every replace target is checked not to be a link just before it is replaced.
   c. Then Should-fix, then Nits, from the v8 review. Write `docs/reviews/2026-11-08-spec-v8-handling.md`. Keep preferring removal: for the lock hand-off, one "start waiting" flag plus a 30-second wait limit is the simple form.
   d. Apply R1, walking **every** operation kind (delete, update, add) and each resume point, not one example (card 0005, seen 5).
2. Re-run the three goldfish on spec v9. If none finds a Blocker, merge pull request 1 (D27, after build and tests pass). Stage 4 then passes.
3. Stage 5 readiness goldfish; Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check in docs/security.md "Secret scan" on the staged changes. Nothing printed (exit 1) means clean.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31).
- **Sec found the delete design sound** (no route to real media). Don't re-add link resolving (D52) to answer the Nits about UNC or subst paths: say they are accepted, since deletes stay inside managed folders.
- The Cecil ban list in modules.md names Emby SDK members from memory; the first EmbyBridge chunk confirms each (the scan fails on a missing name by design).
- Owed to build chunks, not v9: the ban fixtures and empty-passing wall tests (card 0002, plus v8 Build 16: SkiaSharp and Settings bans), the meta-test and built-features.txt, SkiaSharp and System.Text.Json versions read from the server. NSubstitute and Mono.Cecil come with the first EmbyBridge chunk (modules.md).
- The old plugin (Emby.TopPicks.dll) is still installed; it is uninstalled at release using docs/releases.md.
- Shallow clone: run `git config remote.origin.fetch '+refs/heads/*:refs/remotes/origin/*'` before tracking `scaffold`.
- device_bash on the owner's PC is broken; listing and staging files works.

## Waiting on the owner
- R1 widened to every operation kind and restart point: **approved by the owner 2026-11-08 and already in CLAUDE.md.** Nothing to ask.
- Tell the owner (no decision needed): D54 refines how the Emby safety copy is taken; v9 will add written restore steps and how many copies are kept.

## Next job
Spec v9: fix v8 Blockers 2 then 1, then Should-fix and Nits, then the goldfish re-review. Read first: CLAUDE.md (R1), docs/reviews/2026-11-08-spec-v8-review.md, docs/decisions.md (D44, D49–D54), docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, docs/releases.md, the part READMEs.

## Cards that applied
0001 (order through Emby), 0005 (R1; now seen 5), 0006 (handover on the unmerged branch), 0008 (real-server proof; D49 and D50 settled its v7 items). No new card this job.
