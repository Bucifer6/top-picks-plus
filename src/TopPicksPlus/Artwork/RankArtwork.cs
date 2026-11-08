using System;
using System.IO;
using SkiaSharp;

namespace TopPicksPlus.Artwork;

/// <summary>
/// Draws the numbered 1000x562 picture used for both folder.png and thumb.png (spec F3, D6, D62, D74).
/// Image bytes in, PNG bytes out. Never throws: anything that goes wrong gives a failed result (D15).
/// </summary>
public static class RankArtwork
{
    public const int Width = Layout.Width;
    public const int Height = Layout.Height;

    private const string FontResource = "TopPicksPlus.Artwork.Anton-Regular.ttf";
    private static readonly object FontLock = new object();
    private static SKData? fontData;
    private static SKTypeface? font;

    /// <summary>
    /// Draws the picture for one copy.
    /// <list type="bullet">
    /// <item>With a readable backdrop: backdrop cropped to fill, left shade, logo in the box (or the title when
    /// there is no readable logo), then the rank number.</item>
    /// <item>With no readable backdrop: the poster centred on black in 40%–100% of the width, then the number (D74, D98).</item>
    /// <item>With neither, or a rank outside 1–10: failed.</item>
    /// </list>
    /// </summary>
    public static ArtworkResult Draw(byte[]? backdrop, byte[]? logo, byte[]? poster, int rank, string? title)
    {
        try
        {
            if (rank < 1 || rank > 10)
                return ArtworkResult.Fail($"rank {rank} is not 1-10");

            var typeface = Font();
            if (typeface == null)
                return ArtworkResult.Fail("embedded Anton font could not be loaded");

            using var backdropImage = Decode(backdrop);
            using var posterImage = backdropImage == null ? Decode(poster) : null;
            if (backdropImage == null && posterImage == null)
                return ArtworkResult.Fail("no readable backdrop or poster");

            using var logoImage = backdropImage != null ? Decode(logo) : null;

            var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            if (surface == null)
                return ArtworkResult.Fail("could not create a drawing surface");

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            if (backdropImage != null)
            {
                DrawCover(canvas, backdropImage, SKRect.Create(0, 0, Width, Height));
                DrawShade(canvas);
                if (logoImage != null)
                    DrawLogo(canvas, logoImage);
                else
                    DrawTitle(canvas, typeface, title);
            }
            else
            {
                DrawContain(canvas, posterImage!, new SKRect(Layout.PosterLeft, 0, Width, Height));
                DrawShade(canvas);
            }

            DrawNumber(canvas, typeface, rank);
            canvas.Flush();

            using var snapshot = surface.Snapshot();
            using var png = snapshot.Encode(SKEncodedImageFormat.Png, 100);
            if (png == null)
                return ArtworkResult.Fail("PNG encoding failed");
            return ArtworkResult.Drawn(png.ToArray());
        }
        catch (Exception ex)
        {
            // D15: a run never fails because of a picture. Includes a missing native libSkiaSharp.
            return ArtworkResult.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static SKTypeface? Font()
    {
        lock (FontLock)
        {
            if (font != null)
                return font;
            using var stream = typeof(RankArtwork).Assembly.GetManifestResourceStream(FontResource);
            if (stream == null)
                return null;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            // The typeface keeps using this data, so it is kept for the life of the plugin, never disposed.
            fontData = SKData.CreateCopy(copy.ToArray());
            font = fontData == null ? null : SKTypeface.FromData(fontData);
            return font;
        }
    }

    // Decodes fully or not at all: unreadable, truncated, empty or oversized input gives null.
    private static SKBitmap? Decode(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;
        using var stream = new MemoryStream(bytes, writable: false);
        using var codec = SKCodec.Create(stream);
        if (codec == null)
            return null;
        var size = codec.Info;
        if (size.Width <= 0 || size.Height <= 0 || (long)size.Width * size.Height > Layout.MaxSourcePixels)
            return null;

        var target = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(target);
        var result = codec.GetPixels(target, bitmap.GetPixels());
        if (result != SKCodecResult.Success)
        {
            bitmap.Dispose();
            return null;
        }

        return bitmap;
    }

    private static SKPaint ImagePaint() => new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };

    // Fills the area, cropping the image's overflow equally from both sides (D62 "cropped to fit, centred").
    private static void DrawCover(SKCanvas canvas, SKBitmap image, SKRect area)
    {
        var scale = Math.Max(area.Width / image.Width, area.Height / image.Height);
        var srcW = area.Width / scale;
        var srcH = area.Height / scale;
        var src = SKRect.Create((image.Width - srcW) / 2f, (image.Height - srcH) / 2f, srcW, srcH);
        using var paint = ImagePaint();
        canvas.DrawBitmap(image, src, area, paint);
    }

    // Fits the whole image inside the area, centred (D74 poster).
    private static void DrawContain(SKCanvas canvas, SKBitmap image, SKRect area)
    {
        var scale = Math.Min(area.Width / image.Width, area.Height / image.Height);
        var w = image.Width * scale;
        var h = image.Height * scale;
        var dest = SKRect.Create(area.Left + ((area.Width - w) / 2f), area.Top + ((area.Height - h) / 2f), w, h);
        using var paint = ImagePaint();
        canvas.DrawBitmap(image, dest, paint);
    }

    private static void DrawShade(SKCanvas canvas)
    {
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(Layout.ShadeClearX, 0),
            new[] { SKColors.Black, SKColors.Black, SKColors.Black.WithAlpha(0) },
            new[] { 0f, Layout.ShadeSolidX / Layout.ShadeClearX, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader, IsAntialias = false };
        canvas.DrawRect(new SKRect(0, 0, Layout.ShadeClearX, Height), paint);
    }

    // Keeps the logo's shape, as large as fits in the box, on the box's bottom-left corner (D62).
    private static void DrawLogo(SKCanvas canvas, SKBitmap logo)
    {
        var boxW = Layout.BoxRight - Layout.BoxLeft;
        var boxH = Layout.BoxBottom - Layout.BoxTop;
        var scale = Math.Min(boxW / logo.Width, boxH / logo.Height);
        var w = logo.Width * scale;
        var h = logo.Height * scale;
        var dest = SKRect.Create(Layout.BoxLeft, Layout.BoxBottom - h, w, h);
        using var paint = ImagePaint();
        canvas.DrawBitmap(logo, dest, paint);
    }

    // White Anton title, up to 2 lines, bottom-left in the logo box (D62, D74).
    private static void DrawTitle(SKCanvas canvas, SKTypeface typeface, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        using var paint = new SKPaint { Typeface = typeface, IsAntialias = true, Color = SKColors.White };
        var boxW = Layout.BoxRight - Layout.BoxLeft;
        var boxH = Layout.BoxBottom - Layout.BoxTop;
        var fit = TitleLayout.Fit(title!, (size, text) =>
        {
            paint.TextSize = size;
            return paint.MeasureText(text);
        }, boxW, boxH);
        if (fit.Lines.Count == 0)
            return;

        paint.TextSize = fit.Size;
        using var path = new SKPath();
        var spacing = Layout.TitleLineSpacing * fit.Size;
        var lastBaseline = Layout.BoxBottom - (Layout.TitleDescent * fit.Size);
        for (var i = 0; i < fit.Lines.Count; i++)
        {
            var baseline = lastBaseline - ((fit.Lines.Count - 1 - i) * spacing);
            using var line = paint.GetTextPath(fit.Lines[i], Layout.BoxLeft, baseline);
            path.AddPath(line);
        }

        DrawWithShadow(canvas, path);
    }

    // The rank number filled into the D62 band: 78% of the height, left edge at 3.5%, never past 22% of the width.
    private static void DrawNumber(SKCanvas canvas, SKTypeface typeface, int rank)
    {
        using var paint = new SKPaint { Typeface = typeface, IsAntialias = true, TextSize = 1000f };
        using var glyphs = paint.GetTextPath(rank.ToString(System.Globalization.CultureInfo.InvariantCulture), 0, 0);
        var bounds = glyphs.TightBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("rank number has no outline");

        var targetH = Layout.NumberBottom - Layout.NumberTop;
        var sy = targetH / bounds.Height;
        var sx = Math.Min(sy, (Layout.NumberRight - Layout.NumberLeft) / bounds.Width);
        var matrix = SKMatrix.CreateScale(sx, sy);
        matrix = matrix.PostConcat(SKMatrix.CreateTranslation(Layout.NumberLeft - (bounds.Left * sx), Layout.NumberTop - (bounds.Top * sy)));

        using var number = new SKPath(glyphs);
        number.Transform(matrix);
        DrawWithShadow(canvas, number);
    }

    private static void DrawWithShadow(SKCanvas canvas, SKPath path)
    {
        using (var shadow = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black.WithAlpha(Layout.ShadowAlpha),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, Layout.ShadowBlurSigma),
        })
        {
            canvas.Save();
            canvas.Translate(Layout.ShadowOffset, Layout.ShadowOffset);
            canvas.DrawPath(path, shadow);
            canvas.Restore();
        }

        using var fill = new SKPaint { IsAntialias = true, Color = SKColors.White, Style = SKPaintStyle.Fill };
        canvas.DrawPath(path, fill);
    }
}
