# Logic

## Responsible for
The rules, as plain calculations with no side effects: choosing the 10 (F2: pins, viewer-days, ties, filler, candidates), deciding what counts as watched (F6), the 90-day cut-off, the one naming rule `SafeName` and every copy, season and episode name (F3), the order chain and filler (F2 step 3–4), the UK-day and 7-day window rules with the time zone passed in, rank dates (rank time minus 0–9 hours), which old folders to take over (F9), and the order guard's "which dates are wrong" comparison (F5).

## Belongs here
Small classes and records that take data in and give an answer back. Also the plain shared types other parts pass around: the paths record and the log interface (Logic defines them but never logs). The current time is passed in, never read from the clock. Everything here gets xUnit tests and Stryker mutation testing (D18).

## Does not belong
Reading or writing files, talking to Emby, drawing pictures, waiting, timers.

## May use
Nothing else in the plugin. Plain .NET (collections, strings, dates).

## Must not use
Store, Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` (Emby) type, `System.IO.File`/`Directory`, networking, `Task`/async, SkiaSharp.
