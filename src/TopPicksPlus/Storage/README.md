# Storage – the plugin's own files

**Responsible for:** reading and writing `managed.json`, `plays.json` and `run.json` in `programdata\plugins\TopPicksPlus\`, and emptying `staging\` at the start of a run (docs/data.md). Every write is atomic: write a temp file, then rename over the old one.

**Belongs here:** file shapes matching docs/data.md, safe load (a missing file means empty; a damaged file is reported, never silently replaced), atomic save.
**Does not belong:** deciding anything (that's Logic), plugin settings (Emby keeps those in `plugins\configurations\TopPicksPlus.xml` via the project root), anything that deletes copies in the Top Picks folder (EmbyLink does that, only for folders on the managed list).
**May use:** Logic (for its record types), .NET.
**Must not use:** Emby SDK, SkiaSharp, Artwork, EmbyLink, Settings.
**Data:** play log holds user IDs and viewing days; it never leaves this folder (F9).
