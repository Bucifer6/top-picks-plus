using System;
using System.Collections.Concurrent;
using System.Linq;
using MediaBrowser.Controller.Library;
using TopPicksPlus.Logic;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// Counting plays (F6, D28, D29, D64, D70): playback start positions are remembered per play session and item; at playback stop,
/// each user gets one play row with the minutes watched, credited to the source title. Nothing is logged while not allowed (D114).
/// </summary>
public static class PlayLogger
{
    private static readonly ConcurrentDictionary<string, (long Ticks, DateTime Utc)> Starts =
        new ConcurrentDictionary<string, (long, DateTime)>();

    /// <summary>Starts not matched by a stop within this long are forgotten (D29: then minutes are 0).</summary>
    private static readonly TimeSpan ForgetAfter = TimeSpan.FromHours(12);

    private static string Key(PlaybackProgressEventArgs e) =>
        (e.PlaySessionId ?? e.DeviceId ?? string.Empty) + ":" + (e.Item?.InternalId ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static void OnPlaybackStart(PlaybackProgressEventArgs e)
    {
        if (e.Item == null || !Counting.IsLoggable(e.Item.GetType().Name))
            return;
        Starts[Key(e)] = (e.PlaybackPositionTicks ?? 0, DateTime.UtcNow);
        var cutoff = DateTime.UtcNow - ForgetAfter;
        foreach (var old in Starts.Where(kv => kv.Value.Utc < cutoff).Select(kv => kv.Key).ToList())
            Starts.TryRemove(old, out _);
    }

    public static void OnPlaybackStopped(Host h, PlaybackStopEventArgs e)
    {
        try
        {
            var item = e.Item;
            if (item == null || !Counting.IsLoggable(item.GetType().Name))
                return;
            double? startMinutes = Starts.TryRemove(Key(e), out var start) ? TimeSpan.FromTicks(start.Ticks).TotalMinutes : (double?)null;
            var stopMinutes = TimeSpan.FromTicks(e.PlaybackPositionTicks ?? 0).TotalMinutes;
            double? runtime = item.RunTimeTicks.HasValue && item.RunTimeTicks.Value > 0 ? TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalMinutes : (double?)null;
            var minutes = Counting.MinutesWatched(startMinutes, stopMinutes, runtime);

            var health = h.Data.Inspect();
            var managedReadable = health.Managed != Storage.FileStatus.Damaged;
            var allowed = EmbyRules.Allowed(Startup.OldPluginLoaded, health.DataNewer, managedReadable);
            var onCopy = h.RelativeToTopPicks(item.Path) != null;
            if (!allowed.LogPlays || (onCopy && !allowed.CreditCopyPlays))
                return;

            var managed = h.Data.LoadManaged().Value ?? Array.Empty<ManagedEntry>();
            var credit = EmbyFacts.CreditFor(h, item, managed);
            if (credit == null)
                return;
            var day = EmbyFacts.UkDay(DateTime.UtcNow);
            foreach (var user in e.Users ?? new System.Collections.Generic.List<MediaBrowser.Controller.Entities.User>())
            {
                h.Data.AppendPlay(new Play(EmbyFacts.UserKey(user), credit.SourceId, credit.EpisodeId, day, minutes,
                    e.PlayedToCompletion, runtime, PlayOrigin.Live));
            }
        }
        catch (Exception ex)
        {
            h.Log.ErrorException("Top Picks Plus: couldn't record a play", ex);
        }
    }
}
