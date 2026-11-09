using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TopPicksPlus.Logic;

/// <summary>
/// Folder and file names inside the Top Picks folder (spec F3, D30, D49, D70, D90, D106, D109). Windows rules are written out here,
/// not read from <c>System.IO.Path</c> (blocked in Logic, and almost empty on Linux). Every name comparison ignores case.
/// </summary>
public static class Naming
{
    /// <summary>No full path to a copy file may be longer than this (D49, D106).</summary>
    public const int MaxPathLength = 240;

    /// <summary>Room kept at the end of every file's base name for the longest suffix the plugin adds (".en.forced.srt", "-thumb.jpg", D106).</summary>
    public const int SuffixAllowance = 20;

    /// <summary>A folder name is never longer than this, whatever the root (D106).</summary>
    public const int MaxFolderLength = 100;

    /// <summary>Used when a title has nothing left after cleaning (D106).</summary>
    public const string EmptyTitle = "Untitled";

    private static readonly char[] Forbidden = BuildForbidden();

    private static readonly HashSet<string> Reserved = new HashSet<string>(
        new[] { "CON", "PRN", "AUX", "NUL" }
            .Concat(Enumerable.Range(1, 9).Select(i => "COM" + i.ToString(CultureInfo.InvariantCulture)))
            .Concat(Enumerable.Range(1, 9).Select(i => "LPT" + i.ToString(CultureInfo.InvariantCulture))),
        StringComparer.OrdinalIgnoreCase);

    private static char[] BuildForbidden()
    {
        var list = new List<char> { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        for (var c = 0; c < 32; c++)
            list.Add((char)c);
        return list.ToArray();
    }

    /// <summary>True if Windows forbids this character in a file or folder name.</summary>
    public static bool IsForbiddenChar(char c) => Array.IndexOf(Forbidden, c) >= 0;

    /// <summary>True if the name (or the part before its first dot) is a Windows device name such as CON or COM1.</summary>
    public static bool IsReservedName(string name)
    {
        var dot = name.IndexOf('.');
        var stem = dot >= 0 ? name.Substring(0, dot) : name;
        return Reserved.Contains(stem.TrimEnd(' '));
    }

    /// <summary>Removes forbidden characters, then trailing dots and spaces. May return an empty string.</summary>
    public static string Clean(string? text)
    {
        var source = text ?? string.Empty;
        var sb = new StringBuilder(source.Length);
        foreach (var c in source)
        {
            if (!IsForbiddenChar(c))
                sb.Append(c);
        }
        return sb.ToString().TrimEnd('.', ' ');
    }

    /// <summary>Adds "_" to a reserved device name (D30): "CON" becomes "CON_", "nul.txt" becomes "nul_.txt".</summary>
    public static string AvoidReserved(string name)
    {
        if (!IsReservedName(name))
            return name;
        var dot = name.IndexOf('.');
        return dot >= 0 ? name.Substring(0, dot) + "_" + name.Substring(dot) : name + "_";
    }

    /// <summary>
    /// The longest folder name that still lets a movie's <c>&lt;folder&gt;\&lt;folder&gt;.strm</c> (plus suffix room) fit in
    /// <see cref="MaxPathLength"/> under a root of this length, capped at <see cref="MaxFolderLength"/> (D106).
    /// </summary>
    public static int MaxFolderLengthFor(int rootLength)
    {
        var room = (MaxPathLength - rootLength - 2 - SuffixAllowance) / 2;
        return Math.Max(1, Math.Min(MaxFolderLength, room));
    }

    /// <summary>
    /// A new copy's folder name (F3): <c>Title (Year)</c>, or <c>Title</c> with no year; cleaned; an empty title becomes
    /// <see cref="EmptyTitle"/>; too long a title is cut so the name fits <paramref name="maxLength"/>; reserved names get "_".
    /// </summary>
    public static string FolderName(string? title, int? year, int maxLength = MaxFolderLength)
    {
        var yearPart = year.HasValue ? " (" + year.Value.ToString(CultureInfo.InvariantCulture) + ")" : string.Empty;
        var clean = Clean(title);
        if (clean.Length == 0)
            clean = EmptyTitle;
        var room = Math.Max(1, maxLength - yearPart.Length);
        if (clean.Length > room)
        {
            clean = clean.Substring(0, room).TrimEnd('.', ' ');
            if (clean.Length == 0)
                clean = EmptyTitle.Substring(0, Math.Min(EmptyTitle.Length, room));
        }
        return AvoidReserved(Clean(clean + yearPart));
    }

    /// <summary>
    /// The name itself if no folder in <paramref name="taken"/> has it (ignoring case), else the first free
    /// <c>name - 2</c>, <c>name - 3</c>, ... (F3, D30).
    /// </summary>
    public static string Unique(string name, IEnumerable<string> taken)
    {
        var set = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(name))
            return name;
        for (var n = 2; ; n++)
        {
            var candidate = name + " - " + n.ToString(CultureInfo.InvariantCulture);
            if (!set.Contains(candidate))
                return candidate;
        }
    }

    /// <summary>A season's folder: <c>Season N</c>, or <c>Specials</c> for season 0 (D49).</summary>
    public static string SeasonFolder(int season) =>
        season == 0 ? "Specials" : "Season " + season.ToString(CultureInfo.InvariantCulture);

    /// <summary>A season's poster, relative to the copy folder: <c>Season N\folder.jpg</c> or <c>Specials\folder.jpg</c> (D70, D109).</summary>
    public static string SeasonPosterPath(int season) => SeasonFolder(season) + "\\folder.jpg";

    /// <summary>The episode code: <c>S01E02</c>, or <c>S01E02-E03</c> for a multi-episode file (D49).</summary>
    public static string EpisodeCode(int season, int episode, int? lastEpisode)
    {
        var code = "S" + Two(season) + "E" + Two(episode);
        if (lastEpisode.HasValue && lastEpisode.Value > episode)
            code += "-E" + Two(lastEpisode.Value);
        return code;
    }

    private static string Two(int n) => n.ToString("00", CultureInfo.InvariantCulture);

    /// <summary>
    /// An episode's file name without extension (D49): <c>Series - SxxEyy - Episode title</c>, or <c>Series - SxxEyy</c> when the
    /// title is empty after cleaning. If longer than <paramref name="maxLength"/>, the episode title is shortened first (and dropped
    /// if need be), then the series part (to one character at least). The episode code is never cut.
    /// </summary>
    public static string EpisodeBaseName(string? series, int season, int episode, int? lastEpisode, string? title, int maxLength = int.MaxValue)
    {
        var code = EpisodeCode(season, episode, lastEpisode);
        var seriesPart = Clean(series);
        if (seriesPart.Length == 0)
            seriesPart = EmptyTitle;
        var titlePart = Clean(title);

        string Build(string s, string t) => t.Length == 0 ? s + " - " + code : s + " - " + code + " - " + t;

        var name = Build(seriesPart, titlePart);
        if (name.Length <= maxLength)
            return AvoidReserved(name);

        var withoutTitle = Build(seriesPart, string.Empty);
        var titleRoom = maxLength - withoutTitle.Length - 3;
        if (titleRoom > 0)
        {
            var cut = titlePart.Substring(0, Math.Min(titlePart.Length, titleRoom)).TrimEnd('.', ' ');
            if (cut.Length > 0)
                return AvoidReserved(Build(seriesPart, cut));
        }
        if (withoutTitle.Length <= maxLength)
            return AvoidReserved(withoutTitle);

        var seriesRoom = Math.Max(1, maxLength - (" - " + code).Length);
        var shortSeries = seriesPart.Substring(0, Math.Min(seriesPart.Length, seriesRoom)).TrimEnd('.', ' ');
        if (shortSeries.Length == 0)
            shortSeries = seriesPart.Substring(0, 1);
        return AvoidReserved(Build(shortSeries, string.Empty));
    }

    /// <summary>
    /// How many characters an episode's base name may have so that
    /// <c>root\folder\season\base + suffix</c> stays within <see cref="MaxPathLength"/> (D106).
    /// </summary>
    public static int EpisodeNameBudget(int rootLength, string folder, string seasonFolder) =>
        MaxPathLength - rootLength - 1 - folder.Length - 1 - seasonFolder.Length - 1 - SuffixAllowance;

    /// <summary>True if <c>root\relativePath</c> is no longer than <see cref="MaxPathLength"/>.</summary>
    public static bool Fits(int rootLength, string relativePath) => rootLength + 1 + relativePath.Length <= MaxPathLength;

    /// <summary>An episode thumb's file name: <c>&lt;episode file name&gt;-thumb.jpg</c> (D70).</summary>
    public static string EpisodeThumbName(string episodeBaseName) => episodeBaseName + "-thumb.jpg";

    /// <summary>A theme video's path in a copy, keeping the source's extension: <c>backdrops\theme.&lt;ext&gt;</c> (D90).</summary>
    public static string ThemeVideoPath(string sourceExtension) => "backdrops\\theme" + Extension(sourceExtension);

    /// <summary>A theme song's path in a copy, keeping the source's extension: <c>theme.&lt;ext&gt;</c> (D55, D90).</summary>
    public static string ThemeSongPath(string sourceExtension) => "theme" + Extension(sourceExtension);

    private static string Extension(string ext)
    {
        var e = Clean(ext).TrimStart('.');
        return e.Length == 0 ? string.Empty : "." + e;
    }

    private static readonly string[] SubtitleExtensions = { ".srt", ".ass", ".ssa", ".vtt", ".sub", ".idx" };

    /// <summary>True for the subtitle types that are copied (F3; <c>.idx</c> travels with its <c>.sub</c>, D55).</summary>
    public static bool IsSubtitle(string fileName) =>
        SubtitleExtensions.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A subtitle's name in the copy (D55): everything after the source video's name is kept, so <c>Film.en.forced.srt</c>
    /// next to <c>Film.mkv</c> becomes <c>&lt;copy&gt;.en.forced.srt</c>. Null if the file isn't a subtitle of that video, or if
    /// its suffix is longer than the <see cref="SuffixAllowance"/> every base name leaves free (D106, D122, B39): such a subtitle
    /// is not copied, so no path ever passes 240 characters. Cutting the suffix would break its language and forced tags.
    /// </summary>
    public static string? SubtitleCopyName(string sourceVideoFileName, string sourceSubtitleFileName, string copyBaseName)
    {
        if (!IsSubtitle(sourceSubtitleFileName))
            return null;
        var dot = sourceVideoFileName.LastIndexOf('.');
        var videoBase = dot > 0 ? sourceVideoFileName.Substring(0, dot) : sourceVideoFileName;
        if (!sourceSubtitleFileName.StartsWith(videoBase + ".", StringComparison.OrdinalIgnoreCase))
            return null;
        var suffix = sourceSubtitleFileName.Substring(videoBase.Length);
        if (suffix.Length > SuffixAllowance)
            return null;
        return copyBaseName + suffix;
    }
}
