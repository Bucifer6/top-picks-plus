---
summary: Challenge round on spec v1 (zoom out, critic, newcomer) and how each finding was handled.
links: [../spec.md, ../decisions.md]
---

# Challenge round, 2026-11-08

## Asked the owner
- Playlist/collection section of real titles vs numbered .strm copies (zoom-out Blocker 1) → owner kept copies (D8).

## Fixed in spec v2
- Plays on copies not mapped to source titles (all three, Blocker) → F6 maps via managed list; C11.
- Date-added approach unproven; series may order by episode date (critic B1, zoom-out 3-4, newcomer 4) → F4 step 4 sets series + all episodes; .nfo carries same date; re-check after artwork refresh; proof test owed before build; C4, C5.
- data.md missing (newcomer B2) → written.
- Top Picks library undefined (newcomer B3) → "Words used here".
- Swap/IDs/crash ordering (critic B2, newcomer 6, zoom-out 5) → staging outside libraries, staying titles updated in place, resume-not-rollback, order guard F5; C21.
- Takeover rule undefined (critic 11, newcomer 5) → F9 rule (.strm + .nfo present before first run); C18.
- Timeout behaviour, row check method (newcomer 7-8, critic 8, 13) → F4 wait rules, check via a non-admin user's Latest row request.
- Number of picks contradiction → fixed at 10 (D11).
- Viewer counting unit, day, series ties (all) → viewer-days, UK day, newest-content rule (D10); C10.
- 50% rule & resumes, unknown runtime (critic 10) → cumulative per day, 20-minute fallback; C12.
- Pins vs missing files (critic 6) → skipped, marked missing; C9.
- Folder name clashes/illegal chars (critic 5, newcomer 14) → F3 rules, managed list keyed by source ID.
- Multi-version, subtitles formats (critic 7, newcomer 16) → F3.
- Concurrency (critic 9) → F1 one run at a time; C22.
- Watched/resume split (zoom-out 2, critic 3) → F7 sync; C14.
- Emby updates/SDK pinning (zoom-out 7) → F10; C20.
- Missed runs, server clock (several Nits) → F1.
- Privacy/parental ratings (critic 15, newcomer 21) → F9.
- Missing checks list (critic 12) → C13, C17, C23-C25 added.
- Doc nits (ordering, table, naming) → fixed.

## Contested / skipped
- Zoom-out 6 (read Playback Reporting each week instead of own log): kept own log, reason in D9 (independence from a third-party plugin). Owner may change.
- Zoom-out 8 (disk churn): partly fixed (theme videos copied once); per-episode .strm/.nfo kept because Emby needs them. Skip rest.
- Mapped drive visibility (critic 7): skipped — Emby already plays these files today, so the drives are visible to it.
