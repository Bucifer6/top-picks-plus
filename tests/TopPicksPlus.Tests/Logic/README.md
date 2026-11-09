# Tests for Logic

xUnit tests for `src/TopPicksPlus/Logic`, one file per rule area, each test named after the check or spec item it proves (e.g. `C10_binge_counts_once_per_day`). Mutation testing (Stryker.NET) on Logic and Storage is added to CI by chunk 1, in the Windows job, failing below 80% (D66, D87). Chunk 1 also writes the tests for the rules behind chunk 4's automatic checks (C57, C63, C71, C74, D79).
