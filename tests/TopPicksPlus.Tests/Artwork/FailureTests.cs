using System;
using System.Linq;
using SkiaSharp;
using TopPicksPlus.Artwork;
using Xunit;
using static TopPicksPlus.Tests.Artwork.TestImages;

namespace TopPicksPlus.Tests.Artwork;

/// <summary>C43 (chunk 3): when drawing can't work, Artwork returns "failed" instead of throwing (D15).</summary>
public class FailureTests
{
    private static readonly byte[] Good = Solid(1280, 720, new SKColor(128, 128, 128));

    public static TheoryData<string, byte[]?> BadImages()
    {
        var png = Good;
        return new TheoryData<string, byte[]?>
        {
            { "null", null },
            { "empty", Array.Empty<byte>() },
            { "random bytes", Enumerable.Range(0, 5000).Select(i => (byte)((i * 73) ^ 0x5A)).ToArray() },
            { "text file", System.Text.Encoding.UTF8.GetBytes("this is not a picture") },
            { "PNG signature only", png.Take(8).ToArray() },
            { "PNG cut in half", png.Take(png.Length / 2).ToArray() },
            { "JPEG header only", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 } },
            { "PNG claiming 60000x60000", HugePngHeader() },
        };
    }

    [Theory]
    [MemberData(nameof(BadImages))]
    public void Bad_backdrop_and_no_poster_returns_failed(string what, byte[]? bad)
    {
        var result = RankArtwork.Draw(bad, Good, null, 1, "Title");
        Assert.True(result.Failed, $"{what}: expected failed");
        Assert.Null(result.Bytes);
        Assert.False(string.IsNullOrEmpty(result.FailureReason));
    }

    [Theory]
    [MemberData(nameof(BadImages))]
    public void Bad_backdrop_and_bad_poster_returns_failed(string what, byte[]? bad)
    {
        var result = RankArtwork.Draw(bad, null, bad, 3, "Title");
        Assert.True(result.Failed, $"{what}: expected failed");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Rank_outside_1_to_10_returns_failed(int rank)
    {
        var result = RankArtwork.Draw(Good, null, null, rank, "Title");
        Assert.True(result.Failed);
        Assert.Contains("rank", result.FailureReason!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u0000\u0001")]
    [InlineData("日本語のタイトル")] // characters Anton doesn't have: drawn as blanks, never a throw
    public void Odd_titles_never_throw(string? title)
    {
        var result = RankArtwork.Draw(Good, null, null, 9, title);
        Assert.False(result.Failed, result.FailureReason);
    }

    [Fact]
    public void Enormous_title_draws_quickly()
    {
        // Review Low 1: a 100,000-character "word" must not hang the run.
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var result = RankArtwork.Draw(Good, null, null, 7, new string('W', 100_000));
        watch.Stop();
        Assert.False(result.Failed, result.FailureReason);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"took {watch.Elapsed.TotalSeconds:F1} s");
    }

    [Fact]
    public void Very_long_title_still_draws()
    {
        var title = string.Join(" ", Enumerable.Repeat("Supercalifragilistic Extraordinarily Lengthy", 20));
        var result = RankArtwork.Draw(Good, null, null, 7, title);
        Assert.False(result.Failed, result.FailureReason);
    }

    [Fact]
    public void Bad_logo_alone_does_not_fail_the_picture()
    {
        var result = RankArtwork.Draw(Good, new byte[] { 9, 9, 9 }, null, 1, "Title");
        Assert.False(result.Failed, result.FailureReason);
    }

    [Fact]
    public void Tiny_images_still_draw()
    {
        var one = Solid(1, 1, SKColors.Red);
        Assert.False(RankArtwork.Draw(one, one, null, 1, null).Failed);
        Assert.False(RankArtwork.Draw(null, null, one, 10, null).Failed);
    }

    // Review finding 9: a real, fully readable PNG just over the 50-million-pixel cap must fail, and one just
    // under it must draw, so the cap (not the decoder) is what refuses it. 7072 x 7072 = 50,013,184 pixels.
    [Fact]
    public void Real_picture_just_over_the_pixel_cap_returns_failed()
    {
        var result = RankArtwork.Draw(GreyPng(7072, 7072), null, null, 1, "Title");
        Assert.True(result.Failed, "a 50.01-million-pixel backdrop was drawn; the D97 cap did not bite");
    }

    [Fact]
    public void Real_picture_just_under_the_pixel_cap_draws()
    {
        var result = RankArtwork.Draw(GreyPng(7000, 7000), null, null, 1, "Title");
        Assert.False(result.Failed, result.FailureReason);
    }

    // A valid grey PNG of any size, compressed to a few tens of KB (every pixel the same).
    private static byte[] GreyPng(int width, int height)
    {
        using var output = new System.IO.MemoryStream();
        output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var header = new byte[13];
        BigEndian(header, 0, (uint)width);
        BigEndian(header, 4, (uint)height);
        header[8] = 8; // bit depth
        header[9] = 0; // greyscale
        Chunk(output, "IHDR", header);

        using var packed = new System.IO.MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(packed, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            var row = new byte[width + 1]; // filter byte 0, then the pixels
            for (var i = 1; i < row.Length; i++)
                row[i] = 128;
            for (var y = 0; y < height; y++)
                zlib.Write(row, 0, row.Length);
        }

        Chunk(output, "IDAT", packed.ToArray());
        Chunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static void Chunk(System.IO.Stream output, string type, byte[] data)
    {
        var body = new byte[4 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(type, 0, 4, body, 0);
        Buffer.BlockCopy(data, 0, body, 4, data.Length);
        var length = new byte[4];
        BigEndian(length, 0, (uint)data.Length);
        var crc = new byte[4];
        BigEndian(crc, 0, Crc32(body, 0, body.Length));
        output.Write(length);
        output.Write(body);
        output.Write(crc);
    }

    private static void BigEndian(byte[] target, int offset, uint value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    // A valid PNG signature and IHDR chunk that claims a picture far too big to decode.
    private static byte[] HugePngHeader()
    {
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0xEA, 0x60, 0x00, 0x00, 0xEA, 0x60, 0x08, 0x02, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        };
        var crc = Crc32(bytes, 12, 17);
        bytes[29] = (byte)(crc >> 24);
        bytes[30] = (byte)(crc >> 16);
        bytes[31] = (byte)(crc >> 8);
        bytes[32] = (byte)crc;
        return bytes;
    }

    private static uint Crc32(byte[] data, int offset, int count)
    {
        var crc = 0xFFFFFFFFu;
        for (var i = offset; i < offset + count; i++)
        {
            crc ^= data[i];
            for (var k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
    }
}
