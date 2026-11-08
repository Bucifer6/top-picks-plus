# EmbyLink – everything that talks to Emby

**Responsible for:**
- the "Refresh Top Picks" scheduled task (Sunday 05:00 server time), Run now, one run at a time, the missed-run start-up rule (F1);
- collecting candidates, pins and play history from Emby and handing them to Logic (F2);
- driving the swap (F3, F4): asking Logic for the plan and Storage to carry out every file change, then rescanning only the Top Picks library and waiting;
- setting Date added directly on each item and episode to the rank date already written in the .nfo during prepare (F4 steps 1 and 4, F5; card 0001);
- the order guard after scans and at start-up, and resuming an unfinished run (F5);
- listening for playback stopped and recording plays (F6), the one-off Playback Reporting import;
- copying watched state and resume points (F7);
- the row check as a non-administrator user (F4 step 6);
- the safety guards: old plugin loaded (exact match, D33), Top Picks library missing, delete only managed folders inside the Top Picks folder (F9);
- linking copy files to source items by path (D28), minutes watched from start/stop positions (D29), the Playback Reporting read through Emby's own SQLite library (D25), the start-up order (D35), takeover (D38), and the in-memory run lock and retry rule (D44). The delete-containment checks (D37), leftovers (D39) and file-level order (D40) are carried out by Storage (D43).

**Belongs here:** thin code that turns Emby objects into plain values for Logic and carries out Logic's answers.
**Does not belong:** the rules themselves (Logic), any file change or JSON file handling (Storage, D43), drawing (Artwork), the admin page (Settings).
**May use:** Logic, Storage, Artwork, project root, Emby SDK.
**Must not use:** Settings, SkiaSharp.
**Tested by:** the owner's checks in docs/checks.md on the real server (D18). Its safety rules are tested automatically in Storage and Logic (D43).
