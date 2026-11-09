using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>The low-level file rules behind atomic saves and link-safe deletes (D24, D37, D82, D120), and the run.json helpers.</summary>
public class SafePathsTests
{
    [Fact, Trait("Check", "C45")]
    public void C45_IsLink_is_false_for_a_plain_folder_a_plain_file_and_a_path_that_isnt_there()
    {
        using var s = new Sandbox();
        var f = Sandbox.Write(Path.Combine(s.Outside, "a.txt"));
        Assert.False(SafePaths.IsLink(s.Outside));
        Assert.False(SafePaths.IsLink(f));
        Assert.False(SafePaths.IsLink(Path.Combine(s.Outside, "missing.txt")));
        Assert.False(SafePaths.IsLink(Path.Combine(s.Outside, "no-folder", "missing.txt")));
    }

    [Theory, Trait("Check", "C45")]
    [InlineData("A", true)]
    [InlineData("A (2001)", true)]
    [InlineData("..", false)]
    [InlineData("A\\B", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void C45_a_folder_name_resolves_only_to_a_direct_child(string? name, bool ok)
    {
        using var s = new Sandbox();
        var full = SafePaths.ChildFolder(s.TopPicks, name, out var problem);
        Assert.Equal(ok, full != null);
        Assert.Equal(ok, problem == null);
        if (ok)
            Assert.Equal(Path.Combine(SafePaths.Full(s.TopPicks), name!), full);
    }

    [Fact, Trait("Check", "C80")]
    public void C80_a_file_path_resolves_inside_the_copy_with_every_part_kept()
    {
        using var s = new Sandbox();
        var dir = s.Folder("A");
        var full = SafePaths.FileInside(SafePaths.Full(dir), "Season 1\\x.strm", out var problem);
        Assert.Null(problem);
        Assert.Equal(Path.Combine(SafePaths.Full(dir), "Season 1", "x.strm"), full);
        Assert.Null(SafePaths.FileInside(SafePaths.Full(dir), "..\\x", out problem));
        Assert.NotNull(problem);
    }

    [Fact, Trait("Check", "C45")]
    public void C45_a_read_only_file_inside_a_dropped_copy_is_still_deleted()
    {
        using var s = new Sandbox();
        var dir = s.Folder("Drop", "a.nfo", "sub\\b.nfo");
        File.SetAttributes(Path.Combine(dir, "a.nfo"), FileAttributes.ReadOnly);
        File.SetAttributes(Path.Combine(dir, "sub", "b.nfo"), FileAttributes.ReadOnly);
        SafePaths.DeleteTree(dir);
        Assert.False(Directory.Exists(dir));
    }

    [Fact, Trait("Check", "C80")]
    public void C80_a_read_only_listed_file_in_a_staying_copy_is_deleted()
    {
        using var s = new Sandbox();
        s.Folder("Stay", "x.srt");
        File.SetAttributes(s.InTopPicks("Stay", "x.srt"), FileAttributes.ReadOnly);
        s.Data.SaveManaged(new[] { new ManagedEntry("Stay", 1, TitleKind.Movie, EntryState.Active, null, null, false, null, new[] { "x.srt" }) });
        Assert.Empty(s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFile, "Stay", path: "x.srt") }).Refused);
        Assert.False(File.Exists(s.InTopPicks("Stay", "x.srt")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_an_atomic_save_creates_a_new_file_and_replaces_an_old_one_leaving_no_temp_file()
    {
        using var s = new Sandbox();
        var f = Path.Combine(s.Outside, "x.json");
        SafePaths.WriteAtomic(f, new byte[] { 1, 2 });
        Assert.Equal(new byte[] { 1, 2 }, File.ReadAllBytes(f));
        SafePaths.WriteAtomic(f, new byte[] { 3 });
        Assert.Equal(new byte[] { 3 }, File.ReadAllBytes(f));
        Assert.Single(Directory.GetFiles(s.Outside));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_failed_save_leaves_no_temp_file()
    {
        using var s = new Sandbox();
        var f = Path.Combine(s.Outside, "x.json");
        SafePaths.WriteAtomic(f, new byte[] { 1 });
        Sandbox.StopBeforeSaving("x.json");
        Assert.Throws<SimulatedStop>(() => SafePaths.WriteAtomic(f, new byte[] { 2 }));
        Sandbox.NoStop();
        Assert.Equal(new[] { f }, Directory.GetFiles(s.Outside));
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(f));
    }

    [SkippableFact, Trait("Check", "C36")]
    public void C36_B31_a_save_waits_for_another_program_that_briefly_holds_the_file_windows()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), Links.WindowsOnly);
        using var s = new Sandbox();
        var f = Path.Combine(s.Outside, "x.json");
        SafePaths.WriteAtomic(f, new byte[] { 1 });
        var held = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read);
        var release = Task.Run(() => { Thread.Sleep(120); held.Dispose(); });
        SafePaths.WriteAtomic(f, new byte[] { 2 });
        release.Wait();
        Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(f));
        Assert.Single(Directory.GetFiles(s.Outside));
    }

    [SkippableFact, Trait("Check", "C36")]
    public void C36_a_save_blocked_for_good_fails_and_leaves_the_old_file_and_no_temp_windows()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), Links.WindowsOnly);
        using var s = new Sandbox();
        var f = Path.Combine(s.Outside, "x.json");
        SafePaths.WriteAtomic(f, new byte[] { 1 });
        using (new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.ThrowsAny<Exception>(() => SafePaths.WriteAtomic(f, new byte[] { 2 }));
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(f));
        Assert.Single(Directory.GetFiles(s.Outside));
    }

    [Fact, Trait("Check", "C37")]
    public void C37_FreeName_gives_the_plain_name_then_numbers()
    {
        using var s = new Sandbox();
        Assert.Equal("a.json", SafePaths.FreeName(s.Outside, "a", ".json"));
        Sandbox.Write(Path.Combine(s.Outside, "a.json"));
        Assert.Equal("a - 2.json", SafePaths.FreeName(s.Outside, "a", ".json"));
        Sandbox.Write(Path.Combine(s.Outside, "a - 2.json"));
        Assert.Equal("a - 3.json", SafePaths.FreeName(s.Outside, "a", ".json"));
        Directory.CreateDirectory(Path.Combine(s.Outside, "a - 3.json"));
        Assert.Equal("a - 4.json", SafePaths.FreeName(s.Outside, "a", ".json"));
    }

    [Fact, Trait("Check", "C79")]
    public void C79_a_run_json_with_null_lists_loads_as_empty_lists()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.RunPath, "{\"version\": 1, \"picks\": null, \"missingPins\": null, \"refusedDeletes\": null, \"unmanagedFolders\": null, \"notices\": null, \"rowCheck\": {\"passed\": true, \"returned\": null, \"extras\": null}}");
        var r = s.Data.LoadRun().Value!;
        Assert.Empty(r.Picks);
        Assert.Empty(r.MissingPins);
        Assert.Empty(r.RefusedDeletes);
        Assert.Empty(r.UnmanagedFolders);
        Assert.Empty(r.Notices);
        Assert.Empty(r.RowCheck!.Returned);
        Assert.Empty(r.RowCheck.Extras);
        Assert.Null(r.Snapshot());
    }

    [Fact, Trait("Check", "C79")]
    public void C79_snapshot_needs_both_status_and_step()
    {
        Assert.Null(new RunRecord { Status = RunStatus.Running }.Snapshot());
        Assert.Null(new RunRecord { Step = RunStep.Swap }.Snapshot());
        var snap = new RunRecord { Status = RunStatus.Running, Step = RunStep.Swap, ResumeCount = 2 }.Snapshot()!;
        Assert.Equal((RunStatus.Running, RunStep.Swap, 2), (snap.Status, snap.Step, snap.ResumeCount));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_run_pick_maps_from_Logic_and_back_to_the_plan_without_rechoosing()
    {
        var p = RunPick.From(new Pick(4, 99, PickReason.ViewerDays, 6), TitleKind.Series, "S (2001)");
        Assert.Equal((4, 99L, TitleKind.Series, "S (2001)", PickReason.ViewerDays, 6), (p.Rank, p.SourceId, p.Kind, p.Folder, p.Reason, p.ViewerDays));
        Assert.False(p.CopyApplied || p.DateApplied || p.ArtworkFailed);
        var planned = p.ToPlanned();
        Assert.Equal((4, 99L, TitleKind.Series, "S (2001)"), (planned.Rank, planned.SourceId, planned.Kind, planned.Folder));
    }

    [Fact, Trait("Check", "C37")]
    public void C37_load_results_report_why_and_damaged_files_have_no_value()
    {
        using var s = new Sandbox();
        s.WriteVersion(DataFolder.StateFileName, 7);
        var st = s.Data.LoadState();
        Assert.Equal(FileStatus.Newer, st.Status);
        Assert.Equal("version 7", st.Problem);
        Assert.Null(st.Value);
        Assert.False(st.Readable);
        var e = Assert.Throws<DataNewerException>(() => s.Data.UpdateState(x => x.TakeoverDone = true));
        Assert.Equal(DataFolder.StateFileName, e.File);
        Sandbox.Write(s.Data.PlaysPath, "nope");
        Assert.Equal(DataFolder.PlaysFileName, Assert.Throws<DamagedFileException>(() => s.Data.TrimPlays(Sandbox.Sunday)).File);
        Assert.Throws<ArgumentException>(() => new DataFolder(""));
        Assert.Throws<ArgumentException>(() => new CopyFolders("", s.Data));
    }

    [Fact, Trait("Check", "C72")]
    public void C72_inspect_names_the_first_newer_file_in_a_fixed_order()
    {
        using var s = new Sandbox();
        s.WriteVersion(DataFolder.StateFileName, 2);
        Assert.Equal(DataFolder.StateFileName, s.Data.Inspect().NewerFile);
        s.WriteVersion(DataFolder.RunFileName, 2);
        Assert.Equal(DataFolder.RunFileName, s.Data.Inspect().NewerFile);
        s.WriteVersion(DataFolder.PlaysFileName, 2);
        Assert.Equal(DataFolder.PlaysFileName, s.Data.Inspect().NewerFile);
        s.WriteVersion(DataFolder.ManagedFileName, 2);
        var h = s.Data.Inspect();
        Assert.Equal(DataFolder.ManagedFileName, h.NewerFile);
        Assert.Equal((FileStatus.Newer, FileStatus.Newer, FileStatus.Newer, FileStatus.Newer), (h.Managed, h.Plays, h.Run, h.State));
        var facts = h.ToGuardFacts(true, true, false, false, true);
        Assert.True(facts.DataNewer && facts.LockHeld && facts.OldPluginLoaded && facts.NfoSaverOn);
        Assert.False(facts.LibraryFound || facts.NfoReaderOn);
        Assert.Equal(DataFolder.ManagedFileName, facts.NewerFile);
        // Newer is not damaged: managed, plays and state count as readable for the damage guards (data newer wins first anyway).
        Assert.True(facts.ManagedReadable && facts.PlaysReadable && facts.StateReadable);
    }

    [Fact, Trait("Check", "C98")]
    public void C98_guard_facts_mark_each_damaged_file_unreadable()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.ManagedPath, "x");
        var f = s.Data.Inspect().ToGuardFacts(false, false, true, true, false);
        Assert.False(f.ManagedReadable);
        Assert.True(f.PlaysReadable && f.StateReadable);
        Assert.False(f.DataNewer);
        Assert.Null(f.NewerFile);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_StartRun_sets_the_rank_time_to_the_whole_minute_and_saves_versions()
    {
        using var s = new Sandbox();
        var r = s.Data.StartRun("r", Sandbox.Now, RunStep.Import, "1.2.3", "4.10.2.0", "GMT Standard Time");
        var back = s.Data.LoadRun().Value!;
        Assert.Equal(Sandbox.RankTime, back.RankTimeUtc);
        Assert.Equal(Sandbox.Now, back.StartedUtc);
        Assert.Equal(RunStep.Import, back.Step);
        Assert.Equal(RunStatus.Running, back.Status);
        Assert.Equal(("1.2.3", "4.10.2.0", "GMT Standard Time"), (back.PluginVersion, back.EmbyVersion, back.TimeZone));
        Assert.Equal("r", r.RunId);
        s.Data.SaveStep(RunStep.Takeover);
        Assert.Equal(RunStep.Takeover, s.Data.LoadRun().Value!.Step);
        s.Data.FailRun("  ");
        Assert.Null(s.Data.LoadRun().Value!.FailReason!.Message);
    }
}
