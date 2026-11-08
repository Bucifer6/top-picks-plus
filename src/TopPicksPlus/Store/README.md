# Store

## Responsible for
Saving and loading the plugin's own files in the plugin data folder `programdata\data\TopPicksPlus\` (D23, data.md): managed.json (the managed list), plays.json (the popularity counts) and run.json (the current or last run). Every write is atomic: write a temp file, then replace. Also empties `staging\` at the start of a new run (never on resume), and makes and prunes the copies in `backups\` (spec F9).

## Belongs here
File reading and writing for those three files, staging and backups, behind the `IStore` interface that Run uses (so Run can be tested with a fake). Missing and damaged files follow spec F9 exactly: a missing file starts empty; a damaged managed.json is reported and left in place (Run then refuses); a damaged run.json or plays.json is renamed aside and reported. Never a crash. Paths come in as Logic's plain paths record.

## Does not belong
Plugin settings (those are Emby's configuration, in Settings). Deciding anything (Logic). Writing copies into the Top Picks folder (Run).

## May use
Logic (its record types), plain .NET file access, System.Text.Json.

## Must not use
Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` type, SkiaSharp.
