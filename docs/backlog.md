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
| B4 | Should fix | The proof plugin may not answer its own questions: in winter UK time equals UTC, so the owner must name a summer-time play; no plan if results are unclear or IDs don't match; page inputs, step order and "run (c) only once" aren't defined. | readiness-4 item 6 | Proof | open – owner's steps on the server; the proof builder drafts them and the owner confirms |
| B5 | Should fix | Stryker set-up needs guessing: Logic and Storage share one project (file patterns not given), whether the 80% bar applies while Storage is empty, and the tool version. | readiness-4 item 7 | Logic | open |
| B6 | Should fix | "Unmanaged folder" isn't defined (any folder not on the list, so "Keep me" every week, or only ones with video files?), nor which part decides. | readiness-4 item 8 | Logic | open |
| B7 | Should fix | A damaged managed.json has no recovery path for the owner (deleting it would leave every copy unmanaged for good), and D55's extra rule for that case has no check. | readiness-4 item 9 | Storage | settled by D95 (owner) |
| B8 | Should fix | The 240-character path rule doesn't say which full path is measured (top-picks or staging) and has no rule for a very long film or series folder name. | readiness-4 item 10 | Logic | open |
| B9 | Should fix | C68 (settings page refuses bad pins) is put in chunk 5, which has no automatic tests, so its rule must be a Logic function; and C83 is built in chunk 1 per the Logic README but listed only under chunk 2. | readiness-4 item 11 | Logic | open |
| B10 | Should fix | The order guard is said to cover metadata refreshes, but it only starts after a library scan; which Emby events start it isn't defined. | readiness-4 item 12 | EmbyLink | open |
| B11 | Should fix | D62's no-backdrop "right-hand part" was made exact by D74 (40–100%), but D62 isn't marked. | readiness-4 item 13 | Artwork | done: D62 marked (D99) |
| B12 | Should fix | Older rows changed by newer ones still not fully marked: D38's "at the start of the first run" (import now comes first, D80), D28's "first episode .strm" (D90: every .strm), D54's "proved by automatic tests" (narrowed by D73, D79). (D68 is now marked by D94.) | readiness-4 item 13 | Logic | open |
| B13 | Nit | Artwork fallback: whether folder.png/thumb.png become the backdrop re-saved as PNG or are left out, what happens with no backdrop, and fixed `.jpg`/`.png` names when the source is another format. | readiness-4 item 14 | Artwork | settled by D98 |
| B14 | Nit | JSON file shapes: top-level layout, date format, and whether "none" is null or left out. | readiness-4 item 15 | Storage | open |
| B15 | Nit | `lastSuccessfulRunUtc`: start or finish time, and that a new run.json must carry it forward. | readiness-4 item 16 | Storage | open |
| B16 | Nit | The Specials poster path `Season N\folder.jpg` doesn't fit the `Specials` folder. | readiness-4 item 17 | Logic | open |
| B17 | Nit | C62's automatic half can't catch the real risk: the per-date offset is worked out in EmbyLink, which has no tests. | readiness-4 item 18 | EmbyLink | open |
| B18 | Nit | "Emby's user list order" and what counts as a "movie and TV library" aren't defined. | readiness-4 item 19 | EmbyLink | open |
| B19 | Nit | Code limits aren't stated: only `IsExternalInit` may be polyfilled and warnings are errors, so `required` members and `[NotNullWhen]` can't be used. | readiness-4 item 20 | Artwork | settled by D97(g) |
| B20 | Nit | Whether the proof project and its CI job stay after use isn't said. | readiness-4 item 21 | Proof | open |
| B21 | Nit | The EmbyLink README still lists "delete only managed folders" among its guards, though D43 moved all deletes to Storage. | readiness-4 item 22 | EmbyLink | open |
| B22 | Nit | A theme the source gains later is never copied, and one it loses is never removed; probably intended but not stated. | readiness-4 item 23 | Logic | open |
| B23 | Nit | Rescan wait: for a series, whether only the series item or every episode must appear. | readiness-4 item 24 | EmbyLink | open |
| B24 | Nit | A run left unfinished while Emby was off over a week resumes with last week's picks and skips the missed-run rule; probably fine but not stated. | readiness-4 item 25 | Logic | open |
| B25 | Nit | If proof (c) shows an hour out, D77 changes Logic code already merged; who edits it, and in which chunk, isn't said. | readiness-4 item 26 | Proof | open |
| B26 | Nit | Go-live list doesn't mention that C5 needs pinning (series at ranks 1, 5, 10) or that C10 takes several days. | readiness-4 item 27 | Go-live | open |
| B27 | Nit | Chunk 4 has no automatic tests (accepted, D63/D73/D79), and there is no list of the Emby SDK interfaces to use. | readiness-4 item 28 | EmbyLink | open |
| B28 | Nit | Old flag wording left: C92 still says `TakeoverDone` false with no D93 mark; D90 and D80 bodies not marked; PascalCase and camelCase names for the same fields with no note. | fixcheck-4 item 3 | Logic | open |
| B29 | Nit | D94(b): the uninstall message at start-up must be held in memory (not stated); Run now in that state writes `lastRefusal`, so C44 should be done before C19 on go-live day. | fixcheck-4 item 4 | Go-live | open |
| B30 | Nit | With state.json unrecoverable (D95's fallback), whether the order guard keeps running isn't said. | fixcheck-4 item 5 | EmbyLink | open |
| B31 | Nit | How a refusal's `lastRefusal` write and a working run's own run.json saves avoid overwriting each other during `alreadyRunning`. | fixcheck-4 item 6 | Storage | open |
| B32 | Low | The accepted libwebp advisory (GHSA-j7hp-h8jx-5ppr) is in the WebP decoder; Artwork still decodes WebP bytes with Emby's copy. Emby already decodes the same images with the same library, so nothing new is exposed. Optional hardening: treat WebP as unreadable, and say so in D97(c) either way. | chunk 3 review item 3 | Artwork (next change) | open |
| B33 | Nit | Layout values not in any doc: title line spacing 1.05 em, ascent 1.10 em, descent 0.13 em; shadow alpha 190 (74.5%, D97(b) says 75%). Add them to D97(b) or set alpha 191. | chunk 3 review item 5 | Artwork (next change) | open |
| B34 | Nit | SKCodec ignores JPEG EXIF orientation, so a rotated backdrop or logo would be drawn sideways. Note it in D97(a) or apply `codec.EncodedOrigin`. | chunk 3 review item 6 | Artwork (next change) | open |
| B35 | Nit | D98 saves the backdrop's bytes as the fallback even when the failure *was* an unreadable backdrop (no poster), so the fallback picture can be blank. `ArtworkResult` doesn't say which input was unreadable; Logic's fallback plan needs that (a reason code on `ArtworkResult`, or Logic picks only a source Artwork could read). | chunk 3 review item 7 | Logic | open |
| B36 | Nit | Commit 6faf7f0 (the "revert") also changed `if-no-files-found` to `error` without saying so; the no-SkiaSharp CI check has no canary and checks only file names. | chunk 3 review item 10 | Logic (chunk 1 CI work) | open |
| B37 | Nit | Title and shadow tests are thin: no canary feeds the title checks a wrong picture; "largest size from 160 in 2 px steps" is untested; `Very_long_title_still_draws` only checks "not failed"; the D97(b) shadow has no test. | chunk 3 review item 11 | Artwork (next change) | open |
