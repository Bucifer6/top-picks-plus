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

| D8 | 2026-11-08 | Keep the numbered copies (.strm library) rather than a playlist of the real titles; sync watched/resume back to the real title; accept duplicates in search | Owner wants the numbered artwork; weighed against a playlist section during the challenge round |
| D9 | 2026-11-08 | The plugin keeps its own play log; Playback Reporting is only used once, to seed the first week | Not dependent on a third-party plugin that could break. Approved by owner at spec gate |
| D10 | 2026-11-08 | "Most watched" = viewer-days in the last 7 days; a binge counts once per user per day | Stops one person's binge outranking what several people watched. Approved by owner at spec gate |
| D11 | 2026-11-08 | The number of picks is fixed at 10 | Artwork, order and pins all assume 10; fewer moving parts. Approved by owner at spec gate |

| D12 | 2026-11-08 | Proof test passed on Emby 4.10.2: changing Date added on a movie and on a series (series only) moves it in the Latest Top Picks row, and it stays after a full scan and restart. The date-added approach (F4/F5) goes ahead | Owner ran the test in the Emby dashboard |

| D13 | 2026-11-08 | Spec v2 approved by the owner; Stage 2 gate passed | Every feature has a check; challenge findings handled (docs/reviews/2026-11-08-challenge.md) |

## Still to decide before building
Stage 3 technical choices, proposed in docs/handover/current.md
