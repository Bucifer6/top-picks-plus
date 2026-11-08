---
summary: Fresh-reviewer re-review of spec v10 (comprehension, builder readiness, security). Not passed: 3 Blockers from two reviewers, all about found entries. For spec v11.
links: [../spec.md, ../checks.md, ../data.md, ../decisions.md, ../security.md, 2026-11-08-spec-v9-handling.md]
---

# Spec v10 re-review, 2026-11-08

Reviewers: **Comp** (2 Blocker, 5 Should, 5 Nit), **Build** (0 Blocker, 7 Should, 4 Nit), **Sec** (1 Blocker, 1 Should, 6 Nit). Run at the same time on commit 4476e6b. Stage 4 **not passed**; pull request 1 stays unmerged.

All three confirm every v9 finding is in the v10 text. The waiting-start and guard changes hold up for all three (one Build ordering detail below). No wall crossed; data.md matches the spec.

## Blockers
1. **Exact-name form used only for deletes (Sec 1, Comp 2, Build 1).** Listing, attribute reads, link tests and link removal on a found entry may use a normal path, which Windows trims. Sec: a junction named `Season 1 ` pointing at `N:\Movies` reads as "doesn't exist", passes check 4, and the delete then lists through it and deletes real media by exact name. Comp/Build: a folder named `x.` or `x ` makes listing fail, so the copy stays `stale` for ever. Fix: **every** action on a found entry and anything below it (list, attributes, link test, link removal, delete) uses the same `\\?\` path, and link-or-not comes from that path or the listing's own entry. C84 adds folders `Season 1 ` (a junction to a test folder) and `x.` (a real folder with files).
2. **Wrong-kind or read-only found entries still block for ever (Comp 1, Build 2, Comp S3).** A file called `Season 1`, a folder called `Heat (1995).nfo` or `x.tpp-tmp`, or a read-only file or folder (desktop.ini icon) makes the write, `File.Replace` or delete fail every week. Fix: before the first write, remove (by exact name) any found entry on a planned path that is the wrong kind; clear ReadOnly before deleting or replacing. Tests in C84.

## Should fix
- **Which checks apply to Store's found entries (Comp S4):** Run's found entries use checks 1, 3, 4; Store's use its own inside-data-folder and no-link rule (check 3 names only top-picks and staging). Fix D56's wording too.
- **Created new, exact steps (Comp S5, Sec 2, Build N8):** open with `FileMode.CreateNew`; remove a linked target first, then choose Replace or Move; prefer `File.Move(…, overwrite: true)` over `File.Replace` (keeps old ACL, streams; fails on read-only: Sec N3, Build N9). TopPicksPlus.xml into backups and theme.mp4 into staging also created new; C76 adds a link at the backup's TopPicksPlus.xml.
- **Lock holder during a waiter's re-check (Comp S6, Build 4):** holder stays `guard` (newcomers wait) until the waiter records `run`.
- **Flag order (Build 3):** a turned-down waiter leaves the waiting count first (flag false), then asks for the guard pass; C73 tests the timeout path.
- **Guard loop on the real server (Build 5, card 0008):** C34 watches 5 minutes after a guard pass for no further pass.
- **Admin attribute untested (Build 7):** W or B reflection test that every route carries `AuthenticatedAttribute` with Roles `Admin`; Sec N6: a server API key with no user is refused by the plugin's own check (C26).
- **Missing checks (Comp 7, Build 6):** staging-folder link skips an update/add; update with missing folder → `stale` (first time and on resume); error after step 12's run.json write; waiter turned down by its own re-check; add blocked by a loose file or link; leftover `.tpp-tmp` deleted.

## Nits
Comp 8 (data.md flag rows mention linked files), 9 (` -2` placement on damaged names, and pruning recognises them), 10 (staging temp name; whether start-up `*.tmp` tidy is top level only: also Sec 8; TopPicksPlus.xml), 11 (mark D55 partly replaced by D56), 12 (a managed copy folder replaced by a link: accept or remove as a link). Build 9 (sharing violation while Emby reads an .nfo: accept or retry once), 10 (F1 "second press turned down at once" now waits too), 11 (a file or link named like this minute's backup folder). Sec 4 (hard links: safe only because nothing opens an existing file; C76 adds a hard-linked state.json), 5 (`\\?\` only on the plugin's own full path plus one listed name; check 3 runs on that same string), 7 (security.md lists `plugins\configurations`; "standard accounts can't read it").

## Lesson
v10 brought in "found entry" for deletes, but the R1 walk followed the delete call, not every other action on the same entry (list, attribute read, link test). Same pattern as card 0005: a new rule checked at one action, not walked through every action that touches the same thing (card 0005, seen 7).
