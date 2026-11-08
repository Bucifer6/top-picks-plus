---
summary: How each finding of the spec v10 re-review was handled in spec v11 (D57), and the R1 walk done before the v11 review.
links: [2026-11-08-spec-v10-review.md, ../spec.md, ../checks.md, ../data.md, ../decisions.md, ../security.md]
---

# Spec v10 review – how it was handled (spec v11)

The review counted 3 Blockers raised (Comp 2, Sec 1), which merge into the 2 below.

## Blockers
| # | Finding | Handled |
|---|---------|---------|
| 1 | Exact-name form used only for deletes; a junction `Season 1 ` passed check 4 through a normal path and the delete then went through it | Fix. "Found entry" now has one **exact path** rule: every action on a found entry and on everything below it (list, attributes, link test, ReadOnly, link removal, delete, replace) uses the plugin's own folder path + `\` + the listed name, with `\\?\` in front; link-or-not comes from the listing's own entry or that string; the safety checks run on the same string. C84 adds the `Season 1 ` junction and an `x.` folder |
| 2 | Wrong-kind and read-only found entries block an update for ever | Fix, by removal. The update's three passes (link sweep, leftover `.tpp-tmp`, extras deleted after writing) are now one **clear-out** before the first write: links removed as links, then everything not in the new set **as the same kind** deleted, then empty folders. A wrong-kind entry is simply not in the new set. ReadOnly is cleared before any delete or replace. C84 adds the cases |

## Should fix
| Finding | Handled |
|---------|---------|
| Which checks apply to Store's found entries (Comp S4) | Fix. Spec "Found entry": Run uses checks 1, 3, 4; Store uses its inside-the-data-folder, no-link rule. D56 marked widened by D57 |
| Created new, exact steps (Comp S5, Sec 2, Build N8) | Fix. `FileMode.CreateNew`; anything at the name removed first; a replacing write goes temp then `File.Move(…, overwrite: true)`; `File.Replace` never used; staging and new backup files written straight; copying = bytes into a file created new (TopPicksPlus.xml, theme video). C76 adds a linked backup `TopPicksPlus.xml` |
| Lock holder during a waiter's re-check (Comp S6, Build 4) | Fix. Holder still reads `guard` while a waiter re-checks, so a start arriving then waits; the waiter stays in the waiting count until it records `run` or is turned down (F1). C73 |
| Flag order (Build 3) | Fix. A turned-down waiter leaves the waiting count first, then asks for the guard pass (F1). C73 tests it on the timeout path |
| Guard loop on the real server (Build 5) | Fix. C34 watches 5 minutes after a guard pass: at most one further pass, changing nothing |
| Admin attribute untested (Build 7, Sec N6) | Fix. C70's B test checks by reflection that every route carries `AuthenticatedAttribute` Roles `Admin`, and that the server API key with no user is refused; C26 adds the API key for the owner |
| Missing checks (Comp 7, Build 6) | Fix. New C85 (staging link, update with missing folder on first try and resume, add blocked by a loose file or link, copy folder turned into a link, retry) and C86 (waiter's own re-check fails; error after step 12's run.json write). Leftover `.tpp-tmp` is in C84 |

## Nits
| Finding | Handled |
|---------|---------|
| Comp 8 flag rows | Fix: data.md rows say a linked (or folder) data file sets them |
| Comp 9 ` -2` placement | Fix: `-2` at the very end, no space; pruning counts them (F9, C76) |
| Comp 10 staging temp name, top-level tidy, TopPicksPlus.xml | Fix: staging written straight (no temp); start-up tidy top level only; xml copied as bytes |
| Comp 11 D55 | Fix: D55 notes the filter was removed by D56 |
| Comp 12 managed copy folder replaced by a link | Fix: removed as a link, then handled as a missing folder (F4 step 7, F9 check 4, C85) |
| Build 9 sharing violation | Fix: a failed write, move or delete is tried once more after 2 seconds (F "Tried once more", C85) |
| Build 10 F1 "turned down at once" | Fix: Run now follows the waiting rules |
| Build 11 a file or link named like this minute's backup | Fix: removed first, then the folder made (F4 step 3, data.md, C76) |
| Sec 4 hard links | Fix: created new never opens an existing file, so a hard link elsewhere is unchanged; C76 adds a hard-linked state.json |
| Sec 5 `\\?\` only on the plugin's own path + listed name | Fix: that is now the definition; checks run on the same string |
| Sec 7 security.md | Fix: adds `plugins\configurations`; says other standard accounts can't read it, administrators can |

## R1 walk (before the v11 review)
Every new or changed rule checked against the F4 step list, run.json `step`/`status`, data.md and the walls. No new `step` value, status or data field. Run and Store still use only plain .NET file access; the exact-path string is built from Logic's paths record. No wall crossed.

- **Every action on a reached-differently thing** (card 0005): for a found entry, list, attributes, link test, ReadOnly, link removal, delete and replace all use the exact path. Planned names are single names, so their normal and exact paths are the same.
- **Delete:** junction `Season 1 ` → listing entry is a reparse point → removed as a link by exact path, target untouched. Real folder `x.` → listed and deleted by exact path. Read-only files → cleared, deleted. **Found by the walk:** a read-only copy folder (the plugin's own, not found) still failed its final delete → ReadOnly rule widened to every file or folder deleted or replaced (C85 f).
- **Update:** clear-out removes links, then the wrong-kind `Season 1` file, the `Heat (1995).nfo` folder, any `x.tpp-tmp`, gone episodes and an old `folder.jpg`; keeps real planned files (read-only ones cleared at their replace) and `backdrops\theme.mp4`; then writes. A stop during or after the clear-out: resume at `swap` repeats it harmlessly (a half-written `.tpp-tmp` is just not in the new set). A write that fails twice: skipped, `stale`, files list = old + planned .strm (some already gone: look-ups find nothing and skip, as before).
- **Add:** target must not exist; a loose file or link there skips it with no entry (C85 c). Staging written straight with `CreateNew` after Store emptied it.
- **Restart points:** `guards`–`plan` and `prepare` unchanged; `swap` walked above; `rescan` onward touch no files.
- **Store, things it didn't make:** **found by the walk:** a folder named `run.json` or `managed.json` would make every read fail (an error, not damage), so every run "could not start" for ever → a data file that is a folder is treated like a link: removed, counts as missing, sets its pending flag (F9, data.md, C76). A file called `staging` or `backups` → replaced by a real folder. A file at this minute's backup folder name → removed, folder made.
- **Several starts at once, first run ends quickly:** A, B, C wait behind the guard; A gets the lock (holder still `guard`, A still counted as waiting), D arrives and waits; A records `run` → B, C, D turned down at once, each leaving the count before asking for a guard pass (passes find a run holding the lock and stop); A ends in a second and runs the guard once. If A's re-check fails instead, it leaves the count, asks for a pass and the others keep waiting (C86). **Found by the walk:** the waiter's place in the count during its re-check wasn't stated → F1 now says it stays counted until it records `run` or is turned down.
- **Retry:** the 2-second wait is given to Run and Store when built (tests set zero); inside Store's plays lock it delays a play event by at most 2 seconds, accepted.
