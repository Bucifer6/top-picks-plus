using System;
using System.IO;
using System.Linq;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>Fixes from the chunk 2 review (D126–D128): S1 delete root re-checked, S2 owner's folder never listed, S3 one-step rebuild.</summary>
public class ReviewFixTests
{
    [SkippableFact, Trait("Check", "C49")]
    public void C49_S1_a_folder_swapped_for_a_junction_after_the_check_is_removed_as_a_link_and_its_target_kept_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        var link = Path.Combine(s.TopPicks, "Drop (2001)");
        Links.Junction(link, target);

        SafePaths.DeleteTree(link);

        Assert.False(SafePaths.Exists(link));
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
    }

    [SkippableFact, Trait("Check", "C49")]
    public void C49_S1_a_folder_swapped_for_a_symbolic_link_is_removed_as_a_link()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "precious.txt"));
        var link = Path.Combine(s.TopPicks, "Drop (2001)");
        Links.FolderSymlink(link, target);

        SafePaths.DeleteTree(link);

        Assert.False(SafePaths.Exists(link));
        Assert.True(File.Exists(Path.Combine(target, "precious.txt")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_S2_an_unlisted_folder_already_in_top_picks_is_never_taken_over_by_a_new_copy()
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(_ => { });
        s.Data.SaveManaged(new ManagedEntry[0]);
        s.Folder("New (2020)", "mine.txt");
        s.Staged("New (2020)", "New (2020).strm");
        var entry = new ManagedEntry("New (2020)", 4, TitleKind.Movie, EntryState.Incoming, 1, Sandbox.RankTime, false);

        var result = s.Copies.Apply(new[]
        {
            new PlanStep(StepKind.AddIncoming, "New (2020)", 1, Sandbox.RankTime, entry: entry),
            new PlanStep(StepKind.MoveIn, "New (2020)", 1, Sandbox.RankTime, path: "New (2020)"),
            new PlanStep(StepKind.MarkActive, "New (2020)", 1, Sandbox.RankTime),
        });

        Assert.Equal("folder already exists and is not on the managed list", Assert.Single(result.Refused).Reason);
        Assert.Equal(new[] { StepOutcome.Refused, StepOutcome.Skipped, StepOutcome.Skipped }, result.Outcomes);
        Assert.Empty(s.Managed());
        Assert.Equal(new[] { "mine.txt" }, Directory.GetFiles(Path.Combine(s.TopPicks, "New (2020)")).Select(f => Path.GetFileName(f)));
        // So no later drop-out can ever delete it.
        Assert.Single(s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFolder, "New (2020)") }).Refused);
        Assert.True(File.Exists(s.InTopPicks("New (2020)", "mine.txt")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_S2_a_resume_with_the_entry_already_listed_still_carries_on()
    {
        using var s = new Sandbox();
        var entry = new ManagedEntry("New (2020)", 4, TitleKind.Movie, EntryState.Incoming, 1, Sandbox.RankTime, false);
        s.Data.SaveManaged(new[] { entry });
        s.Folder("New (2020)", "New (2020).strm");
        var result = s.Copies.Apply(new[] { new PlanStep(StepKind.AddIncoming, "New (2020)", 1, Sandbox.RankTime, entry: entry) });
        Assert.Empty(result.Refused);
        Assert.Single(s.Managed());
    }

    [Fact, Trait("Check", "C98")]
    public void C98_S3_a_stop_during_the_rebuild_leaves_the_damaged_state_json_in_place_and_the_next_run_rebuilds_it()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new[] { new ManagedEntry("A", 1, TitleKind.Movie, EntryState.Active, 1, Sandbox.RankTime, false) });
        Sandbox.Write(s.Data.StatePath, "{ broken");

        Sandbox.StopBeforeSaving(DataFolder.StateFileName);
        Assert.Throws<SimulatedStop>(() => s.Data.RebuildState(Sandbox.Now));
        Sandbox.NoStop();

        // Never "no state.json" (which would mean a first install: takeover again, listening-since reset).
        Assert.Equal("{ broken", Sandbox.Text(s.Data.StatePath));
        Assert.Empty(Directory.GetFiles(s.DataRoot, "state.json.damaged-*"));
        Assert.Equal(FileStatus.Damaged, s.Data.LoadState().Status);

        Assert.Equal(RebuildOutcome.Rebuilt, s.Data.RebuildState(Sandbox.Now));
        Assert.True(s.Data.LoadState().Value!.TakeoverDone);
        Assert.Equal("{ broken", Sandbox.Text(Assert.Single(Directory.GetFiles(s.DataRoot, "state.json.damaged-*"))));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_save_with_a_backup_keeps_the_old_file_under_the_backup_name_in_the_same_replace()
    {
        using var s = new Sandbox();
        var f = Path.Combine(s.Outside, "x.json");
        var old = Path.Combine(s.Outside, "x.old");
        SafePaths.WriteAtomic(f, new byte[] { 1 });
        SafePaths.WriteAtomic(f, new byte[] { 2 }, old);
        Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(f));
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(old));
    }

    [SkippableFact, Trait("Check", "C72")]
    public void C72_a_data_file_that_cant_be_opened_stops_every_write_as_damaged_windows()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), Links.WindowsOnly);
        using var s = new Sandbox();
        s.Data.UpdateRun(r => r.RunId = "r");
        s.Data.SaveManaged(new ManagedEntry[0]);
        var before = Sandbox.Text(s.Data.ManagedPath);
        using (new FileStream(s.Data.RunPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            // It might be a newer file we can't see: nothing is written (D128).
            Assert.Equal(DataFolder.RunFileName, Assert.Throws<DamagedFileException>(() => s.Data.UpdateManaged(l => l)).File);
            Assert.False(s.Data.SaveListeningSinceOnce(Sandbox.Now));
        }
        Assert.Equal(before, Sandbox.Text(s.Data.ManagedPath));
        Assert.False(File.Exists(s.Data.StatePath));
    }
}
