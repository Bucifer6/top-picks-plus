# Logic

## Responsible for
The rules, as plain calculations with no side effects: choosing the 10 (F2: pins, viewer-days, ties, filler, candidates), deciding what counts as watched (F6), the 90-day cut-off, the one naming rule `SafeName` and every copy, season and episode name (F3), the order chain and filler (F2 step 3–4), the UK-day and 7-day window rules with the time zone passed in, rank dates (rank time minus 0–9 hours), the order guard's "which dates are wrong" comparison (F5), the roll-up sums (F6), the path checks used before writes and deletes (F9), the run step to resume from, the give-up-after-3 rule and which guard failures are `refused` or `failed` on resume (F5, D36), the run status rules (`finished` / `incomplete`, spec "Words used here"), whether an item path is inside the Top Picks folder and the Top Picks library comparisons (F9), the settings save rules and the administrator rule (F8), pin clean-up for a hand-edited settings file (F2 step 1), the data.md field rules that decide when a file is damaged (F9), the swap order (deletes, updates, adds) and which files an update must remove (F4 step 7), the row-check verdict and the choice of check user from a plain list of users (F4 step 11), stale entries in the plan, the target date (rank date or stale date, D53) for step 9 and the order guard, the path tidy and the single-name rule (spec "Words used here", D52), and building each .nfo's text with an XML writer (F3; Run writes it to disk).

## Belongs here
Small classes and records that take data in and give an answer back. Also the plain shared types other parts pass around: the paths record, the settings record and the log interface (Logic defines them but never logs). The current time is passed in, never read from the clock. Everything here gets xUnit tests and Stryker mutation testing (D18).

## Does not belong
Reading or writing files, talking to Emby, drawing pictures, waiting, timers.

## May use
Nothing else in the plugin. Plain .NET (collections, strings, dates).

## Must not use
Store, Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` or `Emby.*` (Emby) type, any file access (`System.IO.File`, `Directory`, `FileStream`, `FileInfo`, `DirectoryInfo`), networking, `System.Diagnostics.Process`, `Task`/async, SkiaSharp.
