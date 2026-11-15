using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using TopPicksPlus.Artwork;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;

namespace TopPicksPlus.EmbyLink;

/// <summary>One file of a copy: a relative path and where its content comes from (text, a source file, or bytes).</summary>
public sealed class CopyFile
{
    public CopyFile(string path, long? itemId = null, string? text = null, string? sourceFile = null, byte[]? bytes = null)
    {
        Path = path;
        ItemId = itemId;
        Text = text;
        SourceFile = sourceFile;
        Bytes = bytes;
    }

    /// <summary>Relative to the copy folder, <c>\</c>-separated.</summary>
    public string Path { get; }

    /// <summary>For a .strm: the source movie or episode (it goes in the entry's <c>items</c>, D28).</summary>
    public long? ItemId { get; }

    public string? Text { get; }
    public string? SourceFile { get; }
    public byte[]? Bytes { get; }
}

/// <summary>Everything one copy holds, plus what the report needs (D49 skipped episodes, D98 artwork).</summary>
public sealed class CopyBuild
{
    public CopyBuild(IReadOnlyList<CopyFile> files, int episodesSkipped, bool artworkFailed)
    {
        Files = files;
        EpisodesSkipped = episodesSkipped;
        ArtworkFailed = artworkFailed;
    }

    public IReadOnlyList<CopyFile> Files { get; }
    public int EpisodesSkipped { get; }
    public bool ArtworkFailed { get; }

    /// <summary>What prepare writes, for the managed entry (D124) and the staying plan (D40).</summary>
    public CopyContents Contents() =>
        new CopyContents(
            Files.Where(f => f.ItemId.HasValue).ToDictionary(f => f.Path, f => f.ItemId!.Value, StringComparer.OrdinalIgnoreCase),
            Files.Where(f => !f.ItemId.HasValue).Select(f => f.Path));

    /// <summary>Writes the whole copy into staging under <paramref name="stagingName"/> (Storage does the writing, D43).</summary>
    public void WriteTo(CopyFolders copies, string stagingName)
    {
        foreach (var f in Files)
        {
            if (f.Text != null)
                copies.WriteStagingText(stagingName, f.Path, f.Text);
            else if (f.Bytes != null)
                copies.WriteStagingFile(stagingName, f.Path, f.Bytes);
            else if (f.SourceFile != null)
                copies.CopyIntoStaging(stagingName, f.Path, f.SourceFile);
        }
    }
}

/// <summary>
/// Works out a copy's files from the source title (F3, D30, D49, D55, D70, D90, D98, D106, D109, D110, D112). The same pick gives
/// the same files every time, so a resume or a re-prepare (D124) can work them out again instead of saving them anywhere.
/// </summary>
public static class CopyBuilder
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// The files of one copy. <paramref name="folder"/> is the copy's folder name (the movie's .strm and .nfo are named after it);
    /// themes are only included for a new copy (D110); <paramref name="stagingRootLength"/> limits paths (D106).
    /// </summary>
    public static CopyBuild Build(Host h, long sourceId, string folder, int rank, DateTime rankDateUtc, bool isNew, int stagingRootLength)
    {
        var item = h.Library.GetItemById(sourceId) ?? throw new InvalidOperationException("source title " + sourceId.ToString(CultureInfo.InvariantCulture) + " no longer exists");
        var dateAdded = EmbyFacts.DateAddedText(rankDateUtc);
        var files = new List<CopyFile>();
        var skipped = 0;

        if (item is Movie movie)
        {
            files.Add(new CopyFile(folder + ".strm", movie.InternalId, CopyText.Strm(movie.Path)));
            files.Add(new CopyFile(folder + ".nfo", text: CopyText.MovieNfo(Meta(movie), dateAdded)));
            files.AddRange(Subtitles(movie.Path, folder, string.Empty));
        }
        else if (item is Series series)
        {
            files.Add(new CopyFile("tvshow.nfo", text: CopyText.TvShowNfo(Meta(series), dateAdded)));
            skipped = AddEpisodes(h, series, folder, dateAdded, stagingRootLength, files);
        }
        else
        {
            throw new InvalidOperationException("source title " + sourceId.ToString(CultureInfo.InvariantCulture) + " is not a movie or series");
        }

        AddImage(files, "fanart.jpg", ImagePath(item, ImageType.Backdrop));
        AddImage(files, "logo.png", ImagePath(item, ImageType.Logo));
        var artworkFailed = AddArtwork(files, item, rank);
        if (isNew)
            files.AddRange(Themes(item));

        // A path may appear only once; the first wins (two episode versions can share a name).
        var unique = files.GroupBy(f => f.Path, Names).Select(g => g.First()).ToList();
        return new CopyBuild(unique, skipped, artworkFailed);
    }

    private static int AddEpisodes(Host h, Series series, string folder, string dateAdded, int stagingRootLength, List<CopyFile> files)
    {
        var skipped = 0;
        var seasons = new Dictionary<int, BaseItem?>();
        var episodes = Catalog.Episodes(h, series.InternalId)
            .Where(Catalog.FileFound)
            .OrderBy(e => e.ParentIndexNumber ?? int.MaxValue)
            .ThenBy(e => e.IndexNumber ?? int.MaxValue)
            .ThenBy(e => e.InternalId);
        foreach (var ep in episodes)
        {
            // D49: an episode with no season or episode number is left out and counted.
            if (!ep.ParentIndexNumber.HasValue || !ep.IndexNumber.HasValue)
            {
                skipped++;
                continue;
            }
            var season = ep.ParentIndexNumber.Value;
            var seasonFolder = Naming.SeasonFolder(season);
            var budget = Naming.EpisodeNameBudget(stagingRootLength, folder, seasonFolder);
            var baseName = Naming.EpisodeBaseName(series.Name, season, ep.IndexNumber.Value, ep.IndexNumberEnd, ep.Name, budget);
            var rel = seasonFolder + "\\" + baseName;
            files.Add(new CopyFile(rel + ".strm", ep.InternalId, CopyText.Strm(ep.Path)));
            files.Add(new CopyFile(rel + ".nfo", text: CopyText.EpisodeNfo(EpisodeMeta(ep, season, series), dateAdded)));
            files.AddRange(Subtitles(ep.Path, baseName, seasonFolder + "\\"));
            AddImage(files, seasonFolder + "\\" + Naming.EpisodeThumbName(baseName), ImagePath(ep, ImageType.Primary));
            if (!seasons.ContainsKey(season))
                seasons[season] = ep.Season;
        }
        foreach (var kv in seasons)
            AddImage(files, Naming.SeasonPosterPath(kv.Key), kv.Value == null ? null : ImagePath(kv.Value, ImageType.Primary));
        return skipped;
    }

    private static TitleMeta Meta(BaseItem item) =>
        new TitleMeta(
            item.Name,
            item.OriginalTitle,
            item.ProductionYear,
            item.Overview,
            item.OfficialRating,
            item.Genres ?? Array.Empty<string>(),
            item.PremiereDate?.UtcDateTime,
            Minutes(item.RunTimeTicks),
            item.ProviderIds == null ? null : new Dictionary<string, string>(item.ProviderIds));

    private static EpisodeMeta EpisodeMeta(Episode ep, int season, Series series) =>
        new EpisodeMeta(ep.Name, season, ep.IndexNumber ?? 0, ep.Overview, ep.PremiereDate?.UtcDateTime, Minutes(ep.RunTimeTicks),
            ep.OfficialRating ?? series.OfficialRating);

    private static int? Minutes(long? ticks) =>
        ticks.HasValue && ticks.Value > 0 ? (int)Math.Round(TimeSpan.FromTicks(ticks.Value).TotalMinutes) : (int?)null;

    /// <summary>The source's subtitles next to its video, renamed after the copy file (D55, D122).</summary>
    private static IEnumerable<CopyFile> Subtitles(string videoPath, string copyBaseName, string prefix)
    {
        var dir = SafeDirectory(videoPath);
        if (dir == null)
            yield break;
        var videoName = Path.GetFileName(videoPath);
        foreach (var file in SafeFiles(dir))
        {
            var name = Naming.SubtitleCopyName(videoName, Path.GetFileName(file), copyBaseName);
            if (name != null)
                yield return new CopyFile(prefix + name, sourceFile: file);
        }
    }

    /// <summary>Theme video (<c>backdrops\theme.*</c>) and theme song (<c>theme.*</c>) from the source's folder (D55, D90).</summary>
    private static IEnumerable<CopyFile> Themes(BaseItem item)
    {
        var dir = item is Series ? item.Path : SafeDirectory(item.Path);
        if (string.IsNullOrEmpty(dir))
            yield break;
        var video = SafeFiles(Path.Combine(dir, "backdrops")).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("theme", StringComparison.OrdinalIgnoreCase));
        if (video != null)
            yield return new CopyFile(Naming.ThemeVideoPath(Path.GetExtension(video)), sourceFile: video);
        var song = SafeFiles(dir!).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("theme", StringComparison.OrdinalIgnoreCase));
        if (song != null)
            yield return new CopyFile(Naming.ThemeSongPath(Path.GetExtension(song)), sourceFile: song);
    }

    /// <summary>
    /// folder.png and thumb.png (D62, D98, D112): the drawn picture, or if drawing fails the backdrop's or poster's own bytes when
    /// Artwork can read them, or nothing. Returns whether drawing failed (<c>artworkFailed</c>).
    /// </summary>
    private static bool AddArtwork(List<CopyFile> files, BaseItem item, int rank)
    {
        var backdrop = ReadImage(ImagePath(item, ImageType.Backdrop));
        var logo = ReadImage(ImagePath(item, ImageType.Logo));
        var poster = ReadImage(ImagePath(item, ImageType.Primary));
        var drawn = RankArtwork.Draw(backdrop, logo, poster, rank, item.Name);
        var choice = EmbyRules.ArtworkFallback(!drawn.Failed, RankArtwork.CanRead(backdrop), RankArtwork.CanRead(poster));
        var bytes = choice switch
        {
            ArtworkChoice.Drawn => drawn.Bytes,
            ArtworkChoice.BackdropBytes => backdrop,
            ArtworkChoice.PosterBytes => poster,
            _ => null,
        };
        if (bytes != null)
        {
            files.Add(new CopyFile("folder.png", bytes: bytes));
            files.Add(new CopyFile("thumb.png", bytes: bytes));
        }
        return EmbyRules.ArtworkFailed(choice);
    }

    private static void AddImage(List<CopyFile> files, string path, string? sourceFile)
    {
        if (sourceFile != null)
            files.Add(new CopyFile(path, sourceFile: sourceFile));
    }

    /// <summary>A local image file of the item, or null (a remote image or a missing file is skipped).</summary>
    private static string? ImagePath(BaseItem item, ImageType type)
    {
        if (!item.HasImage(type, 0))
            return null;
        var path = item.GetImagePath(type, 0);
        return !string.IsNullOrEmpty(path) && !path.Contains("://") && SafeFileExists(path) ? path : null;
    }

    private static byte[]? ReadImage(string? path)
    {
        if (path == null)
            return null;
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
        {
            return null;
        }
    }

    private static bool SafeFileExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch (Exception e) when (e is ArgumentException || e is NotSupportedException)
        {
            return false;
        }
    }

    private static string? SafeDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        try
        {
            return Path.GetDirectoryName(path);
        }
        catch (Exception e) when (e is ArgumentException || e is PathTooLongException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> SafeFiles(string dir)
    {
        try
        {
            return Directory.Exists(dir) ? Directory.GetFiles(dir) : Array.Empty<string>();
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {
            return Array.Empty<string>();
        }
    }
}
