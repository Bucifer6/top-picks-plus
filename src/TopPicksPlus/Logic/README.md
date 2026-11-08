# Logic

## Responsible for
The rules, as plain calculations with no side effects: choosing the 10 (F2: pins, viewer-days, ties, filler, candidates), deciding what counts as watched (F6), the 90-day cut-off, folder names (F3), rank dates (rank time minus 0–9 hours), which old folders to take over (F9), and the order guard's "which dates are wrong" comparison (F5).

## Belongs here
Small classes and records that take data in and give an answer back. The current time is passed in, never read from the clock. Everything here gets xUnit tests and Stryker mutation testing (D18).

## Does not belong
Reading or writing files, talking to Emby, drawing pictures, waiting, timers.

## May use
Nothing else in the plugin. Plain .NET (collections, strings, dates).

## Must not use
Store, Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` (Emby) type, `System.IO.File`/`Directory`, networking, `Task`/async, SkiaSharp.
