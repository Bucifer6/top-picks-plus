# Top Picks Plus

An Emby Server plugin that fills a **"Latest Top Picks"** row on the home screen with the 10 titles your household watched most
this week, numbered 1 to 10 and in the right order every time. Pins you choose come first; recently added titles fill any gaps.

It replaces the older Top Picks plugin, whose row slowly fell out of order because Emby ignores a changed `<dateadded>` for a
title it already knows.

- Movies and TV series, from the libraries you choose (default: every movie and TV library).
- Up to 10 pins, in your order, ahead of the most watched.
- Numbered artwork for each pick, so the order is visible at a glance.
- Watched ticks and resume points follow the real title, so watching a pick counts as watching the film or episode.
- A settings page with **Run now**, the pins, and a plain-words report of the last run.

Built for Emby Server 4.10 (compiled against the 4.9 SDK, .NET Standard 2.0).

## Install

1. Download `TopPicksPlus.dll` from the latest [release](../../releases/latest).
2. Copy it into Emby's plugins folder (on Windows: `%AppData%\Emby-Server\programdata\plugins`) and restart Emby.
3. If the old **Top Picks** plugin is installed, remove the triggers from its scheduled task first, then uninstall it and restart.
   Top Picks Plus does nothing while the old plugin is loaded, and on its first run it takes over the old plugin's folders.
4. Open **Dashboard → Plugins → Top Picks Plus** and press **Run now**.

## The Top Picks library

The row is Emby's own "Latest" row for a library whose folder is `programdata\data\top-picks`. The plugin builds a small folder
for each pick there (pointer files to the real media, an `.nfo` with the rank date, and the numbered artwork).

- **Already have one** (from the old plugin): it is found by its folder and used as it is. It is never changed or deleted.
- **Don't have one:** with **Create the Top Picks library if it's missing** ticked (the default), the first run that starts
  creates it, called *Top Picks* (or *Top Picks Plus* if that name is taken), with the Nfo reader on and no Nfo saver.
  Untick it to make the library yourself: any content type, folder `programdata\data\top-picks`, **Nfo** on under Metadata
  readers, and no **Nfo** metadata saver. If either setting is wrong the run refuses and the report says which to change.

## When it runs

A scheduled task called **Refresh Top Picks**, every Sunday at 05:00 server time. Change the day and time, or add triggers, in
**Dashboard → Scheduled Tasks → Refresh Top Picks**. **Run now** on the settings page works at any time. If Emby was off at the
scheduled time and the last good run is over a week old, a run starts 10 minutes after Emby starts.

A run that stops on an error tries again 5 minutes later, up to 3 times, carrying on with the same picks. A run that is refused
(for example the old plugin is still installed) changes nothing and waits for the next trigger.

## Settings (Dashboard → Plugins → Top Picks Plus)

| Setting | What it does |
|---------|--------------|
| Libraries to choose from | Empty = every movie and TV library. The Top Picks library is never offered. |
| Row-check user | The user whose view of the row is checked after each run. Default: the first user who isn't an administrator and has no restrictions. |
| Create the Top Picks library if it's missing | On by default; see above. |
| Pins | Search, add, move up or down, remove. Saved straight away. |

Press **Save** after changing the first three. The **Last run report** at the bottom shows each pick and why it was chosen,
the row check, and any notices (for example a library folder that couldn't be reached).

## How picks are chosen

1. Pins, in your order (a pin whose title or file is gone is reported as missing).
2. The most watched titles of the last 7 days, counted in viewer-days: each person who watched a title on a given day counts
   once, whatever they watched of it (at least half a film or episode, 20 minutes, or marked played).
3. Recently added titles fill any places left.

A library folder that can't be reached stops the choice if Emby has titles in it (a drive offline must never push its titles out
of the row); a missing folder with no titles in it (a leftover path) is skipped and reported.

## Safety

- It deletes only folders it made itself, and only inside `top-picks`; anything else there is left alone.
- It never changes your media files, only reads them.
- Plays are kept for 90 days in the plugin's own data folder, visible only to administrators.

## Uninstall

Uninstall it from **Dashboard → Plugins**, restart Emby, then remove the Top Picks library and the `data\top-picks` and
`plugins\TopPicksPlus` folders if you no longer want them.

## For developers

- `src/TopPicksPlus/` – the plugin: `Logic` (pure rules), `Storage` (every file change), `Artwork` (numbered pictures),
  `EmbyLink` (everything that talks to Emby), `Settings` (the page and report). Each folder has a README.
- `tests/` – xUnit tests for Logic, Storage and Artwork, plus a boundary check between the parts.
- Builds run only in GitHub Actions (`.github/workflows/build.yml`): build, tests, mutation testing (80% bar on Logic and Storage),
  audit and secret scan. A `v1.2.3` tag on `main` publishes a release with the DLL.
- Docs: [spec](docs/spec.md), [decisions](docs/decisions.md), [checks](docs/checks.md), [data files](docs/data.md),
  [code map](CODEMAP.md).
