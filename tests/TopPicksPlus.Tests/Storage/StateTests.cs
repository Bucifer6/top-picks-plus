using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>
/// state.json, the import step and takeover order (D38, D80, D91, D93, D95, D96, C48, C78, C91, C94, C98, C99, C100).
/// </summary>
public class StateTests
{
    private static readonly IReadOnlyDictionary<long, long> NoSeries = new Dictionary<long, long>();

    private static IReadOnlyDictionary<string, long?> NoMatch(FolderListing f) =>
        f.Files.Where(x => x.EndsWith(".strm", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x, _ => (long?)null);

    private static void Marker(Sandbox s, string runId, params Play[] plays) =>
        s.Data.UpdatePlays(_ => new PlayLog(plays, new ImportMarker { RunId = runId, Source = SeedSource.PlaybackReporting }));

    // ---- C98, C94: a damaged state.json ----

    [Fact, Trait("Check", "C98")]
    public void C98_a_damaged_state_json_is_renamed_rebuilt_with_a_notice_and_the_run_carries_on()
    {
        using var s = new Sandbox();
        Marker(s, "old", new Play("u", 1, null, new DateTime(2026, 11, 10), 5, true, 9, PlayOrigin.PlaybackReporting),
            new Play("u", 2, null, new DateTime(2026, 11, 8), 5, true, 9, PlayOrigin.PlaybackReporting));
        s.Data.SaveManaged(new[] { new ManagedEntry("A", 1, TitleKind.Movie, EntryState.Active, 1, Sandbox.RankTime, false) });
        Sandbox.Write(s.Data.StatePath, "{ broken");

        var guard = RunRules.Guards(s.Data.Inspect().ToGuardFacts(false, false, true, true, false));
        Assert.False(guard.Refused);
        Assert.True(guard.RebuildState);

        Assert.Equal(RebuildOutcome.Rebuilt, s.Data.RebuildState(Sandbox.Now));

        var st = s.Data.LoadState();
        Assert.Equal(FileStatus.Ok, st.Status);
        Assert.True(st.Value!.PlaybackReportingImported);
        Assert.True(st.Value.TakeoverDone);
        // Earliest play day (2026-11-08), never later than that UK day's start (D121).
        Assert.Equal(new DateTime(2026, 11, 7, 23, 0, 0, DateTimeKind.Utc), st.Value.ListeningSinceUtc);
        var setAside = Assert.Single(Directory.GetFiles(s.DataRoot, "state.json.damaged-*"));
        Assert.Equal("state.json.damaged-20261115T050030Z", Path.GetFileName(setAside));
        Assert.Equal("{ broken", Sandbox.Text(setAside));
        Assert.Equal("stateRebuilt", Assert.Single(s.Data.LoadRun().Value!.Notices).Kind);
        Assert.Equal(RebuildOutcome.NotNeeded, s.Data.RebuildState(Sandbox.Now));
        Assert.False(RunRules.Guards(s.Data.Inspect().ToGuardFacts(false, false, true, true, false)).RebuildState);
    }

    [Fact, Trait("Check", "C98")]
    public void C98_rebuild_with_no_marker_no_entries_and_no_plays_is_not_imported_not_taken_over_and_listening_from_now()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.StatePath, "[]");
        Assert.Equal(RebuildOutcome.Rebuilt, s.Data.RebuildState(Sandbox.Now));
        var st = s.Data.LoadState().Value!;
        Assert.False(st.PlaybackReportingImported);
        Assert.False(st.TakeoverDone);
        Assert.Equal(Sandbox.Now, st.ListeningSinceUtc);
    }

    [Fact, Trait("Check", "C98")]
    public void C98_rebuild_counts_a_past_run_in_run_json_as_takeover_done()
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(r => r.LastSuccessfulRunUtc = Sandbox.Now.AddDays(-3));
        Sandbox.Write(s.Data.StatePath, "{ broken");
        s.Data.RebuildState(Sandbox.Now);
        Assert.True(s.Data.LoadState().Value!.TakeoverDone);
    }

    [Theory, Trait("Check", "C98")]
    [InlineData(DataFolder.ManagedFileName)]
    [InlineData(DataFolder.PlaysFileName)]
    public void C98_C94_with_managed_or_plays_also_unreadable_it_refuses_stateDamaged_and_nothing_is_renamed_or_written(string other)
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(r => r.RunId = "x");
        Sandbox.Write(s.Data.StatePath, "{ broken");
        Sandbox.Write(s.DataFile(other), "{ also broken");
        var before = s.Snapshot();

        var guard = RunRules.Guards(s.Data.Inspect().ToGuardFacts(false, false, true, true, false));
        Assert.Equal(Refusal.StateDamaged, guard.Reason);
        Assert.DoesNotContain("backup", guard.Detail!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RebuildOutcome.Refused, s.Data.RebuildState(Sandbox.Now));
        Assert.False(s.Data.SaveListeningSinceOnce(Sandbox.Now));
        Assert.Throws<DamagedFileException>(() => s.Data.UpdateState(st => st.TakeoverDone = true));
        Assert.Equal(before, s.Snapshot());
    }

    [Fact, Trait("Check", "C94")]
    public void C94_a_missing_state_json_is_not_damaged_all_flags_start_unset()
    {
        using var s = new Sandbox();
        Assert.Equal(FileStatus.Missing, s.Data.Inspect().State);
        Assert.Equal(RebuildOutcome.NotNeeded, s.Data.RebuildState(Sandbox.Now));
        Assert.False(RunRules.Guards(s.Data.Inspect().ToGuardFacts(false, false, true, true, false)).RebuildState);
        Assert.False(File.Exists(s.Data.StatePath));
    }

    // ---- C91: listeningSinceUtc once ----

    [Fact, Trait("Check", "C91")]
    public void C91_listeningSinceUtc_is_saved_once_and_a_later_save_leaves_it_unchanged()
    {
        using var s = new Sandbox();
        Assert.True(s.Data.SaveListeningSinceOnce(Sandbox.Now));
        Assert.False(s.Data.SaveListeningSinceOnce(Sandbox.Now.AddDays(3)));
        Assert.Equal(Sandbox.Now, s.Data.LoadState().Value!.ListeningSinceUtc);
    }

    [Fact, Trait("Check", "C91")]
    public void C91_listeningSinceUtc_is_not_saved_while_state_json_is_damaged()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.StatePath, "{ broken");
        Assert.False(s.Data.SaveListeningSinceOnce(Sandbox.Now));
        Assert.Equal("{ broken", Sandbox.Text(s.Data.StatePath));
    }

    // ---- C78, C99: the import step ----

    private static Play Imported(long id) => new Play("u", id, null, new DateTime(2026, 11, 12), 60, false, 100, PlayOrigin.PlaybackReporting);

    [Fact, Trait("Check", "C78")]
    public void C78_import_writes_rows_and_marker_in_one_save_then_sets_the_flag()
    {
        using var s = new Sandbox();
        s.Data.AppendPlay(new Play("live", 9, null, Sandbox.Sunday, 50, false, 100, PlayOrigin.Live));
        Assert.True(s.Data.ImportStep("r1", SeedSource.PlaybackReporting, new[] { Imported(1), Imported(2) }));
        var log = s.Data.LoadPlays().Value!;
        Assert.Equal(new long[] { 9, 1, 2 }, log.Plays.Select(p => p.SourceId));
        Assert.Equal("r1", log.Marker!.RunId);
        Assert.True(s.Data.LoadState().Value!.PlaybackReportingImported);
    }

    [Fact, Trait("Check", "C78")]
    public void C78_import_with_no_rows_still_saves_the_marker()
    {
        using var s = new Sandbox();
        Assert.True(s.Data.ImportStep("r1", SeedSource.None, new Play[0]));
        Assert.Equal(SeedSource.None, s.Data.LoadPlays().Value!.Marker!.Source);
    }

    [Fact, Trait("Check", "C78")]
    public void C78_stopped_after_plays_json_but_before_state_json_the_resume_imports_nothing_and_only_sets_the_flag()
    {
        using var s = new Sandbox();
        Sandbox.StopBeforeSaving(DataFolder.StateFileName);
        Assert.Throws<SimulatedStop>(() => s.Data.ImportStep("r1", SeedSource.PlaybackReporting, new[] { Imported(1), Imported(2) }));
        Sandbox.NoStop();
        Assert.Equal(2, s.Data.LoadPlays().Value!.Plays.Count);
        Assert.False(s.Data.LoadState().Value!.PlaybackReportingImported);

        Assert.False(s.Data.ImportStep("r1", SeedSource.PlaybackReporting, new[] { Imported(1), Imported(2) }));

        Assert.Equal(2, s.Data.LoadPlays().Value!.Plays.Count);
        Assert.True(s.Data.LoadState().Value!.PlaybackReportingImported);
    }

    [Fact, Trait("Check", "C78")]
    public void C78_stopped_before_plays_json_was_saved_the_resume_imports_once()
    {
        using var s = new Sandbox();
        Sandbox.StopBeforeSaving(DataFolder.PlaysFileName);
        Assert.Throws<SimulatedStop>(() => s.Data.ImportStep("r1", SeedSource.PlaybackReporting, new[] { Imported(1) }));
        Sandbox.NoStop();
        Assert.Empty(s.Data.LoadPlays().Value!.Plays);
        Assert.Null(s.Data.LoadPlays().Value!.Marker);
        Assert.False(File.Exists(s.Data.StatePath));

        Assert.True(s.Data.ImportStep("r1", SeedSource.PlaybackReporting, new[] { Imported(1) }));
        Assert.Single(s.Data.LoadPlays().Value!.Plays);
        Assert.True(s.Data.LoadState().Value!.PlaybackReportingImported);
    }

    [Fact, Trait("Check", "C99")]
    public void C99_a_marker_from_an_earlier_failed_run_means_the_new_run_imports_nothing_and_sets_the_flag()
    {
        using var s = new Sandbox();
        Marker(s, "failed-run", Imported(1));
        s.Data.UpdateState(st => st.PlaybackReportingImported = false);

        Assert.False(s.Data.ImportStep("new-run", SeedSource.PlaybackReporting, new[] { Imported(1), Imported(5) }));

        var log = s.Data.LoadPlays().Value!;
        Assert.Single(log.Plays);
        Assert.Equal("failed-run", log.Marker!.RunId);
        Assert.True(s.Data.LoadState().Value!.PlaybackReportingImported);
    }

    [Fact, Trait("Check", "C78")]
    public void C78_import_never_writes_over_a_damaged_plays_json()
    {
        using var s = new Sandbox();
        Sandbox.Write(s.Data.PlaysPath, "{ broken");
        Assert.Throws<DamagedFileException>(() => s.Data.ImportStep("r1", SeedSource.None, new Play[0]));
        Assert.False(File.Exists(s.Data.StatePath));
    }

    // ---- C48, C46, C100: takeover ----

    [Fact, Trait("Check", "C46")]
    public void C46_an_old_plugin_series_folder_with_strm_only_in_season_folders_is_adopted()
    {
        using var s = new Sandbox();
        s.Folder("Old Show", "tvshow.nfo", "Season 1\\Old Show - S01E01.strm", "Season 1\\Old Show - S01E01.nfo", "folder.png");
        s.Folder("Keep me", "note.txt");
        s.Folder("Stray strm", "x.strm");

        var adopted = s.Copies.TakeOver(f => f.Files.Where(x => x.EndsWith(".strm")).ToDictionary(x => x, _ => (long?)500), new Dictionary<long, long> { [500] = 50 });

        Assert.Equal(new[] { "Old Show" }, adopted);
        var e = s.Entry("Old Show");
        Assert.True(e.Adopted);
        Assert.Equal(EntryState.Active, e.State);
        Assert.Null(e.Rank);
        Assert.Equal(50, e.SourceId);
        Assert.Equal(TitleKind.Series, e.Kind);
        Assert.Equal(500, e.Items["Season 1\\Old Show - S01E01.strm"]);
        Assert.Equal(new[] { "folder.png", "Season 1\\Old Show - S01E01.nfo", "tvshow.nfo" }, e.Files);
        Assert.True(s.Data.LoadState().Value!.TakeoverDone);
        Assert.Equal(new[] { "Stray strm" }, s.Copies.UnmanagedFolders());
    }

    [Fact, Trait("Check", "C46")]
    public void C46_reading_a_strm_gives_the_path_and_never_reads_through_a_bad_name()
    {
        using var s = new Sandbox();
        s.Folder("Old", "Season 1\\a.strm");
        Sandbox.Write(s.InTopPicks("Old", "Season 1\\a.strm"), "\uFEFFN:\\Movies\\A.mkv\r\n");
        Sandbox.Write(Path.Combine(s.Outside, "x.strm"), "secret");
        Assert.Equal("N:\\Movies\\A.mkv", s.Copies.ReadStrm("Old", "Season 1\\a.strm"));
        Assert.Null(s.Copies.ReadStrm("Old", "..\\..\\..\\outside\\x.strm"));
        Assert.Null(s.Copies.ReadStrm("..", "outside\\x.strm"));
        Assert.Null(s.Copies.ReadStrm("Old", "missing.strm"));
    }

    [Fact, Trait("Check", "C48")]
    public void C48_takeover_run_twice_adopts_nothing_already_on_the_list()
    {
        using var s = new Sandbox();
        s.Folder("Old A", "a.strm", "a.nfo");
        Assert.Single(s.Copies.TakeOver(NoMatch, NoSeries));
        s.Data.UpdateState(st => st.TakeoverDone = false); // takeoverDone lost
        Assert.Empty(s.Copies.TakeOver(NoMatch, NoSeries));
        Assert.Single(s.Managed());
    }

    [Fact, Trait("Check", "C48")]
    public void C48_takeover_saves_managed_before_state_so_a_stop_between_leaves_takeoverDone_false_and_a_repeat_adopts_nothing_twice()
    {
        using var s = new Sandbox();
        s.Folder("Old A", "a.strm", "a.nfo");
        s.Folder("Old B", "Season 1\\b.strm", "tvshow.nfo");

        Sandbox.StopBeforeSaving(DataFolder.StateFileName);
        Assert.Throws<SimulatedStop>(() => s.Copies.TakeOver(NoMatch, NoSeries));
        Sandbox.NoStop();
        Assert.Equal(2, s.Managed().Count);
        Assert.False(s.Data.LoadState().Value!.TakeoverDone);

        Assert.Empty(s.Copies.TakeOver(NoMatch, NoSeries));
        Assert.Equal(new[] { "Old A", "Old B" }, s.Managed().Select(e => e.Folder));
        Assert.True(s.Data.LoadState().Value!.TakeoverDone);
    }

    [Fact, Trait("Check", "C48")]
    public void C48_takeover_stopped_before_managed_json_leaves_state_untouched()
    {
        using var s = new Sandbox();
        s.Folder("Old A", "a.strm", "a.nfo");
        Sandbox.StopBeforeSaving(DataFolder.ManagedFileName);
        Assert.Throws<SimulatedStop>(() => s.Copies.TakeOver(NoMatch, NoSeries));
        Sandbox.NoStop();
        Assert.False(File.Exists(s.Data.ManagedPath));
        Assert.False(File.Exists(s.Data.StatePath));
    }

    [Fact, Trait("Check", "C48")]
    public void C48_takeover_never_writes_over_a_damaged_managed_json()
    {
        using var s = new Sandbox();
        s.Folder("Old A", "a.strm", "a.nfo");
        Sandbox.Write(s.Data.ManagedPath, "{ broken");
        Assert.Throws<DamagedFileException>(() => s.Copies.TakeOver(NoMatch, NoSeries));
        Assert.Equal("{ broken", Sandbox.Text(s.Data.ManagedPath));
        Assert.False(File.Exists(s.Data.StatePath));
    }

    [SkippableFact, Trait("Check", "C48")]
    public void C48_takeover_never_looks_inside_a_junction()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "a.strm"));
        Sandbox.Write(Path.Combine(target, "a.nfo"));
        Links.Junction(Path.Combine(s.TopPicks, "Linked"), target);
        Assert.Empty(s.Copies.TakeOver(NoMatch, NoSeries));
        Assert.Empty(s.Copies.Listings().Single().Files);
    }

    [Fact, Trait("Check", "C100")]
    public void C100_managed_json_deleted_after_takeover_the_next_run_adopts_the_copies_again()
    {
        using var s = new Sandbox();
        s.Folder("Dune (2021)", "Dune (2021).strm", "Dune (2021).nfo", "folder.png");
        s.Folder("Show (2019)", "tvshow.nfo", "Season 1\\Show - S01E01 - Pilot.strm", "Season 1\\Show - S01E01 - Pilot.nfo");
        s.Folder("Keep me", "note.txt");
        s.Data.UpdateState(st => st.TakeoverDone = true);
        s.Data.UpdateRun(r => r.LastSuccessfulRunUtc = Sandbox.Now.AddDays(-7));
        // The owner deleted the damaged managed.json (D95).
        Assert.False(File.Exists(s.Data.ManagedPath));

        Assert.True(s.Data.TakeoverNeeded());
        var takeoverDone = !s.Data.TakeoverNeeded();
        Assert.Equal(RunStep.Takeover, RunRules.FirstStep(playbackReportingImported: true, takeoverDone));

        s.Copies.TakeOver(NoMatch, NoSeries);

        Assert.Equal(new[] { "Dune (2021)", "Show (2019)" }, s.Managed().Select(e => e.Folder));
        Assert.All(s.Managed(), e => Assert.True(e.Adopted));
        Assert.Empty(s.Copies.UnmanagedFolders());
        Assert.False(s.Data.TakeoverNeeded());
        Assert.Equal(RunStep.Leftovers, RunRules.FirstStep(true, !s.Data.TakeoverNeeded()));
        Assert.True(Directory.Exists(Path.Combine(s.TopPicks, "Keep me")));
    }

    [Fact, Trait("Check", "C100")]
    public void C100_an_empty_managed_json_is_not_missing_so_no_second_takeover()
    {
        using var s = new Sandbox();
        s.Data.UpdateState(st => st.TakeoverDone = true);
        s.Data.SaveManaged(new ManagedEntry[0]);
        Assert.False(s.Data.TakeoverNeeded());
    }

    [Fact, Trait("Check", "C100")]
    public void C100_first_install_needs_takeover()
    {
        using var s = new Sandbox();
        Assert.True(s.Data.TakeoverNeeded());
    }
}
