# Handover

## Job just finished
2026-11-16: PRs #9, #10, #11 merged. The owner installed the proof plugin: it loads on 4.10.2, but its result boxes were missing because Emby hides `[ReadOnly(true)]` fields (B55); fixed in the proof and in the settings page's report box (merge bc11770, proof build 136 confirmed on the server). Then, at the owner's request, v1.0.0 was tagged before the proof (D149).

## Where it stopped
Stage 6 Build, done; v1.0.0 released. The proof's write and read-back (steps 7–13) have not been run. Nothing of Top Picks Plus is on the owner's server yet; the proof plugin (build 136) is installed.

## What is still owed (smallest first)
1. Owner runs the rest of the proof (proof/TopPicksProof/README.md steps 7–13) **before** installing v1.0.0, and pastes the results. Claude records them and acts as D132 says in its own pull request, which also deletes proof/ and the `proof` job (D130); any code change ships as v1.0.1 (D149).
2. Go-live day (D86) with the checks in docs/checks.md. Go-live notes: B26, B49, B50, B51, B53, B54; also check the settings page's "Last run report" box shows (B55).

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
- Run the rest of the proof (steps 7–13) and paste the results.

## Next job
Record the proof results (D132, D134, D149), v1.0.1 if they need a change, then go-live day (D86). Read first: CLAUDE.md, CODEMAP.md, docs/decisions.md D129–D148, docs/backlog.md Go-live rows.

## Cards that applied
0001, 0003, 0004, 0006, 0007, 0012–0015, 0017, 0018; new: 0019, 0020. None retired.
