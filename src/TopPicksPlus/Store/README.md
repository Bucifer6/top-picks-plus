# Store

## Responsible for
Saving and loading the plugin's own files in `programdata\plugins\TopPicksPlus\` (data.md): managed.json (the managed list), plays.json (the play log) and run.json (the current or last run). Every write is atomic: write a temp file, then rename. Also empties the `staging\` folder at the start of a run.

## Belongs here
File reading and writing for those three files and the staging folder, and what to do if a file is missing or damaged (start empty, never crash).

## Does not belong
Plugin settings (those are Emby's configuration, in Settings). Deciding anything (Logic). Writing copies into the Top Picks folder (Run).

## May use
Logic (its record types), plain .NET file access, System.Text.Json.

## Must not use
Artwork, EmbyBridge, Run, Settings, any `MediaBrowser.*` type, SkiaSharp.
