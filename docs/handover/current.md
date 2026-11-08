# Handover

## Job just finished
Spec v11 written (v10's 2 Blockers fixed: exact path for every action on a found entry, one clear-out before an update's first write, ReadOnly cleared; all Should-fix and Nits; R1 walk, D57), re-reviewed by three fresh agents: **not passed, 3 Blockers** (docs/reviews/2026-11-08-spec-v11-review.md). Then the **owner decided D58: no more in-place updates; every copy is rebuilt fresh each week.**

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v11 is commit 414d163; the v11 review, D58 and this handover are the commits after.
- main holds pull request 2, an older parallel scaffold; **pull request 1 is the real line**. Never work from `stage4/scaffold` (card 0006).
- No plugin code exists anywhere, on purpose.

## What is still owed (smallest first)
1. Spec v12, in this order:
   a. **Apply D58 first, by removing, not adding.** The swap has only `delete` and `add`. Plan (F4 step 5): delete every managed copy, add every pick. Prepare (step 6): every pick's whole copy in staging, theme video included. Remove: the `update` operation and everything only it needed (the clear-out, `.tpp-tmp`, in-place replacing writes, the skipped-update `stale` files-list rule, wrong-kind entries inside copy folders, F5's update resume, F3 "a title that stays keeps its folder name"). A stayer's new folder gets ` - 2` (or back to no suffix) from F3's existing name-clash rule, because last week's folder is being deleted this run, so Emby always sees a new path. `stale` now only comes from a skipped delete. Then go through data.md, checks (C84, C85 and any check naming an update), the Run and Store READMEs, modules.md and D44/D53 for anything that mentions an update.
   b. Then the v11 Blockers that still apply: **1** Store's replacing writes go back to `File.Replace` (target exists) or `File.Move` (it doesn't), ReadOnly cleared first; Run no longer replaces any file. **2** the waiter's re-check lists its own conditions and never the holder test. **3** a read-only link in Store's folders: clear ReadOnly on the link itself (test that its target is unchanged) or put the acceptance to the owner.
   c. Then the v11 Should-fix and Nits that still apply after D58 (the review file marks the areas; several, such as "making folders" and "the new set", go away with updates). Write `docs/reviews/2026-11-08-spec-v11-handling.md`, saying which findings D58 removed.
   d. R1 walk (card 0005, seen 8): every new or changed rule against the step list, run.json, data.md, the walls **and the decisions** (D14 netstandard2.0); delete and add and each resume point; odd, linked, wrong-kind and read-only things in each folder, the Top Picks folder itself included; several quick starts.
2. Re-run the three fresh reviewers on spec v12. If none finds a Blocker, merge pull request 1 (D27, after build and tests pass). Stage 4 then passes.
3. Stage 5 readiness review; Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check in docs/security.md "Secret scan" on the staged changes. Nothing printed (exit 1) means clean.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31). The plugin targets **netstandard2.0** (D14): check any API against it before writing it into the spec (v11 Blocker 1 came from skipping this).
- **Don't edit R1's wording in CLAUDE.md** without the owner's OK; card 0005's `seen` count lives on the card, not in R1.
- D58 is a removal job: if v12 grows longer than v11, something has gone wrong.
- A skipped delete plus the stayer's fresh add means two copies of one title for a week (the stale one sorts last with the stale date, D53; the row check calls that `partial`). Check the spec says so.
- Owed to build chunks, not v12: ban fixtures and empty-passing wall tests (card 0002), the meta-test and built-features.txt, SkiaSharp and System.Text.Json versions read from the server.
- Shallow clone: run `git config remote.origin.fetch '+refs/heads/*:refs/remotes/origin/*'` before tracking `scaffold`.

## Waiting on the owner
- Nothing blocking. Possible later question: card 0009 (simplify an area after two rounds of new Blockers) may be worth proposing as a standing rule if it proves itself again.

## Next job
Spec v12: apply D58 (delete and add only), then the v11 Blockers that remain, then Should-fix and Nits, then the R1 walk and the fresh re-review. Read first: CLAUDE.md (R1), D58, cards 0005 and 0009, docs/reviews/2026-11-08-spec-v11-review.md, docs/reviews/2026-11-08-spec-v10-handling.md, docs/spec.md, docs/data.md, docs/checks.md, the Run and Store READMEs.

## Cards that applied
0005 (seen 8), 0006 (handover on the unmerged branch). New card 0009.
