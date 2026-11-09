using System;
using System.IO;
using SkiaSharp;
using TopPicksPlus.Artwork;
using Xunit;
using static TopPicksPlus.Tests.Artwork.TestImages;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>
/// C65, D87(d): writes the sample pictures the owner looks at before chunk 3 is kept. CI sets
/// ARTWORK_SAMPLES_DIR and keeps the folder as the artifact artwork-samples-&lt;version&gt;.
/// Backdrops and logos are made up here (no film images in the repo); titles mirror docs/reference/artwork/.
/// </summary>
public class SampleWriter
{
    private static readonly SKTypeface Anton = TestImages.Anton();

    [Fact]
    public void Writes_the_C65_sample_pictures()
    {
        var folder = Environment.GetEnvironmentVariable("ARTWORK_SAMPLES_DIR");
        if (string.IsNullOrWhiteSpace(folder))
            folder = Path.Combine(AppContext.BaseDirectory, "artwork-samples");
        Directory.CreateDirectory(folder);

        // Rank 1 with a logo, like "Adulthood" (yellow title logo over a green woodland still).
        Write(folder, "rank01-with-logo-Adulthood.png", 1,
            Backdrop(new SKColor(70, 95, 55), new SKColor(35, 40, 30), 1),
            TextLogo(Anton, "ADULTHOOD", new SKColor(245, 215, 40)), null, "Adulthood");

        // Rank 2 without a logo, like "Beast": the title is written in white Anton capitals (D100).
        Write(folder, "rank02-no-logo-Beast.png", 2,
            Backdrop(new SKColor(60, 55, 50), new SKColor(20, 18, 16), 2), null, null, "Beast");

        // Rank 10: two digits squeezed into the same 22% band; a two-line title with no logo.
        Write(folder, "rank10-no-logo-By-Any-Means.png", 10,
            Backdrop(new SKColor(110, 70, 40), new SKColor(30, 20, 15), 10), null, null, "By Any Means");

        // Extra (not part of C65): no backdrop, so the poster is centred on black (D74).
        Write(folder, "extra-rank05-no-backdrop-poster-Verity.png", 5, null, null,
            PosterLike(), "Verity");
    }

    private static void Write(string folder, string name, int rank, byte[]? backdrop, byte[]? logo, byte[]? poster, string title)
    {
        var result = RankArtwork.Draw(backdrop, logo, poster, rank, title);
        Assert.False(result.Failed, $"{name}: {result.FailureReason}");
        using (var picture = Decode(result.Bytes!))
            Assert.Null(LayoutChecks.NumberProblem(picture, rank, Anton));
        File.WriteAllBytes(Path.Combine(folder, name), result.Bytes!);
    }

    private static byte[] PosterLike()
    {
        using var bitmap = new SKBitmap(680, 1000);
        using (var canvas = new SKCanvas(bitmap))
        {
            using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 1000),
                       new[] { new SKColor(90, 10, 20), new SKColor(15, 5, 8) }, SKShaderTileMode.Clamp))
            using (var paint = new SKPaint { Shader = shader })
                canvas.DrawRect(0, 0, 680, 1000, paint);
            using var text = new SKPaint { Typeface = Anton, TextSize = 150, IsAntialias = true, Color = SKColors.White };
            canvas.DrawText("VERITY", 120, 880, text);
        }

        return Png(bitmap);
    }
}
