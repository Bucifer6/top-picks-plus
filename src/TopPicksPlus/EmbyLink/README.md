# EmbyLink – everything that talks to Emby

**Responsible for:**
- the "Refresh Top Picks" scheduled task (Sunday 05:00 server time), Run now, one run at a time, the missed-run start-up rule (F1);
- collecting candidates, pins and play history from Emby and handing them to Logic (F2);
- driving the swap (F3, F4): asking Logic for the plan and Storage to carry out every file change, then rescanning only the Top Picks library and waiting;
- setting Date added directly on each item and episode to the rank date already written in the .nfo during prepare (F4 steps 1 and 4, F5; card 0001);
- the order guard after library changes and at start-up, and resuming an unfinished run (F5);
- listening for playback stopped and recording plays (F6), the one-off first-week import (from "last played" dates only: the SDK can't read Playback Reporting, D129);
- copying watched state and resume points (F7);
- the row check as a non-administrator user (F4 step 6), comparing ranked copies only (D83);
- running a run's steps in the D80 order (guards, import, takeover, leftovers, prepare …); guards again at every resume, before `resumeCount` goes up; a refusal leaves the waiting run alone and the next trigger after it clears resumes it; a problem found mid-step goes through the retry path (D94); reading and saving the plugin's flags through Storage's state.json (D93), never through PluginConfiguration;
- the guards' Emby facts: old plugin loaded (exact match, D33), Top Picks library missing or set up wrong (D53). Every delete and its containment checks are Storage's (D37, D43, B21);
- linking copy files to source items by path (D28), minutes watched from start/stop positions (D29), the "last played" fallback (D25, D129), the start-up order (D35), takeover (D38), and the in-memory run lock and retry rule (D44). The delete-containment checks (D37), leftovers (D39) and file-level order (D40) are carried out by Storage (D43).

**Belongs here:** thin code that turns Emby objects into plain values for Logic and carries out Logic's answers.
**Does not belong:** the rules themselves (Logic), any file change or JSON file handling (Storage, D43), drawing (Artwork), the admin page (Settings).
**May use:** Logic, Storage, Artwork, project root, Emby SDK.
**Must not use:** Settings, SkiaSharp.
**Tested by:** the owner's checks in docs/checks.md on the real server (D18). Every rule behind an "(Automatic)" check is a Logic function built in chunk 1 (D79); EmbyLink only wires it to Emby, proved by code review and the go-live checks (D73). No fake-Emby tests. Its safety rules are tested automatically in Storage and Logic (D43).
**On-server proofs:** loading in 4.10.2 and how Emby reads .nfo dates come from the separate proof plugin (`proof/TopPicksProof/`, D76, D77), run after the whole build, before go-live (D134). Never install any part of Top Picks Plus early to prove these.

**Files (chunk 4):**
| File | What it does |
|------|--------------|
| `Host.cs` | The Emby services the run uses (`IApplicationHost`, `IApplicationPaths`, `ILibraryManager`, `IUserManager`, `IUserDataManager`, `IUserViewManager`, `IProviderManager`, `IFileSystem`, a logger), the Top Picks folder, Storage's `DataFolder` and `CopyFolders`. With `ISessionManager` and `ILogManager` (EntryPoint) that is the full SDK list (B27) |
| `EntryPoint.cs` | `IServerEntryPoint`: start-up plan (D35, D90), `listeningSinceUtc` (D91), events (plays, watched, library changes → order guard, D135); `Startup.OldPluginLoaded` |
| `RefreshTask.cs` | `IScheduledTask` "Refresh Top Picks", weekly Sunday 05:00 |
| `RunEngine.cs` | The lock, triggers, refusals, retries and held messages (D44, D78, D94, D143) |
| `RunSteps.cs` | One attempt: import, takeover, leftovers, prepare, swap, rescan, dates, artwork, check, record (D80, D137–D140, D142) |
| `CopyBuilder.cs` | Every file of a copy, worked out again each time (D140) |
| `Catalog.cs` | Candidates, pins, episodes (F2, D136) |
| `EmbyFacts.cs` | Library, guards, users, old plugin, UK time, crediting, a copy's Emby items, library folders and whether a missing one holds titles, creating a missing Top Picks library (D20, D28, D33, D53, D64, D90, D147, D150, D151) |
| `HomeScreen.cs` | The Top Picks slideshow on each user's home screen in place of the row, or the row back (D152) |
| `OrderGuard.cs` | Puts dates back (F5, D52) |
| `PlayLogger.cs` | Plays from playback start/stop (F6, D29, D141) |
| `Watched.cs` | Watched state between copies and sources, with echo detection (F7, D103, D141) |

**Watch out:** nothing here compiles locally; check Emby signatures before writing (card 0018, D145). Emby's SDK needs System.Memory 4.6.0 to compile against (D146).
