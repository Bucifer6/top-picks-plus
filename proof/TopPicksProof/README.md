# proof/TopPicksProof – the separate proof plugin

**What it is:** a tiny Emby plugin, "Top Picks Proof" (`TopPicksProof.dll`, namespace `TopPicksProof`, plugin ID `f48a4d65-31b3-42a1-bb92-ca577d011926`). It is **never** Top Picks Plus and never part of it: its own project, and the plugin never references it (D76, D77).

**Why it exists:** some things can only be proved on the owner's real server, but nothing of Top Picks Plus is installed before go-live (D63). This plugin proves them instead, without touching the Top Picks row.

**It only does two things** (D129 dropped the third):
1. Shows that a netstandard2.0 plugin built against the pinned Emby SDK (MediaBrowser.Server.Core 4.9.1.90, D14) loads in Emby 4.10.2, and which time zone the server uses (C76).
2. In a throwaway test library the owner creates on `programdata\data\top-picks-proof\`, writes two movie folders (each a .strm pointing to a movie file the owner picks, plus an .nfo with `<dateadded>` 15 July 2026 12:00 and 15 January 2026 12:00, server time) and, after the owner refreshes that library, reports the date Emby read back for each, with a verdict (D50, D67, C62, D131).

It does **not** read Playback Reporting: the pinned SDK has no SQLite library, so the first week comes from "last played" dates (D25, D129). The page says so.

**How it works (D131):** one settings page drawn by Emby (Simple UI: no HTML, no script, no web endpoint). One file picker and one tick box; the write happens when the owner presses Save. Results are worked out afresh each time the page opens and also go to Emby's log. Code: `ProofPlugin.cs` (the plugin and page hooks), `ProofOptions.cs` (the page's fields), `Proofs.cs` (the write and the read-back).

**It never:** writes to the Top Picks folder, changes the row, deletes anything, listens to plays, keeps data, overwrites a file, or changes the movie file it points to. It writes only once: if `top-picks-proof` already holds anything other than its own four files, it refuses.

**Built by:** CI, in its own `proof` job; the DLL is kept 3 days as the artifact `TopPicksProof-0.0.0-proof.<run>`, never attached to a Release (D77, D87). The job also fails if a future SDK ever ships SQLite files (D129).
**May use:** Emby SDK, .NET. **Must not use:** anything in src/TopPicksPlus.
**Tested by:** check C76 on the server; no automatic tests.
**Afterwards:** chunk 4's pull request deletes this folder and the `proof` job once the results are recorded (D130).

## Owner's steps on the server (B4; for the owner to confirm)

Nothing here touches the Top Picks row or the old plugin. Allow about 15 minutes.

**Get the file**
1. Open the pull request on GitHub, click **Checks**, then the **build** run. On the run's **Summary** page, under **Artifacts**, click **TopPicksProof-0.0.0-proof.N** to download a zip (you must be signed in to GitHub; it is kept for 3 days).
2. Unzip it. Inside is one file, `TopPicksProof.dll`.

**Install it**
3. Copy `TopPicksProof.dll` into Emby's plugins folder: `programdata\plugins` (the same folder the old Top Picks plugin's DLL is in). Leave the old plugin where it is.
4. In Emby, go to **Dashboard → Restart** (or restart the Emby service) and wait until it is back.
5. Go to **Dashboard → Plugins**. "Top Picks Proof" should be listed. *(If it isn't, stop here and tell Claude; that is a result too.)*

**Write the two test folders**
6. Click **Top Picks Proof**, then **Settings**.
7. In **Any movie file**, browse to any film file on your server (for example one in N:\Movies). It is only read, never changed.
8. Tick **Write the two test folders when I press Save (only once)** and press **Save**.
9. Close the page and open it again. **What happened when the test folders were written** should start with "Written".

**Make the test library**
10. Go to **Dashboard → Libraries → New Library**. Type: **Movies**. Name: **Top Picks Proof**. Folder: add `programdata\data\top-picks-proof` (inside Emby's programdata folder). Leave the other settings as they are and press OK.
11. Wait for Emby to finish scanning it (a minute or two). Two items appear: "Top Picks Proof July" and "Top Picks Proof January". If they don't, open the library's menu and choose **Scan library files**.

**Read the results**
12. Open the Top Picks Proof settings page again. Click in the **Results** box, select all of it (Ctrl+A, Ctrl+C) and paste it into the chat with Claude.

**Clean up** (after pasting the results)
13. **Dashboard → Libraries**: remove the "Top Picks Proof" library.
14. **Dashboard → Plugins**: uninstall Top Picks Proof, then restart Emby.
15. Delete the folder `programdata\data\top-picks-proof` by hand.
16. Check the Top Picks row on the home screen looks the same as before.

**If something looks wrong:** paste whatever the page shows anyway. What happens with each result is already decided (D132); Claude only asks you if a decision of yours is needed.
