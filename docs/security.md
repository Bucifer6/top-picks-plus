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
| Backups with a tested restore | Before first install: old plugin DLL and settings backed up on the Desktop (D17). Plugin data files are rebuildable except plays.json; Stage 8 adds a restore try |
| Safe error messages | Run report says what failed in plain words, never a stack trace to non-admins; settings page is admin-only (F8) |
| Bad input refused, never a crash | Damaged or missing data files start empty (Store README); bad pins show "missing" (F2) |
| Deletes only what it made | F9: only managed folders, only inside the Top Picks folder; a check (C18) proves it |
| Personal data | None kept (D21): only daily viewer counts per title, 90 days; user IDs exist only for the current day's not-yet-counted plays. Stays on the server |
