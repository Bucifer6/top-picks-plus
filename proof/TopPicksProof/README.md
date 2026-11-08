# proof/TopPicksProof – the separate proof plugin

**What it is:** a tiny Emby plugin, "Top Picks Proof" (`TopPicksProof.dll`, namespace `TopPicksProof`, plugin ID `f48a4d65-31b3-42a1-bb92-ca577d011926`). It is **never** Top Picks Plus and never part of it: its own project, and the plugin never references it (D76, D77).

**Why it exists:** some things can only be proved on the owner's real server, but nothing of Top Picks Plus is installed before go-live (D63). This plugin proves them instead, without touching the Top Picks row.

**It only does three things:**
1. Shows that a netstandard2.0 plugin built against the pinned Emby SDK (MediaBrowser.Server.Core 4.9.1.90, D14) loads in Emby 4.10.2 (C76).
2. Opens `programdata\data\playback_reporting.db` **read-only** through Emby's own SQLite library and reports whether `DateCreated` is UTC or local, checked against a play the owner names, and what `ItemId` and `UserId` look like next to Emby's InternalId and user GUID (D25, D48). If the pinned SDK doesn't give access to that library, it says "cannot read" (then the import uses only the "last played" fallback).
3. In a throwaway test library the owner creates on `programdata\data\top-picks-proof\`, writes two movie folders (each a .strm pointing to a movie file the owner picks, plus an .nfo with `<dateadded>` in July and in January) and, after the owner refreshes that library, reports the date Emby read back for each (D50, D67, C62).

**It never:** writes to the Top Picks folder, changes the row, deletes anything, listens to plays, keeps data, or changes the movie files it points to. It refuses if `top-picks-proof` already holds files it didn't write. Results show only on its own admin page and in Emby's log.

**When:** built in its own pull request as the first job of chunk 4. The owner installs it next to the old plugin (they don't interfere: it listens to nothing and only reads Playback Reporting's file), runs it, copies the page into the chat, then uninstalls it, removes the test library and deletes the `top-picks-proof` folder. This happens **before chunk 4 is kept**.

**Built by:** CI, in its own `proof` job; the DLL is kept 3 days as the artifact `TopPicksProof-<version>`, never attached to a Release (D77, D87).
**May use:** Emby SDK, .NET. **Must not use:** anything in src/TopPicksPlus.
**Tested by:** check C76 on the server; no automatic tests.
