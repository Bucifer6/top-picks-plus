# Handover

## Job just finished
Spec job and Stage 3: spec approved (D13); owner delegated technical decisions, recorded as D14–D19.

## Where it stopped
Stage 4 (Scaffold) not started. Branch main, docs only, no code.

## What is still owed (smallest first)
1. (done: D14–D19)
2. Stage 4 scaffold: folders with READMEs, build workflow, boundary check; goldfish review.
3. Stage 5 readiness goldfish (score must be above 90).
4. Stage 6 build in chunks the owner can check, Stage 7 review, Stage 8 release (old plugin removed at the same time).

## Watch out for
- Old plugin (Emby.TopPicks.dll) is still installed and must be uninstalled when Top Picks Plus goes in (F9 guard).
- device_bash on the owner's PC is broken (virtualisation off); listing and staging files works. top-picks and plugins\configurations folders are connected directly.
- NuGet is blocked in the cloud workspace, so nothing can be compiled there; GitHub Actions only.
- Emby's HTTP port is 48096 locally (system.xml), public 8096 behind Caddy.

## Waiting on the owner
Nothing right now; owner has delegated technical decisions to Claude; still bring anything hard to undo.

## Next job
Stage 4 scaffold: folders + READMEs, build workflow (D16), test projects (D18), then goldfish review. Read first: CLAUDE.md, docs/spec.md, docs/decisions.md, docs/data.md.

## Cards that applied
0001

## Quiz
# Handover quiz (answer from current.md alone; do not open answers.md)
1. Which stage is next, and what does it produce?
2. Who made the technical decisions D14–D19, and why?
3. What has to happen to the old plugin, and when?
4. Where can the plugin not be compiled, and why?
5. What score must the readiness review give before building?

## Answers
# Answer key (only the marking goldfish reads this)
1. Stage 4 Scaffold: folders + READMEs, build workflow, test projects, then goldfish review (Next job).
2. Claude; the owner delegated them (Job just finished; Waiting on the owner).
3. Uninstalled when Top Picks Plus goes in, at release (Watch out for).
4. The cloud workspace; NuGet is blocked there (Watch out for).
5. Above 90 (What is still owed 3).

## Quiz result (2026-11-08, scaffold job)
5 of 5 right, marked by a fresh reviewer. Handover fault found: owed item 2 and Next job listed different Stage 4 parts (one missed the boundary check, the other the test projects); fixed in the next handover.
