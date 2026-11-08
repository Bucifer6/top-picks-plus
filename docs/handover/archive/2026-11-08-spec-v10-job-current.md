# Handover

## Job just finished
Spec v10 written (v9 Blockers fixed: found entries deleted by exact name, check 2 only for names the plugin writes; waiting starts turned down when a run takes the lock; all Should-fix and Nits, D56; docs/reviews/2026-11-08-spec-v9-handling.md with the R1 walk), then re-reviewed by three fresh agents: **not passed, 3 Blockers** (docs/reviews/2026-11-08-spec-v10-review.md).

## Where it stopped
- Stage 4 (Scaffold), still not passed.
- Branch `scaffold`, pull request 1 open and **not merged**. Spec v10 is commit 4476e6b; this handover and the v10 review are the commit after.
- main holds pull request 2, an older parallel scaffold; **pull request 1 is the real line**. Never work from `stage4/scaffold` (card 0006).
- No plugin code exists anywhere, on purpose.

## What is still owed (smallest first)
1. Spec v11, in this order:
   a. **Blocker 1 (exact-name form everywhere).** Why: v10 made only the *delete* of a found entry use the `\\?\` path, so a junction named `Season 1 ` (trailing space) pointing at `N:\Movies` reads as "not there", passes check 4, and the delete then lists through it and deletes real media. Fix: every action on a found entry and everything below it (list, attributes, link test, link removal, delete) uses the same `\\?\` path; link-or-not comes from that path or the listing's own entry.
   b. **Blocker 2 (wrong-kind and read-only found entries).** Why: a file called `Season 1`, a folder called `Heat (1995).nfo` or `x.tpp-tmp`, or a read-only file or folder makes the update fail every week, so the copy stays `stale` for ever. Fix: before the first write, remove (by exact name) any found entry on a planned path that is the wrong kind; clear ReadOnly before deleting or replacing.
   c. Then the Should-fix, then the Nits, from the v10 review. Write `docs/reviews/2026-11-08-spec-v10-handling.md`. Add the missing checks it lists.
   d. R1 walk as card 0005 now says (seen 7): also walk **every action** on a thing whose access a rule changes (list, attributes, link test, write, delete), and a wrong-kind and a read-only entry.
2. Re-run the three fresh reviewers on spec v11. If none finds a Blocker, merge pull request 1 (D27, after build and tests pass). Stage 4 then passes.
3. Stage 5 readiness review; Stage 6 build in chunks; Stage 7 review; Stage 8 release.

## Watch out for
- **Before every commit**, run the pattern check in docs/security.md "Secret scan" on the staged changes. Nothing printed (exit 1) means clean.
- **Nothing is compiled here or on the owner's PC.** Only GitHub Actions builds (D16), on Windows (D31).
- **Don't edit R1's wording in CLAUDE.md** without the owner's OK; card 0005's `seen` count lives on the card, not in R1.
- The Builder reviewer found the waiting-start fix sound only if a turned-down waiter leaves the waiting count **before** asking for the guard pass (v10 review Should-fix). Easy to miss.
- Prefer removal over new detail: Sec suggests `File.Move(…, overwrite: true)` instead of `File.Replace`, which also drops two Nits.
- Owed to build chunks, not v11: ban fixtures and empty-passing wall tests (card 0002), the meta-test and built-features.txt, SkiaSharp and System.Text.Json versions read from the server.
- Shallow clone: run `git config remote.origin.fetch '+refs/heads/*:refs/remotes/origin/*'` before tracking `scaffold`.

## Waiting on the owner
- Nothing blocking. One honest note for the owner: every review round since v3 has found new Blockers, mostly in the copy-folder file handling. If v11 doesn't pass, it may be worth asking the owner whether a simpler copy-handling design is wanted rather than more rules.

## Next job
Spec v11: fix v10 Blockers 1 then 2, then Should-fix and Nits, then the fresh re-review. Read first: CLAUDE.md (R1), card 0005, docs/reviews/2026-11-08-spec-v10-review.md, docs/reviews/2026-11-08-spec-v9-handling.md, docs/decisions.md (D56), docs/spec.md, docs/data.md, docs/checks.md, the Run and Store READMEs.

## Cards that applied
0005 (seen 7), 0006 (handover on the unmerged branch). No new card this job.

## Quiz result
Taken cold at the start of the spec v11 job, 2026-11-08, marked by a fresh agent: 7 right, 0 partly, 0 wrong. Marker's note: "3 Blockers" counted the reviewers' raised findings (Comp 2, Sec 1), which merge into the two described; the next handover says the count plainly.
