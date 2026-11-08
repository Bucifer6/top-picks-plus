# Storage – the plugin's own files

**Responsible for:** reading and writing `managed.json`, `plays.json` and `run.json` in `programdata\plugins\TopPicksPlus\`, and **every file change in the Top Picks folder and staging** (D43): writing copy files into staging, emptying staging, moving folders in, rewriting files in staying copies, and deleting folders and files only after the D37 checks, never following junctions or links. It carries out the swap and resume steps that Logic plans (D26, D39, D40). Every JSON write is atomic: write a temp file, then rename over the old one.

**Belongs here:** file shapes matching docs/data.md, safe load (a missing file means empty; a damaged file is reported, never silently replaced), atomic save.
**Does not belong:** deciding anything (that's Logic), plugin settings (Emby keeps those in `plugins\configurations\TopPicksPlus.xml` via the project root), talking to Emby (rescans, dates, events: EmbyLink).
**May use:** Logic (for its record types), .NET.
**Must not use:** Emby SDK, SkiaSharp, Artwork, EmbyLink, Settings, project root.
**JSON:** System.Text.Json, compiled against but not shipped (D24). **Damaged files:** D32. **Tests:** tests/TopPicksPlus.Tests/Storage (D36, D43), with temporary folders; junction tests run on the Windows CI job.
**Data:** play log holds user IDs and viewing days; it never leaves this folder (F9).
