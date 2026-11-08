# Store

## Responsible for
The plugin's own files in the plugin data folder `<programdata>\data\TopPicksPlus\` (D23, data.md): managed.json, plays.json, run.json, run.previous.json and state.json (D29). Every write is atomic: write a temp file, then replace; leftover temp files are removed at start-up. One lock around every change to plays.json, and the roll-up (F6, D33); one lock around every change to managed.json. Checking every field against data.md's rules when reading, and handling damaged files as spec F9 says (D37: managed.json refuses runs, state.json reset, run.json renamed aside, plays.json deleted at once). Emptying `staging\` (F4 step 6). Making and pruning `backups\` (copying TopPicksPlus.xml as a file, never reading it).

## Belongs here
File reading and writing for those files, staging and backups, behind the `IStore` interface Run uses (so Run can be tested with a fake). Missing and damaged files follow spec F9 exactly; never a crash. Its own deletes stay inside the plugin data folder with real-location and no-link checks. Paths come in as Logic's plain paths record.

## Does not belong
Plugin settings (Settings). Deciding anything (Logic; the roll-up's sums are Logic functions). Writing or reading copies in the Top Picks folder (Run). Interpreting the settings file (Settings, EmbyBridge).

## May use
Logic (its record types and field rules), plain .NET file access, System.Text.Json (compiled against, not shipped, D30).

## Must not use
Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` or `Emby.*` type, SkiaSharp, `System.Net`, `System.Diagnostics.Process`, the root namespace, any file outside the plugin data folder except copying TopPicksPlus.xml into backups.
