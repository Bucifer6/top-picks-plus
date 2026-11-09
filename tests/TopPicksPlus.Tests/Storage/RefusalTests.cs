using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>
/// "Data newer" blocks every write (D85, D93, D94, C72, C83); a refusal writes only <c>lastRefusal</c> (D94, C95); and two
/// writers can't overwrite each other (B31, D120).
/// </summary>
public class RefusalTests
{
    /// <summary>All four files written normally, plus a copy folder and staging, so "nothing written" can be proved.</summary>
    private static Sandbox Full()
    {
        var s = new Sandbox();
        s.Data.SaveManaged(new[] { new ManagedEntry("A (2001)", 1, TitleKind.Movie, EntryState.Leaving, null, null, false) });
        s.Data.AppendPlay(new Play("u", 1, null, Sandbox.Sunday, 10, true, 20, PlayOrigin.Live));
        s.Data.StartRun("r1", Sandbox.Now, RunStep.Swap, "1.0.0", "4.10.2.0", "GMT");
        s.Data.UpdateState(st => st.TakeoverDone = true);
        s.Folder("A (2001)", "A (2001).strm");
        s.Staged("B (2002)", "B (2002).strm");
        return s;
    }

    public static TheoryData<string> EachFile => new TheoryData<string>
    {
        DataFolder.ManagedFileName, DataFolder.PlaysFileName, DataFolder.RunFileName, DataFolder.StateFileName,
    };

    private static void MakeNewer(Sandbox s, string file)
    {
        var path = s.DataFile(file);
        var text = Sandbox.Text(path).Replace("\"version\": 1", "\"version\": 2");
        Sandbox.Write(path, text);
    }

    [Theory, Trait("Check", "C72"), MemberData(nameof(EachFile))]
    public void C72_any_newer_file_makes_runs_refuse_data_newer_and_nothing_is_renamed_deleted_or_written(string newer)
    {
        using var s = Full();
        MakeNewer(s, newer);
        var before = s.Snapshot();

        var health = s.Data.Inspect();
        Assert.True(health.DataNewer);
        Assert.Equal(newer, health.NewerFile);
        var guard = RunRules.Guards(health.ToGuardFacts(lockHeld: true, oldPluginLoaded: true, libraryFound: false, nfoReaderOn: false, nfoSaverOn: true));
        Assert.Equal(Refusal.DataNewer, guard.Reason);
        Assert.False(guard.WriteLastRefusal);

        // Every write path refuses.
        Assert.False(s.Data.RecordRefusal(Refusal.DataNewer, guard.Detail, Sandbox.Now));
        Assert.False(s.Data.RecordRefusal(Refusal.LibraryMissing, "x", Sandbox.Now));
        Assert.Throws<DataNewerException>(() => s.Data.AppendPlay(new Play("u", 2, null, Sandbox.Sunday, 1, true, 1, PlayOrigin.Live)));
        Assert.Throws<DataNewerException>(() => s.Data.SaveStep(RunStep.Rescan));
        Assert.Throws<DataNewerException>(() => s.Data.UpdateState(st => st.PlaybackReportingImported = true));
        Assert.Throws<DataNewerException>(() => s.Data.SaveManaged(new ManagedEntry[0]));
        Assert.Throws<DataNewerException>(() => s.Data.ImportStep("r1", SeedSource.None, new Play[0]));
        Assert.Throws<DataNewerException>(() => s.Data.StartRun("r2", Sandbox.Now, RunStep.Import, null, null, null));
        Assert.Throws<DataNewerException>(() => s.Data.SetAsideDamaged(Sandbox.Now, Sandbox.Sunday));
        Assert.False(s.Data.SaveListeningSinceOnce(Sandbox.Now));
        Assert.False(s.Data.AddNotice("x", "y", Sandbox.Now));
        Assert.Throws<DataNewerException>(() => s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFolder, "A (2001)") }));
        Assert.Throws<DataNewerException>(() => s.Copies.EmptyStaging());
        Assert.Throws<DataNewerException>(() => s.Copies.WriteStagingText("C", "C.strm", "x"));
        Assert.Throws<DataNewerException>(() => s.Copies.TakeOver(_ => new System.Collections.Generic.Dictionary<string, long?>(), new System.Collections.Generic.Dictionary<long, long>()));

        Assert.Equal(before, s.Snapshot());
    }

    [Fact, Trait("Check", "C72")]
    public void C72_a_newer_state_json_alongside_a_damaged_one_is_never_rebuilt()
    {
        using var s = Full();
        MakeNewer(s, DataFolder.PlaysFileName);
        Sandbox.Write(s.Data.StatePath, "{ broken");
        var before = s.Snapshot();
        Assert.Throws<DataNewerException>(() => s.Data.RebuildState(Sandbox.Now));
        Assert.Equal(before, s.Snapshot());
    }

    [Fact, Trait("Check", "C83")]
    public void C83_with_plays_json_newer_no_play_is_logged_and_none_of_the_four_files_is_written_lastRefusal_included()
    {
        using var s = Full();
        MakeNewer(s, DataFolder.PlaysFileName);
        var before = s.Snapshot();
        var allowed = EmbyRules.Allowed(oldPluginLoaded: false, dataNewer: s.Data.Inspect().DataNewer, managedReadable: true);
        Assert.False(allowed.LogPlays);
        Assert.False(allowed.WriteDataFiles);
        Assert.Throws<DataNewerException>(() => s.Data.AppendPlay(new Play("u", 3, null, Sandbox.Sunday, 1, true, 1, PlayOrigin.Live)));
        Assert.False(s.Data.RecordRefusal(Refusal.DataNewer, "plays.json", Sandbox.Now));
        Assert.Equal(before, s.Snapshot());
    }

    [Fact, Trait("Check", "C72")]
    public void C72_a_file_turning_newer_between_the_check_and_the_write_still_stops_the_write()
    {
        using var s = Full();
        // The newer DLL writes run.json while this one is about to save managed.json: the gate reads all four again at save time.
        var health = s.Data.Inspect();
        Assert.False(health.DataNewer);
        MakeNewer(s, DataFolder.RunFileName);
        var before = Sandbox.Text(s.Data.ManagedPath);
        Assert.Throws<DataNewerException>(() => s.Data.SaveManaged(new ManagedEntry[0]));
        Assert.Equal(before, Sandbox.Text(s.Data.ManagedPath));
    }

    // ---- A refusal writes only lastRefusal (D94, C95) ----

    [Fact, Trait("Check", "C95")]
    public void C95_a_refusal_at_a_resume_writes_only_lastRefusal_and_the_waiting_run_is_unchanged()
    {
        using var s = Full();
        s.Data.UpdateRun(r =>
        {
            r.ResumeCount = 1;
            r.Picks.Add(new RunPick { Rank = 1, SourceId = 2, Folder = "B (2002)", Kind = TitleKind.Movie, Reason = PickReason.Filler });
        });
        var before = s.Snapshot();
        var runBefore = Sandbox.Text(s.Data.RunPath);
        var decision = RunRules.OnTrigger(Trigger.Sunday, s.Data.LoadRun().Value!.Snapshot(), s.Data.Inspect().ToGuardFacts(false, false, libraryFound: false, true, false));
        Assert.Equal(TriggerAction.Refuse, decision.Action);
        Assert.True(decision.Refusal!.WriteLastRefusal);

        Assert.True(s.Data.RecordRefusal(decision.Refusal!.Reason!.Value, decision.Refusal!.Detail, Sandbox.Now.AddHours(1)));

        var after = s.Snapshot();
        foreach (var k in before.Keys.Where(k => !k.EndsWith(DataFolder.RunFileName)))
            Assert.Equal(before[k], after[k]);
        var r = s.Data.LoadRun().Value!;
        Assert.Equal(RunStatus.Running, r.Status);
        Assert.Equal(RunStep.Swap, r.Step);
        Assert.Equal(1, r.ResumeCount);
        Assert.Equal("B (2002)", Assert.Single(r.Picks).Folder);
        Assert.Equal(Refusal.LibraryMissing, r.LastRefusal!.Reason);
        Assert.Equal(Sandbox.Now.AddHours(1), r.LastRefusal.Time);
        // Taking lastRefusal out again gives back exactly the run.json from before.
        Assert.Equal(StripRefusal(runBefore), StripRefusal(Sandbox.Text(s.Data.RunPath)));

        // Once the library is back, the next trigger resumes from swap, and resumeCount becomes 2.
        var resume = RunRules.OnTrigger(Trigger.RunNow, r.Snapshot(), s.Data.Inspect().ToGuardFacts(false, false, true, true, false));
        Assert.Equal(TriggerAction.Resume, resume.Action);
        s.Data.SaveResumeCount(resume.ResumeCount);
        Assert.Equal(2, s.Data.LoadRun().Value!.ResumeCount);
        Assert.Equal(RunStep.Swap, s.Data.LoadRun().Value!.Step);
    }

    private static string StripRefusal(string runJson)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(runJson)!.AsObject();
        node.Remove("lastRefusal");
        return node.ToJsonString();
    }

    [Fact, Trait("Check", "C95")]
    public void C95_a_refusal_with_no_run_writes_only_lastRefusal_and_nothing_else_appears()
    {
        using var s = new Sandbox();
        Assert.True(s.Data.RecordRefusal(Refusal.OldPluginLoaded, "uninstall", Sandbox.Now));
        Assert.Equal(new[] { DataFolder.RunFileName }, Directory.GetFiles(s.DataRoot).Select(f => Path.GetFileName(f)));
        var r = s.Data.LoadRun().Value!;
        Assert.Null(r.Snapshot());
        Assert.Equal(Refusal.OldPluginLoaded, r.LastRefusal!.Reason);
    }

    [Fact, Trait("Check", "C95")]
    public void C95_a_refusal_never_writes_over_a_damaged_run_json()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.RunPath, "{ broken");
        Assert.False(s.Data.RecordRefusal(Refusal.LibraryMissing, "x", Sandbox.Now));
        Assert.Equal("{ broken", Sandbox.Text(s.Data.RunPath));
    }

    // ---- B31, D120: two writers ----

    [Fact, Trait("Check", "C95")]
    public void C95_B31_a_refusal_during_a_working_run_never_loses_the_runs_own_saves_and_vice_versa()
    {
        using var s = Full();
        var a = new DataFolder(s.DataRoot); // the working run
        var b = new DataFolder(s.DataRoot); // a refused Run now, through its own instance
        Parallel.For(0, 60, i =>
        {
            if (i % 2 == 0)
                a.UpdateRun(r => r.PlaysLast7Days++);
            else
                b.RecordRefusal(Refusal.AlreadyRunning, "a run is already in progress " + i, Sandbox.Now.AddSeconds(i));
        });
        var run = s.Data.LoadRun().Value!;
        Assert.Equal(30, run.PlaysLast7Days);
        Assert.Equal(RunStatus.Running, run.Status);
        Assert.Equal(Refusal.AlreadyRunning, run.LastRefusal!.Reason);
        Assert.Empty(Directory.GetFiles(s.DataRoot, "*" + SafePaths.TempSuffix));
    }

    [Fact, Trait("Check", "C95")]
    public void C95_B31_plays_logged_while_a_run_saves_managed_and_run_are_never_lost()
    {
        using var s = new Sandbox();
        var logger = new DataFolder(s.DataRoot);
        var run = new DataFolder(s.DataRoot);
        Parallel.For(0, 60, i =>
        {
            switch (i % 3)
            {
                case 0: logger.AppendPlay(new Play("u" + i, i, null, Sandbox.Sunday, 1, true, 1, PlayOrigin.Live)); break;
                case 1: run.UpdateManaged(l => { l.Add(new ManagedEntry("F" + i, i, TitleKind.Movie, EntryState.Active, null, null, false)); return l; }); break;
                default: run.UpdateRun(r => r.ImportIgnored++); break;
            }
        });
        Assert.Equal(20, s.Data.LoadPlays().Value!.Plays.Count);
        Assert.Equal(20, s.Managed().Count);
        Assert.Equal(20, s.Data.LoadRun().Value!.ImportIgnored);
    }

    [Fact, Trait("Check", "C95")]
    public void C95_B31_a_reader_holding_a_file_open_does_not_stop_a_save()
    {
        using var s = Full();
        using (var reader = new FileStream(s.Data.RunPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            s.Data.SaveStep(RunStep.Rescan);
        }
        Assert.Equal(RunStep.Rescan, s.Data.LoadRun().Value!.Step);
    }
}
