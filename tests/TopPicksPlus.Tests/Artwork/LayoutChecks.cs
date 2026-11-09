using System;
using System.Collections.Generic;
using SkiaSharp;
using static TopPicksPlus.Tests.Artwork.TestImages;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>
/// Reads a drawn picture and says what is wrong with it against D62/D74, or null if nothing is.
/// Every expected position is written here from D62's fractions, not read from the plugin's Layout,
/// so a wrong value in the plugin fails. LayoutCheckCanaries proves each check catches a wrong picture.
/// </summary>
internal static class LayoutChecks
{
    private const int Tolerance = 4;

    // D62: number left edge 3.5%, top 12%, bottom 90%, inside the 22% band; it must read as the right digits.
    public static string? NumberProblem(SKBitmap picture, int rank, SKTypeface anton)
    {
        // Searches only left of the logo box (25%), so a white title never counts as part of the number.
        var box = Bounds(picture, IsWhite, new SKRectI(0, 0, (int)(0.25 * W), H));
        if (box == null)
            return "no white number found in the left 25%";

        var b = box.Value;
        var problems = new List<string>();
        Near(problems, "left edge", b.Left, 0.035 * W);
        Near(problems, "top", b.Top, 0.12 * H);
        Near(problems, "bottom", b.Bottom, 0.90 * H);
        if (b.Right > (0.22 * W) + 2)
            problems.Add($"right edge {b.Right} is past 22% ({0.22 * W})");

        // Shape kept (D97 b): the number keeps Anton's width-to-height ratio, and is squeezed sideways
        // only when that would make it wider than the 3.5%-22% band; then it fills the band exactly.
        var height = b.Bottom - b.Top + 1;
        var width = b.Right - b.Left + 1;
        var natural = NumberRatio(anton, rank) * height;
        var expectedWidth = Math.Min(natural, (0.22 - 0.035) * W);
        if (Math.Abs(width - expectedWidth) > Math.Max(Tolerance, 0.03 * expectedWidth))
            problems.Add($"width {width} should be {expectedWidth:F0} (Anton's shape kept, squeezed only to fit the band)");

        var drawn = Grid((x, y) => IsWhite(picture.GetPixel(x, y)), b);
        var best = 0;
        var bestScore = -1.0;
        var own = 0.0;
        for (var n = 1; n <= 10; n++)
        {
            var score = Overlap(drawn, NumberTemplate(anton, n));
            if (n == rank)
                own = score;
            if (score > bestScore)
            {
                bestScore = score;
                best = n;
            }
        }

        if (best != rank)
            problems.Add($"the number reads as {best} (overlap {bestScore:F2}), not {rank} ({own:F2})");
        else if (own < 0.85)
            problems.Add($"the number only loosely matches {rank} in Anton (overlap {own:F2})");

        return problems.Count == 0 ? null : $"number at {Describe(box)}: {string.Join("; ", problems)}";
    }

    // D62: solid black from 0 to 22% of the width, fading to clear by 40%. Needs a picture drawn over a
    // flat backdrop of brightness `flat`; reads the row near the bottom, under the number.
    public static string? ShadeProblem(SKBitmap picture, int flat)
    {
        const int row = 550;
        var problems = new List<string>();
        foreach (var x in new[] { 2, 60, 120, 180, 215 })
        {
            var v = Brightness(picture.GetPixel(x, row));
            if (v > 12)
                problems.Add($"x={x} is {v}, should be black");
        }

        foreach (var x in new[] { 265, 310, 355 })
        {
            var expected = flat * (x - (0.22 * W)) / ((0.40 - 0.22) * W);
            var v = Brightness(picture.GetPixel(x, row));
            if (Math.Abs(v - expected) > 15)
                problems.Add($"x={x} is {v}, should be about {expected:F0} (fading)");
        }

        foreach (var x in new[] { 405, 500, 700, 995 })
        {
            var v = Brightness(picture.GetPixel(x, row));
            if (v < flat - 12)
                problems.Add($"x={x} is {v}, should be clear ({flat})");
        }

        return problems.Count == 0 ? null : "shade: " + string.Join("; ", problems);
    }

    // D62: the logo keeps its shape, as large as fits in 25-92% x 35-94%, on the box's bottom-left corner.
    public static string? LogoProblem(SKBitmap picture, int logoWidth, int logoHeight, Func<SKColor, bool> isLogo)
    {
        var box = Bounds(picture, isLogo, new SKRectI(0, 0, W, H));
        if (box == null)
            return "no logo found";

        double left = 0.25 * W, right = 0.92 * W, top = 0.35 * H, bottom = 0.94 * H;
        var scale = Math.Min((right - left) / logoWidth, (bottom - top) / logoHeight);
        var problems = new List<string>();
        var b = box.Value;
        Near(problems, "left", b.Left, left);
        Near(problems, "bottom", b.Bottom + 1, bottom);
        Near(problems, "right", b.Right + 1, left + (logoWidth * scale));
        Near(problems, "top", b.Top, bottom - (logoHeight * scale));
        return problems.Count == 0 ? null : $"logo at {Describe(box)}: {string.Join("; ", problems)}";
    }

    private static void Near(List<string> problems, string what, double actual, double expected)
    {
        if (Math.Abs(actual - expected) > Tolerance)
            problems.Add($"{what} {actual} should be {expected:F1}");
    }
}
