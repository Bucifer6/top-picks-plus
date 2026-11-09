# Handover

## Job just finished
Chunk 1 (Logic) built, reviewed (one docs Blocker, fixed) and ready for the owner to merge.

## Where it stopped
Stage 6 Build. Pull request #7, branch build/chunk1-logic: Logic code in src/TopPicksPlus/Logic, tests in tests/TopPicksPlus.Tests/Logic, the CI changes (D105, D115), decisions D101–D116, review fixes, backlog B38–B44, cards 0011–0014 and this handover. CI green on ae02758 (run 42582467934: 355 tests, Stryker 91.21%). Later commits are docs only, so CI skips them on purpose (paths-ignore).
Still nothing on the owner's server.

## What is still owed (smallest first)
1. Owner merges #7.
2. Chunk 2 (Storage): new branch from main, fresh review. Open Storage rows first: B31, B40 (managed.json missing after takeover must re-adopt, C100), B42 (folder name checks, 8.3 short names) and the Storage half of every check on the chunk 2 list.
3. Then the proof plugin, chunk 4 (EmbyLink), chunk 5 (Settings) (D92). Nothing goes on the server until all are merged (D63); the proof plugin is the one exception (D76).

## Watch out for
- **Draft pull requests don't run CI any more (D115).** Open as draft if the permission check insists, then mark it ready, or nothing builds.
- **CI minutes (owner pays):** a PR run costs about 11 billed minutes (Windows counts double); a PR that changes tests is a full Stryker run (card 0014). Keep pushes few; dotnet can't run here, so CI is the only build.
- **Deliberate breaks must compile** (card 0013): warnings are errors, so change a number, not `if (false)`.
- **Walled modules: no namespace-looking string constants** (card 0012), or the wall check fails.
- **No backup (D95, owner).** A damaged state.json is rebuilt from the other files; a damaged managed.json is deleted by the owner and the copies are adopted again. Never write "restore from backup": the owner chose not to keep one.
- **Each builder reads the open backlog rows for its chunk first** and fixes or decides them (D92). Findings below Blocker go to docs/backlog.md; re-reviews check only fixes.
- **Older decisions are marked "[Amended]"/"[Replaced]"** at the start of their row (card 0007). Fix every README and check that repeats an amended rule (the C94 miss this job).
- docs/security.md no longer exists; reviewers check security against D37, D82 and D93–D96.
- This workspace can't download Actions artifacts (403): print samples into the CI log temporarily, then revert. Commit as Claude (noreply@anthropic.com), a stop hook checks. NuGet blocked. The shallow clone fetches only main: use an explicit refspec for other branches.

## Waiting on the owner
- Merge #7.
- Optional: keep or drop the Windows test job (about 11 minutes per PR run). Claude kept it.

## Next job
Build chunk 2 (Storage). Read first: CLAUDE.md, src/TopPicksPlus/Storage/README.md, docs/data.md, docs/decisions.md (D24, D32, D37, D38, D43, D80–D82, D85, D93–D96, D101–D116), docs/checks.md chunk 2 list, docs/backlog.md Storage rows.

## Cards that applied
0001–0010; 0007 seen again (C94). New this job: 0011–0014.
