# Chunk 3 (Artwork) review (71bde0f)

**Verdict: Keep: yes.** No Critical or High security finding, no Blocker. Two "Should fix" test gaps and one undocumented behaviour are worth fixing before chunk 1 starts. None of them changes what the owner sees in the C65 samples.

Reviewer: fresh agent, 2026-11-08. HEAD confirmed as 71bde0f87dedbf6d214b0bc5c3c7e1b31086b401. Scope: `git diff 65be3f7..71bde0f`. Nothing was compiled here because NuGet is blocked. I read CI run 42503121978 (green, 96 tests) and the deliberate-break run 42502908461 (head 21e56cf, 18 failed). The artifact download was refused (403). I looked at the sample PNGs from the earlier temporary sample-print run instead; the plugin code is identical at 71bde0f. I compared them with docs/reference/artwork/.

## What was checked and found sound

- **No secrets.** Gitleaks passed in CI, and a grep of src/ and tests/ found no keys or tokens.
- **Nothing ships, writes or reaches the network.**
  - The plugin's Artwork code only reads bytes and returns bytes: no file writes, no network calls.
  - The CI output folder holds only TopPicksPlus.dll, .deps.json and .pdb (run log, step "Check the plugin ships no SkiaSharp").
  - The SkiaSharp reference is `ExcludeAssets="runtime;native" PrivateAssets="all"`.
  - Only the test `SampleWriter` writes files.
- **Bad input gets a clean "failed".**
  - `Draw` wraps everything in `catch (Exception)`. This also covers a missing native library, which surfaces as a TypeInitializationException or DllNotFoundException.
  - Decoding is all-or-nothing: an incomplete decode is treated as unreadable.
  - A failed pixel allocation throws inside the try block.
  - The rank is checked before anything is decoded.
- **The temporary commits are fully undone.**
  - `git diff 0a2f184 6faf7f0 -- src` is empty.
  - No `DELIBERATE` or `TEMP` text remains anywhere.
  - The base64 sample-print step from 93b47ca and 7409439 is gone.
  - The only leftover difference from 0a2f184 is `if-no-files-found: warn` → `error`, which tightens the check (finding 10).
- **D97(d)'s break proof is real.** Run 42502908461 ran on 21e56cf and shows 18 failures: number ×10 plus a canary, logo ×3, shade, poster, title and samples.
- **The layout constants in `Layout.cs` match D62 and D74**: shade 22% and 40%; number 3.5%, 22%, 12% and 90%; box 25–92% × 35–94%; poster from 40%; title 160 → 40 px in 2 px steps; 50 M pixel cap. The samples look like the reference row: big heavy number on solid black, logo on the bottom-left of the box. Whether the look is right is still the owner's C65 call.
- **The audit exception is narrow.** It covers SkiaSharp and SkiaSharp.NativeAssets.* at 2.88.3 only, and its self-test proves that another package, or SkiaSharp at another version, still fails. `NuGetAuditSuppress` names the one advisory.

## Findings

1. **Security – Low – `src/TopPicksPlus/Artwork/TitleLayout.cs` lines 29–55 and 88–94 (`Fit` at 40 px, `Shorten`).**
   - **What's wrong:** an extremely long title, especially one huge "word", gets shortened one character at a time, measuring the whole remaining string each time. That is quadratic. A 10,000-character word takes about 10⁸ character measurements (seconds); 100,000 characters would effectively hang the weekly run. Nothing caps the title length.
   - **Why it matters:** the brief says extreme titles must never hang. Titles come from the owner's own Emby metadata, so the risk is low.
   - **Fix:** cut the title to a sane length (about 200 characters) before fitting, or shorten by binary search.

2. **Security – Low – `.github/workflows/build.yml` line 87 (the audit filter).**
   - **What's wrong:** the build.yml filter allows any advisory against SkiaSharp 2.88.3, not just GHSA-j7hp-h8jx-5ppr. Only the restore step names the advisory (`NuGetAuditSuppress`). A second advisory against 2.88.3 would therefore be caught only by restore-time NuGet audit plus TreatWarningsAsErrors. That second defence was not proved here: no global.json yet, and SDK defaults vary.
   - **Why it matters:** D65 means to accept one named advisory, not every future one.
   - **Fix:** also require `GHSA-j7hp-h8jx-5ppr` in the allowed regex, and add a canary line for SkiaSharp 2.88.3 with a different advisory URL.

3. **Security – Low – `src/TopPicksPlus/Artwork/RankArtwork.cs` lines 106–128 (`Decode`).**
   - **What's wrong:** the accepted advisory is the libwebp heap overflow (CVE-2023-4863). `Decode` will decode WebP bytes with Emby's copy of the library, inside Emby's process.
   - **Why the exception still holds:** Emby already decodes the same library images with the same copy, so the plugin adds no new input source or exposure.
   - **Optional hardening:** treat `codec.EncodedFormat == SKEncodedImageFormat.Webp` as unreadable. Emby backdrops are normally JPEG or PNG. Worth a line in D97(c) whichever way it goes.

4. **Drift – Should fix – `src/TopPicksPlus/Artwork/RankArtwork.cs` lines 42, 46 and 65–69; `tests/.../LayoutTests.cs` line 124.**
   - **What's wrong:** with no readable backdrop, the code draws neither the logo nor the title on the poster picture, and a test asserts that. No decision says this. D62, D74, D97(a) and spec F3 only say "the poster, centred on black". Also, the poster is never decoded when a backdrop exists.
   - **Why it matters:** it is a visible behaviour choice. It is probably right, since posters carry their own title art. But it was made silently, and chunk 1's builder can't tell it apart from a bug.
   - **Fix:** record it in D97(a).

5. **Drift – Nit – `src/TopPicksPlus/Artwork/Layout.cs` lines 36–43.**
   - **What's wrong:** these values appear in no doc:
     - title line spacing 1.05 em, ascent 1.10 em, descent 0.13 em;
     - shadow alpha 190 (74.5%; D97(b) says 75%, and 191 would be closer).
   - **Why it matters:** they are layout values a later builder could "correct" without knowing they were chosen.
   - **Fix:** add a line to D97(b).

6. **Drift – Nit – `src/TopPicksPlus/Artwork/RankArtwork.cs` lines 106–128.**
   - **What's wrong:** SKCodec does not apply JPEG EXIF orientation, so a backdrop or logo carrying a rotation tag would be drawn sideways. No doc mentions this.
   - **Why it matters:** rare for Emby/TMDB backdrops.
   - **Fix:** note it in D97(a), or apply `codec.EncodedOrigin`.

7. **Drift – Nit – D98 against `ArtworkResult`, for the chunk 1 backlog.**
   - **What's wrong:** D98 says that when drawing fails "if the source has a backdrop, the backdrop's own bytes are saved". When the failure *is* an unreadable backdrop (and no poster), that saves the unreadable bytes as folder.png and thumb.png. `ArtworkResult` doesn't say which input was unreadable, and Logic can't know.
   - **Why it matters:** the "fallback" picture can be blank, which is exactly the case D98 asks to revisit.
   - **Fix:** a backlog row for Logic; optionally a reason code on `ArtworkResult`.

8. **Test quality – Should fix – `tests/TopPicksPlus.Tests/Artwork/LayoutChecks.cs` lines 18–55 with `TestImages.cs` lines 80–103 (`Grid`).**
   - **What's wrong:** the number shape check is blind to width-to-height ratio. `Grid` stretches the drawn number's bounding box and the template's onto the same 40×64 grid. A digit stretched or squeezed sideways therefore matches Anton perfectly. The only width check is "right edge ≤ 22%". A real mistake would pass every test: for example, scaling every number to fill the whole 3.5–22% band (non-uniform `sx`), or halving `sx`. That would make "1" a fat block, contrary to D97(b)'s "squeezed sideways only when it is wider".
   - **Why it matters:** the number is the main feature of the picture.
   - **Fix:** also compare the drawn box's width-to-height ratio with the template's (within about 5%) for 1–9, and require 10 to end at about 22%. Add a canary that stretches a "1".

9. **Test quality – Should fix – `tests/TopPicksPlus.Tests/Artwork/FailureTests.cs` lines 27 and 98–113.**
   - **What's wrong:** the "PNG claiming 60000x60000" case stops after the IHDR chunk with no image data. SkiaSharp most likely refuses to create a codec for it, so it returns "failed" before the plugin's 50 M pixel cap (`RankArtwork.cs` line 115) is reached. Deleting the cap would leave every test green.
   - **Why it matters:** the cap is D97(a)'s guard against decompression bombs and running out of memory, and it is the only one.
   - **Fix:** add a real, fully compressed solid-colour PNG just over the cap (for example 7100×7100, a few tens of KB) and assert it fails. Optionally add one just under the cap that draws.

10. **Test quality – Nit – the 6faf7f0 commit and `build.yml` lines 57 and 60–65.**
    - **What's wrong:**
      - The "revert" commit also changed `if-no-files-found: warn` → `error`, which its message doesn't mention. The end state is better, but it is not a pure revert.
      - The no-SkiaSharp check has no canary (card 0003) and only looks at file names.
    - **Why it matters:** low. The log shows the check really listed the folder.

11. **Test quality – Nit – the title and shadow tests.**
    - **What's wrong:**
      - No canary feeds the title checks a wrong picture. In the break run, the title test failed only because of the number.
      - "Largest size from 160 in 2 px steps" is untested: `TitleLayoutTests.cs` lines 32–38 only assert size < 160.
      - `FailureTests.Very_long_title_is_cut_with_an_ellipsis_and_still_draws` (lines 74–80) only asserts "not failed", so its name over-claims.
      - The D97(b) shadow (75%, sigma 6, 3 px offset) has no test at all.
    - **Why it matters:** a wrong title size, or a missing shadow, would pass CI. The owner's C65 look is the backstop.

12. **Drift – Nit – `CLAUDE.md` Docs list.**
    - **What's wrong:** it says checks.md holds C1–C97, but it holds C1–C100 (C98–C100 were added at Stage 5). This predates the chunk, but the chunk edited that list (D1–D99) and missed it.

## Not findings (noted)

- The shade is also drawn on the poster picture (D97(b)). With the poster limited to 40–100% it is black on black, so it does nothing but is harmless.
- The no-logo samples ("Beast", "By Any Means") show the title as written in mixed case, while today's row shows logos in capitals. D97(b) chose "as written". That is for the owner to judge at C65.

## How it was handled

Handled on build/chunk3-artwork by the chunk 3 builder, 2026-11-08. The owner approved the C65 samples (run 42503121978) the same day; that is recorded under C65 in checks.md.

- **"Not findings", title case:** the no-logo title is now drawn in capitals. This is D100 (decided by Claude; the owner had no preference). D97(b) is amended and marked. Covered by a new test (`Title_is_written_in_capitals`), and the samples are regenerated.
- **1 (Low), quadratic title shortening:** fixed. Titles are cut to 200 characters (ending in "…") before fitting. New tests: one shows the fitter never measures more than about 200 characters, and one shows a 100,000-character title draws within 5 seconds.
- **2 (Low), audit filter:** fixed. The build.yml exception now also requires GHSA-j7hp-h8jx-5ppr. Its self-test has a canary line for SkiaSharp 2.88.3 with a different advisory, which must stay flagged.
- **3 (Low), WebP decoding:** moved to the backlog as B32 (Artwork, next change).
- **4 (Should fix), poster picture behaviour:** now written in D97(a): the poster picture draws no logo or title, and the poster isn't read when a backdrop is readable.
- **5 (Nit), undocumented layout values:** moved to the backlog as B33.
- **6 (Nit), EXIF orientation:** moved to the backlog as B34.
- **7 (Nit), D98 fallback with an unreadable backdrop:** moved to the backlog as B35 (Logic).
- **8 (Should fix), number width-to-height ratio:** fixed. `NumberProblem` now checks the drawn width against Anton's own ratio, squeezed only to fit the band. That means 1–9 keep their shape and 10 fills the band to 22%. New canaries: a stretched "1", a squeezed "4", a "10" short of 22%, and a "1" a quarter too wide.
- **9 (Should fix), pixel cap untested:** fixed. A real, compressed 7072×7072 PNG (just over 50 M pixels) must fail, and a 7000×7000 one must draw.
- **10 (Nit), revert commit and no-SkiaSharp check:** moved to the backlog as B36 (chunk 1 CI work).
- **11 (Nit), title and shadow tests:** moved to the backlog as B37.
- **12 (Nit), CLAUDE.md C range:** fixed (C1–C100).
