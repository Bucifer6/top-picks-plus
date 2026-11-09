using System;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;

namespace TopPicksPlus.Tests.Logic;

/// <summary>C51 (D30, D49, D106), the name rules of C80 (D82) and of D37, and the file names of C70/C93 (D55, D70, D90).</summary>
public class NamingTests
{
    [Theory, Trait("Check", "C51")]
    [InlineData("Alien: Covenant", 2017, "Alien Covenant (2017)")]
    [InlineData("What?*<>|\"/\\", 2001, "What (2001)")]
    [InlineData("Mr. Robot...", null, "Mr. Robot")]
    [InlineData("Trailing   ", null, "Trailing")]
    [InlineData("Plain", null, "Plain")]
    [InlineData("", 1999, "Untitled (1999)")]
    [InlineData("???", null, "Untitled")]
    [InlineData(null, null, "Untitled")]
    [InlineData("CON", null, "CON_")]
    [InlineData("nul", null, "nul_")]
    [InlineData("com3", null, "com3_")]
    [InlineData("LPT9.part", null, "LPT9_.part")]
    [InlineData("COM10", null, "COM10")]
    [InlineData("CONSOLE", null, "CONSOLE")]
    public void C51_folder_names(string? title, int? year, string expected) =>
        Assert.Equal(expected, Naming.FolderName(title, year));

    [Fact, Trait("Check", "C51")]
    public void C51_control_characters_are_removed()
    {
        Assert.Equal("AB (2000)", Naming.FolderName("A\tB\u0001", 2000));
    }

    [Fact, Trait("Check", "C51")]
    public void C51_clashes_ignoring_case_get_2_then_3()
    {
        Assert.Equal("Dune (2021)", Naming.Unique("Dune (2021)", new[] { "Other" }));
        Assert.Equal("Dune (2021) - 2", Naming.Unique("Dune (2021)", new[] { "dune (2021)" }));
        Assert.Equal("Dune (2021) - 3", Naming.Unique("Dune (2021)", new[] { "DUNE (2021)", "Dune (2021) - 2" }));
        Assert.Equal("Dune (2021) - 2", Naming.Unique("Dune (2021)", new[] { "Dune (2021)", "Dune (2021) - 3" }));
    }

    [Fact, Trait("Check", "C51")]
    public void C51_specials_and_seasons()
    {
        Assert.Equal("Specials", Naming.SeasonFolder(0));
        Assert.Equal("Season 1", Naming.SeasonFolder(1));
        Assert.Equal("Season 12", Naming.SeasonFolder(12));
        Assert.Equal("Show - S00E03 - Pilot", Naming.EpisodeBaseName("Show", 0, 3, null, "Pilot"));
        Assert.Equal("Show - S01E02", Naming.EpisodeBaseName("Show", 1, 2, null, "  "));
        Assert.Equal("Show - S01E02", Naming.EpisodeBaseName("Show", 1, 2, null, null));
        Assert.Equal("Show - S10E100 - Big", Naming.EpisodeBaseName("Show", 10, 100, null, "Big"));
    }

    [Fact, Trait("Check", "C51")]
    public void C51_double_episode()
    {
        Assert.Equal("S01E02-E03", Naming.EpisodeCode(1, 2, 3));
        Assert.Equal("S01E02", Naming.EpisodeCode(1, 2, 2));
        Assert.Equal("S01E02", Naming.EpisodeCode(1, 2, null));
        Assert.Equal("Show - S02E09-E10 - Two Parter", Naming.EpisodeBaseName("Show", 2, 9, 10, "Two Parter"));
    }

    [Fact, Trait("Check", "C51")]
    public void C51_episode_titles_are_cleaned_like_folder_names()
    {
        Assert.Equal("Show - S01E01 - Who Are You", Naming.EpisodeBaseName("Show", 1, 1, null, "Who: Are You?..."));
    }

    [Fact, Trait("Check", "C51")]
    public void C51_long_names_cut_the_episode_title_first_then_the_series_never_the_code()
    {
        var series = new string('S', 60);
        var title = new string('T', 200);
        var cut = Naming.EpisodeBaseName(series, 1, 2, null, title, 100);
        Assert.Equal(100, cut.Length);
        Assert.StartsWith(series + " - S01E02 - T", cut);

        var noTitle = Naming.EpisodeBaseName(series, 1, 2, null, title, 70);
        Assert.Equal(series + " - S01E02", noTitle);

        var shortSeries = Naming.EpisodeBaseName(series, 1, 2, null, title, 30);
        Assert.True(shortSeries.Length <= 30, shortSeries);
        Assert.EndsWith(" - S01E02", shortSeries);
        Assert.StartsWith("SSS", shortSeries);
    }

    [Theory, Trait("Check", "C51")]
    [InlineData(83)]   // the real top-picks root: C:\Users\Admin\AppData\Roaming\Emby-Server\programdata\data\top-picks
    [InlineData(95)]   // staging is deeper: ...\programdata\plugins\TopPicksPlus\staging (D106 measures the longer root)
    [InlineData(150)]
    public void C51_no_path_is_over_240_characters(int rootLength)
    {
        var maxFolder = Naming.MaxFolderLengthFor(rootLength);
        var folder = Naming.FolderName(new string('F', 300), 2020, maxFolder);
        Assert.True(folder.Length <= maxFolder);
        Assert.EndsWith(" (2020)", folder);
        Assert.True(Naming.Fits(rootLength, folder + "\\" + folder + ".en.forced.srt"), "movie subtitle path too long");

        var season = Naming.SeasonFolder(1);
        var budget = Naming.EpisodeNameBudget(rootLength, folder, season);
        var ep = Naming.EpisodeBaseName(new string('S', 200), 1, 1, 2, new string('T', 200), budget);
        Assert.True(Naming.Fits(rootLength, folder + "\\" + season + "\\" + Naming.EpisodeThumbName(ep)), "thumb path too long");
        Assert.True(Naming.Fits(rootLength, folder + "\\" + season + "\\" + ep + ".en.forced.srt"), "episode subtitle path too long");
    }

    [Fact, Trait("Check", "C51")]
    public void C51_folder_names_are_capped_at_100()
    {
        Assert.Equal(100, Naming.MaxFolderLengthFor(0));
        Assert.True(Naming.FolderName(new string('x', 500), null).Length <= Naming.MaxFolderLength);
        Assert.False(Naming.Fits(10, new string('a', 230)));
        Assert.True(Naming.Fits(10, new string('a', 229)));
    }

    [Theory, Trait("Check", "C80")]
    [InlineData("")]
    [InlineData(new object?[] { null })]
    [InlineData("C:\\Windows\\x.dll")]
    [InlineData("c:x.txt")]
    [InlineData("\\top.txt")]
    [InlineData("\\\\server\\share\\x")]
    [InlineData("/etc/passwd")]
    [InlineData("..")]
    [InlineData("..\\other\\x.strm")]
    [InlineData("Season 1\\..\\..\\x")]
    [InlineData(".\\x")]
    [InlineData("Season 1\\\\x.strm")]
    [InlineData("a/b.strm")]
    [InlineData("bad|name.nfo")]
    [InlineData("bad?.nfo")]
    [InlineData("trailing.")]
    [InlineData("Season 1 \\x.nfo")]
    [InlineData("CON")]
    [InlineData("Season 1\\aux.srt")]
    [InlineData("Season 1\\")]
    public void C80_bad_paths_in_items_or_files_are_refused(string? path) =>
        Assert.NotNull(PathRules.RelativePathProblem(path));

    [Theory, Trait("Check", "C80")]
    [InlineData("Film (2020).strm")]
    [InlineData("Season 1\\Show - S01E01 - Pilot.strm")]
    [InlineData("Specials\\folder.jpg")]
    [InlineData("backdrops\\theme.mkv")]
    [InlineData("Film (2020).en.forced.srt")]
    [InlineData("..hidden")]
    [InlineData("CONSOLE.txt")]
    public void C80_plain_relative_paths_pass(string path) =>
        Assert.Null(PathRules.RelativePathProblem(path));

    [Theory, Trait("Check", "C45")]
    [InlineData("")]
    [InlineData(new object?[] { null })]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("C:\\")]
    [InlineData("top-picks\\..")]
    [InlineData("name.")]
    [InlineData("name ")]
    [InlineData("PRN")]
    [InlineData("bad*")]
    public void D37_folder_names_that_are_never_deleted(string? folder) =>
        Assert.NotNull(PathRules.FolderNameProblem(folder));

    [Theory, Trait("Check", "C45")]
    [InlineData("Dune (2021)")]
    [InlineData("Dune (2021) - 2")]
    [InlineData("The Walking Dead (2010)")]
    public void D37_plain_folder_names_pass(string folder) =>
        Assert.Null(PathRules.FolderNameProblem(folder));

    [Fact, Trait("Check", "C70")]
    public void C70_subtitles_keep_their_full_suffix_and_idx_goes_with_sub()
    {
        Assert.Equal("Copy (2020).en.forced.srt", Naming.SubtitleCopyName("Film.mkv", "Film.en.forced.srt", "Copy (2020)"));
        Assert.Equal("Copy.srt", Naming.SubtitleCopyName("Film.mkv", "Film.srt", "Copy"));
        Assert.Equal("Copy.en.idx", Naming.SubtitleCopyName("Film.mkv", "Film.en.idx", "Copy"));
        Assert.Equal("Copy.en.sub", Naming.SubtitleCopyName("Film.mkv", "Film.en.sub", "Copy"));
        Assert.Null(Naming.SubtitleCopyName("Film.mkv", "Film.nfo", "Copy"));
        Assert.Null(Naming.SubtitleCopyName("Film.mkv", "Other.srt", "Copy"));
        Assert.Null(Naming.SubtitleCopyName("Film.mkv", "Filmography.srt", "Copy"));
        Assert.True(Naming.IsSubtitle("x.VTT"));
        Assert.True(Naming.IsSubtitle("x.ass"));
        Assert.True(Naming.IsSubtitle("x.ssa"));
        Assert.False(Naming.IsSubtitle("x.txt"));
    }

    [Fact, Trait("Check", "C70")]
    public void C70_episode_thumbs_and_season_posters()
    {
        Assert.Equal("Show - S01E01 - Pilot-thumb.jpg", Naming.EpisodeThumbName("Show - S01E01 - Pilot"));
        Assert.Equal("Season 2\\folder.jpg", Naming.SeasonPosterPath(2));
        // B16 / D109: season 0's poster goes in Specials.
        Assert.Equal("Specials\\folder.jpg", Naming.SeasonPosterPath(0));
    }

    [Fact, Trait("Check", "C93")]
    public void C93_themes_keep_their_own_extension()
    {
        Assert.Equal("backdrops\\theme.mkv", Naming.ThemeVideoPath(".mkv"));
        Assert.Equal("backdrops\\theme.mp4", Naming.ThemeVideoPath("mp4"));
        Assert.Equal("theme.flac", Naming.ThemeSongPath(".flac"));
        Assert.Equal("theme.mp3", Naming.ThemeSongPath("mp3"));
        Assert.Equal("theme", Naming.ThemeSongPath(""));
    }

    [Fact, Trait("Check", "C51")]
    public void C51_forbidden_list_is_written_out_not_read_from_the_machine()
    {
        foreach (var c in "<>:\"/\\|?*")
            Assert.True(Naming.IsForbiddenChar(c), c.ToString());
        Assert.All(Enumerable.Range(0, 32), i => Assert.True(Naming.IsForbiddenChar((char)i)));
        Assert.False(Naming.IsForbiddenChar('-'));
        Assert.False(Naming.IsForbiddenChar(' '));
        Assert.False(Naming.IsForbiddenChar('é'));
    }
}
