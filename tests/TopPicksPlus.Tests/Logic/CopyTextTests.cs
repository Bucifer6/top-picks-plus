using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TopPicksPlus.Logic;
using Xunit;

namespace TopPicksPlus.Tests.Logic;

/// <summary>Rank times (C53), dateadded text (C62), the order guard rule (C56), .nfo and .strm text (C52, C93).</summary>
public class CopyTextTests
{
    private static readonly DateTime Start = new DateTime(2026, 11, 15, 5, 0, 42, 731, DateTimeKind.Utc);

    [Fact, Trait("Check", "C53")]
    public void C53_rank_time_is_cut_to_the_minute_and_pick_n_is_n_minus_1_hours_earlier()
    {
        var rankTime = RankTimes.RankTime(Start);
        Assert.Equal(new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc), rankTime);
        Assert.Equal(DateTimeKind.Utc, rankTime.Kind);
        for (var n = 1; n <= 10; n++)
            Assert.Equal(rankTime.AddHours(-(n - 1)), RankTimes.RankDate(rankTime, n));
        Assert.Equal(new DateTime(2026, 11, 14, 20, 0, 0), RankTimes.RankDate(rankTime, 10));
    }

    [Fact, Trait("Check", "C53")]
    public void C53_rank_dates_are_strictly_descending_and_a_resume_keeps_them()
    {
        var rankTime = RankTimes.RankTime(Start);
        var first = Enumerable.Range(1, 10).Select(n => RankTimes.RankDate(rankTime, n)).ToList();
        for (var i = 1; i < first.Count; i++)
            Assert.True(first[i] < first[i - 1]);
        // A resume uses the saved rank time, never the resume's own start (D27).
        var again = Enumerable.Range(1, 10).Select(n => RankTimes.RankDate(rankTime, n)).ToList();
        Assert.Equal(first, again);
        Assert.NotEqual(RankTimes.RankTime(Start.AddMinutes(37)), rankTime);
    }

    [Theory, Trait("Check", "C53")]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-3)]
    public void C53_ranks_outside_1_to_10_are_refused(int rank) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RankTimes.RankDate(Start, rank));

    [Fact, Trait("Check", "C62")]
    public void C62_dateadded_uses_each_dates_own_offset_on_a_clock_change_sunday()
    {
        // 2027-03-28: clocks go forward at 01:00 UTC. A 05:00 BST run starts 04:00 UTC; ranks 1-4 are BST, ranks 5-10 before 01:00 UTC (GMT).
        var rankTime = new DateTime(2027, 3, 28, 4, 0, 0, DateTimeKind.Utc);
        TimeSpan OffsetAt(DateTime utc) => utc >= new DateTime(2027, 3, 28, 1, 0, 0, DateTimeKind.Utc) ? TimeSpan.FromHours(1) : TimeSpan.Zero;
        var texts = Enumerable.Range(1, 10).Select(n =>
        {
            var d = RankTimes.RankDate(rankTime, n);
            return RankTimes.DateAddedText(d, OffsetAt(d));
        }).ToList();
        Assert.Equal("2027-03-28 05:00:00", texts[0]);
        Assert.Equal("2027-03-28 03:00:00", texts[2]);
        Assert.Equal("2027-03-28 02:00:00", texts[3]);
        Assert.Equal("2027-03-28 00:00:00", texts[4]);
        Assert.Equal("2027-03-27 19:00:00", texts[9]);
    }

    [Fact, Trait("Check", "C62")]
    public void C62_dateadded_format_is_yyyy_MM_dd_HH_mm_ss()
    {
        Assert.Equal("2026-07-05 06:07:08", RankTimes.DateAddedText(new DateTime(2026, 7, 5, 5, 7, 8, DateTimeKind.Utc), TimeSpan.FromHours(1)));
        Assert.Equal("2026-01-05 05:07:08", RankTimes.DateAddedText(new DateTime(2026, 1, 5, 5, 7, 8, DateTimeKind.Utc), TimeSpan.Zero));
    }

    [Fact, Trait("Check", "C56")]
    public void C56_order_guard_resets_a_ranked_series_and_its_episodes_and_skips_unranked()
    {
        var rankDate = new DateTime(2026, 11, 15, 1, 0, 0, DateTimeKind.Utc);
        var items = new List<(long, DateTime)>
        {
            (1, rankDate),                 // the series, right
            (2, rankDate.AddDays(3)),      // a new episode added later
            (3, rankDate),
            (4, rankDate.AddSeconds(-1)),
        };
        Assert.Equal(new long[] { 2, 4 }, RankTimes.ItemsToReset(rankDate, items));
        Assert.Empty(RankTimes.ItemsToReset(null, items));
        Assert.False(RankTimes.NeedsReset(null, rankDate.AddDays(5)));
        Assert.False(RankTimes.NeedsReset(rankDate, rankDate.AddMilliseconds(500)));
        Assert.True(RankTimes.NeedsReset(rankDate, rankDate.AddHours(1)));
    }

    [Fact, Trait("Check", "C56")]
    public void C56_order_guard_list_holds_only_active_ranked_entries()
    {
        var d = new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc);
        var entries = new[]
        {
            Make.Entry("A", 1, EntryState.Active, 1, rankDate: d),
            Make.Entry("B", 2, EntryState.Active, null),
            Make.Entry("C", 3, EntryState.Leaving, null),
            Make.Entry("D", 4, EntryState.Incoming, 4, rankDate: d),
            Make.Entry("E", null, EntryState.Active, null, adopted: true),
            Make.Entry("F", 6, EntryState.Active, 2, rankDate: null),
        };
        Assert.Equal(new[] { "A" }, SwapPlan.Guarded(entries).Select(e => e.Folder));
    }

    private static readonly TitleMeta Meta = new TitleMeta(
        "Fast & <Furious>",
        originalTitle: "Original \"Q\"",
        year: 2009,
        plot: "Line 1\nLine 2 & more",
        ageRating: "15",
        genres: new[] { "Action", "Crime" },
        premiered: new DateTime(2009, 4, 3),
        runtimeMinutes: 107,
        providerIds: new Dictionary<string, string> { { "Imdb", "tt1013752" }, { "Tmdb", "13804" }, { "Tvdb", "" } });

    [Fact, Trait("Check", "C52")]
    public void C52_movie_nfo_holds_every_D30_field_written_safely()
    {
        var text = CopyText.MovieNfo(Meta, "2026-11-15 05:00:00");
        var x = XDocument.Parse(text).Root!;
        Assert.Equal("movie", x.Name.LocalName);
        Assert.Equal("Fast & <Furious>", x.Element("title")!.Value);
        Assert.Equal("Original \"Q\"", x.Element("originaltitle")!.Value);
        Assert.Equal("2009", x.Element("year")!.Value);
        Assert.Equal("Line 1\nLine 2 & more", x.Element("plot")!.Value);
        Assert.Equal("15", x.Element("mpaa")!.Value);
        Assert.Equal(new[] { "Action", "Crime" }, x.Elements("genre").Select(g => g.Value));
        Assert.Equal("2009-04-03", x.Element("premiered")!.Value);
        Assert.Equal("107", x.Element("runtime")!.Value);
        Assert.Equal("2026-11-15 05:00:00", x.Element("dateadded")!.Value);
        Assert.Contains("&amp; &lt;Furious&gt;", text);
    }

    [Fact, Trait("Check", "C52")]
    public void C52_tvshow_and_episode_nfo()
    {
        var show = XDocument.Parse(CopyText.TvShowNfo(Meta, "2026-11-15 05:00:00")).Root!;
        Assert.Equal("tvshow", show.Name.LocalName);
        Assert.Equal("15", show.Element("mpaa")!.Value);
        Assert.Equal("2026-11-15 05:00:00", show.Element("dateadded")!.Value);

        var ep = XDocument.Parse(CopyText.EpisodeNfo(new EpisodeMeta("A & B", 2, 7, "Plot <x>", new DateTime(2010, 10, 31), 44, "18"), "2026-11-15 04:00:00")).Root!;
        Assert.Equal("episodedetails", ep.Name.LocalName);
        Assert.Equal("A & B", ep.Element("title")!.Value);
        Assert.Equal("2", ep.Element("season")!.Value);
        Assert.Equal("7", ep.Element("episode")!.Value);
        Assert.Equal("Plot <x>", ep.Element("plot")!.Value);
        Assert.Equal("2010-10-31", ep.Element("aired")!.Value);
        Assert.Equal("44", ep.Element("runtime")!.Value);
        Assert.Equal("18", ep.Element("mpaa")!.Value);
        Assert.Equal("2026-11-15 04:00:00", ep.Element("dateadded")!.Value);
    }

    [Fact, Trait("Check", "C52")]
    public void C52_missing_fields_are_left_out_and_bad_characters_dropped()
    {
        var x = XDocument.Parse(CopyText.MovieNfo(new TitleMeta("Bad\u0001Char\uFFFE 😀"), "d")).Root!;
        Assert.Equal("BadChar 😀", x.Element("title")!.Value);
        Assert.Null(x.Element("year"));
        Assert.Null(x.Element("mpaa"));
        Assert.Empty(x.Elements("uniqueid"));
    }

    [Fact, Trait("Check", "C93")]
    public void C93_provider_ids_are_uniqueid_with_lower_case_type_and_empty_ones_left_out()
    {
        var x = XDocument.Parse(CopyText.MovieNfo(Meta, "d")).Root!;
        var ids = x.Elements("uniqueid").Select(u => (u.Attribute("type")!.Value, u.Value)).ToList();
        Assert.Equal(new[] { ("imdb", "tt1013752"), ("tmdb", "13804") }, ids);
    }

    [Fact, Trait("Check", "C93")]
    public void C93_strm_is_the_path_alone_with_no_line_ending()
    {
        Assert.Equal("N:\\Movies\\Dune (2021)\\Dune.mkv", CopyText.Strm("N:\\Movies\\Dune (2021)\\Dune.mkv"));
        Assert.Equal("N:\\x.mkv", CopyText.Strm("N:\\x.mkv\r\n"));
        Assert.DoesNotContain("\n", CopyText.Strm("K:\\Series\\a.mkv\n"));
    }
}
