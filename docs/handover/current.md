# Handover

## Job just finished
The whole build: proof plugin (PR #9), chunk 4 EmbyLink (PR #10) and chunk 5 Settings (PR #11), each reviewed by a fresh agent (no Blockers; Should-fix items fixed; smaller ones in the backlog). The owner chose to run the proof and the checks after the whole build (D134).

## Where it stopped
Stage 6 Build, done. Three stacked pull requests: #9 (base main), #10 (base #9's branch), #11 (base #10's branch). GitHub retargets each to main as the one below merges. Decisions D129–D148, cards 0017–0020. Nothing is on the owner's server yet.

## What is still owed (smallest first)
1. Owner merges #9, then #10, then #11 (in that order; each needs green CI on main's retarget).
2. Owner runs the proof plugin (proof/TopPicksProof/README.md steps; B4) and pastes the results. Claude records them and acts as D132 says (amended by D134) in its own pull request, which also deletes proof/ and the `proof` job (D130).
3. Release v1.0.0 (tag on main, only when the owner says, D57), then go-live day (D86) with the checks in docs/checks.md. Go-live notes: B26, B49, B50, B51, B53, B54.

## Watch out for
- **Stacked PRs:** merge in order. If one is merged out of order, its base may need changing by hand.
- **Draft pull requests don't run CI (D115)**, because a draft is work in progress: open it as a normal PR.
- **The repo is public:** never commit secrets or personal details, because anyone can read it. When recording proof results, leave out user names and file paths.
- **No local builds (D5),** because the owner chose CI as the only build; this session ran on the owner's PC, which is the Emby server (D133). Never touch Emby's folders from a session. Emby signatures can be read, read-only, with a reflection script over the SDK DLLs in the NuGet cache (card 0019, D145).
- **Don't push while CI is running,** because a new push cancels the running Windows job and starts again.
- **CI logs need a signed-in browser:** the public API gives job results and annotations but not the compiler output; open the job page in the browser pane and expand the failed step.
- **There is no GitHub CLI on the owner's PC:** push works through git; PRs are opened in the browser pane while the owner is signed in to GitHub.
- **Watched state only moves forward; an offline drive or vanished title never empties the row (D147),** because the owner's real library and watched ticks are at stake.
- **Windows-only tests are named `_junction`/`_windows`**; the Windows job fails unless at least 11 pass (D125), so raise the number when adding more.
- **No backup (D95, owner).** Never write "restore from backup", because there is none.
- Commit as Claude (noreply@anthropic.com); a stop hook checks.

## Waiting on the owner
- Merge #9, #10, #11 in order; confirm the proof steps (B4); run the proof.

## Next job
Record the proof results (D132, D134), then the v1.0.0 release and go-live day (D86). Read first: CLAUDE.md, CODEMAP.md, docs/decisions.md D129–D148, docs/backlog.md Go-live rows.

## Cards that applied
0001, 0003, 0004, 0006, 0007, 0012–0015, 0017, 0018; new: 0019, 0020. None retired.
