using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>
/// Safe loads, atomic saves, file shapes and damaged files (D24, D32, D36, D81, D88, D117, C37, C40, C79).
/// </summary>
public class DataFileTests
{
    private static ManagedEntry Sample() => new ManagedEntry("Dune (2021)", 42, TitleKind.Series, EntryState.Active, 3,
        Sandbox.RankTime.AddHours(-2), false,
        new Dictionary<string, long?> { ["Season 1\\Dune - S01E01 - Arrival.strm"] = 1001, ["old.strm"] = null },
        new[] { "tvshow.nfo", "Season 1\\folder.jpg" });

    // ---- Missing = empty ----

    [Fact, Trait("Check", "C37")]
    public void C37_missing_files_load_as_empty_and_readable()
    {
        using var s = new Sandbox();
        var m = s.Data.LoadManaged();
        Assert.Equal(FileStatus.Missing, m.Status);
        Assert.True(m.Readable);
        Assert.Empty(m.Value!);
        Assert.Empty(s.Data.LoadPlays().Value!.Plays);
        Assert.Null(s.Data.LoadPlays().Value!.Marker);
        Assert.Null(s.Data.LoadRun().Value!.Snapshot());
        var st = s.Data.LoadState().Value!;
        Assert.False(st.TakeoverDone);
        Assert.False(st.PlaybackReportingImported);
        Assert.Null(st.ListeningSinceUtc);
        // Loading writes nothing.
        Assert.Empty(Directory.GetFiles(s.DataRoot));
    }

    // ---- Shapes (D88, D117) ----

    [Fact, Trait("Check", "C79")]
    public void C79_managed_round_trips_every_field_and_is_camelCase_with_version_first()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new[] { Sample(), new ManagedEntry("Old", null, TitleKind.Movie, EntryState.Leaving, null, null, true) });
        var back = s.Managed();
        var e = back[0];
        Assert.Equal("Dune (2021)", e.Folder);
        Assert.Equal(42, e.SourceId);
        Assert.Equal(TitleKind.Series, e.Kind);
        Assert.Equal(EntryState.Active, e.State);
        Assert.Equal(3, e.Rank);
        Assert.Equal(Sandbox.RankTime.AddHours(-2), e.RankDateUtc);
        Assert.Equal(DateTimeKind.Utc, e.RankDateUtc!.Value.Kind);
        Assert.False(e.Adopted);
        Assert.Equal(1001, e.Items["Season 1\\Dune - S01E01 - Arrival.strm"]);
        Assert.Contains("old.strm", e.Items.Keys);
        Assert.Null(e.Items["old.strm"]);
        Assert.Equal(new[] { "tvshow.nfo", "Season 1\\folder.jpg" }, e.Files);
        Assert.Null(back[1].SourceId);
        Assert.Null(back[1].Rank);
        Assert.True(back[1].Adopted);

        var text = Sandbox.Text(s.Data.ManagedPath);
        Assert.StartsWith("{\n  \"version\": 1,", text.Replace("\r\n", "\n"));
        Assert.Contains("\"entries\":", text);
        Assert.Contains("\"rankDateUtc\": \"2026-11-15T03:00:00Z\"", text);
        Assert.Contains("\"state\": \"active\"", text);
        Assert.Contains("\"kind\": \"series\"", text);
        Assert.Contains("\"state\": \"leaving\"", text);
        // "none" is left out of the file (D117): the second entry has no sourceId, rank or rankDateUtc.
        using var doc = JsonDocument.Parse(text);
        var second = doc.RootElement.GetProperty("entries")[1];
        Assert.False(second.TryGetProperty("sourceId", out _));
        Assert.False(second.TryGetProperty("rank", out _));
        Assert.False(second.TryGetProperty("rankDateUtc", out _));
        // No byte-order mark.
        Assert.NotEqual(0xEF, File.ReadAllBytes(s.Data.ManagedPath)[0]);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_plays_round_trip_with_origin_day_and_marker()
    {
        using var s = new Sandbox();
        var plays = new[]
        {
            new Play("u1", 7, 70, new DateTime(2026, 11, 14), 42.5, false, 50, PlayOrigin.Live),
            new Play("u2", 8, null, new DateTime(2026, 11, 10), 0, true, null, PlayOrigin.LastPlayed),
            new Play("u3", 9, null, new DateTime(2026, 11, 9), 30, false, 100, PlayOrigin.PlaybackReporting),
        };
        s.Data.UpdatePlays(_ => new PlayLog(plays, new ImportMarker { RunId = "r1", Source = SeedSource.PlaybackReporting }));
        var back = s.Data.LoadPlays().Value!;
        Assert.Equal(3, back.Plays.Count);
        Assert.Equal("u1", back.Plays[0].UserId);
        Assert.Equal(70, back.Plays[0].EpisodeId);
        Assert.Equal(new DateTime(2026, 11, 14), back.Plays[0].DayUk);
        Assert.Equal(42.5, back.Plays[0].Minutes);
        Assert.Equal(50, back.Plays[0].RuntimeMinutes);
        Assert.Equal(PlayOrigin.LastPlayed, back.Plays[1].Origin);
        Assert.True(back.Plays[1].MarkedPlayed);
        Assert.Null(back.Plays[1].RuntimeMinutes);
        Assert.Equal(PlayOrigin.PlaybackReporting, back.Plays[2].Origin);
        Assert.Equal("r1", back.Marker!.RunId);
        Assert.Equal(SeedSource.PlaybackReporting, back.Marker.Source);
        var text = Sandbox.Text(s.Data.PlaysPath);
        Assert.Contains("\"dayUk\": \"2026-11-14\"", text);
        Assert.Contains("\"origin\": \"playbackReporting\"", text);
        Assert.Contains("\"importMarker\":", text);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_run_json_saves_and_loads_every_report_field()
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(r =>
        {
            r.RunId = "run-1";
            r.StartedUtc = Sandbox.Now;
            r.RankTimeUtc = Sandbox.RankTime;
            r.Step = RunStep.Check;
            r.Status = RunStatus.Failed;
            r.FailReason = new FailReasonRecord { Reason = "error", Message = "boom" };
            r.ResumeCount = 2;
            r.Picks.Add(new RunPick { Rank = 1, SourceId = 9, Kind = TitleKind.Series, Folder = "S (2001)", Reason = PickReason.Pinned, ViewerDays = 4, CopyApplied = true, DateApplied = true, ArtworkFailed = true, EpisodesSkipped = 3 });
            r.MissingPins.Add(77);
            r.PlaysLast7Days = 12;
            r.SeedSource = SeedSource.LastPlayed;
            r.ImportIgnored = 5;
            r.RefusedDeletes.Add(new RefusedDelete { Folder = "..", Reason = "folder name is . or .." });
            r.RefusedDeletes.Add(new RefusedDelete { Folder = "A", Path = "..\\x", Reason = "path part is . or .." });
            r.UnmanagedFolders.Add("Stray");
            r.RowCheck = new RowCheckRecord { Passed = false, UserId = "u9", FellBackToAdmin = true, Returned = { "1", "3" }, Extras = { "Stray" }, Retried = true };
            r.PluginVersion = "1.0.0";
            r.EmbyVersion = "4.10.2.0";
            r.TimeZone = "GMT Standard Time";
            r.Message = "hello";
            r.LastSuccessfulRunUtc = Sandbox.Now.AddDays(-7);
            r.LastRefusal = new RefusalRecord { Time = Sandbox.Now, Reason = Refusal.LibrarySetup, Detail = "turn on the reader" };
            r.Notices.Add(new Notice { Time = Sandbox.Now, Kind = "playsDamaged", Detail = "renamed" });
        });

        var r = s.Data.LoadRun().Value!;
        Assert.Equal("run-1", r.RunId);
        Assert.Equal(Sandbox.Now, r.StartedUtc);
        Assert.Equal(Sandbox.RankTime, r.RankTimeUtc);
        Assert.Equal(RunStep.Check, r.Step);
        Assert.Equal(RunStatus.Failed, r.Status);
        Assert.Equal("boom", r.FailReason!.Message);
        Assert.Equal(2, r.ResumeCount);
        var p = Assert.Single(r.Picks);
        Assert.Equal((1, 9L, TitleKind.Series, "S (2001)", PickReason.Pinned, 4), (p.Rank, p.SourceId, p.Kind, p.Folder, p.Reason, p.ViewerDays));
        Assert.True(p.CopyApplied && p.DateApplied && p.ArtworkFailed);
        Assert.Equal(3, p.EpisodesSkipped);
        Assert.Equal(new long[] { 77 }, r.MissingPins);
        Assert.Equal(12, r.PlaysLast7Days);
        Assert.Equal(SeedSource.LastPlayed, r.SeedSource);
        Assert.Equal(5, r.ImportIgnored);
        Assert.Equal(2, r.RefusedDeletes.Count);
        Assert.Null(r.RefusedDeletes[0].Path);
        Assert.Equal("..\\x", r.RefusedDeletes[1].Path);
        Assert.Equal(new[] { "Stray" }, r.UnmanagedFolders);
        Assert.True(r.RowCheck!.FellBackToAdmin);
        Assert.Equal("u9", r.RowCheck.UserId);
        Assert.Equal(new[] { "1", "3" }, r.RowCheck.Returned);
        Assert.Equal(new[] { "Stray" }, r.RowCheck.Extras);
        Assert.True(r.RowCheck.Retried);
        Assert.False(r.RowCheck.Passed);
        Assert.Equal("4.10.2.0", r.EmbyVersion);
        Assert.Equal("GMT Standard Time", r.TimeZone);
        Assert.Equal("1.0.0", r.PluginVersion);
        Assert.Equal("hello", r.Message);
        Assert.Equal(Sandbox.Now.AddDays(-7), r.LastSuccessfulRunUtc);
        Assert.Equal(Refusal.LibrarySetup, r.LastRefusal!.Reason);
        Assert.Equal("turn on the reader", r.LastRefusal.Detail);
        Assert.Equal("playsDamaged", Assert.Single(r.Notices).Kind);
        Assert.Equal(new RunSnapshot(RunStatus.Failed, RunStep.Check, 2).ResumeCount, r.Snapshot()!.ResumeCount);

        var text = Sandbox.Text(s.Data.RunPath);
        foreach (var name in new[] { "runId", "startedUtc", "rankTimeUtc", "step", "status", "failReason", "resumeCount", "picks", "copyApplied",
                     "dateApplied", "artworkFailed", "episodesSkipped", "missingPins", "playsLast7Days", "seedSource", "importIgnored", "refusedDeletes",
                     "unmanagedFolders", "rowCheck", "fellBackToAdmin", "extras", "retried", "pluginVersion", "embyVersion", "timeZone", "message",
                     "lastSuccessfulRunUtc", "lastRefusal", "detail", "notices" })
            Assert.Contains("\"" + name + "\":", text);
        Assert.Contains("\"reason\": \"librarySetup\"", text);
        Assert.Contains("\"step\": \"check\"", text);
    }

    [Fact, Trait("Check", "C48")]
    public void C48_state_json_holds_only_the_four_flag_fields_and_saving_it_changes_no_other_file()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new[] { Sample() });
        s.Data.UpdatePlays(l => l);
        s.Data.UpdateRun(r => r.RunId = "x");
        var before = s.Snapshot();

        s.Data.UpdateState(st => { st.TakeoverDone = true; st.PlaybackReportingImported = true; st.ListeningSinceUtc = Sandbox.Now; });

        var after = s.Snapshot();
        foreach (var k in before.Keys)
            Assert.Equal(before[k], after[k]);
        using var doc = JsonDocument.Parse(Sandbox.Text(s.Data.StatePath));
        Assert.Equal(new[] { "version", "takeoverDone", "playbackReportingImported", "listeningSinceUtc" },
            doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "ListeningSinceUtc", "PlaybackReportingImported", "TakeoverDone", "Version" },
            typeof(PluginState).GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
    }

    // ---- Atomic save (D24) ----

    [Fact, Trait("Check", "C36")]
    public void C36_atomic_save_a_stop_before_the_rename_leaves_the_old_file_whole_and_no_temp_file_behind()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new[] { Sample() });
        var before = File.ReadAllBytes(s.Data.ManagedPath);

        Sandbox.StopBeforeSaving(DataFolder.ManagedFileName);
        Assert.Throws<SimulatedStop>(() => s.Data.SaveManaged(new ManagedEntry[0]));
        Sandbox.NoStop();

        Assert.Equal(before, File.ReadAllBytes(s.Data.ManagedPath));
        Assert.Single(s.Managed());
        Assert.Empty(Directory.GetFiles(s.DataRoot, "*" + SafePaths.TempSuffix));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_atomic_save_replaces_the_whole_file_in_one_step()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(Enumerable.Range(1, 50).Select(i => new ManagedEntry("F" + i, i, TitleKind.Movie, EntryState.Active, null, null, false)));
        s.Data.SaveManaged(new[] { Sample() });
        Assert.Single(s.Managed());
        Assert.Equal(new[] { "managed.json" }, Directory.GetFiles(s.DataRoot).Select(f => Path.GetFileName(f)));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_file_written_into_a_copy_is_atomic_too()
    {
        using var s = new Sandbox();
        s.Staged("New (2020)", "New (2020).nfo");
        var path = Path.Combine(s.Staging, "New (2020)", "New (2020).nfo");
        Sandbox.StopBeforeSaving("New (2020).nfo");
        Assert.Throws<SimulatedStop>(() => s.Copies.WriteStagingText("New (2020)", "New (2020).nfo", "changed"));
        Sandbox.NoStop();
        Assert.Equal("New (2020)/New (2020).nfo/new", Sandbox.Text(path));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!));
    }

    // ---- Damaged files (D32, C37, C40) ----

    [Theory, Trait("Check", "C37")]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"entries\": []}")]
    [InlineData("{\"version\": 0, \"entries\": []}")]
    [InlineData("{\"version\": \"1\", \"entries\": []}")]
    [InlineData("{\"version\": 1, \"entries\": [{\"folder\": \"A\", \"kind\": \"film\"}]}")]
    [InlineData("{\"version\": 1, \"entries\": [{\"folder\": \"A\", \"rankDateUtc\": \"2026-11-15 05:00\"}]}")]
    [InlineData("{\"version\": 1, \"entries\": [null]}")]
    [InlineData("{\"version\": 1, \"entries\": null}")]
    [InlineData("{\"version\": 1, \"entries\": [], ")]
    public void C37_a_damaged_managed_json_is_reported_never_replaced_and_runs_refuse_with_data_damaged(string text)
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.ManagedPath, text);
        s.Folder("A", "A.strm");
        var before = s.Snapshot();

        var loaded = s.Data.LoadManaged();
        Assert.Equal(FileStatus.Damaged, loaded.Status);
        Assert.False(loaded.Readable);
        Assert.NotNull(loaded.Problem);

        var health = s.Data.Inspect();
        var guard = RunRules.Guards(health.ToGuardFacts(false, false, true, true, false));
        Assert.Equal(Refusal.DataDamaged, guard.Reason);

        // Nothing may change managed.json, delete a folder, or rename it.
        Assert.Throws<DamagedFileException>(() => s.Data.SaveManaged(new ManagedEntry[0]));
        Assert.Throws<DamagedFileException>(() => s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFolder, "A") }));
        Assert.Empty(s.Data.SetAsideDamaged(Sandbox.Now, Sandbox.Sunday));
        Assert.Equal(before, s.Snapshot());
    }

    [Fact, Trait("Check", "C37")]
    public void C37_a_damaged_plays_json_is_renamed_with_the_day_and_a_new_log_started_with_a_notice()
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(_ => { });
        Sandbox.Write(s.Data.PlaysPath, "{ broken");

        var renamed = s.Data.SetAsideDamaged(Sandbox.Now, Sandbox.Sunday);

        Assert.Equal(new[] { "plays.damaged-2026-11-15.json" }, renamed);
        Assert.Equal("{ broken", Sandbox.Text(s.DataFile("plays.damaged-2026-11-15.json")));
        Assert.Equal(FileStatus.Missing, s.Data.LoadPlays().Status);
        s.Data.AppendPlay(new Play("u", 1, null, Sandbox.Sunday, 10, true, 20, PlayOrigin.Live));
        Assert.Single(s.Data.LoadPlays().Value!.Plays);
        var notice = Assert.Single(s.Data.LoadRun().Value!.Notices);
        Assert.Equal("playsDamaged", notice.Kind);
        Assert.Contains("plays.damaged-2026-11-15.json", notice.Detail);
    }

    [Fact, Trait("Check", "C37")]
    public void C37_a_second_damaged_plays_json_the_same_day_gets_a_number()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.DataFile("plays.damaged-2026-11-15.json"), "first");
        Sandbox.Write(s.Data.PlaysPath, "second");
        Assert.Equal(new[] { "plays.damaged-2026-11-15 - 2.json" }, s.Data.SetAsideDamaged(Sandbox.Now, Sandbox.Sunday));
        Assert.Equal("first", Sandbox.Text(s.DataFile("plays.damaged-2026-11-15.json")));
        Assert.Equal("second", Sandbox.Text(s.DataFile("plays.damaged-2026-11-15 - 2.json")));
    }

    [Fact, Trait("Check", "C37")]
    public void C37_a_damaged_plays_json_is_never_written_over_before_it_is_set_aside()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.PlaysPath, "{ broken");
        Assert.Throws<DamagedFileException>(() => s.Data.AppendPlay(new Play("u", 1, null, Sandbox.Sunday, 1, true, 1, PlayOrigin.Live)));
        Assert.Equal("{ broken", Sandbox.Text(s.Data.PlaysPath));
    }

    [Fact, Trait("Check", "C40")]
    public void C40_a_damaged_run_json_is_renamed_and_the_next_run_starts_normally()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.RunPath, "{\"version\": 1, \"status\": \"sideways\"}");
        Assert.Equal(FileStatus.Damaged, s.Data.LoadRun().Status);
        Assert.Throws<DamagedFileException>(() => s.Data.StartRun("r2", Sandbox.Now, RunStep.Leftovers, null, null, null));

        Assert.Equal(new[] { "run.damaged-2026-11-15.json" }, s.Data.SetAsideDamaged(Sandbox.Now, Sandbox.Sunday));

        // "No unfinished run" (D32): nothing to resume, and a new run starts.
        Assert.Null(s.Data.LoadRun().Value!.Snapshot());
        var r = s.Data.StartRun("r2", Sandbox.Now, RunStep.Leftovers, "1.0.0", "4.10.2.0", "GMT");
        Assert.Equal(RunStatus.Running, r.Status);
        Assert.Equal("r2", s.Data.LoadRun().Value!.RunId);
        Assert.Equal("runDamaged", Assert.Single(s.Data.LoadRun().Value!.Notices).Kind);
        Assert.True(File.Exists(s.DataFile("run.damaged-2026-11-15.json")));
    }

    // ---- The run record (D31, D94, D118) ----

    [Fact, Trait("Check", "C79")]
    public void C79_B15_a_new_run_carries_lastSuccessfulRunUtc_lastRefusal_and_notices_forward_and_nothing_else()
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(r =>
        {
            r.RunId = "old";
            r.Status = RunStatus.Finished;
            r.Picks.Add(new RunPick { Rank = 1, Folder = "X" });
            r.RefusedDeletes.Add(new RefusedDelete { Folder = "..", Reason = "x" });
            r.LastSuccessfulRunUtc = Sandbox.Now.AddDays(-7);
            r.LastRefusal = new RefusalRecord { Time = Sandbox.Now.AddDays(-1), Reason = Refusal.LibraryMissing };
            r.Notices.Add(new Notice { Time = Sandbox.Now.AddDays(-2), Kind = "playsDamaged", Detail = "d" });
        });

        var started = s.Data.StartRun("new", Sandbox.Now, RunStep.Leftovers, "1.0.0", "4.10.2.0", "GMT");

        var r = s.Data.LoadRun().Value!;
        Assert.Equal("new", r.RunId);
        Assert.Equal(Sandbox.RankTime, r.RankTimeUtc);
        Assert.Equal(0, r.ResumeCount);
        Assert.Empty(r.Picks);
        Assert.Empty(r.RefusedDeletes);
        Assert.Equal(Sandbox.Now.AddDays(-7), r.LastSuccessfulRunUtc);
        Assert.Equal(Refusal.LibraryMissing, r.LastRefusal!.Reason);
        Assert.Single(r.Notices);
        Assert.Equal(RunStatus.Running, started.Status);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_B15_lastSuccessfulRunUtc_is_the_finish_time_of_a_finished_or_incomplete_run_and_a_failed_run_keeps_the_old_one()
    {
        using var s = new Sandbox();
        s.Data.StartRun("a", Sandbox.Now, RunStep.Leftovers, null, null, null);
        s.Data.FinishRun(RunStatus.Incomplete, Sandbox.Now.AddMinutes(40));
        Assert.Equal(Sandbox.Now.AddMinutes(40), s.Data.LoadRun().Value!.LastSuccessfulRunUtc);
        Assert.Equal(RunStep.Done, s.Data.LoadRun().Value!.Step);

        s.Data.StartRun("b", Sandbox.Now.AddDays(7), RunStep.Leftovers, null, null, null);
        s.Data.FailRun("disk full");
        var r = s.Data.LoadRun().Value!;
        Assert.Equal(RunStatus.Failed, r.Status);
        Assert.Equal("error", r.FailReason!.Reason);
        Assert.Equal("disk full", r.FailReason.Message);
        Assert.Equal(Sandbox.Now.AddMinutes(40), r.LastSuccessfulRunUtc);

        s.Data.StartRun("c", Sandbox.Now.AddDays(14), RunStep.Leftovers, null, null, null);
        s.Data.FinishRun(RunStatus.Finished, Sandbox.Now.AddDays(14).AddMinutes(20));
        Assert.Equal(Sandbox.Now.AddDays(14).AddMinutes(20), s.Data.LoadRun().Value!.LastSuccessfulRunUtc);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_notices_from_before_the_run_are_cleared_when_it_reaches_done_and_newer_ones_stay()
    {
        using var s = new Sandbox();
        s.Data.AddNotice("playsDamaged", "old", Sandbox.Now.AddDays(-1));
        s.Data.StartRun("a", Sandbox.Now, RunStep.Leftovers, null, null, null);
        s.Data.AddNotice("stateRebuilt", "this run", Sandbox.Now.AddSeconds(5));
        s.Data.FinishRun(RunStatus.Finished, Sandbox.Now.AddMinutes(30));
        Assert.Equal(new[] { "stateRebuilt" }, s.Data.LoadRun().Value!.Notices.Select(n => n.Kind));
    }

    [Fact, Trait("Check", "C79")]
    public void C79_refused_deletes_are_added_once_each()
    {
        using var s = new Sandbox();
        var x = new RefusedDelete { Folder = "..", Reason = "r" };
        s.Data.AddRefusedDeletes(new[] { x });
        s.Data.AddRefusedDeletes(new[] { new RefusedDelete { Folder = "..", Reason = "r" }, new RefusedDelete { Folder = "a", Path = "b", Reason = "r" } });
        Assert.Equal(2, s.Data.LoadRun().Value!.RefusedDeletes.Count);
        Assert.Equal("..", RefusedDelete.From(new RefusedPath("..", "p", "q")).Folder);
    }

    [Fact, Trait("Check", "C24")]
    public void C24_trim_removes_plays_older_than_90_days_and_keeps_the_import_marker()
    {
        using var s = new Sandbox();
        s.Data.UpdatePlays(_ => new PlayLog(new[]
        {
            new Play("u", 1, null, Sandbox.Sunday.AddDays(-91), 10, true, 20, PlayOrigin.Live),
            new Play("u", 2, null, Sandbox.Sunday.AddDays(-90), 10, true, 20, PlayOrigin.Live),
            new Play("u", 3, null, Sandbox.Sunday, 10, true, 20, PlayOrigin.Live),
        }, new ImportMarker { RunId = "r0", Source = SeedSource.None }));
        s.Data.TrimPlays(Sandbox.Sunday);
        var log = s.Data.LoadPlays().Value!;
        Assert.Equal(new long[] { 2, 3 }, log.Plays.Select(p => p.SourceId));
        Assert.Equal("r0", log.Marker!.RunId);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_times_read_back_as_utc_and_a_time_without_Z_is_damaged()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.RunPath, "{\"version\": 1, \"startedUtc\": \"2026-11-15T05:00:30.5Z\"}");
        Assert.Equal(new DateTime(2026, 11, 15, 5, 0, 30, 500, DateTimeKind.Utc), s.Data.LoadRun().Value!.StartedUtc);
        Assert.Equal(DateTimeKind.Utc, s.Data.LoadRun().Value!.StartedUtc!.Value.Kind);
        Sandbox.Write(s.Data.RunPath, "{\"version\": 1, \"startedUtc\": \"2026-11-15T05:00:30\"}");
        Assert.Equal(FileStatus.Damaged, s.Data.LoadRun().Status);
        Sandbox.Write(s.Data.RunPath, "{\"version\": 1, \"startedUtc\": \"2026-11-15T05:00:30+01:00\"}");
        Assert.Equal(FileStatus.Damaged, s.Data.LoadRun().Status);
    }

    [Fact, Trait("Check", "C79")]
    public void C79_a_local_time_is_saved_as_utc()
    {
        using var s = new Sandbox();
        var local = Sandbox.Now.ToLocalTime();
        s.Data.UpdateState(st => st.ListeningSinceUtc = local);
        Assert.Equal(Sandbox.Now, s.Data.LoadState().Value!.ListeningSinceUtc);
    }

    [Theory, Trait("Check", "C37")]
    [InlineData("{\"version\": 1, \"plays\": [{\"sourceId\": 1, \"dayUk\": \"2026-11-15\"}]}")]
    [InlineData("{\"version\": 1, \"plays\": [{\"userId\": \"u\", \"sourceId\": 1, \"dayUk\": \"15/11/2026\"}]}")]
    [InlineData("{\"version\": 1, \"plays\": [], \"importMarker\": {\"source\": \"none\"}}")]
    [InlineData("{\"version\": 1, \"plays\": [{\"userId\": \"u\", \"sourceId\": 1, \"dayUk\": \"2026-11-15\", \"origin\": 2}]}")]
    [InlineData("{\"version\": 1, \"plays\": [null]}")]
    public void C37_a_plays_json_with_a_bad_row_is_damaged(string text)
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.PlaysPath, text);
        Assert.Equal(FileStatus.Damaged, s.Data.LoadPlays().Status);
    }

    [Fact, Trait("Check", "C37")]
    public void C37_invalid_utf8_is_damaged()
    {
        using var s = new Sandbox();
        File.WriteAllBytes(s.Data.ManagedPath, new byte[] { (byte)'{', 0xFF, 0xFE, (byte)'}' });
        Assert.Equal(FileStatus.Damaged, s.Data.LoadManaged().Status);
    }

    [Fact, Trait("Check", "C37")]
    public void C37_an_entry_with_an_unsafe_folder_name_is_kept_as_written_so_it_can_be_refused_and_reported()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.ManagedPath, "{\"version\": 1, \"entries\": [{\"folder\": \"..\", \"kind\": \"movie\", \"state\": \"leaving\"}, {\"kind\": \"movie\", \"state\": \"leaving\"}]}");
        var m = s.Managed();
        Assert.Equal(new[] { "..", "" }, m.Select(e => e.Folder));
    }

    [Fact, Trait("Check", "C79")]
    public void C79_unknown_extra_fields_in_a_version_1_file_are_ignored()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.StatePath, "{\"version\": 1, \"takeoverDone\": true, \"later\": 5}");
        Assert.True(s.Data.LoadState().Value!.TakeoverDone);
    }
}
