using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using TopPicksPlus.Logic;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// Watched state follows the real title (F7, D55, D103): a change on a copy is copied to its source, and a copy takes on the
/// source's played state when it is created or updated. The plugin's own writes on copies are remembered for 60 seconds so their
/// echo is not copied back (<see cref="EmbyRules.IsEcho"/>).
/// </summary>
public static class Watched
{
    private static readonly ConcurrentDictionary<string, (DateTime Utc, bool Played)> OwnWrites =
        new ConcurrentDictionary<string, (DateTime, bool)>();

    private static string Key(User user, BaseItem item) => EmbyFacts.UserKey(user) + ":" + item.InternalId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>D103: copies each user's played state from <paramref name="source"/> onto the copy item, remembering each write.</summary>
    public static void CopyPlayedOnto(Host h, IEnumerable<User> users, BaseItem source, BaseItem copy, CancellationToken ct)
    {
        foreach (var user in users)
        {
            var from = h.UserData.GetUserData(user, source);
            var onto = h.UserData.GetUserData(user, copy);
            if (from == null || onto == null || onto.Played == from.Played)
                continue;
            onto.Played = from.Played;
            OwnWrites[Key(user, copy)] = (DateTime.UtcNow, from.Played);
            h.UserData.SaveUserData(user, copy, onto, UserDataSaveReason.TogglePlayed, ct);
        }
    }

    /// <summary>
    /// F7, D55: a user marked a copy played or unplayed, finished it, or stopped part-way: the same goes onto the source movie or
    /// episode. Only copies on the managed list with a source are followed; nothing happens while watched sync isn't allowed (D114).
    /// </summary>
    public static void OnUserDataSaved(Host h, UserDataSaveEventArgs e)
    {
        try
        {
            var item = e.Item;
            var user = e.User;
            var data = e.UserData;
            if (item == null || user == null || data == null)
                return;
            if (e.SaveReason != UserDataSaveReason.TogglePlayed && e.SaveReason != UserDataSaveReason.PlaybackFinished)
                return;
            var rel = h.RelativeToTopPicks(item.Path);
            if (rel == null)
                return;
            var health = h.Data.Inspect();
            if (!EmbyRules.Allowed(Startup.OldPluginLoaded, health.DataNewer, health.Managed != Storage.FileStatus.Damaged).SyncWatched)
                return;

            OwnWrites.TryGetValue(Key(user, item), out var own);
            if (EmbyRules.IsEcho(own.Utc == default ? (DateTime?)null : own.Utc, own.Played, DateTime.UtcNow, data.Played))
                return;

            var managed = h.Data.LoadManaged().Value;
            if (managed == null)
                return;
            var credit = EmbyRules.CreditFor(EmbyFacts.PlayedItem(h, item), managed, null);
            if (credit == null)
                return;
            var source = h.Library.GetItemById(credit.EpisodeId ?? credit.SourceId);
            // Only a movie or an episode carries watched state; a series copy's file with no matched episode is left alone.
            if (source == null || source is MediaBrowser.Controller.Entities.TV.Series)
                return;
            var target = h.UserData.GetUserData(user, source);
            if (target == null)
                return;
            target.Played = data.Played;
            target.PlaybackPositionTicks = data.PlaybackPositionTicks;
            if (data.LastPlayedDate.HasValue)
                target.LastPlayedDate = data.LastPlayedDate;
            h.UserData.SaveUserData(user, source, target, e.SaveReason, CancellationToken.None);
        }
        catch (Exception ex)
        {
            h.Log.ErrorException("Top Picks Plus: couldn't copy watched state to the real title", ex);
        }
    }

    /// <summary>Forgets own writes older than the echo window, so the list stays small.</summary>
    internal static void Prune()
    {
        var cutoff = DateTime.UtcNow - EmbyRules.EchoWindow;
        foreach (var key in OwnWrites.Where(kv => kv.Value.Utc < cutoff).Select(kv => kv.Key).ToList())
            OwnWrites.TryRemove(key, out _);
    }
}
