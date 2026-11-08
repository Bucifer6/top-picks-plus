---
summary: Release plan and release log.
links: [decisions.md, protected.md]
---

# Releases

## Plan
- **v1.0.0:** full replacement (F1–F10). Released on a weekday, not just before a Sunday 05:00 run. Install and rollback as D17; the old plugin is uninstalled at the same time (F9 guard).
- Exit: every check without an Auto mark or marked A in checks.md tried by the owner (including C26, C29–C31, C34, C45, C48), every automatic test passing in the release build, a restore of plays.json from a backup tried (C63), P1 and P2 pass, one week's Sunday run finished with the row in order.

## Log
| Date | Version | What changed | Rollback used? |
|------|---------|--------------|----------------|

## First install steps (v1.0.0, D34, D17)
1. Back up the old plugin's DLL and settings (already on the Desktop, D17).
2. In Emby: Dashboard → Plugins → uninstall the old Top Picks plugin. Stop Emby.
3. Open `C:\Users\Admin\AppData\Roaming\Emby-Server\programdata\data\top-picks` and delete every folder the old plugin made: any folder with a .strm file in it, directly or in a season folder one level down. Keep any folder you made yourself.
4. Copy the new `TopPicksPlus.dll` (from the GitHub Release, checksum matching its `.sha256`) into `programdata\plugins`. Start Emby.
5. About 10 minutes later the first run starts by itself (or press Run now on the plugin's settings page). The first week's row is your pins, then the newest titles; real viewing counts from the next week.
6. Rollback: stop Emby, delete TopPicksPlus.dll, empty top-picks of this plugin's folders, put the old plugin back, start Emby; it rebuilds its own folders at its next run.
