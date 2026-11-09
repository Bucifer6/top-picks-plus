# Handover

## Job just finished
Chunk 2 (Storage) built, reviewed (no Blockers; review fixes S1–S4 and nits done, fix-check run) and ready for the owner to merge.

## Where it stopped
Stage 6 Build. Pull request #8, branch build/chunk2-storage: Storage code in src/TopPicksPlus/Storage, tests in tests/TopPicksPlus.Tests/Storage, small Logic changes (D122 subtitle suffix cap, D123 `RunRules.TakeoverNeeded`), decisions D117–D128, backlog B14, B15, B31, B39, B40, B42, B45 done and B46–B47 opened, cards 0015–0016 and this handover. CI green on d1fdad1 (run 42615760262: 520 tests on Windows, 12 Windows-only tests ran; Stryker 89.47%). Later commits are docs only, so CI skips them on purpose (paths-ignore).
Still nothing on the owner's server.

## What is still owed (smallest first)
1. Owner merges #8. Main's own CI is red until then (run 42585800882: Stryker `--since:HEAD~1` didn't resolve after #7); the fix is in #8 (D125).
2. Proof plugin (proof/TopPicksProof, D76, D77): new branch from main, fresh review. Open Proof rows first: B4 (owner's steps on the server: the builder drafts them, the owner confirms), B20, B25. It is the one thing that goes on the server before everything is merged (D63, D76).
3. Then chunk 4 (EmbyLink), chunk 5 (Settings) (D92).

## Watch out for
- **Draft pull requests don't run CI (D115).** Open as a normal PR, or mark it ready.
- **The repo is public:** never commit secrets or new personal details. A PR that changes tests is a full Stryker run (card 0014); dotnet can't run here, so CI is the only build.
- **Windows-only tests are named `_junction`/`_windows`**; the Windows job fails unless at least 11 pass (D125). Raise the number when adding more.
- **Windows full paths expand 8.3 short names** (card 0015): never trust a "direct child" check on a name that wasn't matched exactly on disk (D119).
- **Timers in tests:** a short timer on the shared thread pool can fail on a busy Windows runner; use a dedicated thread and a generous retry budget.
- **Deliberate breaks must compile** (card 0013); **no namespace-looking string constants in walled modules** (card 0012).
- **Interface owed by chunk 4:** EmbyLink supplies `CopyContents` and a `Reprepare(name, into)` callback (D124).
- **No backup (D95, owner).** Never write "restore from backup".
- **Each builder reads the open backlog rows for its chunk first** and fixes or decides them (D92). Older decisions are marked "[Amended]"/"[Replaced]" (card 0007); fix every README and check that repeats an amended rule.
- Commit as Claude (noreply@anthropic.com), a stop hook checks. NuGet blocked; Actions artifacts can't be downloaded here (403). The shallow clone fetches only main: use an explicit refspec for other branches.

## Waiting on the owner
- Merge #8.

## Next job
Build the proof plugin. Read first: CLAUDE.md, proof/TopPicksProof (README), docs/decisions.md (D62, D63, D76, D77, D79, D87), docs/checks.md (C62, C76), docs/backlog.md Proof rows.

## Cards that applied
0001–0014; new this job: 0015, 0016.
