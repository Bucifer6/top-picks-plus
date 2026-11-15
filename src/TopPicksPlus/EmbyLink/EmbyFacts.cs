using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;

namespace TopPicksPlus.EmbyLink;

/// <summary>The Top Picks library as Emby reports it (F9, D53).</summary>
public sealed class TopPicksLibrary
{
    public TopPicksLibrary(long id, bool nfoReaderOn, bool nfoSaverOn)
    {
        Id = id;
        NfoReaderOn = nfoReaderOn;
        NfoSaverOn = nfoSaverOn;
    }

    public long Id { get; }
    public bool NfoReaderOn { get; }
    public bool NfoSaverOn { get; }
}

/// <summary>Plain facts read from Emby for Logic's rules: the library, the guards, users, the old plugin and UK time (D90).</summary>
public static class EmbyFacts
{
    private const string Nfo = "Nfo";

    /// <summary>The library whose folder is the Top Picks folder (compared ignoring case, D35), or null.</summary>
    public static TopPicksLibrary? FindTopPicksLibrary(Host h) =>
        FindLibrary(h, Same(h.TopPicksPath));

    private static TopPicksLibrary? FindLibrary(Host h, Func<string, bool> isTopPicks)
    {
        foreach (var folder in h.Library.GetVirtualFolders())
        {
            if (folder.Locations == null || !folder.Locations.Any(isTopPicks))
                continue;
            var options = folder.LibraryOptions;
            var readerOff = options?.DisabledLocalMetadataReaders?.Contains(Nfo, StringComparer.OrdinalIgnoreCase) ?? false;
            var saverOn = options?.MetadataSavers?.Contains(Nfo, StringComparer.OrdinalIgnoreCase) ?? false;
            return new TopPicksLibrary(h.Library.GetInternalId(folder.ItemId), !readerOff, saverOn);
        }
        return null;
    }

    private static Func<string, bool> Same(string path)
    {
        var want = Normal(path);
        return p => string.Equals(Normal(p), want, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normal(string? path) => (path ?? string.Empty).Replace('/', '\\').TrimEnd('\\');

    /// <summary>
    /// The libraries automatic picks come from (F2 step 5, B18): the chosen ones, or, with none chosen, every library whose
    /// content type is movies or TV shows; never the Top Picks library. InternalIds.
    /// </summary>
    public static IReadOnlyList<long> CandidateLibraries(Host h, IReadOnlyCollection<long> chosen)
    {
        var topPicks = Same(h.TopPicksPath);
        var ids = new List<long>();
        foreach (var folder in h.Library.GetVirtualFolders())
        {
            if (folder.Locations != null && folder.Locations.Any(topPicks))
                continue;
            var id = h.Library.GetInternalId(folder.ItemId);
            var wanted = chosen.Count > 0
                ? chosen.Contains(id)
                : string.Equals(folder.CollectionType, "movies", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(folder.CollectionType, "tvshows", StringComparison.OrdinalIgnoreCase);
            if (wanted)
                ids.Add(id);
        }
        return ids;
    }

    /// <summary>
    /// D147: the first folder of the given libraries that can't be reached (a media drive offline), or null. A run doesn't choose
    /// picks while one is missing, so an unplugged drive can never empty the row.
    /// </summary>
    public static string? UnreachableLibraryFolder(Host h, IReadOnlyCollection<long> libraryIds)
    {
        foreach (var folder in h.Library.GetVirtualFolders())
        {
            if (!libraryIds.Contains(h.Library.GetInternalId(folder.ItemId)))
                continue;
            foreach (var location in folder.Locations ?? Array.Empty<string>())
            {
                if (!SafeDirectoryExists(location))
                    return location;
            }
        }
        return null;
    }

    private static bool SafeDirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// The Emby items of one copy (D28, D137): for a series, the series folder first; then each .strm with its source item. An
    /// item Emby doesn't have yet is null.
    /// </summary>
    public static IReadOnlyList<(BaseItem? Item, long? SourceId)> CopyItems(Host h, ManagedEntry entry)
    {
        var folder = Path.Combine(h.TopPicksPath, entry.Folder);
        var list = new List<(BaseItem?, long?)>();
        if (entry.Kind == TitleKind.Series)
            list.Add((h.Library.FindByPath(folder, true), null));
        foreach (var kv in entry.Items)
            list.Add((h.Library.FindByPath(Path.Combine(folder, kv.Key), false), kv.Value));
        return list;
    }

    /// <summary>D33: the old plugin is loaded if any installed plugin matches its assembly name or ID.</summary>
    public static bool OldPluginLoaded(Host h) =>
        EmbyRules.OldPluginLoaded((h.App.Plugins ?? Array.Empty<MediaBrowser.Common.Plugins.IPlugin>())
            .Select(p => new InstalledPlugin(SafeAssemblyName(p.AssemblyFilePath), p.Id)));

    private static string? SafeAssemblyName(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        try
        {
            return Path.GetFileNameWithoutExtension(path);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The guard facts (D80, D94, D116): Storage's file health plus Emby's facts.</summary>
    public static GuardFacts GuardFacts(Host h, DataHealth health, bool lockHeld, bool oldPluginLoaded, TopPicksLibrary? library) =>
        health.ToGuardFacts(lockHeld, oldPluginLoaded, library != null, library?.NfoReaderOn ?? true, library?.NfoSaverOn ?? false);

    /// <summary>A user's ID as stored everywhere (D48): the user GUID, 32 hex digits.</summary>
    public static string UserKey(User user) => user.Id.ToString("N");

    /// <summary>Every user, in the order Emby lists them (D20, D114).</summary>
    public static IReadOnlyList<User> AllUsers(Host h) => h.Users.GetUserList(new UserQuery()) ?? Array.Empty<User>();

    /// <summary>Users as the row-check rule needs them (D20).</summary>
    public static IReadOnlyList<UserInfo> UserInfos(Host h, IEnumerable<User> users) =>
        users.Select(u =>
        {
            var policy = u.Policy ?? h.Users.GetUserPolicy(u);
            return new UserInfo(UserKey(u), policy?.IsAdministrator ?? false,
                policy?.MaxParentalRating.HasValue ?? false, !(policy?.EnableAllFolders ?? true));
        }).ToList();

    /// <summary>The UK date (the server's own zone, D90) of a UTC moment.</summary>
    public static DateTime UkDay(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Local).Date;

    /// <summary>The <c>&lt;dateadded&gt;</c> text for a rank date, with the server's offset for that moment (D50, D67, B17).</summary>
    public static string DateAddedText(DateTime rankDateUtc) =>
        RankTimes.DateAddedText(rankDateUtc, TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(rankDateUtc, DateTimeKind.Utc)));

    /// <summary>A movie or episode as a played item for crediting (D28, D64).</summary>
    public static PlayedItem PlayedItem(Host h, BaseItem item) =>
        new PlayedItem(item.InternalId, SeriesOf(item), h.RelativeToTopPicks(item.Path));

    /// <summary>An episode's series InternalId, or null.</summary>
    public static long? SeriesOf(BaseItem item) =>
        item is IHasSeries hasSeries && hasSeries.SeriesId != 0 ? hasSeries.SeriesId : (long?)null;

    /// <summary>
    /// What a play on <paramref name="item"/> counts for (D28, D64, C71): through the managed list, or for a copy not on the list,
    /// through the item its .strm points to.
    /// </summary>
    public static Credit? CreditFor(Host h, BaseItem item, IReadOnlyList<ManagedEntry> managed)
    {
        var played = PlayedItem(h, item);
        PlayedItem? strmTarget = null;
        var rel = played.TopPicksRelativePath;
        if (rel != null)
        {
            var slash = rel.IndexOf('\\');
            var folder = slash > 0 ? rel.Substring(0, slash) : rel;
            var listed = managed.Any(e => string.Equals(e.Folder, folder, StringComparison.OrdinalIgnoreCase));
            if (!listed && slash > 0)
            {
                var target = h.Copies.ReadStrm(folder, rel.Substring(slash + 1));
                var targetItem = string.IsNullOrEmpty(target) ? null : h.Library.FindByPath(target, false);
                if (targetItem != null)
                    strmTarget = PlayedItem(h, targetItem);
            }
        }
        return EmbyRules.CreditFor(played, managed, strmTarget);
    }

    /// <summary>The run's report header (F10): this plugin's and Emby's versions and the zone used for UK time (D90).</summary>
    public static (string Plugin, string Emby, string Zone) Versions(Host h) =>
        (Plugin.BuildVersion, h.App.ApplicationVersion?.ToString() ?? "unknown", TimeZoneInfo.Local.Id);
}
