# Handover

## Job just finished
Spec interview and challenge round (3 fresh reviewers) for Top Picks Plus; spec rewritten as v2.

## Where it stopped
Stage 2 (Spec), not yet gated. Branch main, docs only, no code. Last commit: "Spec v2 after challenge round".

## What is still owed (smallest first)
1. Owner runs the proof test (below) and reports results.
2. Record the result as a decision; if it fails, redesign F4/F5 before anything else.
3. Spec gate: confirm every feature has a check and no open questions; owner agrees.
4. Stage 3 decisions: Emby plugin SDK version to pin, image-drawing library, GitHub Actions build.
5. Stage 4 scaffold (folders, READMEs, build workflow), goldfish review; Stage 5 readiness.

## Proof test (owner, about 10 minutes, in the Emby web dashboard)
A. Movie: open a movie copy at rank 4 or lower in the Top Picks library, Edit metadata, set Date added to one hour in the future, save. Does it jump to the front of the Latest Top Picks row?
B. Persistence: Dashboard, Scan all libraries; then restart Emby. Is it still at the front?
C. Series: on IT Welcome to Derry (rank 3), set only the series' Date added one hour in the future (leave episodes). Does the series move to the front? If not, the Latest row is using episode dates (spec F4 step 4 already sets both).
D. Run the old plugin's task or rescan afterwards to put things back.

## Watch out for
- The whole fix depends on the proof test. Do not start building before it passes.
- The old plugin writes <dateadded> into .nfo files; Emby ignores it for existing items. That is the bug.
- device_bash on the owner's PC is broken (virtualisation off in BIOS); files can still be listed and staged. Top-picks folder and plugin configurations folder are connected directly.
- NuGet is blocked in the cloud workspace; builds happen in GitHub Actions only (D5).
- Decisions D9-D11 are builder defaults the owner may change.

## Waiting on the owner
- Proof test results (A-C).
- Any objection to D9 (own play log), D10 (viewer-days), D11 (fixed 10).

## Next job
Record the proof test result and pass the spec gate. Read first: CLAUDE.md, docs/spec.md, docs/decisions.md, docs/reviews/2026-11-08-challenge.md.

## Cards that applied
0001
