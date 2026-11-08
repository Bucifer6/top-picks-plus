# Run

## Responsible for
Carrying out the weekly update in the order of spec F4: create run.json, guards, roll-up, backup, takeover, import, choose, plan (written to run.json first), prepare in staging, swap (one operation at a time, each marked done), rescan, set dates and reset played state, refresh artwork, check the row, record. The one run lock (D32); resuming by run.json `step` (F5); the order guard (F5); the missed-run rule (F1); the write and delete checks before every write, move, delete or replace in the Top Picks folder or staging (F9).

## Belongs here
The step-by-step orchestration, and writing copy folders (.strm, .nfo, subtitles, artwork files) into staging and the Top Picks folder. Source bytes (subtitles, images, theme video) come from EmbyBridge's adapters; Run only writes them.

## Does not belong
The rules themselves (Logic), Emby calls and reading source files (EmbyBridge's interfaces), the plugin's own data files, backups and the takeover zip (Store), drawing (Artwork), settings (it gets a plain settings record).

## May use
Logic, Store (through `IStore`), Artwork (through `IArtwork`), EmbyBridge (its interfaces only), plain .NET file access for the Top Picks folder and staging only.

## Must not use
Settings, any `MediaBrowser.*` type, SkiaSharp, `System.Net`, the root namespace, any file outside the Top Picks folder and staging.
