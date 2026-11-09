using System;
using System.Collections.Generic;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;
using static TopPicksPlus.Tests.Logic.Make;

namespace TopPicksPlus.Tests.Logic;

/// <summary>Choosing the 10 (spec F2).</summary>
public class ChoosingTests
{
    private static List<Candidate> Movies(int count, int firstId = 1) =>
        Enumerable.Range(firstId, count).Select(i => Movie(i, addedDaysAgo: i)).ToList();

    private static long[] Ids(ChooseResult r) => r.Picks.Select(p => p.SourceId).ToArray();

    [Fact, Trait("Check", "C6")]
    public void C6_no_pins_fills_all_10_from_what_was_watched()
    {
        var candidates = Movies(15);
        var scores = Days(Enumerable.Range(1, 12).Select(i => ((long)i, 20 - i)).ToArray());
        var r = Choosing.Choose(candidates, Array.Empty<PinStatus>(), scores);
        Assert.Equal(10, r.Picks.Count);
        Assert.Equal(Enumerable.Range(1, 10).Select(i => (long)i), Ids(r));
        Assert.All(r.Picks, p => Assert.Equal(PickReason.ViewerDays, p.Reason));
        Assert.Equal(Enumerable.Range(1, 10), r.Picks.Select(p => p.Rank));
        Assert.Equal(19, r.Picks[0].ViewerDays);
    }

    [Fact, Trait("Check", "C7")]
    public void C7_three_pins_first_in_order_then_most_watched()
    {
        var candidates = Movies(20);
        var scores = Days((5, 9), (6, 8), (7, 7), (8, 6), (9, 5), (10, 4), (11, 3), (12, 2), (13, 1));
        var r = Choosing.Choose(candidates, Pins(17, 3, 19), scores);
        Assert.Equal(new long[] { 17, 3, 19, 5, 6, 7, 8, 9, 10, 11 }, Ids(r));
        Assert.Equal(new[] { PickReason.Pinned, PickReason.Pinned, PickReason.Pinned }, r.Picks.Take(3).Select(p => p.Reason));
        Assert.All(r.Picks.Skip(3), p => Assert.Equal(PickReason.ViewerDays, p.Reason));
    }

    [Fact, Trait("Check", "C8")]
    public void C8_ten_pins_are_exactly_the_row_in_order()
    {
        var pins = new long[] { 40, 30, 20, 10, 50, 60, 70, 80, 90, 100 };
        var r = Choosing.Choose(Movies(100), Pins(pins), Days((1, 50), (2, 40)));
        Assert.Equal(pins, Ids(r));
        Assert.All(r.Picks, p => Assert.Equal(PickReason.Pinned, p.Reason));
    }

    [Fact, Trait("Check", "C8")]
    public void C8_pins_outside_the_chosen_libraries_still_count()
    {
        // D23: pins need not be candidates.
        var r = Choosing.Choose(Movies(3), Pins(999), Days());
        Assert.Equal(999, r.Picks[0].SourceId);
    }

    [Fact, Trait("Check", "C13")]
    public void C13_four_watched_then_newest_additions_as_filler()
    {
        var candidates = Movies(20);
        var scores = Days((15, 3), (16, 2), (17, 2), (18, 1));
        var r = Choosing.Choose(candidates, Array.Empty<PinStatus>(), scores);
        Assert.Equal(10, r.Picks.Count);
        // Movie i was added i days ago, so the newest unpicked ones are 1, 2, 3 ...
        Assert.Equal(new long[] { 15, 16, 17, 18, 1, 2, 3, 4, 5, 6 }, Ids(r));
        Assert.All(r.Picks.Skip(4), p => Assert.Equal(PickReason.Filler, p.Reason));
        Assert.All(r.Picks.Skip(4), p => Assert.Equal(0, p.ViewerDays));
    }

    [Fact, Trait("Check", "C35")]
    public void C35_full_ties_go_to_the_lower_id_as_a_number()
    {
        var same = Sunday.AddDays(-5);
        var candidates = new[] { 100L, 99L, 1000L, 7L }
            .Select(id => new Candidate(id, TitleKind.Movie, same, new DateTime(2020, 1, 1), true)).ToList();
        var scores = Days((100, 2), (99, 2), (1000, 2), (7, 2));
        for (var i = 0; i < 5; i++)
        {
            var shuffled = candidates.OrderBy(c => (c.SourceId * 7919 + i * 31) % 13).ToList();
            var r = Choosing.Choose(shuffled, Array.Empty<PinStatus>(), scores);
            Assert.Equal(new long[] { 7, 99, 100, 1000 }, Ids(r));
        }
    }

    [Fact, Trait("Check", "C69")]
    public void C69_ties_go_to_newest_content_then_newer_premiere()
    {
        var candidates = new[]
        {
            new Candidate(1, TitleKind.Movie, Sunday.AddDays(-10), new DateTime(2024, 1, 1), true),
            new Candidate(2, TitleKind.Series, Sunday.AddDays(-2), new DateTime(2000, 1, 1), true),
            new Candidate(3, TitleKind.Movie, Sunday.AddDays(-10), new DateTime(2025, 1, 1), true),
            new Candidate(4, TitleKind.Movie, Sunday.AddDays(-10), null, true),
        };
        var r = Choosing.Choose(candidates, Array.Empty<PinStatus>(), Days((1, 3), (2, 3), (3, 3), (4, 3)));
        Assert.Equal(new long[] { 2, 3, 1, 4 }, Ids(r));
    }

    [Fact, Trait("Check", "C69")]
    public void C69_more_viewer_days_beats_newer_content()
    {
        var candidates = new[] { Movie(1, addedDaysAgo: 100), Movie(2, addedDaysAgo: 1) };
        var r = Choosing.Choose(candidates, Array.Empty<PinStatus>(), Days((1, 4), (2, 3)));
        Assert.Equal(new long[] { 1, 2 }, Ids(r));
    }

    [Fact, Trait("Check", "C69")]
    public void C69_a_pin_that_is_also_most_watched_appears_once()
    {
        var r = Choosing.Choose(Movies(12), Pins(5, 5), Days((5, 10), (6, 9)));
        Assert.Single(r.Picks, p => p.SourceId == 5);
        Assert.Equal(PickReason.Pinned, r.Picks[0].Reason);
        Assert.Equal(10, r.Picks[0].ViewerDays);
        Assert.Equal(6, r.Picks[1].SourceId);
        Assert.Equal(10, r.Picks.Count);
    }

    [Fact, Trait("Check", "C69")]
    public void C69_a_movie_with_no_file_is_skipped()
    {
        var candidates = new[] { Movie(1, available: false), Movie(2) };
        var r = Choosing.Choose(candidates, Array.Empty<PinStatus>(), Days((1, 9)));
        Assert.Equal(new long[] { 2 }, Ids(r));
        Assert.False(Choosing.IsAvailable(TitleKind.Movie, 0));
        Assert.True(Choosing.IsAvailable(TitleKind.Movie, 1));
    }

    [Fact, Trait("Check", "C69")]
    public void C69_fewer_than_10_candidates_gives_a_shorter_row()
    {
        var r = Choosing.Choose(Movies(4), Array.Empty<PinStatus>(), Days());
        Assert.Equal(4, r.Picks.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, r.Picks.Select(p => p.Rank));
    }

    [Fact, Trait("Check", "C69")]
    public void C69_a_title_listed_twice_appears_once()
    {
        var r = Choosing.Choose(new[] { Movie(1), Movie(1), Movie(2) }, Array.Empty<PinStatus>(), Days());
        Assert.Equal(new long[] { 1, 2 }, Ids(r));
    }

    [Fact, Trait("Check", "C69")]
    public void C69_plays_for_titles_that_are_not_candidates_do_not_score()
    {
        var r = Choosing.Choose(Movies(2), Array.Empty<PinStatus>(), Days((50, 99)));
        Assert.DoesNotContain(50L, Ids(r));
    }

    [Fact, Trait("Check", "C88")]
    public void C88_series_with_some_files_is_a_candidate_and_series_pin_needs_one_file()
    {
        Assert.True(Choosing.IsAvailable(TitleKind.Series, 1));
        Assert.False(Choosing.IsAvailable(TitleKind.Series, 0));
        var pins = new[] { new PinStatus(8, Choosing.IsAvailable(TitleKind.Series, 0)), new PinStatus(9, Choosing.IsAvailable(TitleKind.Series, 3)) };
        var r = Choosing.Choose(new[] { Series(1) }, pins, Days());
        Assert.Equal(new long[] { 9, 1 }, Ids(r));
        Assert.Equal(new long[] { 8 }, r.MissingPins);
    }

    [Fact, Trait("Check", "C9")]
    public void Missing_pin_is_skipped_reported_and_the_rest_move_up()
    {
        var pins = new[] { new PinStatus(5, true), new PinStatus(6, false), new PinStatus(7, true) };
        var r = Choosing.Choose(Movies(10), pins, Days());
        Assert.Equal(new long[] { 5, 7 }, Ids(r).Take(2));
        Assert.Equal(2, r.Picks[1].Rank);
        Assert.Equal(new long[] { 6 }, r.MissingPins);
    }

    [Theory, Trait("Check", "C68")]
    [InlineData("Movie", false, 3, 99L, PinRefusal.None)]
    [InlineData("Series", false, 9, 99L, PinRefusal.None)]
    [InlineData("Movie", false, 10, 99L, PinRefusal.TooMany)]
    [InlineData("Movie", false, 3, 2L, PinRefusal.Duplicate)]
    [InlineData("Episode", false, 3, 99L, PinRefusal.NotMovieOrSeries)]
    [InlineData("BoxSet", false, 3, 99L, PinRefusal.NotMovieOrSeries)]
    [InlineData("Folder", false, 3, 99L, PinRefusal.NotMovieOrSeries)]
    [InlineData(null, false, 3, 99L, PinRefusal.NotMovieOrSeries)]
    [InlineData("Movie", true, 3, 99L, PinRefusal.InTopPicksLibrary)]
    public void C68_pin_rules(string? type, bool inTopPicks, int existingCount, long newId, PinRefusal expected)
    {
        var existing = Enumerable.Range(1, existingCount).Select(i => (long)i).ToList();
        Assert.Equal(expected, PinRules.CheckAdd(existing, newId, type, inTopPicks));
    }
}
