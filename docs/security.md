---
summary: Security baseline for rigour level 1 (personal) and how each item is met.
links: [spec.md, decisions.md]
---

# Security (level 1 – Personal)

| Item | How it is met |
|------|---------------|
| Secrets only in .env, never committed | The plugin needs no secrets. .gitignore excludes .env, keys and certificates |
| Secret scan | Before every commit Claude runs a pattern check on the staged changes (keys, tokens, passwords, private-key blocks; the command is in docs/handover/current.md). gitleaks then scans the full history on every build (build.yml). The full gitleaks scanner can't be run in the cloud workspace (outside downloads are blocked). |
| Dependency vulnerability audit | `dotnet list package --vulnerable` fails the build (build.yml) |
| Backups with a tested restore | Before first install: old plugin DLL and settings backed up on the Desktop (D17). Before every run's swap: managed.json, plays.json and TopPicksPlus.xml into `backups\YYYY-MM-DD\`, 8 kept; the Top Picks folder zipped before the first takeover (spec F9). Restore steps in spec F9; C30 tries a restore, and Stage 8 repeats it before release |
| Safe error messages | Run report says what failed in plain words; stack traces only in Emby's log. Every plugin API action checks for an administrator on the server and refuses anyone else with no data (F8, C26) |
| Bad input refused, never a crash | Damaged data files are handled as spec F9 says (damaged managed.json refuses every run; others renamed aside), C30. Names from Emby go through SafeName and .nfo files through an XML writer (F3, C28); a path with a line break is left out; bad pins show "missing" (F2) |
| Deletes only what it made | F9 delete guard: managed only, single name, real location inside the Top Picks folder or staging, never through a junction or link (C18, C27). A disconnected drive refuses the run (D25, C29) |
| Personal data | None kept (D21): only daily viewer counts per title, 90 days; user IDs exist only for the current day's not-yet-counted plays. Stays on the server |
| No network | The plugin makes no network calls. Automatically enforced for Logic only (BoundaryTests.cs); for the other parts, checked by the Stage 7 review |
