---
summary: How each finding of the spec v6 re-review was handled in spec v7 (fix, contest, ask the owner, skip).
links: [2026-11-08-spec-v6-review.md, ../spec.md, ../data.md, ../checks.md, ../decisions.md, ../modules.md]
---

# Spec v6 findings: handling (spec v7, 2026-11-08)

## Blockers
| # | Finding | Handling |
|---|---------|----------|
| 1 | D39 only half proved (Sec 1) | **Fix (settled by the owner).** Proof test passed on all three parts (D43). Recorded in spec F7, D43, C14 (C14 stays the release check). |
| 2 | Give-up count off by one (Comp 1) | **Fix.** Gives up when `resumeCount` reaches 3 (the third stop): spec F5, D36 note, data.md range 0–3, C55 (two resumes, then gives up). |
| 3 | Skipped delete/update leaves a stale entry (Comp 2) | **Fix.** New `stale` state (D44): rank and rank date cleared, order guard ignores it, next plan deletes or updates it, row check allows stale copies only after the expected picks (`partial`). Spec "Stale copy", F4 steps 5, 7–11, F5, F6, F9; data.md; C39, C77. |

## Should fix
| Finding | Handling |
|---------|----------|
| Comp 3 skipped add adopting a folder | **Fix.** Target checked before the `incoming` entry is written; on resume the entry is removed (F4 step 7, C65). |
| Comp 4 error inside step 1 | **Fix.** `failed` covers any error once run.json is written; a failure to write run.json is logged and answered "the run could not start" (F4 step 1, C79). |
| Comp 5 starting values, `timeZoneWarning` | **Fix.** data.md "Starting values"; set at step 1 from the passed zone (C79). |
| Comp 6 "finished" with shared state | **Fix.** Finished = became played during this playback; marked by hand = Emby's "played toggled" reason only (F6, data.md, C12, C78, C34). |
| Comp 7 state.json lock | **Fix.** Store holds a state lock (spec Pending flags, data.md, Store README). |
| Comp 8 write check on one file of an update | **Fix.** All checks run before an operation's first change; any failure skips the whole operation (F4 step 7, C77). |
| Comp 9 "allowed to see" | **Fix.** Defined, with an EmbyBridge action (F4 step 11, EmbyBridge README). |
| Comp 10 guard wait and turned-down Sunday task | **Fix.** Guard lets go within 90 s and carries on later; missed-run rule also at 00:15 daily (D45, F1, F5, C40, C73). |
| Comp 11 EmbyBridge resolving Top Picks paths | **Fix.** Item check compares with the library folder path Emby holds, no file access; folder resolving in EmbyBridge's adapter (D46, F9). |
| Build 1 no EmbyBridge test layer | **Asked the owner: yes (D47).** Layer B with NSubstitute; C59, C69, C70 gain B tests. |
| Build 2 C49 can't fail | **Fix.** C49 is now a wall test (layer W) over EmbyBridge's interfaces with a failing fixture (card 0002). |
| Build 3 who chooses the check user | **Fix.** Logic, from EmbyBridge's plain user records (F4 step 11, modules, Logic README). |
| Build 4 no link-resolving API on netstandard2.0 | **Fix.** `GetFinalPathNameByHandle` in EmbyBridge only; Run and Store use the reparse-point attribute; no new link (D46). |
| Build 5 no check for Store's delete guard | **Fix.** C76. |
| Build 6 unchecked rules | **Fix.** C78 (double mark, clearing), C79 (`timeZoneWarning`, step 10 read-back). |
| Sec 2 back up Emby's own database | **Asked the owner: yes (D48).** releases.md, security.md, C81. |
| Sec 3 per-user .nfo elements, artwork refresh | **Fix.** Banned elements (F3, C35); image-only refresh confirmed in C34. |
| Sec 4 every API route as non-admin; POST only | **Fix** POST/GET rule (F8, EmbyBridge README, security.md). The route-by-route test is in C70's B layer (D47). |
| Sec 5 scan EmbyBridge for user-data calls and writes | **Asked the owner: yes (D47).** Mono.Cecil wall test in C49. |

## Nits
| Finding | Handling |
|---------|----------|
| Comp 12 pending-flag wording | Fix ("copied as true"). |
| Comp 13 `step` on refusal | Fix (statuses, F4 step 1). |
| Comp 14 missed-run hook owner | Fix (Run's hook, EmbyBridge README). |
| Comp 15 step 12 write order | Fix: run.json first, then state.json (F4 step 12). |
| Comp 16 report while running | Fix (F8, C80). |
| Comp 17 data.md version, pick flags, `skipped` type | Fix (data.md). |
| Comp 18 "read" vs "parsed" | Fix (F9). |
| Build 7 .nfo text owner | Fix: Logic builds the text, Run writes it (F3, Logic README). |
| Build 8 settings source in the root | Fix (spec wiring, modules.md). |
| Build 9 smaller unchecked rules | Skip: covered by C77–C80 where they mattered. |
| Build 10 SkiaSharp in the test project | Skip: already owed to the Artwork build chunk (checks.md). |
| Build 11 missing ban fixtures, two wall tests that pass empty | Skip for now, **owed**: added in the first build chunk with the other test code (card 0002); in the handover. |
| Build 12 meta-test not enforced | Skip: created in the first build chunk (checks.md). |
| Sec 6 roll-up failing for days | Skip: a run's roll-up failing fails the run, so it shows in the weekly report. |
| Sec 7 XML restore, unfinished backups | Fix (C63, F9 pruning, data.md). |
| Sec 8 titles in play-path Debug lines | Fix (F4, F6, C48, security.md). |
| Sec 9 scheduled audit only on main | Skip: GitHub runs schedules on the default branch, which is where releases come from. |
| Sec 10 kept .strm folders at install | Fix (releases.md step 3). |

## R1 and card 0005
Every change above was checked against the F4 step list, run.json `step`/`status`, data.md and the walls. Old rules touching changed ideas were re-read: path comparisons (spec "Text comparisons", security.md), the order guard's wait (F1), play logging (C48), stale entries in the tidy, write checks, row check and play matching, and step 12's order against resume and the missed-run rule.
