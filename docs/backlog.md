---
summary: Findings below Blocker, set aside for the chunk that deals with them (D92). Each chunk's builder fixes or decides its items before the chunk is kept, records the answer in decisions.md, and sets the status here to done.
links: [decisions.md, checks.md, reviews/2026-11-08-readiness-4.md]
---

# Backlog

Chunks are listed in build order (D92): Artwork, Logic, Storage, Proof, EmbyLink, Settings, then Go-live day. A builder starting a chunk reads every open row for it first.

| ID | Severity | Finding | Source | Chunk | Status |
|----|----------|---------|--------|-------|--------|
| B1 | Should fix | When a copy's rank and rank date are written or cleared in managed.json (staying, leaving, refused-delete and adopted entries) isn't defined, so two entries could end up with the same rank after a failed swap. | readiness-4 item 3 | Logic | open |
| B2 | Should fix | Copying each user's watched state onto new and updated copies has no step in the run, no `step` value and no resume rule, and nothing stops the plugin's own writes from being copied back to the real title. | readiness-4 item 4 | Logic | open |
| B3 | Should fix | The row-check requests aren't exact (limit, grouping, recursion, item types, how a series matches its episodes), and it isn't said that the actual row must itself be in rank order 1..n. | readiness-4 item 5 | Logic | open |
| B4 | Should fix | The proof plugin may not answer its own questions: in winter UK time equals UTC, so the owner must name a summer-time play; no plan if results are unclear or IDs don't match; page inputs, step order and "run (c) only once" aren't defined. | readiness-4 item 6 | Proof | open |
| B5 | Should fix | Stryker set-up needs guessing: Logic and Storage share one project (file patterns not given), whether the 80% bar applies while Storage is empty, and the tool version. | readiness-4 item 7 | Logic | open |
| B6 | Should fix | "Unmanaged folder" isn't defined (any folder not on the list, so "Keep me" every week, or only ones with video files?), nor which part decides. | readiness-4 item 8 | Logic | open |
| B7 | Should fix | A damaged managed.json has no recovery path for the owner (deleting it would leave every copy unmanaged for good), and D55's extra rule for that case has no check. | readiness-4 item 9 | Storage | open |
| B8 | Should fix | The 240-character path rule doesn't say which full path is measured (top-picks or staging) and has no rule for a very long film or series folder name. | readiness-4 item 10 | Logic | open |
| B9 | Should fix | C68 (settings page refuses bad pins) is put in chunk 5, which has no automatic tests, so its rule must be a Logic function; and C83 is built in chunk 1 per the Logic README but listed only under chunk 2. | readiness-4 item 11 | Logic | open |
| B10 | Should fix | The order guard is said to cover metadata refreshes, but it only starts after a library scan; which Emby events start it isn't defined. | readiness-4 item 12 | EmbyLink | open |
| B11 | Should fix | D62's no-backdrop "right-hand part" was made exact by D74 (40–100%), but D62 isn't marked. | readiness-4 item 13 | Artwork | open |
| B12 | Should fix | Older rows changed by newer ones still not fully marked: D38's "at the start of the first run" (import now comes first, D80), D28's "first episode .strm" (D90: every .strm), D54's "proved by automatic tests" (narrowed by D73, D79). (D68 is now marked by D94.) | readiness-4 item 13 | Logic | open |
| B13 | Nit | Artwork fallback: whether folder.png/thumb.png become the backdrop re-saved as PNG or are left out, what happens with no backdrop, and fixed `.jpg`/`.png` names when the source is another format. | readiness-4 item 14 | Artwork | open |
| B14 | Nit | JSON file shapes: top-level layout, date format, and whether "none" is null or left out. | readiness-4 item 15 | Storage | open |
| B15 | Nit | `lastSuccessfulRunUtc`: start or finish time, and that a new run.json must carry it forward. | readiness-4 item 16 | Storage | open |
| B16 | Nit | The Specials poster path `Season N\folder.jpg` doesn't fit the `Specials` folder. | readiness-4 item 17 | Logic | open |
| B17 | Nit | C62's automatic half can't catch the real risk: the per-date offset is worked out in EmbyLink, which has no tests. | readiness-4 item 18 | EmbyLink | open |
| B18 | Nit | "Emby's user list order" and what counts as a "movie and TV library" aren't defined. | readiness-4 item 19 | EmbyLink | open |
| B19 | Nit | Code limits aren't stated: only `IsExternalInit` may be polyfilled and warnings are errors, so `required` members and `[NotNullWhen]` can't be used. | readiness-4 item 20 | Artwork | open |
| B20 | Nit | Whether the proof project and its CI job stay after use isn't said. | readiness-4 item 21 | Proof | open |
| B21 | Nit | The EmbyLink README still lists "delete only managed folders" among its guards, though D43 moved all deletes to Storage. | readiness-4 item 22 | EmbyLink | open |
| B22 | Nit | A theme the source gains later is never copied, and one it loses is never removed; probably intended but not stated. | readiness-4 item 23 | Logic | open |
| B23 | Nit | Rescan wait: for a series, whether only the series item or every episode must appear. | readiness-4 item 24 | EmbyLink | open |
| B24 | Nit | A run left unfinished while Emby was off over a week resumes with last week's picks and skips the missed-run rule; probably fine but not stated. | readiness-4 item 25 | Logic | open |
| B25 | Nit | If proof (c) shows an hour out, D77 changes Logic code already merged; who edits it, and in which chunk, isn't said. | readiness-4 item 26 | Proof | open |
| B26 | Nit | Go-live list doesn't mention that C5 needs pinning (series at ranks 1, 5, 10) or that C10 takes several days. | readiness-4 item 27 | Go-live | open |
| B27 | Nit | Chunk 4 has no automatic tests (accepted, D63/D73/D79), and there is no list of the Emby SDK interfaces to use. | readiness-4 item 28 | EmbyLink | open |
