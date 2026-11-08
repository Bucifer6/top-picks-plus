---
summary: Decisions, with date and who made them (the owner, or Claude where the owner delegated). Nothing is decided unless it is here.
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
| D14 | 2026-11-08 | Build against Emby's plugin SDK MediaBrowser.Server.Core **4.9.1.90** (latest stable on NuGet, .NET Standard 2.0), pinned exactly; the plugin targets netstandard2.0 | Newest stable SDK; Emby 4.10 loads 4.9-built plugins. First build chunk's check is that it loads in 4.10.2. Decided by Claude (owner delegated Stage 3) |
| D15 | 2026-11-08 | Draw the numbered artwork with SkiaSharp, compiled against but **not shipped with** the plugin, so it uses the copy Emby already loads; numbers drawn in an embedded open-licence (SIL OFL) bold font. If drawing fails, the copy gets the plain backdrop and the report says so | No clash with Emby's own image library; no Windows-only System.Drawing. Decided by Claude |
| D16 | 2026-11-08 | GitHub Actions builds and runs the tests on every branch and pull request (DLL kept 3 days as an artifact). A version tag (v1.0.0 etc.) publishes a GitHub Release with the DLL attached; the owner downloads from Releases | Releases don't use artifact storage, which filled up on another repo. Decided by Claude |
| D17 | 2026-11-08 | Install = copy TopPicksPlus.dll into programdata\plugins and restart Emby. Rollback = put the previous DLL back (every Release keeps its DLL) and restart. At first install the old Top Picks plugin is uninstalled from the dashboard; its DLL and settings are already backed up on the Desktop | Simple, one-step rollback. Decided by Claude |
| D18 | 2026-11-08 | Tests: automatic unit tests (xUnit) for choosing, counting, naming, takeover and ordering logic, with mutation testing (Stryker.NET) on that logic in CI; Emby-side behaviour proved by the owner's checks in checks.md | Logic can be tested without Emby; Emby behaviour needs the real server. Decided by Claude |
| D19 | 2026-11-08 | Code changes go on branches with pull requests; Claude merges only when the build, tests and the fresh-reviewer review pass. Nothing is pushed straight to main from here on | Keeps main always installable. Decided by Claude |
| D20 | 2026-11-08 | Row check (F4 step 6) looks through one user picked in settings; default = the first non-administrator user with no parental controls or other restrictions. A shorter row passes if it reads 1, 2, 3… in order with no gaps | A restricted user's row can be shorter or different without anything being wrong. Owner's choice (Q1) |
| D21 | 2026-11-08 | "Last 7 days" = the 7 full UK-time days before the run day (for the Sunday run: Sunday 00:00 to Saturday 23:59 of the previous week), on Run now as well. Plays are kept for 90 full days | Same window every week; Sunday's early hours count in the next week. Owner's choice (Q2) |
| D22 | 2026-11-08 | The 50% rule is per episode for a series: sittings of the same episode by the same user add up; minutes from different episodes don't | Sampling a show for a few minutes shouldn't count as watching it. Owner's choice (Q3) |
| D23 | 2026-11-08 | A pin counts whatever library it is in, as long as its file exists; the chosen libraries limit only the most-watched and filler picks | A pin is the owner's deliberate choice. Owner's choice (Q4) |

## Still to decide before building
(none)
