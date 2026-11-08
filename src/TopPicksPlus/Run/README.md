# Run

## Responsible for
Carrying out the weekly update in order (F4): prepare in staging, swap in (delete drop-outs, update stayers in place, move new ones in), rescan, set dates, refresh artwork, check the row, record. Only one run at a time (F1); resuming an unfinished run after a restart (F5); the order guard after scans and at start-up (F5); the missed-run rule (F1); the first-run takeover (F9); deleting only managed folders inside the Top Picks folder (F9).

## Belongs here
The step-by-step orchestration and the writing of copy folders (.strm, .nfo, subtitles, artwork files) into staging and the Top Picks folder.

## Does not belong
The rules themselves (Logic), Emby calls (go through EmbyBridge's interfaces), drawing (Artwork).

## May use
Logic, Store, Artwork, EmbyBridge (its interfaces).

## Must not use
Settings, any `MediaBrowser.*` type directly, SkiaSharp directly.
