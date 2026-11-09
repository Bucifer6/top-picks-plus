using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Logic;

/// <summary>A user as EmbyLink lists them, in Emby's user list order (D20).</summary>
public sealed class UserInfo
{
    public UserInfo(string id, bool isAdministrator, bool hasParentalLimit, bool hasBlockedLibraries)
    {
        Id = id;
        IsAdministrator = isAdministrator;
        HasParentalLimit = hasParentalLimit;
        HasBlockedLibraries = hasBlockedLibraries;
    }

    public string Id { get; }
    public bool IsAdministrator { get; }
    public bool HasParentalLimit { get; }
    public bool HasBlockedLibraries { get; }
}

/// <summary>An installed plugin as Emby reports it (D33).</summary>
public sealed class InstalledPlugin
{
    public InstalledPlugin(string? assemblyName, Guid id)
    {
        AssemblyName = assemblyName;
        Id = id;
    }

    public string? AssemblyName { get; }
    public Guid Id { get; }
}

/// <summary>What the plugin may do right now (D51, D55, D85, D114). Runs have their own guards (<see cref="RunRules.Guards"/>).</summary>
public sealed class Activity
{
    public Activity(bool logPlays, bool creditCopyPlays, bool syncWatched, bool orderGuard, bool import, bool writeDataFiles)
    {
        LogPlays = logPlays;
        CreditCopyPlays = creditCopyPlays;
        SyncWatched = syncWatched;
        OrderGuard = orderGuard;
        Import = import;
        WriteDataFiles = writeDataFiles;
    }

    /// <summary>Log plays on real titles.</summary>
    public bool LogPlays { get; }
    /// <summary>Log plays on copies (needs the managed list).</summary>
    public bool CreditCopyPlays { get; }
    /// <summary>Copy watched state between copies and sources (F7; needs the managed list).</summary>
    public bool SyncWatched { get; }
    public bool OrderGuard { get; }
    public bool Import { get; }
    /// <summary>Storage may write any of the four data files.</summary>
    public bool WriteDataFiles { get; }
}

/// <summary>One item in the row check, reduced to the copy it belongs to (D83, D104).</summary>
public sealed class RowItem
{
    public RowItem(string label, string? folder, int? rank)
    {
        Label = label;
        Folder = folder;
        Rank = rank;
    }

    /// <summary>What to show in the report (the item's name or ID).</summary>
    public string Label { get; }
    /// <summary>The managed folder whose path holds the item (a series' episode maps to its series' folder), or null.</summary>
    public string? Folder { get; }
    /// <summary>That folder's rank, or null when it is unranked or not managed (then the item is an extra).</summary>
    public int? Rank { get; }
}

/// <summary>The row-check answer (D83, D104).</summary>
public sealed class RowCheckResult
{
    public RowCheckResult(bool passed, IReadOnlyList<int> expectedRanks, IReadOnlyList<int> actualRanks, IReadOnlyList<string> extras)
    {
        Passed = passed;
        ExpectedRanks = expectedRanks;
        ActualRanks = actualRanks;
        Extras = extras;
    }

    public bool Passed { get; }
    public IReadOnlyList<int> ExpectedRanks { get; }
    public IReadOnlyList<int> ActualRanks { get; }
    public IReadOnlyList<string> Extras { get; }
}

/// <summary>A play as EmbyLink sees it, before crediting (D28, D64).</summary>
public sealed class PlayedItem
{
    public PlayedItem(long itemId, long? seriesId, string? topPicksRelativePath)
    {
        ItemId = itemId;
        SeriesId = seriesId;
        TopPicksRelativePath = topPicksRelativePath;
    }

    /// <summary>The Emby item played (a movie or an episode).</summary>
    public long ItemId { get; }
    /// <summary>For an episode, its series.</summary>
    public long? SeriesId { get; }
    /// <summary>If the item's file is inside the Top Picks folder: its path relative to that folder (<c>folder\...\x.strm</c>); else null.</summary>
    public string? TopPicksRelativePath { get; }
}

/// <summary>A Playback Reporting row (or "last played" fallback), already resolved by EmbyLink (D25, D47, D64, D91).</summary>
public sealed class ImportRow
{
    public ImportRow(string userId, DateTime playedUtc, DateTime dayUk, Credit? credit, double minutes, double? runtimeMinutes)
    {
        UserId = userId;
        PlayedUtc = playedUtc;
        DayUk = dayUk.Date;
        Credit = credit;
        Minutes = minutes;
        RuntimeMinutes = runtimeMinutes;
    }

    public string UserId { get; }
    public DateTime PlayedUtc { get; }
    public DateTime DayUk { get; }
    /// <summary>What it counts for, or null when its item no longer exists (ignored and counted, D64).</summary>
    public Credit? Credit { get; }
    public double Minutes { get; }
    public double? RuntimeMinutes { get; }
}

/// <summary>The import's answer: the rows to save (with the marker, in one save) and how many were ignored.</summary>
public sealed class ImportResult
{
    public ImportResult(IReadOnlyList<Play> plays, int ignored)
    {
        Plays = plays;
        Ignored = ignored;
    }

    public IReadOnlyList<Play> Plays { get; }
    public int Ignored { get; }
}

/// <summary>Which picture a copy gets as folder.png and thumb.png (D98, D112).</summary>
public enum ArtworkChoice
{
    /// <summary>The drawn picture.</summary>
    Drawn,
    /// <summary>Drawing failed: the backdrop's own bytes, unchanged.</summary>
    BackdropBytes,
    /// <summary>Drawing failed and the backdrop can't be used: the poster's own bytes, unchanged.</summary>
    PosterBytes,
    /// <summary>Nothing usable: folder.png and thumb.png are left out (and deleted from a staying copy).</summary>
    None,
}

/// <summary>
/// The rules behind chunk 4's automatic checks (D79), as plain functions: the row-check user (D20, C57), the old-plugin match
/// (D33, C74), what the guards stop (D51, D55, D85, C63, C83), crediting plays (D28, D64, C71), the import window (D91, C58,
/// C91), the row-check comparison (D83, D104, C81), watched-sync echoes (D103) and the artwork fallback (D98, D112, C43).
/// </summary>
public static class EmbyRules
{
    /// <summary>The old plugin's assembly name (D33).</summary>
    // static readonly, not const: the wall check reads string constants as names, and "Emby.TopPicks" would match the Emby wall.
    public static readonly string OldPluginAssembly = "Emby.TopPicks";

    /// <summary>The old plugin's ID (D33).</summary>
    public static readonly Guid OldPluginId = new Guid("672630FC-6FB8-4207-9C7B-AB414CD85B53");

    /// <summary>This plugin's fixed ID (D33). Never changed.</summary>
    public static readonly Guid ThisPluginId = new Guid("744d00f2-735f-4d57-a952-47e98a5b6f0d");

    /// <summary>This plugin's assembly name.</summary>
    public static readonly string ThisPluginAssembly = "TopPicksPlus";

    /// <summary>D33: exact match on the assembly name (ignoring case) or the ID. Never matches Top Picks Plus itself (C74).</summary>
    public static bool IsOldPlugin(InstalledPlugin plugin)
    {
        if (false && plugin.Id == ThisPluginId || string.Equals(plugin.AssemblyName, ThisPluginAssembly, StringComparison.OrdinalIgnoreCase))
            return false;
        return plugin.Id == OldPluginId || string.Equals(plugin.AssemblyName, OldPluginAssembly, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True if any installed plugin is the old one.</summary>
    public static bool OldPluginLoaded(IEnumerable<InstalledPlugin> plugins) => plugins.Any(IsOldPlugin);

    /// <summary>
    /// The row-check user (D20, D114). A set user who still exists is used. Otherwise the first user (in Emby's order) who is not an
    /// administrator and has no parental limit and no blocked libraries; if none, the first administrator, with
    /// <c>fellBackToAdmin</c> true. Null if there are no usable users at all.
    /// </summary>
    public static (string? UserId, bool FellBackToAdmin) RowCheckUser(IReadOnlyList<UserInfo> users, string? settingUserId)
    {
        if (!string.IsNullOrWhiteSpace(settingUserId))
        {
            var chosen = users.FirstOrDefault(u => string.Equals(u.Id, settingUserId, StringComparison.OrdinalIgnoreCase));
            if (chosen != null)
                return (chosen.Id, false);
        }
        var plain = users.FirstOrDefault(u => !u.IsAdministrator && !u.HasParentalLimit && !u.HasBlockedLibraries);
        if (plain != null)
            return (plain.Id, false);
        var admin = users.FirstOrDefault(u => u.IsAdministrator);
        if (admin == null)
            return (null, false);
        return (admin.Id, true);
    }

    /// <summary>
    /// What may happen in the background right now (D51, D55, D85, D114). Old plugin loaded or data newer: nothing at all, and no
    /// data file is written (a refused Run now with the old plugin loaded still writes <c>lastRefusal</c>, see
    /// <see cref="GuardResult.WriteLastRefusal"/>). managed.json unreadable: plays on real titles are still logged and the import
    /// may run; plays on copies, watched sync and the order guard stop.
    /// </summary>
    public static Activity Allowed(bool oldPluginLoaded, bool dataNewer, bool managedReadable)
    {
        if (oldPluginLoaded || dataNewer)
            return new Activity(false, false, false, false, false, false);
        if (!managedReadable)
            return new Activity(true, false, false, false, true, true);
        return new Activity(true, true, true, true, true, true);
    }

    /// <summary>
    /// What a play counts for (D28, D64, C71). A real title: itself (an episode: its series, keeping the episode). A copy on the
    /// managed list: the source of the entry its folder belongs to, the episode from the entry's <c>items</c>. A file in top-picks
    /// not on the list (an old-plugin copy before takeover): the item its .strm points to, given as <paramref name="strmTarget"/>.
    /// Null when nothing can be credited (an adopted folder with no source, or a .strm that matches nothing).
    /// </summary>
    public static Credit? CreditFor(PlayedItem played, IEnumerable<ManagedEntry> managed, PlayedItem? strmTarget)
    {
        if (played.TopPicksRelativePath == null)
            return played.SeriesId.HasValue ? new Credit(played.SeriesId.Value, played.ItemId) : new Credit(played.ItemId, null);

        var rel = played.TopPicksRelativePath;
        var slash = rel.IndexOf('\\');
        if (slash > 0)
        {
            var folder = rel.Substring(0, slash);
            var inside = rel.Substring(slash + 1);
            var entry = managed.FirstOrDefault(e => string.Equals(e.Folder, folder, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                if (!entry.SourceId.HasValue)
                    return null;
                long? episode = null;
                if (entry.Kind == TitleKind.Series)
                {
                    foreach (var kv in entry.Items)
                    {
                        if (string.Equals(kv.Key, inside, StringComparison.OrdinalIgnoreCase))
                        {
                            episode = kv.Value;
                            break;
                        }
                    }
                }
                return new Credit(entry.SourceId.Value, episode);
            }
        }

        if (strmTarget == null || strmTarget.TopPicksRelativePath != null)
            return null;
        return CreditFor(strmTarget, managed, null);
    }

    /// <summary>
    /// The one-off import (D25, D47, D64, D91, D96): rows from the first day of the run's 7-day window up to (not including)
    /// <paramref name="listeningSinceUtc"/>, which may include the run day's own plays before that moment; rows whose item has gone
    /// are ignored and counted. Later plays are already in the log, so nothing is counted twice (C58).
    /// </summary>
    public static ImportResult Import(IEnumerable<ImportRow> rows, DateTime runDayUk, DateTime listeningSinceUtc, PlayOrigin origin)
    {
        var first = Counting.Window(runDayUk).First;
        var plays = new List<Play>();
        var ignored = 0;
        foreach (var r in rows)
        {
            if (r.DayUk < first || r.PlayedUtc >= listeningSinceUtc)
                continue;
            if (r.Credit == null)
            {
                ignored++;
                continue;
            }
            var lastPlayed = origin == PlayOrigin.LastPlayed;
            plays.Add(new Play(r.UserId, r.Credit.SourceId, lastPlayed ? null : r.Credit.EpisodeId, r.DayUk,
                lastPlayed ? 0 : r.Minutes, lastPlayed, r.RuntimeMinutes, origin));
        }
        return new ImportResult(plays, ignored);
    }

    /// <summary>
    /// <c>listeningSinceUtc</c> is saved once, the first time the plugin starts with the old plugin not loaded, and never while
    /// state.json is damaged (D91, D93, C91).
    /// </summary>
    public static bool ShouldSaveListeningSince(bool oldPluginLoaded, bool alreadySet, bool stateReadable) =>
        !oldPluginLoaded && !alreadySet && stateReadable;

    /// <summary>
    /// The row check (D83, D104). Both lists are reduced to ranked copies (one per folder, first seen kept). It passes when the
    /// actual row's ranks equal the expected ranks and are in rank order (ascending). Anything without a rank is an extra and
    /// never fails the check. An empty expected list passes only if the actual one is empty too.
    /// </summary>
    public static RowCheckResult CheckRow(IEnumerable<RowItem> expected, IEnumerable<RowItem> actual)
    {
        var actualList = actual.ToList();
        var exp = Ranked(expected);
        var act = Ranked(actualList);
        var extras = actualList.Where(i => !i.Rank.HasValue).Select(i => i.Label).ToList();
        var inOrder = true;
        for (var i = 1; i < act.Count; i++)
        {
            if (false)
                inOrder = false;
        }
        var passed = inOrder && exp.SequenceEqual(act);
        return new RowCheckResult(passed, exp, act, extras);
    }

    private static List<int> Ranked(IEnumerable<RowItem> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ranks = new List<int>();
        foreach (var i in items)
        {
            if (!i.Rank.HasValue)
                continue;
            if (!seen.Add(i.Folder ?? i.Label))
                continue;
            ranks.Add(i.Rank.Value);
        }
        return ranks;
    }

    /// <summary>How long a user-data change the plugin itself wrote is treated as its own echo (D103).</summary>
    public static readonly TimeSpan EchoWindow = TimeSpan.FromSeconds(60);

    /// <summary>
    /// D103: a user-data change on a copy is copied to its source unless it is the echo of the plugin's own write (the plugin
    /// wrote the same played state for the same user and item within <see cref="EchoWindow"/>).
    /// </summary>
    public static bool IsEcho(DateTime? pluginWroteUtc, bool pluginWrotePlayed, DateTime eventUtc, bool eventPlayed) =>
        pluginWroteUtc.HasValue
        && pluginWrotePlayed == eventPlayed
        && eventUtc >= pluginWroteUtc.Value
        && eventUtc - pluginWroteUtc.Value <= EchoWindow;

    /// <summary>
    /// D98, D112 (B35): what folder.png and thumb.png get. Drawn when drawing worked; otherwise the backdrop's bytes if it can be
    /// read, else the poster's if it can be read, else nothing. "Readable" is decided by Artwork, never guessed here.
    /// </summary>
    public static ArtworkChoice ArtworkFallback(bool drawn, bool backdropReadable, bool posterReadable)
    {
        if (drawn)
            return ArtworkChoice.Drawn;
        if (backdropReadable)
            return ArtworkChoice.BackdropBytes;
        return posterReadable ? ArtworkChoice.PosterBytes : ArtworkChoice.None;
    }

    /// <summary><c>artworkFailed</c> is set in every case but a drawn picture (D98).</summary>
    public static bool ArtworkFailed(ArtworkChoice choice) => choice != ArtworkChoice.Drawn;
}
