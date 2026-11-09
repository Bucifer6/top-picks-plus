using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Logic;

/// <summary>
/// Counting plays (spec F2 step 2, F6): the 7-day and 90-day windows (D21), whether a user counts for a title on a day
/// (marked played, 50% of the runtime, or 20 minutes with no runtime; per episode for series, D22), viewer-days (D10),
/// minutes watched (D29) and what is logged (D70). UK dates are passed in; nothing here reads the clock or a time zone.
/// </summary>
public static class Counting
{
    /// <summary>With no known runtime, this many minutes count as watched (F6).</summary>
    public const double MinutesWhenNoRuntime = 20;

    /// <summary>Plays are kept for this many full UK days before the run day (D21).</summary>
    public const int KeepDays = 90;

    /// <summary>The scoring window is this many full UK days before the run day (D21).</summary>
    public const int WindowDays = 7;

    /// <summary>The first and last UK day of the 7-day window for a run on <paramref name="runDayUk"/> (D21): the 7 days before it.</summary>
    public static (DateTime First, DateTime Last) Window(DateTime runDayUk)
    {
        var day = runDayUk.Date;
        return (day.AddDays(-WindowDays), day.AddDays(-1));
    }

    /// <summary>True if the UK day is inside the 7-day window for the run day.</summary>
    public static bool InWindow(DateTime dayUk, DateTime runDayUk)
    {
        var (first, last) = Window(runDayUk);
        var d = dayUk.Date;
        return d >= first && d <= last;
    }

    /// <summary>
    /// The plays kept by the trim (D21, C24): those on or after the 90th full UK day before the run day. Plays on the run day
    /// (and later) are kept.
    /// </summary>
    public static IReadOnlyList<Play> Trim(IEnumerable<Play> plays, DateTime runDayUk)
    {
        var oldestKept = runDayUk.Date.AddDays(-KeepDays);
        return plays.Where(p => p.DayUk >= oldestKept).ToList();
    }

    /// <summary>
    /// Minutes watched in one sitting (D29): stop minus start, never below 0 or above the runtime. With no start seen, 0.
    /// </summary>
    public static double MinutesWatched(double? startMinutes, double stopMinutes, double? runtimeMinutes)
    {
        if (!startMinutes.HasValue)
            return 0;
        var minutes = stopMinutes - startMinutes.Value;
        if (double.IsNaN(minutes) || minutes < 0)
            minutes = 0;
        if (runtimeMinutes.HasValue && runtimeMinutes.Value >= 0 && minutes > runtimeMinutes.Value)
            minutes = runtimeMinutes.Value;
        return minutes;
    }

    /// <summary>Only movies and episodes are logged (D70); the item type is Emby's type name.</summary>
    public static bool IsLoggable(string? itemType) =>
        string.Equals(itemType, "Movie", StringComparison.OrdinalIgnoreCase)
        || string.Equals(itemType, "Episode", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the sittings of one user on one item (a movie, or one episode) on one day count (F6): any marked played, or the
    /// minutes add up to at least half the runtime, or to at least 20 minutes when the runtime isn't known.
    /// </summary>
    public static bool Counts(IReadOnlyCollection<Play> sittings)
    {
        if (sittings.Count == 0)
            return false;
        if (sittings.Any(p => p.MarkedPlayed))
            return true;
        var total = sittings.Sum(p => p.Minutes);
        var runtime = sittings.Select(p => p.RuntimeMinutes).FirstOrDefault(r => r.HasValue && r.Value > 0);
        return runtime.HasValue ? total >= runtime.Value / 2 : total >= MinutesWhenNoRuntime;
    }

    /// <summary>
    /// Viewer-days per source title in the 7-day window (D10, D21, D22): each user who counts for the title on a UK day adds 1.
    /// For a series, sittings add up only within the same episode; any counting episode makes the user count that day.
    /// </summary>
    public static IReadOnlyDictionary<long, int> ViewerDays(IEnumerable<Play> plays, DateTime runDayUk)
    {
        var result = new Dictionary<long, int>();
        var userDays = plays
            .Where(p => InWindow(p.DayUk, runDayUk))
            .GroupBy(p => (p.SourceId, p.UserId, p.DayUk));
        foreach (var userDay in userDays)
        {
            var counts = userDay
                .GroupBy(p => p.EpisodeId)
                .Any(item => Counts(item.ToList()));
            if (!counts)
                continue;
            result.TryGetValue(userDay.Key.SourceId, out var n);
            result[userDay.Key.SourceId] = n + 1;
        }
        return result;
    }

    /// <summary>Plays recorded in the 7-day window, for the report's <c>playsLast7Days</c> (F8).</summary>
    public static int PlaysInWindow(IEnumerable<Play> plays, DateTime runDayUk) =>
        plays.Count(p => InWindow(p.DayUk, runDayUk));
}
