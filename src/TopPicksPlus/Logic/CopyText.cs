using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TopPicksPlus.Logic;

/// <summary>Rank times and rank dates (D27, card 0001).</summary>
public static class RankTimes
{
    /// <summary>The rank time: the run's start in UTC, cut down to the whole minute (D27).</summary>
    public static DateTime RankTime(DateTime runStartUtc) =>
        new DateTime(runStartUtc.Ticks - runStartUtc.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc);

    /// <summary>Pick <paramref name="rank"/>'s rank date: rank time minus (rank − 1) hours (D27). Rank must be 1–10.</summary>
    public static DateTime RankDate(DateTime rankTimeUtc, int rank)
    {
        if (rank < 1 || rank > Choosing.PickCount)
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "rank must be 1-10");
        return DateTime.SpecifyKind(rankTimeUtc, DateTimeKind.Utc).AddHours(-rank);
    }

    /// <summary>
    /// The <c>&lt;dateadded&gt;</c> text (D50, D67): the UTC rank date shifted by the server's UTC offset <em>for that moment</em>
    /// (EmbyLink works it out per date and passes it in), written <c>yyyy-MM-dd HH:mm:ss</c>.
    /// </summary>
    public static string DateAddedText(DateTime rankDateUtc, TimeSpan offsetAtThatMoment) =>
        (rankDateUtc + offsetAtThatMoment).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// The order guard (F5, D52): a ranked copy's item (the series, or any of its episodes) needs resetting when its date added
    /// differs from the rank date. Unranked copies (no rank date) are never touched. Compared to the second.
    /// </summary>
    public static bool NeedsReset(DateTime? rankDateUtc, DateTime currentDateAddedUtc)
    {
        if (!rankDateUtc.HasValue)
            return false;
        var diff = (currentDateAddedUtc - rankDateUtc.Value).Duration();
        return diff >= TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// The items of one ranked copy the order guard resets (D52): the series or movie itself and every episode, whichever has a
    /// date added that differs from the rank date. Returns the IDs to reset (empty for an unranked copy).
    /// </summary>
    public static IReadOnlyList<long> ItemsToReset(DateTime? rankDateUtc, IEnumerable<(long ItemId, DateTime DateAddedUtc)> items) =>
        rankDateUtc.HasValue
            ? items.Where(i => NeedsReset(rankDateUtc, i.DateAddedUtc)).Select(i => i.ItemId).ToList()
            : (IReadOnlyList<long>)Array.Empty<long>();
}

/// <summary>Metadata for a movie or series .nfo (D30, D90). Every field may be missing.</summary>
public sealed class TitleMeta
{
    public TitleMeta(
        string? title,
        string? originalTitle = null,
        int? year = null,
        string? plot = null,
        string? ageRating = null,
        IReadOnlyList<string>? genres = null,
        DateTime? premiered = null,
        int? runtimeMinutes = null,
        IReadOnlyDictionary<string, string>? providerIds = null)
    {
        Title = title;
        OriginalTitle = originalTitle;
        Year = year;
        Plot = plot;
        AgeRating = ageRating;
        Genres = genres ?? Array.Empty<string>();
        Premiered = premiered;
        RuntimeMinutes = runtimeMinutes;
        ProviderIds = providerIds ?? new Dictionary<string, string>();
    }

    public string? Title { get; }
    public string? OriginalTitle { get; }
    public int? Year { get; }
    public string? Plot { get; }
    public string? AgeRating { get; }
    public IReadOnlyList<string> Genres { get; }
    public DateTime? Premiered { get; }
    public int? RuntimeMinutes { get; }
    public IReadOnlyDictionary<string, string> ProviderIds { get; }
}

/// <summary>Metadata for an episode .nfo (D30).</summary>
public sealed class EpisodeMeta
{
    public EpisodeMeta(string? title, int season, int episode, string? plot = null, DateTime? aired = null, int? runtimeMinutes = null, string? ageRating = null)
    {
        Title = title;
        Season = season;
        Episode = episode;
        Plot = plot;
        Aired = aired;
        RuntimeMinutes = runtimeMinutes;
        AgeRating = ageRating;
    }

    public string? Title { get; }
    public int Season { get; }
    public int Episode { get; }
    public string? Plot { get; }
    public DateTime? Aired { get; }
    public int? RuntimeMinutes { get; }
    public string? AgeRating { get; }
}

/// <summary>
/// The text of every .strm and .nfo (D30, D90). Storage writes it as UTF-8 with no byte-order mark (C93); .strm text has no line
/// ending. Text is XML-escaped and characters XML can't hold are dropped.
/// </summary>
public static class CopyText
{
    /// <summary>The .strm text: the video path alone, no line ending (D90).</summary>
    public static string Strm(string videoPath) => videoPath.TrimEnd('\r', '\n');

    /// <summary>The movie .nfo (D30).</summary>
    public static string MovieNfo(TitleMeta meta, string dateAdded) => TitleNfo("movie", meta, dateAdded);

    /// <summary>The series tvshow.nfo (D30).</summary>
    public static string TvShowNfo(TitleMeta meta, string dateAdded) => TitleNfo("tvshow", meta, dateAdded);

    private static string TitleNfo(string root, TitleMeta m, string dateAdded)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>\n");
        sb.Append('<').Append(root).Append(">\n");
        Element(sb, "title", m.Title);
        Element(sb, "originaltitle", m.OriginalTitle);
        Element(sb, "year", m.Year?.ToString(CultureInfo.InvariantCulture));
        Element(sb, "plot", m.Plot);
        Element(sb, "mpaa", m.AgeRating);
        foreach (var g in m.Genres)
            Element(sb, "genre", g);
        Element(sb, "premiered", m.Premiered?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Element(sb, "runtime", m.RuntimeMinutes?.ToString(CultureInfo.InvariantCulture));
        foreach (var p in m.ProviderIds.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(p.Value))
                continue;
            sb.Append("  <uniqueid type=\"").Append(Escape(p.Key.ToLowerInvariant())).Append("\">")
              .Append(Escape(p.Value)).Append("</uniqueid>\n");
        }
        Element(sb, "dateadded", dateAdded);
        sb.Append("</").Append(root).Append(">\n");
        return sb.ToString();
    }

    /// <summary>An episode .nfo (D30).</summary>
    public static string EpisodeNfo(EpisodeMeta m, string dateAdded)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>\n");
        sb.Append("<episodedetails>\n");
        Element(sb, "title", m.Title);
        Element(sb, "season", m.Season.ToString(CultureInfo.InvariantCulture));
        Element(sb, "episode", m.Episode.ToString(CultureInfo.InvariantCulture));
        Element(sb, "plot", m.Plot);
        Element(sb, "aired", m.Aired?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Element(sb, "runtime", m.RuntimeMinutes?.ToString(CultureInfo.InvariantCulture));
        Element(sb, "mpaa", m.AgeRating);
        Element(sb, "dateadded", dateAdded);
        sb.Append("</episodedetails>\n");
        return sb.ToString();
    }

    private static void Element(StringBuilder sb, string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        sb.Append("  <").Append(name).Append('>').Append(Escape(value!)).Append("</").Append(name).Append(">\n");
    }

    /// <summary>XML-escapes text (&amp;, &lt;, &gt;, quotes) and drops characters XML 1.0 can't hold.</summary>
    public static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&apos;"); break;
                default:
                    if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        sb.Append(c).Append(text[i + 1]);
                        i++;
                    }
                    else if (IsXmlChar(c))
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private static bool IsXmlChar(char c) =>
        c == '\t' || c == '\n' || c == '\r' || (c >= 0x20 && c <= 0xD7FF) || (c >= 0xE000 && c <= 0xFFFD);
}
