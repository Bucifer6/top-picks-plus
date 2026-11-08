---
summary: Fresh-reviewer re-review of spec v9 (comprehension, builder readiness, security). Not passed: 2 Blockers, both narrow. For spec v10.
links: [../spec.md, ../checks.md, ../data.md, ../modules.md, ../security.md, ../releases.md, 2026-11-08-spec-v8-handling.md]
---

# Spec v9 re-review, 2026-11-08

Reviewers: **Comp** (1 Blocker, 4 Should, 3 Nit), **Build** (1 Blocker, 7 Should, 10 Nit), **Sec** (0 Blocker, 4 Should, 5 Nit). Run at the same time on commit 4c69da6. Stage 4 **not passed**; pull request 1 stays unmerged.

All three agree every v8 finding is in the v9 text. Sec found no realistic route to real media, another library, watch history or resume points; hard links can't cross from C: to N:/K:; every stop point of delete, update, add and Store writes is safe. Build found no wall crossed and data.md matching the spec.

## Blockers
1. **Waiting starts after a fast run (Build B1, Comp S1).** Starts waiting on the guard keep waiting up to 30 s. If the first run ends inside that time, the next waiter gets a free lock, passes the re-check and runs a second run (contradicts C22, C73); and the guard run at the end of that run sees the flag still true and stops, so if the last waiter then times out nobody runs the guard again. Fix: a waiting start is turned down at once when a run (holder `run`) takes the lock.
2. **Check 2 blocks deletes of things the plugin didn't name (Comp B1, Build N10).** Check 2 (single names) runs before every delete, so a file someone put inside a copy folder with `~` or a trailing space (an Office `~$` lock file) skips the delete or update every week: the copy stays `stale` for ever. Fix: check 2 applies only to names the plugin writes (copy folder, planned file paths); deleting what is found inside a managed folder uses checks 3 and 4 only.

## Should fix
- **Guard work lost (Comp S2, Build N9):** drop the self-change filter (the loop ends by itself: a pass that changes nothing makes no events); a trigger during a pass, or a start that is turned down after the guard stopped for it, means "one more pass when this one ends".
- **Check 4 repeated (Sec S1):** repeat the "on the way" check just before each write, rename, delete, folder listing and the add's move (cheap attribute reads); accept the split-second race left.
- **Create new, never write into an existing file (Sec S2, Build S4):** one rule for every file the plugin or Store writes, including the copy to run.previous.json (use the atomic write), resumed backups (an unfinished backup folder is deleted and made again) and Run's staging writes; a linked data file sets its pending flag (Build N10); C76 tests it.
- **Links inside a staging folder (Build S2):** before an update's or add's first change, list its staging folder; a link anywhere in it skips the operation.
- **Update with a missing folder (Comp S3):** copy folder or staging folder missing → skipped, entry `stale`.
- **Error after step 12's run.json write (Comp S4):** only logged; the run stays as recorded; lock released; guard runs once.
- **Admin check (Sec S3):** also `[Authenticated(Roles = "Admin")]` on every route; add a logged-out admin token and a disabled admin to C26 or C34.
- **Safety copy readable by other accounts (Sec S4):** keep it in `C:\Users\Admin\EmbySafetyCopy\`.
- **C73 wording (Build S1):** "waits for the item date in progress".
- **EmbyBridge actions (Build S3):** look up a source item's runtime and series; the Emby version; the server's own time zone; which library a pin is in.
- **Tests (Build S5, S7):** `..` path in C59 (L, B); C19 automatic (R, B): only `Emby.TopPicks` refused.
- **Same-minute backup folder (Build S6):** a complete folder for this minute is kept and the backup skipped, for every run.

## Nits
Comp N1 (flag set again while already true: accept, note in C82), N2 (a skipped update's `skipped` line goes in the `done` write; a retried update becomes `active`), N3 (Run now's answer while it waits). Build N1 (empty season folders removed), N2 (rank and rankDateUtc null exactly when `stale`), N3 (file symlink via `File.CreateSymbolicLink`), N4 (30 s wait on the fake clock), N5 (step 1 guard: "missing beside a copy-looking folder"), N6 (checks.md SkiaSharp tense; tests README parts), N7 (modules.md rows; stray "(F5); Before" in the EmbyBridge README), N8 (no test of root wiring), N10 (same-minute damaged-file names). Sec N1 (folder link: non-recursive `Directory.Delete`; file link: `File.Delete`), N2 (`File.Replace` when the old file exists, else `File.Move`), N3 (OneDrive placeholders carry the reparse attribute: accept), N4 (more log paths concern a user: step 11's users, check user, visibility action, admin check), N5 (a backup folder that is itself a link is removed as a link).

## Lesson
The R1 walk followed each operation over **the plugin's own files** and **one start at a time**. Both Blockers sat outside that: a file the plugin didn't name, and several starts with a run that ends quickly (card 0005, seen 6).
