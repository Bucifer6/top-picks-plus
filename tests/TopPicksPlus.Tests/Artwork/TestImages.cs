using System;
using System.IO;
using System.Linq;
using SkiaSharp;
using TopPicksPlus.Artwork;
using Xunit;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>Synthetic input pictures and helpers for reading the drawn output. Nothing here uses the plugin's drawing code.</summary>
internal static class TestImages
{
    public const int W = 1000;
    public const int H = 562;

    public static byte[] Solid(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        return Png(bitmap);
    }

    public static byte[] Png(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public static SKBitmap Decode(byte[] png)
    {
        var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);
        return bitmap;
    }

    /// <summary>Draws the picture and checks it worked and is 1000x562.</summary>
    public static SKBitmap DrawOk(byte[]? backdrop, byte[]? logo, byte[]? poster, int rank, string? title)
    {
        var result = RankArtwork.Draw(backdrop, logo, poster, rank, title);
        Assert.False(result.Failed, "drawing failed: " + result.FailureReason);
        var bitmap = Decode(result.Bytes!);
        Assert.Equal(W, bitmap.Width);
        Assert.Equal(H, bitmap.Height);
        return bitmap;
    }

    /// <summary>Bounding box (inclusive pixels) of every pixel in the area that matches, or null if none do.</summary>
    public static SKRectI? Bounds(SKBitmap bitmap, Func<SKColor, bool> match, SKRectI area)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (var y = area.Top; y < area.Bottom; y++)
        for (var x = area.Left; x < area.Right; x++)
        {
            if (!match(bitmap.GetPixel(x, y)))
                continue;
            left = Math.Min(left, x);
            right = Math.Max(right, x);
            top = Math.Min(top, y);
            bottom = Math.Max(bottom, y);
        }

        return left == int.MaxValue ? null : new SKRectI(left, top, right, bottom);
    }

    public static bool IsWhite(SKColor c) => c.Red > 200 && c.Green > 200 && c.Blue > 200;

    public static SKTypeface Anton()
    {
        using var stream = typeof(RankArtwork).Assembly.GetManifestResourceStream("TopPicksPlus.Artwork.Anton-Regular.ttf");
        Assert.NotNull(stream);
        using var copy = new MemoryStream();
        stream!.CopyTo(copy);
        var typeface = SKTypeface.FromData(SKData.CreateCopy(copy.ToArray()));
        Assert.NotNull(typeface);
        return typeface;
    }

    /// <summary>A coarse on/off grid of a shape's bounding box: each cell is on when most of its pixels are.</summary>
    public static bool[,] Grid(Func<int, int, bool> on, SKRectI box, int cols = 40, int rows = 64)
    {
        var grid = new bool[cols, rows];
        var w = box.Right - box.Left + 1;
        var h = box.Bottom - box.Top + 1;
        for (var gy = 0; gy < rows; gy++)
        for (var gx = 0; gx < cols; gx++)
        {
            int x0 = box.Left + (gx * w / cols), x1 = box.Left + ((gx + 1) * w / cols);
            int y0 = box.Top + (gy * h / rows), y1 = box.Top + ((gy + 1) * h / rows);
            int count = 0, total = 0;
            for (var y = y0; y < Math.Max(y1, y0 + 1); y++)
            for (var x = x0; x < Math.Max(x1, x0 + 1); x++)
            {
                total++;
                if (on(x, y))
                    count++;
            }

            grid[gx, gy] = count * 2 > total;
        }

        return grid;
    }

    /// <summary>Overlap of two grids: shared on-cells over all on-cells (1 = identical).</summary>
    public static double Overlap(bool[,] a, bool[,] b)
    {
        int both = 0, either = 0;
        for (var x = 0; x < a.GetLength(0); x++)
        for (var y = 0; y < a.GetLength(1); y++)
        {
            if (a[x, y] && b[x, y]) both++;
            if (a[x, y] || b[x, y]) either++;
        }

        return either == 0 ? 0 : (double)both / either;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool[,]> Templates = new();

    /// <summary>The grid of a number written in Anton by the test itself, independently of the plugin.</summary>
    public static bool[,] NumberTemplate(SKTypeface anton, int number) =>
        Templates.GetOrAdd(number, n => MakeNumberTemplate(anton, n));

    private static bool[,] MakeNumberTemplate(SKTypeface anton, int number)
    {
        using var bitmap = new SKBitmap(1400, 800);
        bitmap.Erase(SKColors.Black);
        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { Typeface = anton, TextSize = 500, IsAntialias = true, Color = SKColors.White })
        {
            canvas.DrawText(number.ToString(System.Globalization.CultureInfo.InvariantCulture), 100, 650, paint);
        }

        var box = Bounds(bitmap, IsWhite, new SKRectI(0, 0, bitmap.Width, bitmap.Height));
        Assert.True(box != null, $"Anton drew nothing for {number}");
        return Grid((x, y) => IsWhite(bitmap.GetPixel(x, y)), box!.Value);
    }

    /// <summary>A made-up backdrop that looks a bit like a film still: a coloured gradient with soft shapes.</summary>
    public static byte[] Backdrop(SKColor top, SKColor bottom, int seed)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using (var canvas = new SKCanvas(bitmap))
        {
            using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 720), new[] { top, bottom }, SKShaderTileMode.Clamp))
            using (var paint = new SKPaint { Shader = shader })
                canvas.DrawRect(0, 0, 1280, 720, paint);

            var random = new Random(seed);
            for (var i = 0; i < 14; i++)
            {
                var c = new SKColor((byte)random.Next(40, 220), (byte)random.Next(40, 220), (byte)random.Next(40, 220), 90);
                using var blob = new SKPaint { Color = c, IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 30) };
                canvas.DrawCircle(random.Next(0, 1280), random.Next(0, 720), random.Next(60, 260), blob);
            }
        }

        return Png(bitmap);
    }

    /// <summary>A made-up title logo: coloured Anton capitals on a transparent background.</summary>
    public static byte[] TextLogo(SKTypeface anton, string text, SKColor color)
    {
        using var paint = new SKPaint { Typeface = anton, TextSize = 200, IsAntialias = true, Color = color };
        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);
        var width = (int)Math.Ceiling(bounds.Width) + 20;
        var height = (int)Math.Ceiling(bounds.Height) + 20;
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(bitmap))
            canvas.DrawText(text, 10 - bounds.Left, 10 - bounds.Top, paint);
        return Png(bitmap);
    }

    public static string Describe(SKRectI? r) => r == null ? "nothing" : $"x {r.Value.Left}-{r.Value.Right}, y {r.Value.Top}-{r.Value.Bottom}";

    public static int Brightness(SKColor c) => new[] { c.Red, c.Green, c.Blue }.Max();
}
