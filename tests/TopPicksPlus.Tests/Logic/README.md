# Tests for Logic

xUnit tests for `src/TopPicksPlus/Logic`, one file per rule area, each test named after the check or spec item it proves (e.g. `C10_binge_counts_once_per_day`) and tagged `[Trait("Check", "C10")]`, so results can be listed per check (`dotnet test --filter Check=C10`). Mutation testing (Stryker.NET 4.16.0) on Logic and Storage runs in the Windows CI job, failing below 80% (D66, D87, D105). Chunk 1 also writes the tests for the rules behind chunk 4's automatic checks (C57, C63, C71, C74, D79).
