# Artwork – the numbered pictures

**Responsible for:** drawing `folder.png` and `thumb.png` at 1000×562 (spec F3, D6): backdrop darkened on the left, big white rank number, logo over the picture; title text when there's no logo; poster on dark when there's no backdrop. Numbers in an embedded SIL OFL bold font.

**Belongs here:** a function that takes image bytes in and gives PNG bytes out, plus the embedded font.
**Does not belong:** finding the backdrop or logo in Emby, saving files into copy folders (EmbyLink does both), deciding the rank.
**May use:** Logic, SkiaSharp. SkiaSharp is compiled against but **not shipped** (D15): the plugin uses the copy Emby already loads. The package reference is added in the Artwork build chunk, with runtime assets excluded.
**Must not use:** Emby SDK, Storage, EmbyLink, Settings, project root.
**Open (before the Artwork chunk):** the SkiaSharp version must match the SkiaSharp.dll in Emby 4.10.2's system folder; the font's licence file goes in the repo with it.
**If drawing fails:** return "failed" so EmbyLink uses the plain backdrop and the report says so (D15). Never throw out of a run.
