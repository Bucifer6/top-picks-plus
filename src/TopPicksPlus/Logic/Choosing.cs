using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Logic;

/// <summary>
/// Choosing the 10 (spec F2): usable pins first in the owner's order, then the most watched candidates by viewer-days, then
/// filler by newest content. Ties: newest content, then newer premiere date, then the lower source ID as a number (D35).
/// </summary>
public static class Choosing
{
    /// <summary>The number of picks is fixed (D11).</summary>
    public const int PickCount = 10;

    /// <summary>
    /// Whether a title can be used (F2 step 5, D61): a movie needs its file; a series needs at least one episode file.
    /// <paramref name="filesFound"/> is the number of the title's video files that can be found.
    /// </summary>
    public static bool IsAvailable(TitleKind kind, int filesFound) => filesFound >= 1;

    /// <summary>
    /// Chooses the picks. <paramref name="candidates"/> are the titles in the chosen libraries (only available ones are used);
    /// <paramref name="pins"/> are the settings' pins in order with whether each is usable; <paramref name="viewerDays"/> comes
    /// from <see cref="Counting.ViewerDays"/>. Plays for titles that aren't candidates don't score.
    /// </summary>
    public static ChooseResult Choose(IEnumerable<Candidate> candidates, IEnumerable<PinStatus> pins, IReadOnlyDictionary<long, int> viewerDays)
    {
        var picks = new List<Pick>();
        var used = new HashSet<long>();
        var missing = new List<long>();

        foreach (var pin in pins)
        {
            if (used.Contains(pin.SourceId))
                continue;
            if (!pin.Usable)
            {
                if (!missing.Contains(pin.SourceId))
                    missing.Add(pin.SourceId);
                continue;
            }
            if (picks.Count >= PickCount)
                break;
            used.Add(pin.SourceId);
            picks.Add(new Pick(picks.Count + 1, pin.SourceId, PickReason.Pinned, Score(viewerDays, pin.SourceId)));
        }

        var pool = candidates
            .Where(c => c.Available)
            .GroupBy(c => c.SourceId)
            .Select(g => g.First())
            .Where(c => !used.Contains(c.SourceId))
            .ToList();

        var watched = pool
            .Where(c => Score(viewerDays, c.SourceId) > 0)
            .OrderByDescending(c => Score(viewerDays, c.SourceId))
            .ThenBy(c => c, TieBreak.Instance)
            .ToList();
        foreach (var c in watched)
        {
            if (picks.Count >= PickCount)
                break;
            used.Add(c.SourceId);
            picks.Add(new Pick(picks.Count + 1, c.SourceId, PickReason.ViewerDays, Score(viewerDays, c.SourceId)));
        }

        var filler = pool
            .Where(c => !used.Contains(c.SourceId))
            .OrderBy(c => c, TieBreak.Instance)
            .ToList();
        foreach (var c in filler)
        {
            if (picks.Count >= PickCount)
                break;
            used.Add(c.SourceId);
            picks.Add(new Pick(picks.Count + 1, c.SourceId, PickReason.Filler, 0));
        }

        return new ChooseResult(picks, missing);
    }

    private static int Score(IReadOnlyDictionary<long, int> viewerDays, long id) =>
        viewerDays.TryGetValue(id, out var n) ? n : 0;

    /// <summary>F2 step 3 and D35: newest content first, then newer premiere (none last), then lower source ID.</summary>
    public sealed class TieBreak : IComparer<Candidate>
    {
        public static readonly TieBreak Instance = new TieBreak();

        public int Compare(Candidate? x, Candidate? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return 1;
            if (y == null)
                return -1;
            var c = y.NewestContentUtc.CompareTo(x.NewestContentUtc);
            if (c != 0)
                return c;
            c = (y.PremiereDate ?? DateTime.MinValue).CompareTo(x.PremiereDate ?? DateTime.MinValue);
            if (c != 0)
                return c;
            return string.CompareOrdinal(x.SourceId.ToString(), y.SourceId.ToString());
        }
    }
}

/// <summary>Why the settings page refuses a new pin (D61, D74, C68). Built here so it can be tested (B9, D107).</summary>
public enum PinRefusal
{
    None,
    TooMany,
    Duplicate,
    NotMovieOrSeries,
    InTopPicksLibrary,
}

/// <summary>The pin rules behind the settings page (D61, D74). Chunk 5 calls this; it is tested here (D107).</summary>
public static class PinRules
{
    /// <summary>At most 10 pins (D61).</summary>
    public const int MaxPins = 10;

    /// <summary>
    /// Whether a new pin may be added to <paramref name="existing"/>: only a movie or series (Emby type name), not an item in the
    /// Top Picks library, not already pinned, and not an 11th.
    /// </summary>
    public static PinRefusal CheckAdd(IReadOnlyList<long> existing, long newId, string? itemType, bool inTopPicksLibrary)
    {
        if (!string.Equals(itemType, "Movie", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(itemType, "Series", StringComparison.OrdinalIgnoreCase))
            return PinRefusal.NotMovieOrSeries;
        if (inTopPicksLibrary)
            return PinRefusal.InTopPicksLibrary;
        if (existing.Contains(newId))
            return PinRefusal.Duplicate;
        if (existing.Count >= MaxPins)
            return PinRefusal.TooMany;
        return PinRefusal.None;
    }
}
