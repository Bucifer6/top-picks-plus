# Run

## Responsible for
Carrying out the weekly update in the order of spec F4: create run.json, guards, roll-up, backup, choose, plan (written to run.json first), prepare in staging, swap (deletes, updates, adds; one operation at a time, each marked done), rescan, set dates, refresh artwork, check the row, record. The one run lock and "waiting to resume" (D32, D36); resuming by run.json `step`, giving up on the third stop of the same run (F5, D36); the order guard, one managed entry at a time under the lock, stopping when a run holds it (F5, D51), and the stale date for stale copies (D53); the missed-run rule (F1); the in-memory playback-started list and turning play events into sittings (F6); checking the Top Picks folder isn't a link (F9); never changing anyone's watched state (D39); catching every error so none reaches Emby (F4); the write and delete checks before every write, move, delete or replace in the Top Picks folder or staging, run on an operation's own folders before its first change, with links found inside removed as links during the operation (F9, F4 step 7); turning a skipped delete or update's entry `stale` (D44).

## Belongs here
The step-by-step orchestration, and writing copy folders (.strm, .nfo, subtitles, artwork files) into staging and the Top Picks folder. Source bytes (subtitles, images, theme video) come from EmbyBridge's adapters; Run only writes them.

## Does not belong
The rules themselves (Logic), Emby calls and reading source files (EmbyBridge's interfaces), the plugin's own data files and backups (Store), drawing (Artwork), settings (it gets a plain settings record).

## May use
Logic, Store (through `IStore`), Artwork (through `IArtwork`), EmbyBridge (its interfaces only), plain .NET file access for the Top Picks folder and staging only.

## Must not use
Settings, any `MediaBrowser.*` or `Emby.*` type, EmbyBridge's concrete classes (interfaces only), SkiaSharp, `System.Net`, `System.Diagnostics.Process`, the root namespace, any file outside the Top Picks folder and staging.
