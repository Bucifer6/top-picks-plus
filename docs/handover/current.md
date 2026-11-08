# Handover

## Job just finished
Spec v9 written (v8 Blockers fixed: update link sweep before any write and replace-time link check; C39 verdicts; all v8 Should-fix and Nits handled, D55; docs/reviews/2026-11-08-spec-v8-handling.md), then re-reviewed by three fresh agents: **not passed, 2 narrow Blockers** (docs/reviews/2026-11-08-spec-v9-review.md). Sec found no route to real media or watch history.

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v9 is commit 4c69da6; this handover and the v9 review are the commit after.
- main holds pull request 2, an older parallel scaffold the owner merged earlier; **pull request 1 is the real line** (owner's choice) and merges cleanly over it. Never work from `stage4/scaffold`. Always read this branch's handover (card 0006).
- No plugin code exists anywhere, on purpose.

## What is still owed (smallest first)
1. Spec v10, in this order:
   a. **Blocker 2 (check 2):** check 2 (single names) applies only to names the plugin writes (copy folder, planned file paths); deleting what is found inside a managed folder uses checks 3 and 4 only.
   b. **Blocker 1 (waiting starts):** a waiting start is turned down at once when a run (holder `run`) takes the lock. Then fix C73 wording ("the item date in progress").
   c. Then Should-fix, then Nits, from the v9 review. Write `docs/reviews/2026-11-08-spec-v9-handling.md`. Keep preferring removal: drop the guard's self-change filter; one "create new, never write into an existing file" rule replaces the special cases.
   d. Apply R1 as card 0005 now says (seen 6): every operation kind and resume point, **plus** something the plugin didn't make inside each folder, **plus** several starts at once with a run that ends quickly.
2. Re-run the three fresh reviewers on spec v10. If none finds a Blocker, merge pull request 1 (D27, after build and tests pass). Stage 4 then passes.
3. Stage 5 readiness review; Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check in docs/security.md "Secret scan" on the staged changes. Nothing printed (exit 1) means clean.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31).
- Don't re-add link resolving (D52) for UNC or subst paths: accepted, since deletes stay inside managed folders.
- The Cecil ban list in modules.md names Emby SDK members from memory; the first EmbyBridge chunk confirms each.
- Owed to build chunks, not v10: ban fixtures and empty-passing wall tests (card 0002, SkiaSharp and Settings bans), the meta-test and built-features.txt, SkiaSharp and System.Text.Json versions read from the server.
- Shallow clone: run `git config remote.origin.fetch '+refs/heads/*:refs/remotes/origin/*'` before tracking `scaffold`.
- The safety copy moves to `C:\Users\Admin\EmbySafetyCopy\` in v10 (v9 review Sec S4).

## Waiting on the owner
- **R1 widening (card 0005 seen 6):** ask the owner whether R1 in CLAUDE.md may add "also walk with something the plugin didn't make inside each folder, and several starts at once where the first run ends quickly". Until approved, apply it as card 0005's advice.

## Next job
Spec v10: fix v9 Blockers 2 then 1, then Should-fix and Nits, then the fresh re-review. Read first: CLAUDE.md (R1), card 0005, docs/reviews/2026-11-08-spec-v9-review.md, docs/reviews/2026-11-08-spec-v8-handling.md, docs/decisions.md (D51–D55), docs/spec.md, docs/data.md, docs/checks.md, docs/modules.md, docs/releases.md, the part READMEs.

## Cards that applied
0005 (seen 6), 0006 (handover on the unmerged branch). No new card this job.
