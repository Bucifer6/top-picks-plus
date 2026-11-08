# Settings – admin page and run report

**Responsible for:** the Dashboard → Plugins → Top Picks Plus page (F8): library choice (Top Picks library never offered), pins (search, add, drag to reorder, remove, "missing" marks), Run now with the in-progress indicator, and the last run report. Administrators only, checked on the server for every request.

**Belongs here:** the page's HTML and script (embedded resources) and the server endpoints it calls.
**Does not belong:** running the refresh (it asks EmbyLink), rules (Logic), file formats (Storage).
**May use:** Logic, Storage, EmbyLink, project root, Emby SDK.
**Must not use:** Artwork, SkiaSharp.
**Personal data:** the play counts shown here are for administrators only (F9).
