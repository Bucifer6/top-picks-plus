using System;
using SkiaSharp;
using Xunit;
using static TopPicksPlus.Tests.Artwork.TestImages;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>
/// Card 0003: a layout check that passes on every picture proves nothing. Each test here hands a check a
/// deliberately wrong picture and requires the check to object. (The same breaks were also made in the
/// plugin code once, and CI failed; see D97.)
/// </summary>
public class LayoutCheckCanaries
{
    private static readonly SKTypeface Anton = TestImages.Anton();
    private static readonly SKColor Grey = new SKColor(128, 128, 128);
    private static readonly SKColor Red = new SKColor(230, 30, 30);

    private static bool IsRed(SKColor c) => c.Red > 180 && c.Green < 80 && c.Blue < 80;

    [Theory]
    [InlineData(1, 7)]
    [InlineData(3, 8)]
    [InlineData(6, 9)]
    [InlineData(8, 3)]
    [InlineData(10, 1)]
    [InlineData(1, 10)]
    public void Number_check_catches_the_wrong_number(int drawn, int claimed)
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), null, null, drawn, null);
        Assert.NotNull(LayoutChecks.NumberProblem(picture, claimed, Anton));
    }

    [Theory]
    [InlineData(0, 40)]   // too low
    [InlineData(0, -30)]  // too high
    [InlineData(40, 0)]   // too far right
    public void Number_check_catches_a_moved_number(int dx, int dy)
    {
        using var right = DrawOk(Solid(1280, 720, Grey), null, null, 4, null);
        using var moved = Shift(right, new SKRectI(0, 0, 300, H), dx, dy);
        Assert.NotNull(LayoutChecks.NumberProblem(moved, 4, Anton));
    }

    [Fact]
    public void Number_check_catches_a_number_that_is_not_Anton()
    {
        // A blocky seven-segment "3" filling exactly the D62 box, so only the shape differs from Anton's "3".
        using var picture = new SKBitmap(W, H);
        picture.Erase(SKColors.Black);
        using (var canvas = new SKCanvas(picture))
        using (var paint = new SKPaint { Color = SKColors.White })
        {
            float l = 35, r = 220, t = 67.44f, btm = 505.8f, bar = 70, mid = (t + btm) / 2;
            canvas.DrawRect(new SKRect(l, t, r, t + bar), paint);
            canvas.DrawRect(new SKRect(l + 40, mid - (bar / 2), r, mid + (bar / 2)), paint);
            canvas.DrawRect(new SKRect(l, btm - bar, r, btm), paint);
            canvas.DrawRect(new SKRect(r - bar, t, r, btm), paint);
        }

        Assert.NotNull(LayoutChecks.NumberProblem(picture, 3, Anton));
    }

    [Theory]
    [InlineData(1, 1.0, 0)]    // "1" stretched to fill the whole band (review finding 8)
    [InlineData(4, 0.5, 0)]    // "4" squeezed to half the band
    [InlineData(10, 0.8, 0)]   // "10" not reaching the 22% edge
    [InlineData(1, 0, 1.25)]   // "1" a quarter wider than Anton's shape, still inside the band
    public void Number_check_catches_a_stretched_or_squeezed_number(int number, double bandShare, double widthFactor)
    {
        // Anton's own outline at the D62 height and place; only the width-to-height ratio is wrong.
        using var picture = new SKBitmap(W, H);
        picture.Erase(SKColors.Black);
        using (var canvas = new SKCanvas(picture))
        using (var paint = new SKPaint { Typeface = Anton, TextSize = 1000, IsAntialias = true, Color = SKColors.White })
        using (var path = paint.GetTextPath(number.ToString(System.Globalization.CultureInfo.InvariantCulture), 0, 0))
        {
            var b = path.TightBounds;
            const float top = 67.44f, height = 438.36f, band = 185f;
            var natural = b.Width * height / b.Height;
            var width = bandShare > 0 ? (float)(band * bandShare) : (float)Math.Min(band, natural * widthFactor);
            canvas.Translate(35, top);
            canvas.Scale(width / b.Width, height / b.Height);
            canvas.Translate(-b.Left, -b.Top);
            canvas.DrawPath(path, paint);
        }

        Assert.NotNull(LayoutChecks.NumberProblem(picture, number, Anton));
    }

    [Theory]
    [InlineData(0.00, 0.00)] // no shade at all
    [InlineData(0.22, 0.22)] // hard edge, no fade
    [InlineData(0.30, 0.50)] // fade in the wrong place
    [InlineData(0.15, 0.40)] // solid part too narrow
    [InlineData(0.22, 0.60)] // fades out too late
    public void Shade_check_catches_a_wrong_shade(double solidTo, double clearBy)
    {
        using var picture = new SKBitmap(W, H);
        for (var x = 0; x < W; x++)
        {
            var f = x / (double)W;
            var dark = f <= solidTo ? 1.0 : f >= clearBy ? 0.0 : (clearBy - f) / (clearBy - solidTo);
            var v = (byte)Math.Round(200 * (1 - dark));
            for (var y = 0; y < H; y++)
                picture.SetPixel(x, y, new SKColor(v, v, v));
        }

        Assert.NotNull(LayoutChecks.ShadeProblem(picture, 200));
    }

    [Fact]
    public void Shade_check_passes_a_hand_made_correct_shade()
    {
        using var picture = new SKBitmap(W, H);
        for (var x = 0; x < W; x++)
        {
            var f = x / (double)W;
            var dark = f <= 0.22 ? 1.0 : f >= 0.40 ? 0.0 : (0.40 - f) / 0.18;
            var v = (byte)Math.Round(200 * (1 - dark));
            for (var y = 0; y < H; y++)
                picture.SetPixel(x, y, new SKColor(v, v, v));
        }

        Assert.Null(LayoutChecks.ShadeProblem(picture, 200));
    }

    [Theory]
    [InlineData(250, 194, 670, 167)]  // top-left of the box instead of bottom-left
    [InlineData(250, 361, 335, 167)]  // squashed: shape not kept
    [InlineData(250, 400, 400, 100)]  // not scaled up to fill the box
    [InlineData(415, 361, 670, 167)]  // centred-ish instead of on the left
    [InlineData(250, 340, 670, 167)]  // floating above the bottom
    public void Logo_check_catches_a_wrong_logo_position(int left, int top, int width, int height)
    {
        using var picture = new SKBitmap(W, H);
        picture.Erase(Grey);
        using (var canvas = new SKCanvas(picture))
        using (var paint = new SKPaint { Color = Red })
            canvas.DrawRect(left, top, width, height, paint);

        Assert.NotNull(LayoutChecks.LogoProblem(picture, 400, 100, IsRed));
    }

    [Fact]
    public void Logo_check_passes_a_hand_made_correct_logo()
    {
        using var picture = new SKBitmap(W, H);
        picture.Erase(Grey);
        using (var canvas = new SKCanvas(picture))
        using (var paint = new SKPaint { Color = Red })
            canvas.DrawRect(250, 528.28f - 167.5f, 670, 167.5f, paint);

        Assert.Null(LayoutChecks.LogoProblem(picture, 400, 100, IsRed));
    }

    // Copies the area's pixels moved by (dx, dy), filling the gap with black.
    private static SKBitmap Shift(SKBitmap source, SKRectI area, int dx, int dy)
    {
        var copy = source.Copy();
        for (var y = area.Top; y < area.Bottom; y++)
        for (var x = area.Left; x < area.Right; x++)
        {
            int sx = x - dx, sy = y - dy;
            var inside = sx >= area.Left && sx < area.Right && sy >= 0 && sy < source.Height;
            copy.SetPixel(x, y, inside ? source.GetPixel(sx, sy) : SKColors.Black);
        }

        return copy;
    }
}
