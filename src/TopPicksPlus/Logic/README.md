# Logic – the pure rules

**Responsible for:** the decisions the plugin makes, as plain C# that never touches Emby, files or pictures, so all of it can be tested automatically (D18):
- choosing the 10: pins, viewer-day scores, tie-breaks, filler, candidate rules (spec F2);
- deciding whether a user counts for a title on a day (50% / 20 minutes / marked played, F6) and the 90-day trim;
- folder names: illegal characters, trailing dots and spaces, missing year, ` - 2` clashes (F3);
- rank times: pick 1 = run time, each pick one hour earlier (F4, card 0001);
- the first-run takeover rule: a folder counts as the old plugin's if it holds at least one .strm and one .nfo (F9);
- which copies drop out, stay or are new (F4 step 2); whether a copy's date needs resetting (F5); the missed-run rule (F1).

**Belongs here:** functions that take plain values (IDs, dates, numbers, names) and return answers.
**Does not belong:** reading or writing files, calling Emby, drawing, settings pages, clocks (pass the time in), "now" or the UK time zone lookup (pass the UK date in).
**May use:** .NET only.
**Must not use:** Emby SDK, SkiaSharp, any other part of the plugin.
**Tests:** `tests/TopPicksPlus.Tests/Logic`, with mutation testing (Stryker.NET) on this folder.
