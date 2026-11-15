# Settings – admin page and run report

**Responsible for:** the Dashboard → Plugins → Top Picks Plus page (F8): library choice (Top Picks library never offered), the row-check user (D20), pins (search, add, move up and down, remove, "missing" marks), Run now with the in-progress indicator, and the last run report. Administrators only: the page is Emby's own plugin settings page, drawn by Emby's plugin UI from C# (D148), so there is no web endpoint of the plugin's own to secure; checked at go-live by C67 (D79). Report field names: data.md and D81.

**Belongs here:** the page's fields (`SettingsPage.cs`), what its buttons do and saving (`SettingsView.cs`, with `SettingsController`), and the report text (`ReportText.cs`).
**Does not belong:** running the refresh (it asks EmbyLink, whose guards decide, D94), rules (Logic: pins use `PinRules.CheckAdd`, D107), file formats (Storage), the plugin's own flags (Storage's state.json; Settings never writes it, D93). Settings only reads run.json.
**May use:** Logic, Storage, EmbyLink, project root, Emby SDK.
**Must not use:** Artwork, SkiaSharp.
**Personal data:** the play counts and user names shown here are for administrators only (F9).
**Saving:** libraries and the row-check user are saved with the page's Save button; pin changes are saved at once. All three live in Emby's plugin settings (`PluginConfiguration`), which holds only the owner's settings (D93).
