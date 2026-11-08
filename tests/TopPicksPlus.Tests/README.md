# tests

## Responsible for
Automatic tests (D18): xUnit tests for Logic and for Run (with fake EmbyBridge adapters), and the boundary check (BoundaryTests.cs) that fails the build if a part crosses a wall.

## Belongs here
One test file per thing tested, named after it. Test names say the check ID where one applies (e.g. `C10_binge_counts_once_per_user_per_day`).

## Does not belong
Anything needing a real Emby server: those are the owner's checks in docs/checks.md.

## May use
Every part of the plugin, xUnit, NetArchTest.

## Must not use
The network, the real Top Picks folder, or the owner's PC.
