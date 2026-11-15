# Handover

## Job just finished
Proof plugin built (proof/TopPicksProof) on branch build/proof-plugin, with its CI `proof` job. Decisions D129–D133, backlog B4, B20, B25 done, CODEMAP.md added, cards 0017–0018.

## Where it stopped
Stage 6 Build. Pull request for build/proof-plugin (link in the PR list). The SDK can't read Playback Reporting (D129), so the proof only proves (a) loading in 4.10.2 and (c) .nfo date added. Nothing of Top Picks Plus is on the owner's server.

## What is still owed (smallest first)
1. Owner confirms the server steps in proof/TopPicksProof/README.md (B4), merges the PR, downloads `TopPicksProof-0.0.0-proof.<run>` from the PR's CI run (kept 3 days: re-run CI if it expired), runs the steps and pastes the results.
2. Claude records the results as a dated row in decisions.md and acts on them as D132 says, **before chunk 4 is kept**.
3. Chunk 4 (EmbyLink): new branch from main; read the open EmbyLink backlog rows first; delete proof/ and the `proof` job in the same PR (D130); supply `CopyContents` and `Reprepare` (D124).
4. Then chunk 5 (Settings) (D92).

## Watch out for
- **Draft pull requests don't run CI (D115)**, because a draft is work in progress: open it as a normal PR.
- **The repo is public:** never commit secrets or personal details, because anyone can read it. When recording proof results, leave out user names and file paths.
- **No local builds (D5),** because the owner chose CI as the only build; this can include sessions running on the owner's own PC, which is also the Emby server (D133). Never touch Emby's folders from a session.
- **Look up Emby API signatures before writing chunk 4 code** (card 0018), because nothing compiles until CI runs: Emby.SDK repo SampleCode targets 4.9.1.90; dev.emby.media/reference has members.
- **First-week import is "last played" only (D129),** because the SDK has no SQLite library; don't plan any Playback Reporting read.
- **Windows-only tests are named `_junction`/`_windows`**; the Windows job fails unless at least 11 pass (D125), so raise the number when adding more.
- **Windows full paths expand 8.3 short names** (card 0015), so never trust a "direct child" check on a name not matched exactly on disk (D119).
- **Deliberate breaks must compile** (card 0013); **no namespace-looking string constants in walled modules** (card 0012), because each cost a CI run.
- **No backup (D95, owner).** Never write "restore from backup", because there is none.
- **Each builder reads the open backlog rows for its chunk first** (D92). Mark amended decisions "[Amended by …]" and fix every README that repeats them (card 0007), because builders read top-down.
- Commit as Claude (noreply@anthropic.com); a stop hook checks. There is no GitHub CLI on the owner's PC: push works through git; PRs are opened in the browser pane while the owner is signed in.

## Waiting on the owner
- Confirm the server steps (B4), merge the proof PR, run the proof and paste the results.

## Next job
After the results: chunk 4 (EmbyLink). Read first: CLAUDE.md, CODEMAP.md, src/TopPicksPlus/EmbyLink/README.md, docs/decisions.md (D129–D133 and the results row), docs/backlog.md EmbyLink rows.

## Cards that applied
0001, 0003, 0006, 0007, 0008 (corrected), 0011 (corrected), 0012–0015; new: 0017, 0018. None retired.
