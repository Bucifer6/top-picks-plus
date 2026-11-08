using System;
using System.Linq;
using TopPicksPlus.Artwork;
using Xunit;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>D74: the no-logo title shrinks to fit 2 lines, no smaller than 40 px, then ends in "…".</summary>
public class TitleLayoutTests
{
    // Fake measure: every character is half the text size wide.
    private static float Measure(float size, string text) => text.Length * size * 0.5f;

    private const float BoxW = 670f;
    private const float BoxH = 331.58f;

    [Fact]
    public void Short_title_gets_the_largest_size_on_one_line()
    {
        var fit = TitleLayout.Fit("Beast", Measure, BoxW, BoxH);
        Assert.Equal(160f, fit.Size);
        Assert.Equal(new[] { "Beast" }, fit.Lines);
    }

    [Fact]
    public void Longer_title_wraps_to_two_lines_before_shrinking()
    {
        var fit = TitleLayout.Fit("The Dog Stars", Measure, BoxW, BoxH);
        Assert.Equal(2, fit.Lines.Count);
        Assert.All(fit.Lines, l => Assert.True(Measure(fit.Size, l) <= BoxW));
        Assert.True(TitleLayout.BlockHeight(2, fit.Size) <= BoxH);
    }

    [Fact]
    public void Title_that_needs_three_lines_shrinks_until_two_fit()
    {
        var fit = TitleLayout.Fit("Alpha Bravo Charlie Delta Echo Foxtrot", Measure, BoxW, BoxH);
        Assert.True(fit.Lines.Count <= 2);
        Assert.True(fit.Size < 160f && fit.Size >= 40f);
        Assert.Equal("Alpha Bravo Charlie Delta Echo Foxtrot", string.Join(" ", fit.Lines));
    }

    [Fact]
    public void Title_too_long_at_40_px_is_cut_and_ends_in_an_ellipsis()
    {
        var title = string.Join(" ", Enumerable.Repeat("Word", 80));
        var fit = TitleLayout.Fit(title, Measure, BoxW, BoxH);
        Assert.Equal(40f, fit.Size);
        Assert.Equal(2, fit.Lines.Count);
        Assert.EndsWith("…", fit.Lines[1]);
        Assert.All(fit.Lines, l => Assert.True(Measure(40f, l) <= BoxW, $"'{l}' is too wide"));
    }

    [Fact]
    public void One_enormous_word_is_cut_with_an_ellipsis()
    {
        var fit = TitleLayout.Fit(new string('W', 200), Measure, BoxW, BoxH);
        Assert.Equal(40f, fit.Size);
        Assert.Single(fit.Lines);
        Assert.EndsWith("…", fit.Lines[0]);
        Assert.True(Measure(40f, fit.Lines[0]) <= BoxW);
    }

    [Fact]
    public void Title_over_200_characters_is_cut_before_fitting()
    {
        var seen = 0;
        float CountingMeasure(float size, string text)
        {
            seen = Math.Max(seen, text.Length);
            return Measure(size, text);
        }

        var fit = TitleLayout.Fit(new string('W', 100_000), CountingMeasure, BoxW, BoxH);
        Assert.True(seen <= 201, $"measured a {seen}-character string; the title was not cut to 200 first");
        Assert.EndsWith("…", fit.Lines[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \t ")]
    public void Empty_title_has_no_lines(string? title)
    {
        Assert.Empty(TitleLayout.Fit(title!, Measure, BoxW, BoxH).Lines);
    }
}
