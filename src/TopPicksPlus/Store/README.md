# Store

## Responsible for
The plugin's own files in the plugin data folder `<programdata>\data\TopPicksPlus\` (D23, data.md): managed.json, plays.json, run.json, run.previous.json and state.json (D29). Every write is atomic: write a temp file, then replace; leftover temp files are removed at start-up. One lock around every change to plays.json, and the roll-up (F6, D33); one lock around every change to managed.json; one lock around every change to state.json (spec "Pending flags"). Checking every field against data.md's rules when reading, and handling damaged files as spec F9 says (D37: managed.json refuses runs, state.json reset, run.json renamed aside, plays.json deleted at once). Emptying `staging\` (F4 step 6). Making and pruning `backups\` (plays.json with `"today": []`, TopPicksPlus.xml copied as a file, never read). Copying run.json to run.previous.json only when it was successful (D41). Carrying the pending flags as spec "Pending flags" says. Removing leftover `*.tmp` files when the root entry point asks at start-up. Saying whether the plugin data folder is a link (reparse-point attribute, F9). A file that can't be opened is an error, never "damaged" (spec F9).

## Belongs here
File reading and writing for those files, staging and backups, behind the `IStore` interface Run uses (so Run can be tested with a fake). Missing and damaged files follow spec F9 exactly; never a crash. Its own deletes stay inside the plugin data folder with full-path and no-link checks (reparse-point attribute; no link on the way to `staging\`, `backups\` or the file; a link inside staging or a backup folder is removed as a link, never entered). A failed write deletes its own `.tmp` at once, C76. Paths come in as Logic's plain paths record.

## Does not belong
Plugin settings (Settings). Deciding anything (Logic; the roll-up's sums are Logic functions). Writing or reading copies in the Top Picks folder (Run). Interpreting the settings file (Settings, EmbyBridge).

## May use
Logic (its record types and field rules), plain .NET file access, System.Text.Json (compiled against, not shipped, D30).

## Must not use
Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` or `Emby.*` type, SkiaSharp, `System.Net`, `System.Diagnostics.Process`, the root namespace, any file outside the plugin data folder except copying TopPicksPlus.xml into backups.
