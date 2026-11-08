# Handover

## Job just finished
Spec job: interview, challenge round, spec v2, proof test passed (D12), spec approved by owner (D13).

## Where it stopped
Stage 3 (Decisions) not started. Branch main, docs only, no code.

## What is still owed (smallest first)
1. Stage 3 technical decisions, proposed to the owner in plain English with a recommendation:
   a. Emby plugin SDK package and version to pin (must load in Emby Server 4.10.2.0).
   b. Image library for drawing the numbered artwork (must run inside Emby on Windows without clashing with Emby's own libraries).
   c. GitHub Actions build: what it runs, where the DLL appears for the owner to download, how long builds are kept (keep artifacts short: the owner's GitHub storage filled up before).
   d. How the owner installs and rolls back (copy the DLL into programdata\plugins, restart Emby; keep the previous DLL).
2. Stage 4 scaffold: folders with READMEs, build workflow, boundary check; goldfish review.
3. Stage 5 readiness goldfish (score must be above 90).
4. Stage 6 build in chunks the owner can check, Stage 7 review, Stage 8 release (old plugin removed at the same time).

## Watch out for
- Old plugin (Emby.TopPicks.dll) is still installed and must be uninstalled when Top Picks Plus goes in (F9 guard).
- device_bash on the owner's PC is broken (virtualisation off); listing and staging files works. top-picks and plugins\configurations folders are connected directly.
- NuGet is blocked in the cloud workspace, so nothing can be compiled there; GitHub Actions only.
- Emby's HTTP port is 48096 locally (system.xml), public 8096 behind Caddy.

## Waiting on the owner
Nothing right now; Stage 3 choices will be brought one at a time.

## Next job
Stage 3 decisions, then scaffold. Read first: CLAUDE.md, docs/spec.md, docs/decisions.md, docs/data.md.

## Cards that applied
0001
