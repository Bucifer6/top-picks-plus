# Cold quiz answers (2026-11-08, start of spec v6 job), from current.md only

1. Branch `scaffold`, pull request 1, not merged. It may be merged (Claude may, per D27) only after the three goldfish (comprehension, builder readiness, security) re-review spec v6 and none finds a Blocker; Stage 4 then passes.

2. Blocker 5: whether Emby 4.10.2 shares watched state between a Top Picks copy and its real title. It depends on how the real Emby server stores data, which fake adapters/docs can't prove (card 0008). Owner was asked: quick look — pick a film in today's Latest Top Picks row that they've watched in the Movies library; does the copy show watched too? Then, to be sure, on one test film mark the copy unwatched and see if the real film turns unwatched (then mark it watched again). It is now answered: yes, shared; owner chose D39 (share it, like today).

3. Order: (a) fix Blockers 1, 2, 3, 4, 6, then apply D39 (which settles 4 and 5); (b) Should-fix, then Nits from the v5 review, writing docs/reviews/2026-11-08-spec-v5-handling.md; (c) apply R1 to every new/changed rule. Then the goldfish re-review. The five: 1 row-check verdict vs C39; 2 store the full planned entry in each run.json operation; 3 resume keeps run.json, pending flags not re-copied; 4 resume point only if higher, first episode only, nothing if source played; 6 back up plays.json with `"today": []` and add a restore check. (D39 removes F7 and the F4 step 9 played-state reset, so Blocker 4 goes away.)

4. build.yml now fails the build when a lock file is missing. Before, it had a fallback that printed lock files to the log; that's gone.

5. NetArchTest bans by name prefix, so banning `System.IO.File` also bans `System.IO.FileNotFoundException`, which SkiaSharp error handling might catch. Ways out: Artwork catches plain `Exception`, or make the ban exact.

6. R1 (2026-11-08, from card 0005, owner-approved): after any spec change, check every new or changed rule against the F4 step list, run.json `step`/`status`, data.md and the part walls before asking for a review.

7. The pattern check on staged changes: `git diff --cached -U0 | grep -E '^\+' | grep -nEi '<secret patterns>'`. Nothing printed (grep exit 1) means clean. Don't download gitleaks.


## Marked by a fresh agent (start of spec v6 job)
7/7 right. Marker noted: handover item 3a was unclear on Blocker 4 (D39 removes it; it should not be fixed first); answer key Q1 adds "build and tests passing" from CLAUDE.md, not current.md.
