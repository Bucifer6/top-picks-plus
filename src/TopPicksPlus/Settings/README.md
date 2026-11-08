# Settings

## Responsible for
The plugin's settings chosen by the owner (data.md: LibraryIds, Pins; run state is in Store's state.json, D29) and the administrator-only settings page and run report in Dashboard → Plugins → Top Picks Plus (F8).

## Belongs here
The configuration class Emby saves as `TopPicksPlus.xml`, and the page's HTML and JavaScript as embedded resources kept inside this folder. The page puts every text from the server on screen as plain text, never HTML (F8). Only EmbyBridge (and the root, to provide the settings source) reads or saves the settings; Run gets them as a plain record.

## Does not belong
Running anything, checking values on save (EmbyBridge's API does that on the server), reading the run report from disk: the page gets both through EmbyBridge's API. Rules (Logic).

## May use
Logic, the Emby SDK types needed for plugin configuration and pages.

## Must not use
Store, Artwork, EmbyBridge, Run, SkiaSharp, `System.Net`, `System.Diagnostics.Process`, the root namespace.
