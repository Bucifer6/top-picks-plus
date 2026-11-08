using System.Linq;
using SkiaSharp;
using Xunit;
using static TopPicksPlus.Tests.Artwork.TestImages;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>The drawn picture matches D62/D74: number, shade, logo, title and poster.</summary>
public class LayoutTests
{
    private static readonly SKTypeface Anton = TestImages.Anton();
    private static readonly SKColor Grey = new SKColor(128, 128, 128);
    private static readonly SKColor Red = new SKColor(230, 30, 30);
    private static readonly SKColor Blue = new SKColor(30, 60, 230);

    private static bool IsRed(SKColor c) => c.Red > 180 && c.Green < 80 && c.Blue < 80;

    private static bool IsBlue(SKColor c) => c.Blue > 180 && c.Red < 80 && c.Green < 110;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void Rank_number_is_the_right_number_in_the_D62_place(int rank)
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), null, null, rank, null);
        Assert.Null(LayoutChecks.NumberProblem(picture, rank, Anton));
    }

    [Fact]
    public void Left_shade_is_black_to_22_percent_and_clear_by_40_percent()
    {
        using var picture = DrawOk(Solid(1280, 720, new SKColor(200, 200, 200)), null, null, 1, null);
        Assert.Null(LayoutChecks.ShadeProblem(picture, 200));
    }

    [Theory]
    [InlineData(400, 100)] // wide logo: limited by the box width
    [InlineData(100, 400)] // tall logo: limited by the box height
    [InlineData(300, 120)]
    public void Logo_keeps_its_shape_and_sits_on_the_bottom_left_of_the_box(int logoWidth, int logoHeight)
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), Solid(logoWidth, logoHeight, Red), null, 4, "Ignored When Logo");
        Assert.Null(LayoutChecks.LogoProblem(picture, logoWidth, logoHeight, IsRed));
        Assert.Null(LayoutChecks.NumberProblem(picture, 4, Anton));
    }

    [Fact]
    public void Logo_is_drawn_instead_of_the_title()
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), Solid(400, 100, Red), null, 3, "Some Title");
        var white = Bounds(picture, IsWhite, new SKRectI(240, 0, W, H));
        Assert.True(white == null, "white title text was drawn as well as the logo: " + Describe(white));
    }

    [Fact]
    public void Backdrop_is_cropped_to_fill_and_centred()
    {
        // 2000x562 backdrop: red outer quarters, grey middle half. Centred cropping shows only the grey.
        using var bitmap = new SKBitmap(2000, 562);
        bitmap.Erase(Red);
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { Color = Grey })
            canvas.DrawRect(500, 0, 1000, 562, paint);

        using var picture = DrawOk(Png(bitmap), null, null, 1, null);
        var red = Bounds(picture, c => c.Red > 120 && c.Green < 90, new SKRectI(0, 0, W, H));
        Assert.True(red == null, "backdrop is not centred: red edge visible at " + Describe(red));
        Assert.InRange(Brightness(picture.GetPixel(700, 300)), 120, 136);
    }

    [Fact]
    public void Title_without_logo_is_white_inside_the_box_on_its_bottom_left_in_at_most_two_lines()
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), null, null, 6, "The Dog Stars");
        var box = Bounds(picture, IsWhite, new SKRectI(240, 0, W, H));
        Assert.True(box != null, "no white title text drawn");
        var b = box!.Value;
        Assert.InRange(b.Left, 250 - 2, 250 + 12);
        Assert.InRange(b.Right, 300, 920 + 2);
        Assert.InRange(b.Top, 197 - 2, 528);
        Assert.InRange(b.Bottom, 528 - 40, 528 + 2);
        Assert.Equal(2, TextLines(picture, b));
        Assert.Null(LayoutChecks.NumberProblem(picture, 6, Anton));
    }

    [Fact]
    public void Short_title_is_one_big_line()
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), null, null, 2, "Beast");
        var b = Bounds(picture, IsWhite, new SKRectI(240, 0, W, H))!.Value;
        Assert.Equal(1, TextLines(picture, b));
        Assert.True(b.Bottom - b.Top > 100, $"title is only {b.Bottom - b.Top}px tall");
    }

    [Fact]
    public void Unreadable_logo_falls_back_to_the_title()
    {
        using var picture = DrawOk(Solid(1280, 720, Grey), new byte[] { 1, 2, 3, 4 }, null, 2, "Beast");
        Assert.True(Bounds(picture, IsWhite, new SKRectI(240, 0, W, H)) != null, "no title drawn");
    }

    [Fact]
    public void No_backdrop_puts_the_poster_centred_on_black_in_the_right_60_percent()
    {
        using var picture = DrawOk(null, Solid(400, 100, Red), Solid(400, 600, Blue), 5, "Verity");
        var blue = Bounds(picture, IsBlue, new SKRectI(0, 0, W, H));
        Assert.True(blue != null, "poster not drawn");
        var b = blue!.Value;
        // 400x600 fitted into 600x562 at 40%-100%: 374.7 wide, centred at x=700.
        Assert.InRange(b.Left, 513 - 3, 513 + 3);
        Assert.InRange(b.Right, 887 - 3, 887 + 3);
        Assert.InRange(b.Top, 0, 1);
        Assert.InRange(b.Bottom, H - 2, H - 1);
        Assert.True(Brightness(picture.GetPixel(300, 300)) < 10, "left of the poster is not black");
        Assert.True(Brightness(picture.GetPixel(960, 300)) < 10, "right of the poster is not black");
        Assert.True(Bounds(picture, IsRed, new SKRectI(0, 0, W, H)) == null, "logo drawn on a poster picture");
        Assert.Null(LayoutChecks.NumberProblem(picture, 5, Anton));
    }

    [Fact]
    public void Unreadable_backdrop_falls_back_to_the_poster()
    {
        using var picture = DrawOk(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, null, Solid(400, 600, Blue), 5, null);
        Assert.True(Bounds(picture, IsBlue, new SKRectI(0, 0, W, H)) != null, "poster not drawn");
    }

    [Fact]
    public void Output_is_a_png()
    {
        var result = TopPicksPlus.Artwork.RankArtwork.Draw(Solid(1280, 720, Grey), null, null, 1, "x");
        Assert.False(result.Failed, result.FailureReason);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, result.Bytes!.Take(8).ToArray());
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void Anton_font_and_its_OFL_licence_are_embedded_in_the_plugin()
    {
        var assembly = typeof(TopPicksPlus.Artwork.RankArtwork).Assembly;
        using var licence = assembly.GetManifestResourceStream("TopPicksPlus.Artwork.OFL.txt");
        Assert.NotNull(licence);
        var text = new System.IO.StreamReader(licence!).ReadToEnd();
        Assert.Contains("SIL Open Font License, Version 1.1", text);
        Assert.Contains("Anton Project Authors", text);
        Assert.Equal("Anton", Anton.FamilyName);
    }

    // Counts separate bands of rows that hold white pixels inside the box.
    private static int TextLines(SKBitmap picture, SKRectI box)
    {
        var lines = 0;
        var inLine = false;
        for (var y = box.Top; y <= box.Bottom + 1; y++)
        {
            var any = false;
            for (var x = box.Left; x <= box.Right && !any; x++)
                any = IsWhite(picture.GetPixel(x, y));
            if (any && !inLine)
                lines++;
            inLine = any;
        }

        return lines;
    }
}
