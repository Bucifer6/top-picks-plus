# Store

## Responsible for
The plugin's own files in the plugin data folder `<programdata>\data\TopPicksPlus\` (D23, data.md): managed.json, plays.json, run.json and state.json (D29). Every write is atomic: write a temp file, then replace. One lock around every change to plays.json, and the roll-up (F6, D33). Emptying `staging\` (F4 step 8). Making, checking and pruning `backups\` (copying TopPicksPlus.xml in read-only), and making and checking the takeover zip (F9, D28). Deleting damaged files on schedule (F9).

## Belongs here
File reading and writing for those files, staging, backups and the zip, behind the `IStore` interface Run uses (so Run can be tested with a fake). Missing and damaged files follow spec F9 exactly; never a crash. Its own deletes stay inside the plugin data folder with real-location and no-link checks. Paths come in as Logic's plain paths record.

## Does not belong
Plugin settings (Settings). Deciding anything (Logic; the roll-up's sums are Logic functions). Writing copies into the Top Picks folder (Run); Store only reads it, for the zip.

## May use
Logic (its record types), plain .NET file access, System.IO.Compression, System.Text.Json (compiled against, not shipped, D30).

## Must not use
Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` type, SkiaSharp, `System.Net`, the root namespace.
