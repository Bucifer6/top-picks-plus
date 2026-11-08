# EmbyBridge

## Responsible for
Everything that talks to Emby, and nothing else does (apart from the Settings page class): the "Refresh Top Picks" scheduled task and its Sunday 05:00 trigger (F1), the start-up timers (F1: guard +2 min, resume +3 min, missed run +10 min), the "playback started" and "playback stopped" listeners and the user-data-changed listener (F6, F7), passing the Europe/London time zone, the paths record and a log into the other parts, finding the Top Picks library by folder path, reading candidates and their files, rescanning only the Top Picks library and waiting (F4 step 3), setting date added on items (F4 step 4, card 0001), refreshing artwork, the row check as a non-administrator user (F4 step 6), copying watched and resume state (F7), the old-plugin guard (F9), the Playback Reporting one-off import (F6), and the API the settings page calls (Run now, pin search, save settings, report). **Every API action checks on the server that the caller is an Emby administrator and otherwise refuses with "Administrators only" and no data (F8).**

## Belongs here
Thin adapters: turn Emby objects into Logic's plain records, and Run's requests into Emby calls. Each adapter sits behind an interface so Run can be tested with fakes.

**How it reaches Run and Store without crossing a wall:** EmbyBridge defines small hook interfaces of its own (for example "start a run", "record a play", "get the last report"). Run implements them, and the plugin's root class (`TopPicksPlus` namespace) puts them into EmbyBridge's static `Hooks` registry at start-up; Emby-created classes (task, listeners, API) only reach Run through that registry. So the scheduled task, the play listener and the settings API never mention Run or Store.

## Does not belong
Rules (Logic), the order of the weekly steps (Run), file formats of the plugin's own data (Store), drawing (Artwork).

## May use
Logic, Settings (its configuration class only: EmbyBridge reads and saves the settings and hands Run a plain settings record from Logic), the Emby SDK (`MediaBrowser.*`, pinned at 4.9.1.90, D14).

## Must not use
Store, Artwork, Run, SkiaSharp.
