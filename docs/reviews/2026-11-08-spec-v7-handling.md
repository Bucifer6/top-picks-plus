---
summary: How each finding of the spec v7 re-review was handled in spec v8 (Fix / Contest / Ask the owner / Skip). Theme: remove rules rather than add them (card 0005).
links: [2026-11-08-spec-v7-review.md, ../spec.md, ../decisions.md]
---

# Spec v7 review: handling (spec v8, 2026-11-08)

## Owner answers this job
- Series proof test passed (D49): deleting a series copy keeps the real episodes watched and resumable. Settles v7 Comp 10.
- Emby's Nfo saver and "save artwork into media folders" were **on** for the Top Picks library; the owner turned them off (D50). Settles v7 Sec 9.
- R1 extended with the walk-one-concrete-case step (card 0005), owner's OK.

## Blockers
1. Links inside a managed folder vs the pre-check (Comp 1, Sec 4) — **Fix.** The pre-check now covers only the operation's own folders; F9 check 4 splits "on the way" (fails) from "inside" (removed as a link during the operation). C27 extended to updates.
2. Half-updated series sorting into the row (Comp 2) — **Fix, D53.** Every item of a stale copy (series and every episode Emby has under it) gets the fixed stale date 2000-01-01, at step 9 and by the order guard. One target-date rule in Logic. C77.

## Should fix
- Order guard limit (Build 1–3, Comp 3, 4, Sec 14) — **Fix, D51**, by removing the 90-second limit and carry-on timer: the guard takes the lock one entry at a time and stops when a run holds it. D40 and D45's guard part marked replaced. Several waiting starts: only one goes ahead (F1). C73.
- Folder resolving (Build 5, 6, 7, 13, 17; Sec 5, 6; Comp 5, 6, 7) — **Fix, D52**, by removing resolving: plain tidied paths, no native calls (the Cecil scan now fails on any P/Invoke), one Top Picks path string everywhere, no other library folder opened. Real-junction tests named (C27, C50, C52, C76).
- Folder boundary (Sec 1) — **Fix.** "Inside" = followed by `\`; `top-picks2` cases in C50, C52, C59.
- Single name aliasing (Sec 2) — **Fix.** Single name = SafeName's cleaning leaves it unchanged; SafeName now also removes `~` (short names). C52.
- Adds and check 1 (Build 4) — **Fix.** An add's checks run just after its `incoming` entry is written; removed again if they fail. Found again by the R1 walk. C65.
- `incoming` adopted after a failed move (Sec 3) — **Fix.** Adopted only when its staging folder is gone. C65.
- Stale `files` list (Comp 13, Build 19) — **Fix.** Union of old and planned .strm files.
- `not appeared` pick fails the check (Comp 12) — **Fix.** Verdicts rewritten; reasons `parental controls` / `no copy` / `not in row`. C39.
- Cecil ban list unnamed, name clash, C49 never due, test packages (Build 8–11, Comp 15, Sec 7) — **Fix.** Named list in modules.md by full Emby type and method, missing-name check, added with the first EmbyBridge chunk with NSubstitute and Mono.Cecil.
- B layer reach and clock (Build 12) — **Fix.** C40, C73, C78 now also B; `IBridgeClock` named.
- Real-server proofs (Sec 8, 10; Comp 8, 9) — **Fix.** C34 and "Before building" list them.
- `failed` write itself failing (Comp 11) — **Fix** (documented as an accepted limit: run waits until the next restart, then resume handles it). No new mechanism.
- Pending flags lost by a refused run (Comp 14) — **Fix**, by removing the step 1 clear: cleared only at step 12. C82.
- `plays.json.tmp` left with user IDs (Sec 13) — **Fix.** Store deletes its `.tmp` at once on a failed write. C74.
- D48 backup (Sec 11, 12) — **Fix, D54.** Whole `data`, `config`, `root`, kept on the PC only, restore tried before the first install. C81, releases.md, security.md.

## Nits
- Fixed: `lastSuccessfulRunUtc` = `finishedUtc`; Run README "3 resumes" wording; "within two days" → "at the next 00:15"; clock interface named; `staging\`/`backups\` in Store's link checks, and a linked `staging\` replaced by a real folder (R1 walk); `.tpp-tmp` created new, leftovers deleted first; "deletes only what it made" exception named in F9 and security.md; stale wording.
- Skipped (no rule change, or noted for build): stale artwork number visible when fewer than 10 appear (it sorts last; accepted); marking a whole series played adds many sittings (counts once per user per day anyway); stale rank null / ranks unique wording; resume `step` reset wording; checks.md B/W wording; 00:15 then 05:00 runs (the 05:00 task is turned down, harmless); how C70 finds routes and B layer scope (build chunk detail); root namespace bans; user paths in step 11 logs and fixed API error text (Stage 7 review); Emby's own config route; modules.md "can't be checked"; start list across midnight.

## R1 walk (card 0005)
- **Delete of a folder with a junction inside:** pre-check passes (the folder itself isn't a link); the walk removes the junction as a link; a failure turns the entry stale with the stale date; resume repeats safely. No clash.
- **Brand-new add:** found the check-1 clash above (fixed) and that `staging\` itself being a link wasn't covered (fixed in F9 Store's deletes). Resume after each write point traced: entry only, moved but not marked, neither. No clash left.
