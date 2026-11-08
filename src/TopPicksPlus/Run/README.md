# Run

## Responsible for
Carrying out the weekly update in the order of spec F4: create run.json, guards, roll-up, backup, choose, plan (written to run.json first), prepare in staging, swap (deletes, updates, adds; one operation at a time, each marked done), rescan, set dates and reset played state, refresh artwork, check the row, record. The one run lock and "waiting to resume" (D32, D36); resuming by run.json `step`, giving up after 3 resumes (F5); the order guard (F5); the missed-run rule (F1); handling a watched-state change on a copy, forward only (F7, D38); catching every error so none reaches Emby (F4); the write and delete checks before every write, move, delete or replace in the Top Picks folder or staging (F9).

## Belongs here
The step-by-step orchestration, and writing copy folders (.strm, .nfo, subtitles, artwork files) into staging and the Top Picks folder. Source bytes (subtitles, images, theme video) come from EmbyBridge's adapters; Run only writes them.

## Does not belong
The rules themselves (Logic), Emby calls and reading source files (EmbyBridge's interfaces), the plugin's own data files and backups (Store), drawing (Artwork), settings (it gets a plain settings record).

## May use
Logic, Store (through `IStore`), Artwork (through `IArtwork`), EmbyBridge (its interfaces only), plain .NET file access for the Top Picks folder and staging only.

## Must not use
Settings, any `MediaBrowser.*` or `Emby.*` type, EmbyBridge's concrete classes (interfaces only), SkiaSharp, `System.Net`, `System.Diagnostics.Process`, the root namespace, any file outside the Top Picks folder and staging.
