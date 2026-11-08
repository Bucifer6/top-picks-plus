# Logic – the pure rules

**Responsible for:** the decisions the plugin makes, as plain C# that never touches Emby, files or pictures, so all of it can be tested automatically (D18):
- choosing the 10: pins, viewer-day scores, tie-breaks, filler, candidate rules (spec F2);
- planning the swap, resume and leftovers as an ordered list of file steps for Storage to carry out (D26, D39, D40, D43);
- refusals (including `librarySetup`, D53), the missed-run rule (D41), the retry limit (D44) and the row-check retry (D45);
- episode and file naming (D49) and the `<dateadded>` text from a UTC date plus a given offset (D50);
- deciding whether a user counts for a title on a day (50% / 20 minutes / marked played, F6) and the 90-day trim;
- folder names: illegal characters, trailing dots and spaces, missing year, ` - 2` clashes (F3);
- rank times: pick 1 = run time, each pick one hour earlier (F4, card 0001);
- the first-run takeover rule: a folder counts as the old plugin's if it, including its subfolders, holds at least one .strm and one .nfo (F9, D38); whether a folder name is safe to delete (D37);
- the text of every .strm and .nfo (D30); the 7-day and 90-day windows (D21); the per-episode 50% rule (D22); the resume plan for an unfinished run (D26);
- which copies drop out, stay or are new (F4 step 2); whether a copy's date needs resetting (F5); the missed-run rule (F1).

**Belongs here:** functions that take plain values (IDs, dates, numbers, names) and return answers.
**Does not belong:** reading or writing files, calling Emby, drawing, settings pages, the clock (`DateTime.Now`: pass the time in), the UK time-zone lookup (pass the UK date in). Files, network and time-zone use are blocked by the boundary check; the clock rule is checked in review.
**May use:** .NET only.
**Must not use:** Emby SDK, SkiaSharp, System.IO (including `Path`), System.Net, TimeZoneInfo, Newtonsoft, Microsoft.Extensions, SQLitePCL, any other part of the plugin.
**Note for folder-name rules (F3):** write the list of characters Windows forbids into Logic yourself; don't use `Path.GetInvalidFileNameChars()`, which is blocked here and returns almost nothing on the Linux build machine.
**Records:** the plain record types the rules work on (pick, play, managed entry) live here; Storage saves and loads them.
**Tests:** `tests/TopPicksPlus.Tests/Logic`. Mutation testing (Stryker.NET) is not in CI yet: chunk 1 adds it, pinned in a dotnet tool manifest, on Logic and Storage, in the Windows job, failing below 80% (D66).
