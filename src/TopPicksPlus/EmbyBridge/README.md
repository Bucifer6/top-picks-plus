# EmbyBridge

## Responsible for
Everything that talks to Emby, and nothing else does (apart from the Settings configuration class and pages):
- the "Refresh Top Picks" scheduled task and its Sunday 05:00 trigger, and the start-up and daily timers (spec F1: roll-up +1 min and 00:05 UK, order guard +2 min, resume +3 min, missed run +10 min and 00:15 UK), each calling a Run hook; every timer reads time through EmbyBridge's `IBridgeClock` interface (now in UTC), so the B tests use a fake clock;
- the "playback started", "playback stopped" and "user data changed" listeners (F6), and the library-change listener that starts the order guard after 2 quiet minutes (F5);
- passing the Europe/London time zone and the clock into Run as plain values;
- finding libraries, users, candidates and their files, images (as bytes, from wherever Emby keeps them) and dates (F2, F3, F6);
- rescanning only the Top Picks library and reporting which copy items have appeared (F4 step 8), setting date added on items (F4 steps 9–10, card 0001), refreshing artwork, the row check as the check user without leaving out watched items, giving the users as plain records and saying which copy items a user is allowed to see (F4 step 11), the old-plugin guard (F9). Before changing any item's date or artwork, it checks the item's path is inside the Top Picks folder with Logic's rule and refuses if not (F9). It has **no action that changes played state or resume points** on any item (D39, C49);
- the **source files** adapter (D28), read-only: list a source video's subtitle files and give their bytes, open a theme video as a stream, test whether a library folder can be reached (spec "Words used here"), and say whether a source video file exists (F2 "can be found"). It never opens the Top Picks or plugin data folders, and makes no native Windows calls (D52);
- the API the settings page calls (Run now through the start-run hook (D35), run status (running / waiting to resume / last status), pin search, check pins (which pins are missing now, F8), save settings with validation by Logic's save rules, report and the previous report). Actions that change something (save settings, Run now) accept POST only, the rest GET only. **Every API action first takes the caller from Emby's authorisation of the request, never a user ID in the request, and refuses anyone who isn't an administrator with "Administrators only" and no data, before anything else, even before answering "starting up" (F8, C70).**

## Belongs here
Thin adapters: turn Emby objects into Logic's plain records, and Run's requests into Emby calls. Each adapter sits behind an interface defined here, so Run can be tested with fakes. The adapters are built by the root entry point from Emby's services.

**How it reaches Run without crossing a wall:** EmbyBridge defines small hook interfaces (for example "start a run", "record a play", "roll up", "get the last report") and a settings-source interface. Run implements the hooks; the root entry point puts them, and a settings source, into EmbyBridge's static `Hooks` registry at start-up. Emby-created classes (task, listeners, API) only reach Run through that registry; before it is filled they drop events and answer "starting up".

**The hooks (implemented by Run):** start a run (with its trigger; refused if a run holds the lock or is waiting to resume), resume, the missed-run rule (+10 minutes and 00:15 UK), roll up, order guard, record a play (playback started with the item's played state at start / stopped with played state at stop / marked by hand, which is only a "user data changed" event with Emby's "played toggled" reason), run status (running, waiting to resume, or idle), read the last report and the previous one. **Settings source:** read the settings as a plain Logic record, and save them (used by the API after validation). Every hook call from a task, timer, listener or API action is wrapped so no error reaches Emby (spec F4).

## Does not belong
Rules (Logic), the order of the weekly steps (Run), the plugin's own data files (Store), drawing (Artwork). Writing or deleting any file: the source-files adapter only reads.

## May use
Logic, Settings (its configuration class only), the Emby SDK (`MediaBrowser.*`, pinned at 4.9.1.90, D14), read-only .NET file access to source library folders.

## Must not use
Store, Artwork, Run, SkiaSharp, `System.Net`, `System.Diagnostics.Process`, the root namespace.
