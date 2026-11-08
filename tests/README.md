# tests – automatic tests

**Responsible for:** proving the Logic rules (D18) and the Storage safety rules (D36, with temporary folders) and enforcing the walls between the plugin's parts (Boundaries/).
**Belongs here:** xUnit tests; architecture rules.
**Does not belong:** tests that need a running Emby server (those are the owner's checks in docs/checks.md).
**Rule:** never change or delete a test to make a build pass; changing a boundary rule needs the owner's approval.
