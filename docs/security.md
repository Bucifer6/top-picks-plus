---
summary: Security baseline for rigour level 1 (personal) and how each item is met.
links: [spec.md, decisions.md]
---

# Security (level 1 – Personal)

| Item | How it is met |
|------|---------------|
| Secrets only in .env, never committed | The plugin needs no secrets. .gitignore excludes .env, keys and certificates |
| Secret scan | Before every commit Claude runs a pattern check on the staged changes (keys, tokens, passwords, private-key blocks; the command is in docs/handover/current.md). gitleaks then scans the commits being pushed (on a pull request, all of its commits) on every build (build.yml); it does not rescan the whole history each time. The full gitleaks scanner can't be run in the cloud workspace (outside downloads are blocked). |
| Dependency vulnerability audit | `dotnet list package --vulnerable` fails the build (build.yml), and a weekly scheduled build repeats it. Restores are locked to the committed lock files (D31) |
| Backups with a tested restore | Before first install: old plugin DLL and settings backed up on the Desktop (D17). At every run (F4 step 3): managed.json, state.json, plays.json without user IDs and TopPicksPlus.xml into `backups\YYYY-MM-DD-HHMM\`, 8 kept; the Top Picks folder zipped before the first takeover (spec F9). Restore steps in spec F9; C30 tries a restore, and Stage 8 repeats it before release |
| Safe error messages | Run report says what failed in plain words; stack traces only in Emby's log. Every plugin API action checks for an administrator on the server and refuses anyone else with no data (F8, C26) |
| Bad input refused, never a crash | Damaged data files are handled as spec F9 says (damaged managed.json refuses every run; others renamed aside), C30. Names from Emby go through SafeName and .nfo files through an XML writer (F3, C28); a path with a line break is left out; bad pins show "missing" (F2) |
| Deletes only what it made | F9 delete guard: managed only, single name, real location inside the Top Picks folder or staging, never through a junction or link (C18, C27). A disconnected drive refuses the run (D25, C29) |
| Personal data | None kept (D21, D33): only daily viewer counts per title, 90 days; user IDs exist only for the current day's plays and are deleted within minutes of the day ending; backups never contain them; a damaged plays file is deleted after a day. Stays on the server |
| No network | The plugin makes no network calls. `System.Net` is banned for every part by the boundary check (BoundaryTests.cs) |
| Build integrity | Actions pinned to commit IDs; the DLL's checksum is taken and the DLL kept before any test runs (D31); releases only from tags on main. Build attestation is skipped at level 1. Protecting main on GitHub (no direct pushes) is an owner setting, owed before release (Stage 8) |
| Settings and page input | Every saved value checked on the server (F8); text from Emby shown as plain text only (F8, C54) |
