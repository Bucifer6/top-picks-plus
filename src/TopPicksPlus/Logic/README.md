# Logic – the pure rules

**Responsible for:** the decisions the plugin makes, as plain C# that never touches Emby, files or pictures, so all of it can be tested automatically (D18):
- choosing the 10: pins, viewer-day scores, tie-breaks, filler, candidate rules (spec F2);
- planning the swap, resume and leftovers as an ordered list of file steps for Storage to carry out (D26, D39, D40, D43);
- refusals (including `librarySetup`, D53, and `stateDamaged`, D93) and the one refusal rule: guards at the start of every run and resume, a refusal leaves the waiting run unchanged and never uses a resume, a problem found mid-step is an error that is retried (D94); the missed-run rule (D41), the retry limit (D44) and the row-check retry (D45);
- episode and file naming (D49) and the `<dateadded>` text from a UTC date plus a given offset (D50);
- deciding whether a user counts for a title on a day (50% / 20 minutes / marked played, F6) and the 90-day trim;
- folder names: illegal characters, trailing dots and spaces, missing year, ` - 2` clashes (F3);
- rank times: pick 1 = run time, each pick one hour earlier (F4, card 0001);
- the first-run takeover rule: a folder counts as the old plugin's if it, including its subfolders, holds at least one .strm and one .nfo (F9, D38); whether a folder name is safe to delete (D37);
- the text of every .strm and .nfo (D30); the 7-day and 90-day windows (D21); the per-episode 50% rule (D22); the resume plan for an unfinished run (D26);
- which copies drop out, stay or are new (F4 step 2); whether a copy's date needs resetting (F5); the missed-run rule (F1, including the damaged-run.json case, D90); whether a run does the takeover, including when managed.json is missing (D123);
- the order of a run (guards → import → takeover → leftovers → prepare …) and what a resume repeats at each step (D80); counting resumes (D78); which status wins (D84);
- **the rules behind chunk 4's automatic checks (D79), built in chunk 1:** the default row-check user from a plain user list (D20, C57); the old-plugin match on assembly name or ID (D33, C74); what the old-plugin guard and "data newer" stop (D51, D85, C63, C83); crediting a play through the managed list, then the .strm path (D64, C71); ignoring imported rows whose item has gone; the import window up to `ListeningSinceUtc` (D91); the row-check comparison of ranked copies and its extras (D83);
- the name rules for file paths inside a copy: relative, plain names, no `.`/`..`, no forbidden characters (D82; Storage does the rest).

**Belongs here:** functions that take plain values (IDs, dates, numbers, names) and return answers.
**Does not belong:** reading or writing files, calling Emby, drawing, settings pages, the clock (`DateTime.Now`: pass the time in), the UK time-zone lookup (pass the UK date in). Files, network and time-zone use are blocked by the boundary check; the clock rule is checked in review.
**May use:** .NET only.
**Must not use:** Emby SDK, SkiaSharp, System.IO (including `Path`), System.Net, TimeZoneInfo, Newtonsoft, Microsoft.Extensions, SQLitePCL, any other part of the plugin.
**Note for folder-name rules (F3):** write the list of characters Windows forbids into Logic yourself; don't use `Path.GetInvalidFileNameChars()`, which is blocked here and returns almost nothing on the Linux build machine.
**Records:** the plain record types the rules work on (pick, play, managed entry) live here, with no JSON attributes; Storage maps them to the file shapes (D88). Logic uses no packages at all.
**Files (chunk 1):** `Records.cs` (plain sealed classes, D113), `Choosing.cs` (F2, pin rules D107), `Counting.cs` (F6, D21, D22, D29), `Naming.cs` (F3, D49, D106, D109), `PathRules.cs` (D37, D82), `CopyText.cs` (rank times D27, `<dateadded>` D50/D67, .nfo/.strm text D30/D90, order guard D52), `RunRules.cs` (guards D94/D116, triggers and resumes D68/D78, start-up D35, missed run D41/D90, status D84), `SwapPlan.cs` (swap, leftovers and staying-file plans D26/D39/D40/D101/D110), `Takeover.cs` (D38, D90, unmanaged folders D102), `EmbyRules.cs` (chunk 4's rules: row-check user, old plugin, what stops, crediting, import, row check D104, watched echo D103, artwork fallback D112).
**Tests:** `tests/TopPicksPlus.Tests/Logic`, each tagged `[Trait("Check", "Cxx")]`. Mutation testing: Stryker.NET 4.16.0 (`.config/dotnet-tools.json`, settings in `tests/TopPicksPlus.Tests/stryker-config.json`) on Logic and Storage in the Windows job, failing below 80%; incremental on pull requests, full on release tags (D66, D105).
