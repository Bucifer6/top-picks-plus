---
summary: How each finding of the spec v9 re-review was handled in spec v10 (D56), and the R1 walk done before the v10 review.
links: [2026-11-08-spec-v9-review.md, ../spec.md, ../checks.md, ../data.md, ../decisions.md, ../releases.md, ../modules.md]
---

# Spec v9 review – how it was handled (spec v10)

## Blockers
| # | Finding | Handled |
|---|---------|---------|
| 2 | Check 2 blocked deletes of files the plugin didn't name (an Office `~$` file kept a copy `stale` for ever) | Fix. New words **found entry** (anything the plugin finds by listing) and **deleted by exact name** (full path in the `\\?\` form, so Windows can't trim a trailing dot or space and look for a different name). F9 check 2 now covers only names the plugin writes; found entries use checks 3 and 4. F4 step 7 delete and update use it; Store's deletes too. New check C84 |
| 1 | Waiting starts after a fast run gave a second run and could leave the guard never re-run | Fix. F1: a waiting start ends in exactly one of three ways; when a start takes the lock as `run`, every start still waiting is turned down at once; a waiter turned down by its own re-check never held the lock as `run`, so others keep waiting; every waiting start turned down asks for one guard pass. The flag is false as soon as nobody waits. C73 rewritten (including "the item date in progress" and the fast-run case) |

## Should fix
| Finding | Handled |
|---------|---------|
| Guard work lost (Comp S2, Build N9) | Fix. Self-change filter removed (a pass that changes nothing starts nothing); a trigger during a pass means one more pass (F5, EmbyBridge README, C73) |
| Check 4 repeated (Sec S1) | Fix. F9 check 4 is made again just before each write, rename, delete, folder listing and the add's move; the split-second race left is accepted at level 1 |
| Create new, never write into an existing file (Sec S2, Build S4) | Fix. One definition, **created new** ("Words used here"), used by Run's staging and update writes and by every Store write (data.md common rules). run.previous.json is written that way at step 1; an unfinished backup folder is deleted and made again; a data file that is a link counts as missing and sets its pending flag (plays.json, state.json). C76 extended |
| Links inside a staging folder (Build S2) | Fix. Before an update's or add's first change its staging folder is listed; a link anywhere in it skips the operation (F4 step 7, F9 check 4) |
| Update with a missing folder (Comp S3) | Fix. Copy folder or staging folder missing: update skipped, entry `stale` (F4 step 7) |
| Error after step 12's run.json write (Comp S4) | Fix. Only logged; run stays as recorded; lock released; guard runs once (F4 step 12) |
| Admin check (Sec S3) | Fix. Every route also carries `[Authenticated(Roles = "Admin")]` (F8, EmbyBridge README); C26 adds a signed-out admin token, a disabled admin and no token |
| Safety copy readable by other accounts (Sec S4) | Fix. `C:\Users\Admin\EmbySafetyCopy\` (releases.md, security.md, D56) |
| C73 wording (Build S1) | Fix (see Blocker 1) |
| EmbyBridge actions (Build S3) | Fix. README adds: a source item's runtime and series, the Emby version, the server's own time zone, a pin's library |
| Tests (Build S5, S7) | Fix. C59 adds a `..` path; C19 becomes automatic (R, B): only `Emby.TopPicks` is refused, not `Emby.TopPicksExtra`, `TopPicksPlus` or a display name containing "Top Picks" |
| Same-minute backup folder (Build S6) | Fix. F4 step 3: a complete folder for this minute is kept and the backup skipped, for every run; an unfinished one is made again (F5 resume points to it; data.md backups) |

## Nits
| Finding | Handled |
|---------|---------|
| Comp N1 flag set again while already true | Fix: noted as accepted in C82 |
| Comp N2 skipped update's `skipped` line; retried update | Fix: written in the same run.json write as `done`; an updated copy's state is `active` (a stale one becomes active again) |
| Comp N3 Run now's answer while it waits | Fix: answers when its wait ends, at most 30 s (F1) |
| Build N1 empty season folders | Fix: removed after an update's deletes |
| Build N2 rank null exactly when stale | Fix: data.md |
| Build N3 file symlinks in tests | Fix: `File.CreateSymbolicLink` (checks.md test notes) |
| Build N4 30 s on the fake clock | Fix: F1 and C73 |
| Build N5 step 1 guard wording | Fix: "missing while the Top Picks folder holds a copy-looking folder" |
| Build N6 SkiaSharp tense; tests README | Fix: "will reference … added by the first Artwork chunk"; tests README lists every layer |
| Build N7 modules.md rows; stray text | Fix: modules.md v10 note; stray "(F5); Before" removed |
| Build N8 no test of root wiring | Skip: wiring only runs inside real Emby; the owner's C1 and C31 exercise it. Recorded |
| Build N10 same-minute damaged-file names | Fix: ` -2`, ` -3` added, never written over (F9) |
| Sec N1 how a link is removed | Fix: **removed as a link** defined (non-recursive `Directory.Delete` / `File.Delete`) |
| Sec N2 `File.Replace` / `File.Move` | Fix: in **created new** |
| Sec N3 OneDrive placeholders carry the reparse attribute | Skip: accepted; the folders are never in OneDrive. Recorded |
| Sec N4 more log paths concern a user | Fix: F4 error-logging line lists step 11's users and check user, the visibility action and the admin check |
| Sec N5 backup folder that is a link | Fix: removed as a link (F9 Store's deletes, Store README) |

## R1 walk (before the v10 review)
**Against the step list, run.json, data.md and walls.** Exact-name deletes and created-new writes are plain .NET file access inside Run's and Store's existing walls; Logic's path tidy already strips `\\?\` for comparisons. run.previous.json's write still fails into "could not start" at step 1. A linked state.json is written fresh with `stateResetPending` true, as a damaged one is; a linked managed.json counts as missing, so the copy-looking-folder rule refuses runs as before. The step 12 error rule uses no new run.json field. The admin attribute lives in EmbyBridge (Emby SDK allowed). No wall changed.

**Every operation and restart point.** Delete: folder name checked (plugin wrote it); listing, link removal and exact-name deletes each preceded by check 4; a resumed delete with the folder gone removes the entry. A found file that is locked (open in Office) still can't be deleted: the delete is skipped and the copy stays `stale` that week, reported; accepted. Update: staging listed for links, copy folder and staging present, link sweep, leftover `.tpp-tmp` deleted, writes created new, found entries not in the new set deleted. **Gap found and fixed:** "not in the new set" must compare ignoring case, or a file whose name differs only in case would be deleted right after being written. Resumed update: the sweep and writes repeat safely; missing staging now skips it as `stale`. Add: `incoming` entry, staging listed, check 4 before the move. **Gap found and fixed:** a loose file or link with the target folder's name blocked the move without being named in the clash rule; the clash rule and the add's existing-target rule now say "folder, file or link". Restart points: `guards`–`plan` (backup follows the same-minute rule), `prepare` (Store empties staging by exact name), `swap`, `rescan` onwards: no new rule changes what they do.

**Something the plugin didn't make, in each folder.** Top Picks folder: unmanaged folders, loose files and links are never touched, and now block a clashing name. Copy folder: odd names deleted by exact name (C84), links swept. Season folder: someone's empty folder is removed with the empty-folder rule (inside a managed folder only). `staging\`: a link under a copy's staging folder skips that operation; a link directly in `staging\` is removed as a link when Store empties it; an odd file in a copy's staging folder moves in with the folder and goes at the next update (inside a managed folder). `backups\`: a non-matching name is kept; a linked backup folder is removed as a link; odd files inside are deleted by exact name when pruned. Data folder: a link named like a data file is removed and counts as missing; odd `*.tmp` files go at start-up.

**Several starts at once where the first run ends quickly.** Guard working; S1, S2, S3 arrive; flag true; guard stops after its item date. S1 gets the lock, re-checks, records `run`: S2 and S3 are turned down at once and each asks for a guard pass, which sees `run` and stops at once. S1 is refused within a second, releases, runs the guard: nobody waits, so a full pass runs; a pass request arriving during it gives one more pass. Timeout instead: S1 times out, the flag goes false, the stopped guard (or the one-more-pass) carries on. **Gap found and fixed:** a waiter whose own re-check turns it down (say a run became waiting to resume) must not count as a run taking the lock; it lets go and the others keep waiting. Resume at +3 minutes can't meet a waiting start on the guard, since the guard doesn't run while a run is waiting to resume.
