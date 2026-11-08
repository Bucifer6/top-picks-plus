---
summary: Decisions the owner has made, with date. Nothing is decided unless it is here.
links: [spec.md]
---

# Decisions

| # | Date | Decision | Why |
|---|------|----------|-----|
| D1 | 2026-11-08 | Build a full replacement for the old Emby.TopPicks plugin, not a companion that fixes its order | Old plugin is abandoned and closed source; a companion would still depend on it |
| D2 | 2026-11-08 | One row, 10 items: manual pins first in the owner's order, the rest by most watched (last 7 days), recently added as filler | Matches how the old plugin is used today, with a top 10 instead of a top 9 |
| D3 | 2026-11-08 | Refresh weekly, Sunday 05:00, plus "Run now" | Fresh picks for the weekend, while nobody is watching |
| D4 | 2026-11-08 | Reuse the existing top-picks folder and library, so the "Latest Top Picks" row stays put | No home-screen setup to redo for each user |
| D5 | 2026-11-08 | Code lives in the private GitHub repo Bucifer6/top-picks-plus; GitHub Actions builds the DLL on every change; the owner downloads and installs it | Owner's choice (changed from building on the PC the same morning) |
| D6 | 2026-11-08 | Same artwork style as today: 1000x562, big white rank number on the left, logo over the backdrop | The owner likes the current look |

| D7 | 2026-11-08 | The plugin is called "Top Picks Plus" in Emby | Matches the repo name; never confused with the old plugin |

## Still to decide
(none)
