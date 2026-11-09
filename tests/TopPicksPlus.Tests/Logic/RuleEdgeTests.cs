using System;
using System.Collections.Generic;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;
using static TopPicksPlus.Tests.Logic.Make;

namespace TopPicksPlus.Tests.Logic;

/// <summary>Edge cases added after the first Stryker run (PathRules 63%, Takeover 76%): each pins down the exact reason or field.</summary>
public class RuleEdgeTests
{
    [Theory, Trait("Check", "C80")]
    [InlineData("", "empty path")]
    [InlineData("\\x", "rooted path")]
    [InlineData("/x", "rooted path")]
    [InlineData("C:x", "path with a drive letter")]
    [InlineData("a\\\\b", "empty part in path")]
    [InlineData("a\\..\\b", "path part is . or ..")]
    [InlineData("a\\.", "path part is . or ..")]
    [InlineData("a/b", "path part holds a path separator")]
    [InlineData("a\\b?", "path part holds a forbidden character")]
    [InlineData("a\\b.", "path part ends in a dot or space")]
    [InlineData("a\\b ", "path part ends in a dot or space")]
    [InlineData("a\\com1.txt", "path part is a Windows device name")]
    public void C80_each_bad_path_gets_its_own_reason(string path, string reason) =>
        Assert.Equal(reason, PathRules.RelativePathProblem(path));

    [Theory, Trait("Check", "C45")]
    [InlineData("", "empty folder name")]
    [InlineData(".", "folder name is . or ..")]
    [InlineData("..", "folder name is . or ..")]
    [InlineData("a\\b", "folder name holds a path separator")]
    [InlineData("a/b", "folder name holds a path separator")]
    [InlineData("a:b", "folder name holds a forbidden character")]
    [InlineData("x.", "folder name ends in a dot or space")]
    [InlineData("AUX", "folder name is a Windows device name")]
    public void D37_each_bad_folder_name_gets_its_own_reason(string folder, string reason) =>
        Assert.Equal(reason, PathRules.FolderNameProblem(folder));

    [Fact, Trait("Check", "C80")]
    public void C80_single_character_names_are_judged_on_their_own()
    {
        Assert.Null(PathRules.RelativePathProblem("a"));
        Assert.Null(PathRules.RelativePathProblem("a\\b"));
        Assert.Null(PathRules.FolderNameProblem("a"));
        Assert.Equal("path part ends in a dot or space", PathRules.RelativePathProblem(" "));
    }

    [Theory, Trait("Check", "C51")]
    [InlineData("CON", true)]
    [InlineData("con.txt", true)]
    [InlineData("Con .txt", true)]
    [InlineData("LPT1", true)]
    [InlineData("COM9", true)]
    [InlineData("COM0", false)]
    [InlineData("LPT10", false)]
    [InlineData("CONX", false)]
    [InlineData("XCON", false)]
    [InlineData(".CON", false)]
    public void C51_reserved_names(string name, bool reserved) =>
        Assert.Equal(reserved, Naming.IsReservedName(name));

    private static FolderListing Folder(string name, params string[] files) => new FolderListing(name, files);

    [Fact, Trait("Check", "C46")]
    public void C46_takeover_needs_both_a_strm_and_an_nfo_and_matches_extensions_ignoring_case()
    {
        Assert.True(Takeover.IsOldPluginFolder(Folder("A", "x.STRM", "Season 1\\y.NFO")));
        Assert.False(Takeover.IsOldPluginFolder(Folder("A", "x.strm", "x.strm.txt")));
        Assert.False(Takeover.IsOldPluginFolder(Folder("A", "x.nfo", "x.nfo.bak")));
        Assert.False(Takeover.IsOldPluginFolder(Folder("A")));
    }

    [Fact, Trait("Check", "C46")]
    public void C46_adopt_takes_the_first_matched_strm_in_name_order_and_lists_every_strm()
    {
        var folder = Folder("Show", "Season 2\\b.strm", "Season 1\\a.strm", "Season 1\\c.strm", "tvshow.nfo");
        var sources = new Dictionary<string, long?> { { "Season 1\\a.strm", null }, { "Season 1\\c.strm", 31 }, { "Season 2\\b.strm", 21 } };
        var seriesOf = new Dictionary<long, long> { { 31, 300 }, { 21, 200 } };
        var e = Takeover.Adopt(folder, sources, seriesOf);
        Assert.Equal(300, e.SourceId);
        Assert.Equal(TitleKind.Series, e.Kind);
        Assert.Equal(3, e.Items.Count);
        Assert.Null(e.Items["Season 1\\a.strm"]);
        Assert.Equal(21, e.Items["Season 2\\b.strm"]);
        Assert.Equal(new[] { "tvshow.nfo" }, e.Files);
        Assert.Equal("Show", e.Folder);
        Assert.Equal(EntryState.Active, e.State);
    }

    [Fact, Trait("Check", "C46")]
    public void C46_no_source_kind_series_only_from_tvshow_nfo_or_a_season_folder()
    {
        Assert.Equal(TitleKind.Series, Takeover.Adopt(Folder("S", "x.strm", "TVSHOW.NFO"), new Dictionary<string, long?>(), new Dictionary<long, long>()).Kind);
        Assert.Equal(TitleKind.Series, Takeover.Adopt(Folder("S", "season 3\\x.strm", "x.nfo"), new Dictionary<string, long?>(), new Dictionary<long, long>()).Kind);
        // A file merely named like a season, with no folder, is not a season folder.
        Assert.Equal(TitleKind.Movie, Takeover.Adopt(Folder("M", "Season 3.strm", "x.nfo"), new Dictionary<string, long?>(), new Dictionary<long, long>()).Kind);
        Assert.Equal(TitleKind.Movie, Takeover.Adopt(Folder("M", "x.strm", "x.nfo", "Seasonal\\a.jpg"), new Dictionary<string, long?>(), new Dictionary<long, long>()).Kind);
    }

    [Fact, Trait("Check", "C46")]
    public void C46_adopt_order_is_by_name_and_listed_names_match_ignoring_case()
    {
        var folders = new[] { Folder("b", "x.strm", "x.nfo"), Folder("A", "x.strm", "x.nfo"), Folder("c", "x.strm", "x.nfo") };
        Assert.Equal(new[] { "A", "b" }, Takeover.ToAdopt(folders, new[] { "C" }).Select(f => f.Name));
    }

    [Fact, Trait("Check", "C85")]
    public void D102_every_listed_video_type_counts_and_other_files_do_not()
    {
        foreach (var ext in new[] { ".strm", ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".ts", ".m2ts", ".iso", ".mpg", ".mpeg", ".webm", ".flv" })
            Assert.Equal(new[] { "F" }, Takeover.UnmanagedFolders(new[] { Folder("F", "a" + ext.ToUpperInvariant()) }, Array.Empty<string>()));
        Assert.Empty(Takeover.UnmanagedFolders(new[] { Folder("F", "a.nfo", "a.jpg", "a.srt", "mkv.txt") }, Array.Empty<string>()));
    }

    [Fact, Trait("Check", "C47")]
    public void Leftovers_leave_active_entries_alone_and_match_folders_ignoring_case()
    {
        var steps = SwapPlan.Leftovers(new[] { Entry("Fine", 1, EntryState.Active, 1), Entry("Came", 2, EntryState.Incoming, 2) }, new[] { "CAME" });
        Assert.Single(steps);
        Assert.Equal(StepKind.MarkActive, steps[0].Kind);
        Assert.Equal("Came", steps[0].Folder);
        Assert.Null(steps[0].Rank);
    }

    [Fact, Trait("Check", "C36")]
    public void SwapNew_carries_on_an_interrupted_incoming_entry_without_adding_it_again()
    {
        var rankTime = new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc);
        var managed = new[] { Entry("New (2020)", 4, EntryState.Incoming, 1, rankDate: rankTime) };
        var picks = new[] { new PlannedPick(1, 4, TitleKind.Movie, "New (2020)") };
        var steps = SwapPlan.SwapNew(managed, picks, rankTime, Array.Empty<string>());
        Assert.Equal(new[] { StepKind.MoveIn, StepKind.MarkActive }, steps.Select(s => s.Kind));
        Assert.All(steps, s => Assert.Equal("New (2020)", s.Folder));
        Assert.All(steps, s => Assert.Equal(1, s.Rank));
        Assert.All(steps, s => Assert.Equal(rankTime, s.RankDateUtc));
        Assert.Empty(SwapPlan.SwapOut(managed, picks, rankTime));
    }

    [Fact, Trait("Check", "C36")]
    public void SwapNew_adds_incoming_with_the_full_entry_before_moving_in()
    {
        var rankTime = new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc);
        var steps = SwapPlan.SwapNew(Array.Empty<ManagedEntry>(), new[] { new PlannedPick(3, 8, TitleKind.Series, "S (2001)") }, rankTime, Array.Empty<string>());
        Assert.Equal(new[] { StepKind.AddIncoming, StepKind.MoveIn, StepKind.MarkActive }, steps.Select(s => s.Kind));
        var e = steps[0].Entry!;
        Assert.Equal("S (2001)", e.Folder);
        Assert.Equal(8, e.SourceId);
        Assert.Equal(TitleKind.Series, e.Kind);
        Assert.Equal(EntryState.Incoming, e.State);
        Assert.Equal(3, e.Rank);
        Assert.Equal(rankTime.AddHours(-2), e.RankDateUtc);
        Assert.False(e.Adopted);
        Assert.Equal("S (2001)", steps[1].Path);
    }

    [Fact, Trait("Check", "C36")]
    public void A_leaving_unranked_entry_is_not_marked_again()
    {
        var steps = SwapPlan.SwapOut(new[] { Entry("Gone", 1, EntryState.Leaving) }, Array.Empty<PlannedPick>(), DateTime.MinValue.AddDays(1));
        Assert.Equal(new[] { StepKind.DeleteFolder, StepKind.RemoveEntry }, steps.Select(s => s.Kind));
    }
}
