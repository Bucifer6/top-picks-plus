# Settings

## Responsible for
The plugin's settings (data.md: LibraryIds, Pins, LastSuccessfulRunUtc, TakeoverDone, PlaybackReportingImported) and the administrator-only settings page and run report in Dashboard → Plugins → Top Picks Plus (F8).

## Belongs here
The configuration class Emby saves as `TopPicksPlus.xml`, and the page's HTML and JavaScript as embedded resources kept inside this folder (no separate resources folder). Only EmbyBridge reads or saves the settings; Run gets them as a plain record through EmbyBridge.

## Does not belong
Running anything or reading the run report from disk: the page gets both through EmbyBridge's API. Rules (Logic).

## May use
Logic, the Emby SDK types needed for plugin configuration and pages.

## Must not use
Store, Artwork, EmbyBridge, Run, SkiaSharp.
