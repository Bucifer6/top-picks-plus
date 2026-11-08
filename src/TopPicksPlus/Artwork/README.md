# Artwork – the numbered pictures

**Responsible for:** drawing `folder.png` and `thumb.png` at 1000×562 (spec F3, D6): backdrop darkened on the left, big white rank number, logo over the picture; title text in capitals when there's no logo (D100); poster on dark when there's no backdrop. Layout exactly as D62 and D74; numbers in the embedded Anton font.

**Belongs here:** `RankArtwork.Draw(backdrop, logo, poster, rank, title)`, which takes image bytes in and gives an `ArtworkResult` out (PNG bytes, or failed with a reason, D97), its layout constants (`Layout`), the title fitting (`TitleLayout`), plus the embedded font `Anton-Regular.ttf` and its licence `OFL.txt`.
**Does not belong:** finding the backdrop or logo in Emby, saving files into copy folders (EmbyLink finds them; Storage saves the files, D43), deciding the rank.
**May use:** Logic, SkiaSharp. SkiaSharp is compiled against but **not shipped** (D15): the plugin uses the copy Emby already loads. The package reference excludes runtime and native assets and is private, so nothing ships; CI fails if a Skia file lands next to the DLL (D97).
**Must not use:** Emby SDK, Storage, EmbyLink, Settings, project root.
**Versions (D34):** SkiaSharp 2.88.3 exactly (matches Emby 4.10.2); font Anton (SIL OFL 1.1, D62) with its licence file in this folder. Tests use native assets for Linux and Windows (D65).
**If drawing fails:** return "failed" so the plain backdrop (or poster) is saved as folder.png/thumb.png and the report says so (D15, D98). Never throw out of a run. Code limits for netstandard2.0: D97(g).

**Layout:** D62, D74 and D97(b), measured from docs/reference/artwork/. Tests: tests/TopPicksPlus.Tests/Artwork (C43, layout checks and their canaries, C65 samples).
