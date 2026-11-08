# tests – automatic tests

**Responsible for:** proving the Logic rules (D18), the Storage safety rules including every file change, swap, resume and delete (D36, D43, with temporary folders), the artwork drawing (D56) and enforcing the walls between the plugin's parts (Boundaries/). Junction tests are skipped on Linux with a reason and run on the Windows CI job (D43).
**Belongs here:** xUnit tests; architecture rules.
**Does not belong:** tests that need a running Emby server (those are the owner's checks in docs/checks.md).
**Rule:** never change or delete a test to make a build pass; changing a boundary rule needs the owner's approval.
