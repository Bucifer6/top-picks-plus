using System;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;
using static TopPicksPlus.Tests.Logic.Make;

namespace TopPicksPlus.Tests.Logic;

/// <summary>The run and resume rules (F1, F4, F5, D35, D39, D41, D44, D45, D68, D78, D80, D84, D90, D94).</summary>
public class RunRulesTests
{
    private static readonly DateTime Now = new DateTime(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc);

    private static RunSnapshot Waiting(RunStep step = RunStep.Swap, int resumes = 1) => new RunSnapshot(RunStatus.Running, step, resumes);

    // ---- Start-up and the missed run ----

    [Fact, Trait("Check", "C26")]
    public void C26_last_run_over_7_days_ago_starts_a_run_at_10_minutes_and_a_resume_skips_it()
    {
        var plan = RunRules.Startup(false, false, Now.AddDays(-7).AddMinutes(-1), true, Now);
        Assert.Equal(new[] { RunRules.StartupAction.OrderGuardAtTwoMinutes, RunRules.StartupAction.MissedRunAtTenMinutes }, plan);
        Assert.Equal(TimeSpan.FromMinutes(10), RunRules.MissedRunDelay);

        var withResume = RunRules.Startup(false, true, Now.AddDays(-30), true, Now);
        Assert.Equal(new[] { RunRules.StartupAction.ResumeAtTwoMinutes }, withResume);
        Assert.Equal(TimeSpan.FromMinutes(2), RunRules.StartupResumeDelay);
    }

    [Fact, Trait("Check", "C26")]
    public void C26_last_run_within_7_days_does_not_start_a_run()
    {
        Assert.False(RunRules.MissedRunDue(Now.AddDays(-7), true, Now));
        Assert.False(RunRules.MissedRunDue(Now.AddDays(-1), true, Now));
        Assert.True(RunRules.MissedRunDue(Now.AddDays(-7).AddSeconds(-1), true, Now));
        Assert.Equal(new[] { RunRules.StartupAction.OrderGuardAtTwoMinutes }, RunRules.Startup(false, false, Now.AddDays(-2), true, Now));
    }

    [Fact, Trait("Check", "C26")]
    public void C26_missed_run_trigger_starts_a_new_run()
    {
        var d = RunRules.OnTrigger(Trigger.MissedRun, null, Ok());
        Assert.Equal(TriggerAction.StartNew, d.Action);
    }

    [Fact, Trait("Check", "C55")]
    public void C55_first_install_with_no_last_run_does_not_start_a_run()
    {
        Assert.False(RunRules.MissedRunDue(null, false, Now));
        Assert.Equal(new[] { RunRules.StartupAction.OrderGuardAtTwoMinutes }, RunRules.Startup(false, false, null, false, Now));
    }

    [Fact, Trait("Check", "C92")]
    public void C92_damaged_run_json_with_takeover_done_starts_the_missed_run_and_without_it_does_not()
    {
        Assert.True(RunRules.MissedRunDue(null, true, Now));
        Assert.Contains(RunRules.StartupAction.MissedRunAtTenMinutes, RunRules.Startup(false, false, null, true, Now));
        Assert.DoesNotContain(RunRules.StartupAction.MissedRunAtTenMinutes, RunRules.Startup(false, false, null, false, Now));
    }

    [Fact, Trait("Check", "C96")]
    public void C96_startup_with_old_plugin_and_an_unfinished_run_does_nothing_then_resumes_without_counting_the_wait()
    {
        Assert.Equal(new[] { RunRules.StartupAction.ReportOldPluginOnly }, RunRules.Startup(true, true, Now.AddDays(-30), true, Now));
        var allowed = EmbyRules.Allowed(oldPluginLoaded: true, dataNewer: false, managedReadable: true);
        Assert.False(allowed.WriteDataFiles);
        Assert.False(allowed.OrderGuard);

        var run = Waiting(RunStep.Rescan, 1);
        var refused = RunRules.OnTrigger(Trigger.StartupResume, run, new GuardFacts(oldPluginLoaded: true));
        Assert.Equal(TriggerAction.Refuse, refused.Action);
        Assert.Equal(1, refused.ResumeCount);

        Assert.Equal(new[] { RunRules.StartupAction.ResumeAtTwoMinutes }, RunRules.Startup(false, true, Now.AddDays(-30), true, Now));
        var resumed = RunRules.OnTrigger(Trigger.StartupResume, run, Ok());
        Assert.Equal(TriggerAction.Resume, resumed.Action);
        Assert.Equal(2, resumed.ResumeCount);
    }

    // ---- Guards and refusals ----

    [Fact, Trait("Check", "C28")]
    public void C28_no_library_refuses_with_library_missing_and_plans_nothing()
    {
        var d = RunRules.OnTrigger(Trigger.RunNow, null, new GuardFacts(libraryFound: false));
        Assert.Equal(TriggerAction.Refuse, d.Action);
        var refusal = d.Refusal!;
        Assert.Equal(Refusal.LibraryMissing, refusal.Reason);
        Assert.True(refusal.WriteLastRefusal);
    }

    [Fact, Trait("Check", "C61")]
    public void C61_nfo_reader_off_or_saver_on_refuses_library_setup_with_which_setting()
    {
        var off = RunRules.Guards(new GuardFacts(nfoReaderOn: false));
        Assert.Equal(Refusal.LibrarySetup, off.Reason);
        Assert.Contains("turn on", off.Detail);
        var saver = RunRules.Guards(new GuardFacts(nfoSaverOn: true));
        Assert.Equal(Refusal.LibrarySetup, saver.Reason);
        Assert.Contains("turn off", saver.Detail);
        Assert.False(RunRules.Guards(Ok()).Refused);
    }

    [Fact, Trait("Check", "C94")]
    public void C94_state_damaged_refuses_only_when_it_cannot_be_rebuilt()
    {
        var alone = RunRules.Guards(new GuardFacts(stateReadable: false));
        Assert.False(alone.Refused);
        Assert.True(alone.RebuildState);

        var withPlays = RunRules.Guards(new GuardFacts(stateReadable: false, playsReadable: false));
        Assert.Equal(Refusal.StateDamaged, withPlays.Reason);
        var withManaged = RunRules.Guards(new GuardFacts(stateReadable: false, managedReadable: false));
        Assert.Equal(Refusal.StateDamaged, withManaged.Reason);
        Assert.False(withManaged.RebuildState);
        Assert.False(RunRules.Guards(Ok()).RebuildState);
        Assert.False(EmbyRules.ShouldSaveListeningSince(false, false, stateReadable: false));
    }

    [Fact, Trait("Check", "C37")]
    public void Damaged_managed_json_refuses_with_data_damaged_and_says_to_delete_it()
    {
        var g = RunRules.Guards(new GuardFacts(managedReadable: false));
        Assert.Equal(Refusal.DataDamaged, g.Reason);
        Assert.Contains("delete", g.Detail);
    }

    [Fact, Trait("Check", "C83")]
    public void C83_data_newer_refuses_writes_nothing_and_wins_over_every_other_guard()
    {
        var all = new GuardFacts(lockHeld: true, dataNewer: true, oldPluginLoaded: true, libraryFound: false, nfoReaderOn: false,
            managedReadable: false, stateReadable: false, playsReadable: false, newerFile: "plays.json");
        var g = RunRules.Guards(all);
        Assert.Equal(Refusal.DataNewer, g.Reason);
        Assert.False(g.WriteLastRefusal);
        Assert.Contains("plays.json", g.Detail);

        var a = EmbyRules.Allowed(false, dataNewer: true, managedReadable: true);
        Assert.False(a.LogPlays);
        Assert.False(a.SyncWatched);
        Assert.False(a.OrderGuard);
        Assert.False(a.Import);
        Assert.False(a.WriteDataFiles);
    }

    [Fact, Trait("Check", "C95")]
    public void C95_guard_refusal_at_resume_leaves_the_run_unchanged_then_resumes_from_swap_with_count_2()
    {
        var run = Waiting(RunStep.Swap, 1);
        var refused = RunRules.OnTrigger(Trigger.Sunday, run, new GuardFacts(libraryFound: false));
        Assert.Equal(TriggerAction.Refuse, refused.Action);
        Assert.Equal(1, refused.ResumeCount);
        Assert.True(refused.Refusal!.WriteLastRefusal);

        foreach (var t in new[] { Trigger.Sunday, Trigger.RunNow, Trigger.StartupResume })
        {
            var d = RunRules.OnTrigger(t, run, Ok());
            Assert.Equal(TriggerAction.Resume, d.Action);
            Assert.Equal(2, d.ResumeCount);
        }
        Assert.False(RunRules.ChoosesPicks(RunStep.Swap, picksSaved: true));

        var newer = RunRules.OnTrigger(Trigger.Sunday, run, new GuardFacts(dataNewer: true));
        Assert.Equal(TriggerAction.Refuse, newer.Action);
        Assert.False(newer.Refusal!.WriteLastRefusal);
        Assert.Equal(1, newer.ResumeCount);
    }

    [Fact, Trait("Check", "C73")]
    public void C73_sunday_trigger_resumes_an_unfinished_run_and_a_refusal_keeps_it_waiting()
    {
        var run = Waiting(RunStep.Dates, 0);
        var d = RunRules.OnTrigger(Trigger.Sunday, run, Ok());
        Assert.Equal(TriggerAction.Resume, d.Action);
        Assert.Equal(1, d.ResumeCount);

        var refused = RunRules.OnTrigger(Trigger.Sunday, run, new GuardFacts(oldPluginLoaded: true));
        Assert.Equal(TriggerAction.Refuse, refused.Action);
        Assert.NotEqual(TriggerAction.MarkFailed, refused.Action);
        Assert.Equal(0, refused.ResumeCount);
    }

    [Fact, Trait("Check", "C22")]
    public void Already_running_is_refused_and_the_working_run_carries_on()
    {
        var d = RunRules.OnTrigger(Trigger.RunNow, Waiting(), new GuardFacts(lockHeld: true));
        Assert.Equal(TriggerAction.Refuse, d.Action);
        Assert.Equal(Refusal.AlreadyRunning, d.Refusal!.Reason);
        Assert.Equal(1, d.ResumeCount);
    }

    [Fact, Trait("Check", "C59")]
    public void Retry_or_startup_slot_with_no_unfinished_run_does_nothing()
    {
        Assert.Equal(TriggerAction.Nothing, RunRules.OnTrigger(Trigger.Retry, null, Ok()).Action);
        Assert.Equal(TriggerAction.Nothing, RunRules.OnTrigger(Trigger.StartupResume, new RunSnapshot(RunStatus.Finished, RunStep.Done, 0), Ok()).Action);
        Assert.Equal(TriggerAction.StartNew, RunRules.OnTrigger(Trigger.Sunday, new RunSnapshot(RunStatus.Failed, RunStep.Swap, 3), Ok()).Action);
    }

    // ---- Errors, retries and resume counting ----

    [Fact, Trait("Check", "C59")]
    public void C59_an_error_is_retried_5_minutes_later_at_most_3_times_then_failed()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), RunRules.RetryDelay);
        var count = 0;
        var tries = 1;
        while (RunRules.OnAttemptStopped(count) == RunRules.AfterStop.PlanResume)
        {
            var d = RunRules.OnTrigger(Trigger.Retry, Waiting(RunStep.Rescan, count), Ok());
            Assert.Equal(TriggerAction.Resume, d.Action);
            count = d.ResumeCount;
            tries++;
        }
        Assert.Equal(3, count);
        Assert.Equal(4, tries);
        Assert.Equal(TriggerAction.MarkFailed, RunRules.OnTrigger(Trigger.StartupResume, Waiting(RunStep.Rescan, 3), Ok()).Action);
    }

    [Fact, Trait("Check", "C59")]
    public void C59_order_guard_runs_while_no_run_holds_the_lock_even_if_run_json_says_running()
    {
        var ok = EmbyRules.Allowed(false, false, true);
        Assert.True(RunRules.OrderGuardMayRun(lockHeld: false, ok));
        Assert.False(RunRules.OrderGuardMayRun(lockHeld: true, ok));
        Assert.False(RunRules.OrderGuardMayRun(false, EmbyRules.Allowed(false, false, managedReadable: false)));
    }

    [Fact, Trait("Check", "C77")]
    public void C77_an_error_then_its_resume_adds_exactly_1()
    {
        var run = Waiting(RunStep.Swap, 0);
        Assert.Equal(RunRules.AfterStop.PlanResume, RunRules.OnAttemptStopped(run.ResumeCount));
        // The error itself changes nothing; only the resume that follows adds 1.
        var d = RunRules.OnTrigger(Trigger.Retry, run, Ok());
        Assert.Equal(1, d.ResumeCount);
        // A restart counts the same way.
        Assert.Equal(2, RunRules.OnTrigger(Trigger.StartupResume, Waiting(RunStep.Swap, 1), Ok()).ResumeCount);
        // A resume refused by a guard adds nothing.
        Assert.Equal(1, RunRules.OnTrigger(Trigger.Retry, Waiting(RunStep.Swap, 1), new GuardFacts(managedReadable: false)).ResumeCount);
    }

    [Theory, Trait("Check", "C77")]
    [InlineData(0, RunRules.AfterStop.PlanResume)]
    [InlineData(1, RunRules.AfterStop.PlanResume)]
    [InlineData(2, RunRules.AfterStop.PlanResume)]
    [InlineData(3, RunRules.AfterStop.Fail)]
    [InlineData(4, RunRules.AfterStop.Fail)]
    public void C77_after_the_first_attempt_and_3_resumes_the_run_fails(int resumes, RunRules.AfterStop expected) =>
        Assert.Equal(expected, RunRules.OnAttemptStopped(resumes));

    [Fact, Trait("Check", "C97")]
    public void C97_a_problem_mid_step_is_an_error_and_fails_only_with_an_error_reason()
    {
        // Mid-step problems never go through Guards; they stop the attempt.
        Assert.Equal(RunRules.AfterStop.PlanResume, RunRules.OnAttemptStopped(0));
        // The retry's guards then refuse, so the run waits unchanged.
        var waiting = RunRules.OnTrigger(Trigger.Retry, Waiting(RunStep.Rescan, 0), new GuardFacts(libraryFound: false));
        Assert.Equal(TriggerAction.Refuse, waiting.Action);
        Assert.Equal(0, waiting.ResumeCount);
        // When resumes run out, the reason is "error" plus the message, never a refusal reason.
        Assert.Equal(RunRules.AfterStop.Fail, RunRules.OnAttemptStopped(3));
        Assert.Equal("error: library vanished during rescan", RunRules.FailReason("library vanished during rescan"));
        Assert.Equal("error", RunRules.FailReason(null));
        foreach (var r in Enum.GetNames(typeof(Refusal)))
            Assert.DoesNotContain(r, RunRules.FailReason("x"), StringComparison.OrdinalIgnoreCase);
    }

    // ---- Status, rescan timeout and the row-check retry ----

    [Fact, Trait("Check", "C29")]
    public void C29_rescan_timeout_is_incomplete_and_still_counts_as_successful()
    {
        var s = RunRules.FinalStatus(failed: false, rescanTimedOut: true);
        Assert.Equal(RunStatus.Incomplete, s);
        Assert.True(RunRules.CountsAsSuccess(s));
        Assert.True(RunRules.CountsAsSuccess(RunStatus.Finished));
        Assert.False(RunRules.CountsAsSuccess(RunStatus.Failed));
        Assert.False(RunRules.CountsAsSuccess(RunStatus.Running));
    }

    [Fact, Trait("Check", "C29")]
    public void C29_order_guard_later_sets_the_dates_of_copies_that_appear()
    {
        var rankDate = new DateTime(2026, 11, 15, 3, 0, 0, DateTimeKind.Utc);
        var late = Entry("Late (2020)", 9, EntryState.Active, 3, rankDate: rankDate);
        Assert.Single(SwapPlan.Guarded(new[] { late }));
        Assert.Equal(new long[] { 77 }, RunRules_ItemsToReset(rankDate, (77, new DateTime(2026, 11, 15, 5, 20, 0))));
    }

    private static long[] RunRules_ItemsToReset(DateTime rankDate, params (long, DateTime)[] items) =>
        RankTimes.ItemsToReset(rankDate, items).ToArray();

    [Fact, Trait("Check", "C82")]
    public void C82_rescan_timeout_wins_over_a_failed_row_check_with_no_retry()
    {
        Assert.Equal(RunStatus.Incomplete, RunRules.FinalStatus(false, true));
        Assert.False(RunRules.RetryRowCheck(passed: false, alreadyRetried: false, rescanTimedOut: true));
        Assert.Equal(RunStatus.Failed, RunRules.FinalStatus(true, true));
        Assert.Equal(RunStatus.Finished, RunRules.FinalStatus(false, false));
    }

    [Fact, Trait("Check", "C60")]
    public void C60_failed_row_check_is_retried_once_then_the_run_still_finishes()
    {
        Assert.True(RunRules.RetryRowCheck(passed: false, alreadyRetried: false, rescanTimedOut: false));
        Assert.False(RunRules.RetryRowCheck(passed: false, alreadyRetried: true, rescanTimedOut: false));
        Assert.False(RunRules.RetryRowCheck(passed: true, alreadyRetried: false, rescanTimedOut: false));
        Assert.Equal(RunStatus.Finished, RunRules.FinalStatus(failed: false, rescanTimedOut: false));
    }

    // ---- The order of a run ----

    [Fact, Trait("Check", "C100")]
    public void C100_B40_takeover_is_needed_until_done_and_again_when_managed_json_is_missing()
    {
        Assert.True(RunRules.TakeoverNeeded(takeoverDone: false, managedMissing: false));
        Assert.True(RunRules.TakeoverNeeded(takeoverDone: false, managedMissing: true));
        Assert.True(RunRules.TakeoverNeeded(takeoverDone: true, managedMissing: true));
        Assert.False(RunRules.TakeoverNeeded(takeoverDone: true, managedMissing: false));
        Assert.Equal(RunStep.Takeover, RunRules.FirstStep(true, !RunRules.TakeoverNeeded(true, true)));
        Assert.Equal(RunStep.Takeover, RunRules.NextStep(RunStep.Import, !RunRules.TakeoverNeeded(true, true)));
    }

    [Fact, Trait("Check", "C36")]
    public void Run_order_is_import_takeover_leftovers_prepare_then_the_swap_steps()
    {
        Assert.Equal(RunStep.Import, RunRules.FirstStep(false, false));
        Assert.Equal(RunStep.Takeover, RunRules.FirstStep(true, false));
        Assert.Equal(RunStep.Leftovers, RunRules.FirstStep(true, true));
        var order = new System.Collections.Generic.List<RunStep> { RunStep.Import };
        while (order.Last() != RunStep.Done)
            order.Add(RunRules.NextStep(order.Last(), takeoverDone: false));
        Assert.Equal(new[] { RunStep.Import, RunStep.Takeover, RunStep.Leftovers, RunStep.Prepare, RunStep.Swap, RunStep.Rescan,
            RunStep.Dates, RunStep.Artwork, RunStep.Check, RunStep.Record, RunStep.Done }, order);
        Assert.Equal(RunStep.Leftovers, RunRules.NextStep(RunStep.Import, takeoverDone: true));
        Assert.True(RunRules.ChoosesPicks(RunStep.Prepare, picksSaved: false));
        Assert.False(RunRules.ChoosesPicks(RunStep.Prepare, picksSaved: true));
        Assert.False(RunRules.ChoosesPicks(RunStep.Rescan, picksSaved: false));
    }

    [Fact, Trait("Check", "C58")]
    public void Any_import_marker_means_no_second_import()
    {
        Assert.False(RunRules.ShouldImport(anyImportMarker: true));
        Assert.True(RunRules.ShouldImport(anyImportMarker: false));
    }

    [Fact, Trait("Check", "C94")]
    public void Every_refusal_has_a_detail_saying_what_to_do()
    {
        var cases = new[]
        {
            new GuardFacts(lockHeld: true), new GuardFacts(dataNewer: true), new GuardFacts(oldPluginLoaded: true),
            new GuardFacts(libraryFound: false), new GuardFacts(nfoReaderOn: false), new GuardFacts(nfoSaverOn: true),
            new GuardFacts(managedReadable: false), new GuardFacts(stateReadable: false, playsReadable: false),
        };
        var expected = new[] { Refusal.AlreadyRunning, Refusal.DataNewer, Refusal.OldPluginLoaded, Refusal.LibraryMissing,
            Refusal.LibrarySetup, Refusal.LibrarySetup, Refusal.DataDamaged, Refusal.StateDamaged };
        for (var i = 0; i < cases.Length; i++)
        {
            var g = RunRules.Guards(cases[i]);
            Assert.Equal(expected[i], g.Reason);
            Assert.False(string.IsNullOrWhiteSpace(g.Detail));
            Assert.False(g.RebuildState);
            Assert.Equal(expected[i] != Refusal.DataNewer, g.WriteLastRefusal);
        }
        Assert.Contains("a data file", RunRules.Guards(new GuardFacts(dataNewer: true)).Detail);
        Assert.Contains("old Top Picks", RunRules.Guards(new GuardFacts(oldPluginLoaded: true)).Detail);
        Assert.False(RunRules.Guards(Ok()).WriteLastRefusal);
        Assert.Null(RunRules.Guards(Ok()).Detail);
    }
}
