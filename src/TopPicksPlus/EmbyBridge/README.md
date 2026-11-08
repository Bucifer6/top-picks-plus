# EmbyBridge

## Responsible for
Everything that talks to Emby, and nothing else does (apart from the Settings configuration class and pages):
- the "Refresh Top Picks" scheduled task and its Sunday 05:00 trigger, and the start-up and daily timers (spec F1: roll-up +1 min and 00:05 UK, order guard +2 min, resume +3 min, missed run +10 min), each calling a Run hook;
- the "playback started", "playback stopped" and "user data changed" listeners (F6, F7), and the library-change listener that starts the order guard after 2 quiet minutes (F5);
- passing the Europe/London time zone and the clock into Run as plain values;
- finding libraries, users, candidates and their files, images and dates, and the Playback Reporting database path (F2, F3, F6);
- rescanning only the Top Picks library and reporting which copy items have appeared (F4 step 10), setting date added and played state on items (F4 steps 11–12, card 0001), refreshing artwork, the row check as the check user (F4 step 13), copying played state and resume points (F7), the old-plugin guard (F9);
- the **source files** adapter (D28), read-only: list a source video's subtitle files and give their bytes, open a theme video as a stream, and test whether a library folder can be reached (spec "Words used here");
- the API the settings page calls (Run now, pin search, save settings with validation, report). **Every API action takes the caller from Emby's authorisation of the request, never a user ID in the request, and refuses anyone who isn't an administrator with "Administrators only" and no data (F8).**

## Belongs here
Thin adapters: turn Emby objects into Logic's plain records, and Run's requests into Emby calls. Each adapter sits behind an interface defined here, so Run can be tested with fakes. The adapters are built by the root entry point from Emby's services.

**How it reaches Run without crossing a wall:** EmbyBridge defines small hook interfaces (for example "start a run", "record a play", "roll up", "get the last report") and a settings-source interface. Run implements the hooks; the root entry point puts them, and a settings source, into EmbyBridge's static `Hooks` registry at start-up. Emby-created classes (task, listeners, API) only reach Run through that registry; before it is filled they drop events and answer "starting up".

## Does not belong
Rules (Logic), the order of the weekly steps (Run), the plugin's own data files (Store), drawing (Artwork). Writing or deleting any file: the source-files adapter only reads.

## May use
Logic, Settings (its configuration class only), the Emby SDK (`MediaBrowser.*`, pinned at 4.9.1.90, D14), read-only .NET file access to source library folders.

## Must not use
Store, Artwork, Run, SkiaSharp, `System.Net`, the root namespace.
