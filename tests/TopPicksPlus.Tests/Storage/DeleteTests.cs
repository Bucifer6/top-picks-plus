using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>
/// The delete checks (D37 folders, D82 files, D119 exact names): a bad entry or path is never deleted and the report names it;
/// links are removed as links and their targets never touched (C45, C49, C80). Tests whose name ends in <c>_junction</c> need
/// Windows; build.yml fails the Windows job if any of them didn't run and pass there (card 0003).
/// </summary>
public class DeleteTests
{
    private static ManagedEntry Listed(string folder, IReadOnlyDictionary<string, long?>? items = null, IReadOnlyList<string>? files = null) =>
        new ManagedEntry(folder, 1, TitleKind.Movie, EntryState.Leaving, null, null, false, items, files);

    private static PlanResult DropOut(Sandbox s, string folder)
    {
        return s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFolder, folder), new PlanStep(StepKind.RemoveEntry, folder) });
    }

    [Theory, Trait("Check", "C45")]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("..\\outside")]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("C:\\Windows")]
    [InlineData("Bad name.")]
    [InlineData("CON")]
    public void C45_a_bad_folder_name_is_never_deleted_and_the_report_names_it(string bad)
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(_ => { });
        s.Data.SaveManaged(new[] { Listed(bad) });
        s.Folder("Keep me", "note.txt");
        Sandbox.Write(Path.Combine(s.Outside, "precious.txt"));
        var before = s.Snapshot();

        var result = DropOut(s, bad);

        Assert.Equal(new[] { StepOutcome.Refused, StepOutcome.Skipped }, result.Outcomes);
        Assert.Single(result.Refused);
        Assert.Equal(bad, result.Refused[0].Folder);
        Assert.Null(result.Refused[0].Path);
        // Nothing deleted, and the entry is kept (still leaving), so it is reported again and never orphaned.
        var after = s.Snapshot();
        Assert.Equal(before.Keys.Where(k => !k.Contains("json")), after.Keys.Where(k => !k.Contains("json")));
        Assert.True(Directory.Exists(s.TopPicks));
        Assert.Single(s.Managed());
        Assert.Contains(s.Data.LoadRun().Value!.RefusedDeletes, r => r.Folder == bad);
    }

    [Fact, Trait("Check", "C45")]
    public void C45_a_good_folder_is_deleted_whole_and_its_entry_removed()
    {
        using var s = new Sandbox();
        s.Folder("Dune (2021)", "Dune (2021).strm", "Dune (2021).nfo", "extra\\mine.txt");
        s.Data.SaveManaged(new[] { Listed("Dune (2021)") });

        var result = DropOut(s, "Dune (2021)");

        Assert.Empty(result.Refused);
        Assert.False(Directory.Exists(Path.Combine(s.TopPicks, "Dune (2021)")));
        Assert.Empty(s.Managed());
        Assert.True(Directory.Exists(s.TopPicks));
    }

    [Fact, Trait("Check", "C45")]
    public void C45_deleting_a_folder_that_is_already_gone_is_safe_to_repeat()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new[] { Listed("Gone") });
        Assert.Empty(DropOut(s, "Gone").Refused);
        Assert.Empty(DropOut(s, "Gone").Refused);
        Assert.Empty(s.Managed());
    }

    [Fact, Trait("Check", "C45")]
    public void C45_a_file_with_the_folder_name_is_not_deleted()
    {
        using var s = new Sandbox();
        Sandbox.Write(Path.Combine(s.TopPicks, "Loose"));
        s.Data.SaveManaged(new[] { Listed("Loose") });
        var result = DropOut(s, "Loose");
        Assert.Equal("not a folder", Assert.Single(result.Refused).Reason);
        Assert.True(File.Exists(Path.Combine(s.TopPicks, "Loose")));
    }

    [SkippableFact, Trait("Check", "C45")]
    public void C45_a_managed_folder_that_is_a_junction_is_not_deleted_and_its_target_is_untouched_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        Links.Junction(Path.Combine(s.TopPicks, "Linked"), target);
        s.Data.SaveManaged(new[] { Listed("Linked") });

        var result = DropOut(s, "Linked");

        Assert.Equal("folder is a junction or link", Assert.Single(result.Refused).Reason);
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
        Assert.True(SafePaths.IsLink(Path.Combine(s.TopPicks, "Linked")));
        Assert.Single(s.Managed());
    }

    [SkippableFact, Trait("Check", "C45")]
    public void C45_a_managed_folder_that_is_a_symbolic_link_is_not_deleted()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        Links.FolderSymlink(Path.Combine(s.TopPicks, "Linked"), target);
        s.Data.SaveManaged(new[] { Listed("Linked") });

        Assert.Single(DropOut(s, "Linked").Refused);
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
    }

    [SkippableFact, Trait("Check", "C49")]
    public void C49_a_junction_inside_a_dropped_copy_is_removed_as_a_link_and_its_target_is_untouched_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        s.Folder("Drop (2001)", "Drop (2001).strm");
        Links.Junction(Path.Combine(s.TopPicks, "Drop (2001)", "inner"), target);
        s.Data.SaveManaged(new[] { Listed("Drop (2001)") });

        Assert.Empty(DropOut(s, "Drop (2001)").Refused);

        Assert.False(Directory.Exists(Path.Combine(s.TopPicks, "Drop (2001)")));
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
    }

    [SkippableFact, Trait("Check", "C49")]
    public void C49_a_symbolic_link_inside_a_dropped_copy_is_removed_as_a_link()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        var file = Sandbox.Write(Path.Combine(s.Outside, "precious.txt"));
        Sandbox.Write(Path.Combine(target, "inside.txt"));
        s.Folder("Drop (2001)", "Drop (2001).strm");
        Links.FolderSymlink(Path.Combine(s.TopPicks, "Drop (2001)", "dirlink"), target);
        Links.FileSymlink(Path.Combine(s.TopPicks, "Drop (2001)", "filelink.txt"), file);
        s.Data.SaveManaged(new[] { Listed("Drop (2001)") });

        Assert.Empty(DropOut(s, "Drop (2001)").Refused);

        Assert.False(Directory.Exists(Path.Combine(s.TopPicks, "Drop (2001)")));
        Assert.True(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(target, "inside.txt")));
    }

    [SkippableFact, Trait("Check", "C49")]
    public void C49_a_staging_folder_that_is_a_junction_is_not_emptied_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        Links.Junction(s.Staging, target);

        Assert.Equal("folder is a junction or link", s.Copies.EmptyStaging());
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
        Assert.Throws<IOException>(() => s.Staged("New (2020)", "New (2020).strm"));
        Assert.False(File.Exists(Path.Combine(target, "New (2020)", "New (2020).strm")));
    }

    [SkippableFact, Trait("Check", "C49")]
    public void C49_a_staging_folder_that_is_a_symbolic_link_is_not_emptied()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        Links.FolderSymlink(s.Staging, target);
        Assert.NotNull(s.Copies.EmptyStaging());
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
    }

    [SkippableFact, Trait("Check", "C49")]
    public void C49_emptying_staging_removes_a_link_inside_as_a_link_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        s.Staged("Old (2000)", "Old (2000).strm");
        Links.Junction(Path.Combine(s.Staging, "Old (2000)", "inner"), target);

        Assert.Null(s.Copies.EmptyStaging());

        Assert.True(Directory.Exists(s.Staging));
        Assert.Empty(Directory.EnumerateFileSystemEntries(s.Staging));
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
    }

    [Fact, Trait("Check", "C49")]
    public void C49_emptying_staging_clears_it_and_leaves_the_data_files()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new ManagedEntry[0]);
        s.Staged("Old (2000)", "Old (2000).strm", "Season 1\\x.strm");
        Assert.Null(s.Copies.EmptyStaging());
        Assert.Empty(Directory.EnumerateFileSystemEntries(s.Staging));
        Assert.True(File.Exists(s.Data.ManagedPath));
    }

    // ---- D82: files inside a staying copy ----

    private static (Sandbox S, string Folder) StayingCopy(params string[] listed)
    {
        var s = new Sandbox();
        s.Data.UpdateRun(_ => { });
        var folder = "Stay (2010)";
        s.Folder(folder, "Stay (2010).strm", "Stay (2010).nfo", "mine.txt");
        s.Data.SaveManaged(new[] { new ManagedEntry(folder, 5, TitleKind.Movie, EntryState.Active, 1, Sandbox.RankTime, false,
            new Dictionary<string, long?> { ["Stay (2010).strm"] = 5 }, listed.ToList()) });
        return (s, folder);
    }

    [Theory, Trait("Check", "C80")]
    [InlineData("")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("\\rooted.txt")]
    [InlineData("\\\\server\\share\\x.txt")]
    [InlineData("..\\..\\outside\\precious.txt")]
    [InlineData("sub\\..\\..\\x.txt")]
    [InlineData("a|b.txt")]
    [InlineData("a/b.txt")]
    public void C80_a_bad_path_in_files_is_never_deleted_or_written_and_the_report_names_it(string bad)
    {
        var (s, folder) = StayingCopy(bad);
        using (s)
        {
            Sandbox.Write(Path.Combine(s.Outside, "precious.txt"));
            var before = s.Snapshot();

            var result = s.Copies.Apply(new[]
            {
                new PlanStep(StepKind.DeleteFile, folder, path: bad),
                new PlanStep(StepKind.RemoveFileFromEntry, folder, path: bad),
                new PlanStep(StepKind.AddFileToEntry, folder, path: bad),
                new PlanStep(StepKind.WriteFile, folder, path: bad),
            });

            Assert.Equal(new[] { StepOutcome.Refused, StepOutcome.Skipped, StepOutcome.Skipped, StepOutcome.Skipped }, result.Outcomes);
            var r = Assert.Single(result.Refused);
            Assert.Equal(folder, r.Folder);
            Assert.Equal(bad, r.Path);
            Assert.Contains(s.Data.LoadRun().Value!.RefusedDeletes, x => x.Path == bad);
            var after = s.Snapshot();
            Assert.Equal(before.Where(k => !k.Key.EndsWith(".json")), after.Where(k => !k.Key.EndsWith(".json")));
            Assert.Contains(bad, s.Entry(folder).Files);
        }
    }

    [Fact, Trait("Check", "C80")]
    public void C80_a_good_listed_file_is_deleted_and_files_not_listed_are_left_alone()
    {
        var (s, folder) = StayingCopy("Stay (2010).nfo");
        using (s)
        {
            var result = s.Copies.Apply(new[]
            {
                new PlanStep(StepKind.DeleteFile, folder, path: "Stay (2010).nfo"),
                new PlanStep(StepKind.RemoveFileFromEntry, folder, path: "Stay (2010).nfo"),
            });
            Assert.Empty(result.Refused);
            Assert.False(File.Exists(s.InTopPicks(folder, "Stay (2010).nfo")));
            Assert.True(File.Exists(s.InTopPicks(folder, "mine.txt")));
            Assert.Empty(s.Entry(folder).Files);
        }
    }

    [SkippableFact, Trait("Check", "C80")]
    public void C80_a_path_through_a_junction_is_never_deleted_junction()
    {
        var (s, folder) = StayingCopy("inner\\precious.txt");
        using (s)
        {
            var target = Path.Combine(s.Outside, "target");
            Sandbox.Write(Path.Combine(target, "precious.txt"));
            Links.Junction(s.InTopPicks(folder, "inner"), target);

            var result = s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFile, folder, path: "inner\\precious.txt") });

            Assert.Equal("path goes through a junction or link", Assert.Single(result.Refused).Reason);
            Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
        }
    }

    [SkippableFact, Trait("Check", "C80")]
    public void C80_a_path_through_a_symbolic_link_is_never_deleted_or_written()
    {
        var (s, folder) = StayingCopy("inner\\precious.txt", "link.txt");
        using (s)
        {
            var target = Path.Combine(s.Outside, "target");
            Sandbox.Write(Path.Combine(target, "precious.txt"), "keep");
            var file = Sandbox.Write(Path.Combine(s.Outside, "file.txt"), "keep");
            Links.FolderSymlink(s.InTopPicks(folder, "inner"), target);
            Links.FileSymlink(s.InTopPicks(folder, "link.txt"), file);
            s.Staged(folder, "inner\\precious.txt", "link.txt");

            var result = s.Copies.Apply(new[]
            {
                new PlanStep(StepKind.DeleteFile, folder, path: "inner\\precious.txt"),
                new PlanStep(StepKind.WriteFile, folder, path: "link.txt"),
            });

            Assert.Equal(2, result.Refused.Count);
            Assert.Equal("file is a link", result.Refused[1].Reason);
            Assert.Equal("keep", Sandbox.Text(Path.Combine(target, "precious.txt")));
            Assert.Equal("keep", Sandbox.Text(file));
        }
    }

    [SkippableFact, Trait("Check", "C80")]
    public void C80_a_staying_copy_folder_that_became_a_junction_has_no_file_touched_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "x.nfo"), "keep");
        Links.Junction(Path.Combine(s.TopPicks, "Stay"), target);
        s.Data.SaveManaged(new[] { new ManagedEntry("Stay", 5, TitleKind.Movie, EntryState.Active, 1, Sandbox.RankTime, false, null, new[] { "x.nfo" }) });

        var result = s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFile, "Stay", path: "x.nfo") });

        Assert.Single(result.Refused);
        Assert.Equal("keep", Sandbox.Text(Path.Combine(target, "x.nfo")));
    }

    // ---- D119 (B42): exact names, so a Windows short name can't reach another folder ----

    [SkippableFact, Trait("Check", "C45")]
    public void C45_B42_a_Windows_short_name_is_refused_and_the_long_folder_is_untouched()
    {
        using var s = new Sandbox();
        var dir = s.Folder("Long folder name (2020)", "x.strm");
        var shortName = ShortNames.Make(dir, "LONGFO~1");
        Skip.If(shortName == null, "this volume has no 8.3 short names, so a short name can't reach anything here");
        var resolves = Directory.Exists(Path.Combine(s.TopPicks, shortName!));
        Skip.IfNot(resolves, "short name '" + shortName + "' was reported but doesn't open the folder on this volume, so it can't reach anything");
        s.Data.UpdateRun(_ => { });
        s.Data.SaveManaged(new[] { Listed(shortName!) });

        var result = DropOut(s, shortName!);

        Assert.Equal("name reaches a different file or folder (short name)", Assert.Single(result.Refused).Reason);
        Assert.True(File.Exists(Path.Combine(dir, "x.strm")));
    }

    [Fact, Trait("Check", "C45")]
    public void C45_names_differing_only_in_case_are_the_same_folder()
    {
        using var s = new Sandbox();
        s.Folder("dune (2021)", "x.strm");
        s.Data.SaveManaged(new[] { Listed("Dune (2021)") });
        var result = DropOut(s, "Dune (2021)");
        if (System.OperatingSystem.IsWindows())
        {
            Assert.Empty(result.Refused);
            Assert.False(Directory.Exists(Path.Combine(s.TopPicks, "dune (2021)")));
        }
        else
        {
            // A case-sensitive file system has no "Dune (2021)": nothing to delete, and "dune (2021)" is left alone.
            Assert.True(Directory.Exists(Path.Combine(s.TopPicks, "dune (2021)")));
        }
    }
}
