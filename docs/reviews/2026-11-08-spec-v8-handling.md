---
summary: How each finding of the spec v8 re-review was handled in spec v9 (D55), and the R1 walk done before the v9 review.
links: [2026-11-08-spec-v8-review.md, ../spec.md, ../checks.md, ../data.md, ../decisions.md, ../releases.md]
---

# Spec v8 review – how it was handled (spec v9)

## Blockers
| # | Finding | Handled |
|---|---------|---------|
| 2 | C39 called a missing pick both `partial` and `failed` | C39 rewritten: only out-of-order picks, an unmatched item or a stale copy before a pick are `failed`. F4 step 11 says a missing pick or a stale copy after the picks is never `failed` |
| 1 | An update wrote before removing links inside its folder | F4 step 7 update: a **link sweep** of the whole folder comes first, before any write; each replaced file is checked again just before its rename; a staging file that is a link fails check 4. F9 check 4 says the same. C27 adds the `Season 1` junction and `Heat (1995).nfo` symlink cases (D55) |

## Should fix
| Finding | Handled |
|---------|---------|
| Store's own writes (Sec 1) | data.md common rules: `.tmp` created new (an old one deleted, as a link if it is one); live file checked not a link before the replace; a data file that is a link is never read (counts as missing) |
| Lock hand-off (Comp 4, 5; Build 3; Sec 5) | Run lock records its holder (`run` / `guard`); one start-waiting flag; a start waits for the guard at most 30 s, then "a run is already in progress"; the guard checks the flag before each entry and each item date; one guard pass at a time, a trigger during a pass is dropped (F1, F5, C73) |
| Guard re-arms itself (Sec 4) | EmbyBridge ignores a library change to an item the plugin itself changed in the last 2 minutes (F5, EmbyBridge README, C73) |
| Guard after a start that never became a run (Comp 6, Build 15) | F5: every ending (finished, incomplete, refused, failed, gave up, could not start) releases the lock then runs the guard once; said again at step 1, the error paragraph and resume's give-up |
| Pending flags at step 12 (Comp 3, Build 4) | Step 12: one `IStore` call, made by Run, clears only the flags copied as true; C82 adds the case |
| Skipped operations and resume (Comp 2) | F4 step 7: managed.json first, then run.json `done`; a skipped operation is marked `done` with its note. data.md `operations.done` says so. Walked below |
| Checks on folders that don't exist (Comp 8) | F9 check 4: a part that doesn't exist yet is not a link and passes |
| Emby actions not named (Build 7) | EmbyBridge README: find a copy item by path, list every episode under a copy series, read date added (and date last media added) |
| Top Picks path as Emby holds it (Build 8, Sec 12) | C34 reads the Top Picks library's folder string and the path form of copy items |
| Test layers (Build 5, 6) | C50 gets S; B added to C12 (played state, drops before wiring), C31 (start-up timers), C32 (library-change debounce), C53 (00:05 timer); the checks.md sentence about EmbyBridge tests corrected |
| Emby safety copy (Sec 2, 3; Build 17) | releases.md: copy adds `plugins\configurations`, lives in `C:\EmbySafetyCopy\YYYY-MM-DD\`, the 2 newest kept, written restore steps (rename aside, never merge, copy back whole, then empty top-picks and delete managed.json). security.md says these copies hold user IDs and full watch history. C81 follows the steps |

## Nits
| Finding | Handled |
|---------|---------|
| Comp 9 empty row | No picks and an empty row passes (F4 step 11, C39) |
| Comp 10 stale series' new episode | Row-check matching falls back to "path inside a managed folder"; such an episode counts as its stale copy |
| Comp 11 files union | One entry per path; planned `sourceItemIds`, old `itemId` |
| Comp 12 add's entry state | Stored `active` in run.json, written `incoming` first (F4 step 7, data.md) |
| Comp 13 `staging\` itself a link | `staging\` itself is "on the way" in check 4 |
| Comp 14 "within seconds" | Gone: a start now waits for one item's date change at most, 30 s limit |
| Comp CLAUDE.md, Build 9 stage line | Updated |
| Build 10 W layer and Cecil | W wording covers C49's Cecil scan |
| Build 11 tests README | Lists B and W layers and NSubstitute, Mono.Cecil |
| Build 12 Run README | Copy-looking folders, link sweep, start-waiting flag, one guard pass at a time added |
| Build 13 start-list limits | New check C83 (layer R) |
| Build 14 "not inside programdata" | Removed: the Top Picks folder is defined under programdata, so it could never fail |
| Build 16 SkiaSharp and Settings ban fixtures | Owed to the build chunks with the other ban fixtures (handover) |
| Sec 6 `.` / `..` parts | Path tidy: a path with a `.` or `..` part is never inside anything |
| Sec 7 short-name aliasing | Accepted: managed names can't hold `~`, so none can be or collide with a Windows short name |
| Sec 8 UNC / subst; libraries at `staging\` | UNC and subst accepted (deletes stay inside managed folders, D52). A library inside or containing the plugin data folder is now refused (F9, C50) |
| Sec 9 locked plays.json roll-up | Accepted: the next timer or run tries again; today's entries wait one more roll-up |
| Sec 10 native SkiaSharp crash | Accepted, as before |
| Sec 11 fixed path for the safety copy | `C:\EmbySafetyCopy\`, with `plugins\configurations` included |

## R1 walk (every operation kind and each restart point)
New or changed rules walked: link sweep; replace-time link check; write order and skipped = `done`; missing parts pass check 4; start-waiting flag and 30 s wait; one guard pass; self-change filter; flags cleared only if copied; Store's link-safe writes.

- **Delete**, folder with a junction inside: check 4 on the way passes; inside, the junction is removed as a link, the folder deleted, entry removed, then `done`. Stop after entry removal, before `done`: resume finds folder and entry gone, removes nothing, marks `done`. Folder already gone on resume: check 4 passes (missing), entry removed. ✓
- **Update**, stayer with junction `Season 1` and symlink `Heat (1995).nfo`: link sweep removes both before any write; `.tpp-tmp` written new; target is no link at rename; old files outside the new set deleted; entry replaced, then `done`. Stop mid-write: resume sweeps again (nothing left), deletes leftover `.tpp-tmp`, rewrites. A file that can't be written: entry → `stale` (files union), then `done` with note; stop before `done` → resume tries the update again, ends `active` or `stale` again: both safe. Staging folder gone on resume: skipped, `stale`, `done`. ✓
- **Add**, new title: `incoming` written; checks run (target doesn't exist: passes; `staging\` and its folder not links); move; `active`; then `done`. Stop after move before `active`: resume sees folder in place, gone from staging → `active`. Target exists while staging is there: skip, `incoming` removed if present, `done` with note. ✓
- **Restart points:** `guards`–`plan` (nothing new touches them; flags only added); `prepare` (Store empties staging, replacing a linked `staging\` with a real folder); `swap` (walked above); `rescan`–`check` (date sets are plugin changes, so their library events are ignored; rescan's new-item events start the guard only after the run, since the guard stops while a run holds the lock); `done` between run.json and state.json (flags stay, cleared next run: harmless).
- **Starts against the guard:** Run now while the guard is on a big stale series: flag set, guard stops before its next item, the start goes ahead well inside 30 s. Ten starts: one goes ahead, nine turned down after their re-check. Resume never meets the guard (the guard doesn't start while a run is waiting). A refused run: lock released, guard runs once, finds no start waiting.
- **Store writes** of managed.json during the swap and of state.json at step 12: `.tmp` new, live file checked; a linked live file is replaced by a real one, never written through.

No clash found with run.json `step`/`status`, data.md or the part walls.
