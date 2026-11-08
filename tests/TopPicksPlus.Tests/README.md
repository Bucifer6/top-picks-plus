# tests

## Responsible for
Automatic tests (D18): xUnit tests for Logic and for Run (with fake EmbyBridge adapters), and the boundary check (BoundaryTests.cs) that fails the build if a part crosses a wall.

## Belongs here
One test file per thing tested, named after it. Every test covering a check carries `[Trait("Check", "C10")]`; that tag is what the meta-test counts (docs/checks.md). Names may also start with the ID (e.g. `C10_binge_counts_once_per_user_per_day`). The meta-test and `built-features.txt` come with the first build chunk. Test layers L, S, R and Art are described in docs/checks.md.

## Does not belong
Anything needing a real Emby server: those are the owner's checks in docs/checks.md.

## May use
Every part of the plugin, xUnit, NetArchTest, SkiaSharp (tests only, at the version Emby loads, for the Art layer and the boundary check).

## Must not use
The network, the real Top Picks folder, or the owner's PC.
