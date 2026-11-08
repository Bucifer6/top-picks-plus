# Run

## Responsible for
Carrying out the weekly update in order (F4): guards and the offline-drive check, backup, count, choose, plan (written to run.json first), prepare in staging, swap in (one operation at a time, each marked done), rescan, set dates, refresh artwork, check the row, record. Only one run at a time (F1); resuming an unfinished run after a restart (F5); the order guard after scans and at start-up (F5); the missed-run rule (F1); the first-run takeover (F9); the delete guard before every delete or replace: managed only, single name, real location inside the Top Picks folder or staging, never through a junction or link (F9).

## Belongs here
The step-by-step orchestration and the writing of copy folders (.strm, .nfo, subtitles, artwork files) into staging and the Top Picks folder.

## Does not belong
The rules themselves (Logic), Emby calls (go through EmbyBridge's interfaces), drawing (Artwork).

## May use
Logic, Store (through `IStore`), Artwork (through `IArtwork`), EmbyBridge (its interfaces), plain .NET file access for the Top Picks folder and staging only.

## Must not use
Settings, any `MediaBrowser.*` type directly, SkiaSharp directly.
