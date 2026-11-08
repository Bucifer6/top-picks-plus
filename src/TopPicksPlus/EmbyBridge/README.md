# EmbyBridge

## Responsible for
Everything that talks to Emby, and nothing else does (apart from the Settings page class): the "Refresh Top Picks" scheduled task and its Sunday 05:00 trigger (F1), the "playback stopped" listener (F6), finding the Top Picks library by folder path, reading candidates and their files, rescanning only the Top Picks library and waiting (F4 step 3), setting date added on items (F4 step 4, card 0001), refreshing artwork, the row check as a non-administrator user (F4 step 6), copying watched and resume state (F7), the old-plugin guard (F9), the Playback Reporting one-off import (F6), and the API the settings page calls (Run now, pin search, report).

## Belongs here
Thin adapters: turn Emby objects into Logic's plain records, and Run's requests into Emby calls. Each adapter sits behind an interface so Run can be tested with fakes.

**How it reaches Run and Store without crossing a wall:** EmbyBridge defines small hook interfaces of its own (for example "start a run", "record a play", "get the last report"). Run implements them, and the plugin's root class (`TopPicksPlus` namespace) connects the two at start-up. So the scheduled task, the play listener and the settings API never mention Run or Store.

## Does not belong
Rules (Logic), the order of the weekly steps (Run), file formats of the plugin's own data (Store), drawing (Artwork).

## May use
Logic, the Emby SDK (`MediaBrowser.*`, pinned at 4.9.1.90, D14).

## Must not use
Store, Artwork, Run, Settings, SkiaSharp.
