using System;
using System.Collections.Generic;
using System.Linq;

namespace TopPicksPlus.Artwork;

/// <summary>
/// Fits the no-logo title into the logo box (D62, D74): the largest size from
/// <see cref="Layout.TitleMaxSize"/> down that fits in at most 2 lines; at
/// <see cref="Layout.TitleMinSize"/> whatever doesn't fit is cut and ends in "…".
/// Pure: the caller passes a function that measures a string's width at a given size.
/// </summary>
internal static class TitleLayout
{
    public const string Ellipsis = "…";

    public sealed class Result
    {
        public Result(float size, IReadOnlyList<string> lines)
        {
            Size = size;
            Lines = lines;
        }

        public float Size { get; }
        public IReadOnlyList<string> Lines { get; }
    }

    public static Result Fit(string title, Func<float, string, float> measure, float boxWidth, float boxHeight)
    {
        var words = (title ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return new Result(Layout.TitleMaxSize, Array.Empty<string>());

        for (var size = Layout.TitleMaxSize; size >= Layout.TitleMinSize; size -= Layout.TitleSizeStep)
        {
            var lines = Wrap(words, s => measure(size, s) <= boxWidth, allowOverlong: false);
            if (lines != null && lines.Count <= Layout.TitleMaxLines && BlockHeight(lines.Count, size) <= boxHeight)
                return new Result(size, lines);
        }

        var min = Layout.TitleMinSize;
        Func<string, bool> fits = s => measure(min, s) <= boxWidth;
        var all = Wrap(words, fits, allowOverlong: true)!;
        var kept = all.Take(Layout.TitleMaxLines).ToList();
        var cut = all.Count > kept.Count;
        for (var i = 0; i < kept.Count; i++)
        {
            var last = i == kept.Count - 1;
            if (!fits(kept[i]) || (last && cut))
                kept[i] = Shorten(kept[i], fits);
        }

        return new Result(min, kept);
    }

    /// <summary>Height of a block of lines, from the top of the tallest glyph to the bottom of the deepest.</summary>
    public static float BlockHeight(int lines, float size) =>
        lines <= 0 ? 0 : ((Layout.TitleAscent + Layout.TitleDescent) * size) + ((lines - 1) * Layout.TitleLineSpacing * size);

    // Greedy wrap. Returns null if a single word is too wide and overlong words aren't allowed.
    private static List<string>? Wrap(string[] words, Func<string, bool> fits, bool allowOverlong)
    {
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (fits(candidate))
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
                lines.Add(current);
            if (!fits(word) && !allowOverlong)
                return null;
            current = word;
        }

        if (current.Length > 0)
            lines.Add(current);
        return lines;
    }

    // Cuts characters from the end until the line plus "…" fits.
    private static string Shorten(string line, Func<string, bool> fits)
    {
        var text = line;
        while (text.Length > 0 && !fits(text.TrimEnd() + Ellipsis))
            text = text.Substring(0, text.Length - 1);
        return text.TrimEnd() + Ellipsis;
    }
}
