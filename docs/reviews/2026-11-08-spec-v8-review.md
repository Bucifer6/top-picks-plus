---
summary: Fresh-reviewer re-review of spec v8 (comprehension, builder readiness, security). Not passed: 2 Blockers, both narrow. For spec v9.
links: [../spec.md, ../checks.md, ../data.md, ../modules.md, ../security.md, ../releases.md, 2026-11-08-spec-v7-handling.md]
---

# Spec v8 re-review, 2026-11-08

Reviewers: **Comp** (1 Blocker, 7 Should, 6 Nit), **Build** (1 Blocker, 7 Should, 9 Nit), **Sec** (0 Blocker, 5 Should, 7 Nit). Run at the same time on commit c1fab5f. Stage 4 **not passed**; pull request 1 stays unmerged.

Sec found no way for a run to delete real media or another library, or touch watch history; D52 (no link resolving) is sound for deletes (junctions, symlinks, mount points, hard links, `top-picks2`, case). Build found every feature F1–F10 has a check and D49–D54 consistent across docs.

## Blockers
1. **Update vs links inside (Comp 1; also Build 2, Sec 1).** F4 step 7 "update" doesn't say when links inside the folder are removed. A junction named like a new-set folder (`Season 1`) fails check 4 at the first write, so the update goes stale (contradicts C27's update case). A *file* symlink with a new-set name (`Heat (1995).nfo`) is never checked: `File.Replace` may write through it. Fix: an update first sweeps its folder and removes every link as a link, before any write; every replace target is checked for the reparse-point attribute just before it is replaced.
2. **C39 contradicts itself and F4 step 11 (Build 1; also Comp 7).** It says a missing pick is `partial`, then lists "missing an expected pick" and "an extra item" under `failed`. Fix: drop those from the `failed` list; say an extra stale copy after the picks is `partial`.

## Should fix
- **Store's own writes (Sec 1):** `<name>.tmp` created with `CreateNew`; a `.tmp` that is a link is deleted as a link; live files checked not to be links before replace.
- **Lock hand-off (Comp 4, Build 3, Sec 5):** the lock must record who holds it (a run or the guard); a "start waiting" flag the guard checks before each entry; a limit on how long a start waits (e.g. 30 s, then "a run is already in progress"); one guard pass at a time, a new trigger while one runs is dropped (Comp 5).
- **Guard re-arms itself (Sec 4):** its own date changes are library events that start it again; ignore events caused by its own changes, or don't re-arm after a pass that changed nothing.
- **Guard after a start that never became a run (Comp 6, Build 15):** "could not start", refused and failed endings must say "run the order guard once".
- **Pending flags at step 12 (Comp 3, Build 4):** clear only the flags that were copied as true, in one locked `IStore` call; say which part does it; add the case to C82 (layer S).
- **Skipped operations and resume (Comp 2):** say a skipped operation is marked `done` (with its note), and the write order of run.json and managed.json.
- **Checks on folders that don't exist (Comp 8):** an add's target before the move, a resumed add whose staging folder has gone, a resumed delete whose folder has gone: missing is not a failure.
- **Emby actions not named (Build 7):** find a copy item by path; list every episode under a copy series; read date added and "date last media added". Add to the EmbyBridge README's adapter actions.
- **Top Picks path as Emby holds it (Build 8, Sec 12):** C34 must read the Top Picks library's folder string and the path form Emby gives copy items (.strm path), since D52 compares strings.
- **Test layers (Build 5, 6):** C50 needs S; B is missing for start-up timers, the 00:05 timer, the library-change debounce, playback events with played state, and drops before wiring; checks.md line 95 still says EmbyBridge tests only check conversions.
- **Emby safety copy (Sec 2, 3; Build 17):** written restore steps (stop Emby, rename live `data`, `config`, `root` aside, never merge, copy back whole, start Emby; after a restore, empty top-picks and delete managed.json); how many copies are kept; security.md must say these copies hold user IDs and full watch history.

## Nits
Comp 9–14 (empty row verdict; episode-to-series matching for a stale series' new episode; which entry wins in the files union; `operations.entry` state for adds; `staging\` itself a link before Run's writes; "within seconds" for a big stale series; CLAUDE.md stage line). Build 9–16 (CLAUDE.md stage; W layer wording covers Cecil IL; tests README layers and packages; Run README copy-looking folders; start-list limits have no check; "not inside programdata" can't fail; guard after failed/refused endings; SkiaSharp and Settings ban fixtures). Sec 6–11 (reject `.`/`..` parts in Emby item paths; short-name aliasing of a missing managed folder; text-only library comparison misses UNC/subst and libraries pointed at `staging\`; roll-up retry when plays.json is locked; native SkiaSharp crash accepted; fixed local path for the safety copy and plugin configurations folder).

## Lesson
The R1 walk covered a delete and an add, but not an **update**, which is where Blocker 1 sat (card 0005, seen 5). Walk every operation kind, not one example.
