# tests – automatic tests

**Responsible for:** proving the Logic rules (D18), the Storage safety rules including every file change, swap, resume and delete (D36, D43, with temporary folders), the artwork drawing (D56) and enforcing the walls between the plugin's parts (Boundaries/). Junction tests are skipped on Linux with a reason and run on the Windows CI job (D43).
**Belongs here:** xUnit tests; architecture rules.
**Does not belong:** tests that need a running Emby server, or fake-Emby tests (those are the owner's checks in docs/checks.md; the rules behind chunk 4's automatic checks are tested as Logic, D79). The proof plugin (`proof/TopPicksProof/`, D76) has no tests here.
**Rule:** never change or delete a test to make a build pass; changing a boundary rule needs the owner's approval.
**Storage tests (chunk 2):** `tests/TopPicksPlus.Tests/Storage`, each tagged `[Trait("Check", "Cxx")]`, in throwaway temp folders (`Sandbox`). Junction and other Windows-only tests end in `_junction` or `_windows` and use `SkippableFact`; the Windows job fails unless they ran and passed there (D125). The two stop-at-every-step swap tests are tagged `Mutation=Exhaustive` and left out of Stryker only.
