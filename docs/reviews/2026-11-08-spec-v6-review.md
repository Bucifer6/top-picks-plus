---
summary: Fresh-reviewer re-review of spec v6 (comprehension, builder readiness, security). Not passed: 3 Blockers, one needs an owner proof test on the real server. For spec v7.
links: [../spec.md, ../data.md, ../checks.md, ../modules.md, ../security.md, 2026-11-08-spec-v5-handling.md]
---

# Spec v6 re-review, 2026-11-08

Reviewers: **Comp** (2 Blocker, 9 Should, 7 Nit), **Build** (0 Blocker, 6 Should, 6 Nit), **Sec** (1 Blocker, 4 Should, 5 Nit). Run at the same time on commit 04e23c4. Stage 4 **not passed**; pull request 1 stays unmerged.

Comp confirmed v5 Blockers 1, 2, 3 and 6 are fixed and no sync or reset wording is left (D39). Build found every feature buildable within the walls; R1 held for `step`/`status`, data.md and the walls.

## Blockers
1. **D39 is only half proved on the real server (Sec 1).** The owner's look showed real-film → copy for films. Unproved: (a) watching a copy marks the real title watched; (b) **deleting a copy folder/item (every weekly drop-out, every install) leaves the real title's watched tick and resume point alone**; (c) episodes share the same way (D42). If (b) fails, every weekly run wipes real history, and C49's fakes can't see it (card 0008, seen 2). **Owner proof test on the old plugin before spec v7 settles the design.**
2. **Give-up count off by one (Comp 1).** F5 gives up when `resumeCount` is "above 3", meaning three stops, but after three stops it is 3. Fix: give up when it reaches 3 after adding, or reword; align F5, D36, data.md range, C55.
3. **Skipped delete/update leaves a stale managed entry (Comp 2).** A skipped update keeps last week's rank and rank date; a skipped delete keeps a non-pick `active` with a rank; the order guard then sets wrong dates and the row breaks. Needs a rule (for example: a skipped delete or update entry gets rank and rank date cleared / marked `stale`, the order guard ignores it, the next plan deletes it).

## Should fix (summarised)
- **Comp:** skipped add with an existing target must not leave an `incoming` entry that later adopts a folder the plugin didn't make (3); an error inside step 1 has no status (4); step 1 must give a starting value for every run.json field, and say when `timeZoneWarning` is set (5); **"finished" with shared watched state**: an already-watched film replayed for 5 minutes counts, against C12; must mean "became played during this playback" (6, a D39 knock-on); state.json has no lock, so a pending flag set by a play event can be lost (7); a write check failing on one file inside an update: whole operation skipped or just the file (8); "allowed to see" undefined and no EmbyBridge action for it (9); the 2-minute wait for the order guard vs a guard with no time limit, and a turned-down Sunday task gives no run that week (10); EmbyBridge can't resolve Top Picks paths with read-only source access (11).
- **Build:** no EmbyBridge test layer, so C70/C59/C69 test Logic's rule, not the wiring (1); C49 can't fail (2); no part owns choosing the check user (3); netstandard2.0 has no link-resolving API: name the method and which part holds the helper (4); no check for Store's own delete guard (5); unchecked rules: double mark counts once, clearing adds nothing, `timeZoneWarning`, step 10 read-back (6).
- **Sec:** back up Emby's own database before install and each release, and try a restore (2); forbid `watched`/`playcount`/`lastplayed`/`resume` in .nfo and prove the artwork refresh doesn't reload or save metadata (3, add to C34); an automatic test over every API route as non-admin and no user; POST only for actions (4); an automatic scan that EmbyBridge has no user-data or played-state calls and no file writes (5).

## Nits
Comp 12–18 (pending-flag wording, `step` on refusal in spec, missed-run hook owner, step 12 write order vs a failed resume, run.previous.json wording and the report while running, data.md version and pick-flag timing and `skipped` type, "read" vs "parsed"); Build 7–12 (.nfo text owner vs C35's L, settings source in the root, smaller unchecked rules, SkiaSharp in the test csproj still to come, missing ban fixtures and two wall tests that pass empty, meta-test not yet enforced); Sec 6–10 (flag roll-up failing 2 days, XML restore untested and unfinished backup folders, titles in play-path Debug lines, scheduled audit only on the default branch, release-notes warning about kept .strm folders).

## Lesson
Removing F7 (D39) traded a sync the plugin controlled for behaviour Emby controls, so the real-server dependency moved rather than went away (card 0008, now seen 2). It also changed what an existing rule means ("finished", Comp 6), which R1's checklist didn't ask about: R1 checks new rules against steps, data and walls, not old rules against a removed feature (card 0005 evidence).
