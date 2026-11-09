using System;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;
using static TopPicksPlus.Tests.Logic.Make;

namespace TopPicksPlus.Tests.Logic;

/// <summary>Counting plays (spec F2 step 2, F6).</summary>
public class CountingTests
{
    [Fact, Trait("Check", "C10")]
    public void C10_binge_counts_once_per_day_and_three_days_count_three()
    {
        var plays = Enumerable.Range(1, 6)
            .Select(e => APlay("ann", 1, Sunday.AddDays(-2), minutes: 45, runtime: 45, episode: 100 + e))
            .Concat(new[] { 1, 3, 5 }.Select(d => APlay("bob", 2, Sunday.AddDays(-d), played: true)))
            .ToList();
        var days = Counting.ViewerDays(plays, Sunday);
        Assert.Equal(1, days[1]);
        Assert.Equal(3, days[2]);
    }

    [Fact, Trait("Check", "C10")]
    public void C10_two_users_on_the_same_day_count_two()
    {
        var days = Counting.ViewerDays(new[] { Watched("ann", 1), Watched("bob", 1) }, Sunday);
        Assert.Equal(2, days[1]);
    }

    [Fact, Trait("Check", "C12")]
    public void C12_two_30_percent_sittings_same_day_count_and_5_minutes_does_not()
    {
        var day = Sunday.AddDays(-1);
        Assert.True(Counting.Counts(new[] { APlay("a", 1, day, 30, runtime: 100), APlay("a", 1, day, 30, runtime: 100) }));
        Assert.False(Counting.Counts(new[] { APlay("a", 1, day, 5, runtime: 100) }));
        Assert.True(Counting.Counts(new[] { APlay("a", 1, day, 50, runtime: 100) }));
        Assert.False(Counting.Counts(new[] { APlay("a", 1, day, 49.9, runtime: 100) }));
        Assert.False(Counting.Counts(Array.Empty<Play>()));
    }

    [Fact, Trait("Check", "C12")]
    public void C12_sittings_on_different_days_do_not_add_up()
    {
        var plays = new[] { APlay("a", 1, Sunday.AddDays(-1), 30), APlay("a", 1, Sunday.AddDays(-2), 30) };
        Assert.Empty(Counting.ViewerDays(plays, Sunday));
    }

    [Fact, Trait("Check", "C24")]
    public void C24_play_91_days_ago_is_trimmed_and_90_days_and_run_day_are_kept()
    {
        var plays = new[]
        {
            APlay("a", 1, Sunday.AddDays(-91)),
            APlay("a", 2, Sunday.AddDays(-90)),
            APlay("a", 3, Sunday),
            APlay("a", 4, Sunday.AddDays(-1)),
        };
        var kept = Counting.Trim(plays, Sunday).Select(p => p.SourceId).ToArray();
        Assert.Equal(new long[] { 2, 3, 4 }, kept);
    }

    [Fact, Trait("Check", "C33")]
    public void C33_sunday_window_is_previous_sunday_to_saturday()
    {
        var (first, last) = Counting.Window(Sunday);
        Assert.Equal(new DateTime(2026, 11, 8), first);
        Assert.Equal(DayOfWeek.Sunday, first.DayOfWeek);
        Assert.Equal(new DateTime(2026, 11, 14), last);
        Assert.Equal(DayOfWeek.Saturday, last.DayOfWeek);
        Assert.False(Counting.InWindow(Sunday, Sunday));
        Assert.False(Counting.InWindow(Sunday.AddDays(-8), Sunday));
        Assert.True(Counting.InWindow(Sunday.AddDays(-7), Sunday));
        Assert.True(Counting.InWindow(Sunday.AddDays(-1), Sunday));
    }

    [Fact, Trait("Check", "C33")]
    public void C33_midweek_run_uses_the_7_days_before_it()
    {
        var wednesday = new DateTime(2026, 11, 18, 13, 45, 0);
        var (first, last) = Counting.Window(wednesday);
        Assert.Equal(new DateTime(2026, 11, 11), first);
        Assert.Equal(new DateTime(2026, 11, 17), last);
        var plays = new[] { APlay("a", 1, wednesday.Date, played: true), APlay("a", 2, new DateTime(2026, 11, 11), played: true), APlay("a", 3, new DateTime(2026, 11, 10), played: true) };
        var days = Counting.ViewerDays(plays, wednesday);
        Assert.Equal(new long[] { 2 }, days.Keys.ToArray());
        Assert.Equal(1, Counting.PlaysInWindow(plays, wednesday));
    }

    [Fact, Trait("Check", "C34")]
    public void C34_30_percent_of_two_episodes_does_not_count_but_two_sittings_of_one_do()
    {
        var day = Sunday.AddDays(-1);
        var twoEpisodes = new[] { APlay("a", 1, day, 15, runtime: 50, episode: 11), APlay("a", 1, day, 15, runtime: 50, episode: 12) };
        Assert.Empty(Counting.ViewerDays(twoEpisodes, Sunday));
        var oneEpisode = new[] { APlay("a", 1, day, 15, runtime: 50, episode: 11), APlay("a", 1, day, 15, runtime: 50, episode: 11) };
        Assert.Equal(1, Counting.ViewerDays(oneEpisode, Sunday)[1]);
    }

    [Fact, Trait("Check", "C34")]
    public void C34_marked_played_on_any_episode_counts()
    {
        var day = Sunday.AddDays(-1);
        var plays = new[] { APlay("a", 1, day, 1, episode: 11), APlay("a", 1, day, 0, played: true, episode: 12) };
        Assert.Equal(1, Counting.ViewerDays(plays, Sunday)[1]);
    }

    [Theory, Trait("Check", "C54")]
    [InlineData(10.0, 40.0, 100.0, 30.0)]   // normal
    [InlineData(40.0, 10.0, 100.0, 0.0)]    // never below 0
    [InlineData(0.0, 150.0, 100.0, 100.0)]  // never above the runtime
    [InlineData(0.0, 150.0, null, 150.0)]   // no runtime: not capped
    [InlineData(null, 80.0, 100.0, 0.0)]    // start not seen: 0
    public void C54_minutes_watched(double? start, double stop, double? runtime, double expected) =>
        Assert.Equal(expected, Counting.MinutesWatched(start, stop, runtime), 6);

    [Fact, Trait("Check", "C54")]
    public void C54_no_runtime_20_minutes_counts_and_marked_played_counts_alone()
    {
        var day = Sunday.AddDays(-1);
        Assert.True(Counting.Counts(new[] { APlay("a", 1, day, 20, runtime: null) }));
        Assert.False(Counting.Counts(new[] { APlay("a", 1, day, 19.9, runtime: null) }));
        Assert.True(Counting.Counts(new[] { APlay("a", 1, day, 10, runtime: null), APlay("a", 1, day, 10, runtime: null) }));
        Assert.True(Counting.Counts(new[] { APlay("a", 1, day, 0, played: true, runtime: 100) }));
        // A zero runtime is treated as unknown.
        Assert.False(Counting.Counts(new[] { APlay("a", 1, day, 5, runtime: 0) }));
    }

    [Theory, Trait("Check", "C75")]
    [InlineData("Movie", true)]
    [InlineData("Episode", true)]
    [InlineData("episode", true)]
    [InlineData("Audio", false)]
    [InlineData("Trailer", false)]
    [InlineData("Series", false)]
    [InlineData("TvChannel", false)]
    [InlineData(null, false)]
    public void C75_only_movies_and_episodes_are_logged(string? type, bool logged) =>
        Assert.Equal(logged, Counting.IsLoggable(type));

    [Fact, Trait("Check", "C75")]
    public void C75_a_play_is_dated_by_its_UK_day()
    {
        var p = new Play("a", 1, null, new DateTime(2026, 11, 14, 23, 59, 0), 10, false, 100, PlayOrigin.Live);
        Assert.Equal(new DateTime(2026, 11, 14), p.DayUk);
        Assert.True(Counting.InWindow(p.DayUk, Sunday));
    }
}
