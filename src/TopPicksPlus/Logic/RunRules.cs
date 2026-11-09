using System;
using System.Collections.Generic;

namespace TopPicksPlus.Logic;

/// <summary>Refusal reasons, a fixed list (D31, D53, D72, D93).</summary>
public enum Refusal
{
    AlreadyRunning,
    DataNewer,
    OldPluginLoaded,
    LibraryMissing,
    LibrarySetup,
    DataDamaged,
    StateDamaged,
}

/// <summary>The saved step of a run (data.md run.json `step`, D80).</summary>
public enum RunStep
{
    Import,
    Takeover,
    Leftovers,
    Prepare,
    Swap,
    Rescan,
    Dates,
    Artwork,
    Check,
    Record,
    Done,
}

/// <summary>How a run ended (D84): failed beats incomplete, which beats finished.</summary>
public enum RunStatus
{
    Running,
    Finished,
    Incomplete,
    Failed,
}

/// <summary>What asked for a run (F1, D44, D68, D90).</summary>
public enum Trigger
{
    Sunday,
    RunNow,
    StartupResume,
    MissedRun,
    Retry,
}

/// <summary>What a trigger leads to.</summary>
public enum TriggerAction
{
    /// <summary>Nothing to do (e.g. the start-up resume slot with no unfinished run).</summary>
    Nothing,
    /// <summary>A guard refused: write only <c>lastRefusal</c> (or, for data newer, nothing at all); the waiting run is unchanged (D94).</summary>
    Refuse,
    /// <summary>Start a new run (new picks, new rank time).</summary>
    StartNew,
    /// <summary>Resume the unfinished run at its saved step, with the same picks and rank time; <c>resumeCount</c> goes up by 1 (D78).</summary>
    Resume,
    /// <summary>The unfinished run has used its 3 resumes: mark it failed (D39).</summary>
    MarkFailed,
}

/// <summary>What the guards see at the start of a run or resume (D80, D94). Plain facts EmbyLink and Storage gather.</summary>
public sealed class GuardFacts
{
    public GuardFacts(
        bool lockHeld = false,
        bool dataNewer = false,
        bool oldPluginLoaded = false,
        bool libraryFound = true,
        bool nfoReaderOn = true,
        bool nfoSaverOn = false,
        bool managedReadable = true,
        bool stateReadable = true,
        bool playsReadable = true,
        string? newerFile = null)
    {
        LockHeld = lockHeld;
        DataNewer = dataNewer;
        OldPluginLoaded = oldPluginLoaded;
        LibraryFound = libraryFound;
        NfoReaderOn = nfoReaderOn;
        NfoSaverOn = nfoSaverOn;
        ManagedReadable = managedReadable;
        StateReadable = stateReadable;
        PlaysReadable = playsReadable;
        NewerFile = newerFile;
    }

    public bool LockHeld { get; }
    public bool DataNewer { get; }
    public bool OldPluginLoaded { get; }
    public bool LibraryFound { get; }
    public bool NfoReaderOn { get; }
    public bool NfoSaverOn { get; }
    public bool ManagedReadable { get; }
    public bool StateReadable { get; }
    public bool PlaysReadable { get; }
    public string? NewerFile { get; }
}

/// <summary>A guard's answer: no refusal, or the reason with its detail and whether <c>lastRefusal</c> may be written.</summary>
public sealed class GuardResult
{
    public GuardResult(Refusal? reason, string? detail, bool rebuildState)
    {
        Reason = reason;
        Detail = detail;
        RebuildState = rebuildState;
    }

    public Refusal? Reason { get; }
    public string? Detail { get; }

    /// <summary>state.json is unreadable but can be rebuilt from the other files (D95); the run carries on after Storage does it.</summary>
    public bool RebuildState { get; }

    public bool Refused => Reason.HasValue;

    /// <summary>"Data newer" writes nothing at all, not even <c>lastRefusal</c> (D85, D94); every other refusal writes only that.</summary>
    public bool WriteLastRefusal => Reason.HasValue && Reason.Value != Refusal.DataNewer;
}

/// <summary>The unfinished-run part of run.json the trigger rules need.</summary>
public sealed class RunSnapshot
{
    public RunSnapshot(RunStatus status, RunStep step, int resumeCount)
    {
        Status = status;
        Step = step;
        ResumeCount = resumeCount;
    }

    public RunStatus Status { get; }
    public RunStep Step { get; }
    public int ResumeCount { get; }

    /// <summary>A run marked running with no lock held is unfinished, never "in progress" (D44).</summary>
    public bool Unfinished => Status == RunStatus.Running;
}

/// <summary>A trigger's decision: the action, the refusal (if any), and the resume count to save.</summary>
public sealed class TriggerDecision
{
    public TriggerDecision(TriggerAction action, GuardResult? refusal, int resumeCount)
    {
        Action = action;
        Refusal = refusal;
        ResumeCount = resumeCount;
    }

    public TriggerAction Action { get; }
    public GuardResult? Refusal { get; }

    /// <summary>The <c>resumeCount</c> run.json holds after this decision (unchanged unless the action is Resume).</summary>
    public int ResumeCount { get; }
}

/// <summary>
/// The run and resume rules (F1, D35, D39, D41, D44, D45, D68, D78, D80, D84, D90, D94).
/// Every function takes plain values; EmbyLink carries out the answer (D79).
/// </summary>
public static class RunRules
{
    /// <summary>A run gets the first attempt plus this many resumes (D68, D78).</summary>
    public const int MaxResumes = 3;

    /// <summary>A resume after an error with Emby still up waits this long (D44).</summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    /// <summary>The start-up resume and the order guard start this long after start-up (D90).</summary>
    public static readonly TimeSpan StartupResumeDelay = TimeSpan.FromMinutes(2);

    /// <summary>The missed run starts this long after start-up (F1).</summary>
    public static readonly TimeSpan MissedRunDelay = TimeSpan.FromMinutes(10);

    /// <summary>The missed-run rule fires when the last successful run is more than this long ago (F1).</summary>
    public static readonly TimeSpan MissedRunAge = TimeSpan.FromDays(7);

    /// <summary>
    /// The guards, in a fixed order (D80, D94, D116). "Data newer" first, because it is the only refusal that writes nothing, so it
    /// must win over any that would write <c>lastRefusal</c> (D85); then "already running" (the working run carries on); then old plugin, library missing, library setup, then state.json damaged with managed.json or plays.json also unreadable
    /// (<c>stateDamaged</c>, D95, C98), then managed.json damaged (<c>dataDamaged</c>). A damaged state.json alone is rebuilt (D95).
    /// </summary>
    public static GuardResult Guards(GuardFacts f)
    {
        if (f.DataNewer)
            return Refuse(Refusal.DataNewer, (f.NewerFile ?? "a data file") + " was written by a newer Top Picks Plus; put that version back");
        if (f.LockHeld)
            return Refuse(Refusal.AlreadyRunning, "a run is already in progress");
        if (f.OldPluginLoaded)
            return Refuse(Refusal.OldPluginLoaded, "uninstall the old Top Picks plugin first");
        if (!f.LibraryFound)
            return Refuse(Refusal.LibraryMissing, "no library uses the Top Picks folder");
        if (!f.NfoReaderOn)
            return Refuse(Refusal.LibrarySetup, "turn on the Nfo metadata reader for the Top Picks library");
        if (f.NfoSaverOn)
            return Refuse(Refusal.LibrarySetup, "turn off the Nfo metadata saver for the Top Picks library");
        if (!f.StateReadable && (!f.ManagedReadable || !f.PlaysReadable))
            return Refuse(Refusal.StateDamaged, "state.json can't be read and can't be rebuilt while managed.json or plays.json is also unreadable");
        if (!f.ManagedReadable)
            return Refuse(Refusal.DataDamaged, "managed.json can't be read; delete it and the next run adopts the copies again (D95)");
        return new GuardResult(null, null, rebuildState: !f.StateReadable);
    }

    private static GuardResult Refuse(Refusal r, string detail) => new GuardResult(r, detail, rebuildState: false);

    /// <summary>
    /// What a trigger does (D44, D68, D78, D94). Guards run first, before <c>resumeCount</c> changes; a refusal leaves the waiting
    /// run exactly as it is. With an unfinished run, any trigger resumes it (same picks), or marks it failed if its 3 resumes are
    /// used. With none, Sunday, Run now and the missed run start a new run; the start-up resume slot and a retry do nothing.
    /// </summary>
    public static TriggerDecision OnTrigger(Trigger trigger, RunSnapshot? run, GuardFacts facts)
    {
        var count = run?.ResumeCount ?? 0;
        var guard = Guards(facts);
        if (guard.Refused)
            return new TriggerDecision(TriggerAction.Refuse, guard, count);

        if (run != null && run.Unfinished)
        {
            if (run.ResumeCount >= MaxResumes)
                return new TriggerDecision(TriggerAction.MarkFailed, null, count);
            return new TriggerDecision(TriggerAction.Resume, null, count + 1);
        }

        if (trigger == Trigger.StartupResume || trigger == Trigger.Retry)
            return new TriggerDecision(TriggerAction.Nothing, null, count);
        return new TriggerDecision(TriggerAction.StartNew, null, count);
    }

    /// <summary>What happens when an attempt stops without reaching done (D78): an error never changes <c>resumeCount</c>.</summary>
    public enum AfterStop
    {
        /// <summary>Resume 5 minutes later (Emby still up) or at start-up.</summary>
        PlanResume,
        /// <summary>The resumes are used up: the run is failed, with <c>failReason</c> = error plus the message (D94c).</summary>
        Fail,
    }

    /// <summary>D78: below 3 resumes started, a resume is planned; at 3, the run fails.</summary>
    public static AfterStop OnAttemptStopped(int resumeCount) =>
        resumeCount <= MaxResumes ? AfterStop.PlanResume : AfterStop.Fail;

    /// <summary>The <c>failReason</c> text for a failed run (D81, D94): always "error" plus the message, never a refusal reason.</summary>
    public static string FailReason(string? message) =>
        string.IsNullOrWhiteSpace(message) ? "error" : "error: " + message;

    /// <summary>What start-up does, in order (D35, D51, D90, D94b).</summary>
    public enum StartupAction
    {
        /// <summary>Old plugin loaded: nothing else, nothing written; the message is held in memory.</summary>
        ReportOldPluginOnly,
        ResumeAtTwoMinutes,
        OrderGuardAtTwoMinutes,
        MissedRunAtTenMinutes,
    }

    /// <summary>
    /// The start-up plan (D35, D90, D94b): old plugin loaded → only the report; else an unfinished run is resumed at 2 minutes
    /// (and the missed-run rule is skipped, B24/D111); else the order guard at 2 minutes, then the missed run at 10 minutes if
    /// <see cref="MissedRunDue"/>.
    /// </summary>
    public static IReadOnlyList<StartupAction> Startup(bool oldPluginLoaded, bool hasUnfinishedRun, DateTime? lastSuccessfulRunUtc, bool takeoverDone, DateTime nowUtc)
    {
        if (oldPluginLoaded)
            return new[] { StartupAction.ReportOldPluginOnly };
        if (hasUnfinishedRun)
            return new[] { StartupAction.ResumeAtTwoMinutes };
        var list = new List<StartupAction> { StartupAction.OrderGuardAtTwoMinutes };
        if (MissedRunDue(lastSuccessfulRunUtc, takeoverDone, nowUtc))
            list.Add(StartupAction.MissedRunAtTenMinutes);
        return list;
    }

    /// <summary>
    /// The missed-run rule (F1, D41, D90): with a last successful run, it fires when that was more than 7 days ago. With none, it
    /// fires only if <c>takeoverDone</c> is true (the plugin has run before, so run.json was lost), never after a first install.
    /// </summary>
    public static bool MissedRunDue(DateTime? lastSuccessfulRunUtc, bool takeoverDone, DateTime nowUtc)
    {
        if (!lastSuccessfulRunUtc.HasValue)
            return false;
        return nowUtc - lastSuccessfulRunUtc.Value > MissedRunAge;
    }

    /// <summary>Which status wins (D84): failed, then incomplete (the rescan wait ran out), then finished.</summary>
    public static RunStatus FinalStatus(bool failed, bool rescanTimedOut)
    {
        if (failed)
            return RunStatus.Failed;
        return rescanTimedOut ? RunStatus.Incomplete : RunStatus.Finished;
    }

    /// <summary>A run counts as successful if it finished or ended incomplete (D31).</summary>
    public static bool CountsAsSuccess(RunStatus status) => status == RunStatus.Finished || status == RunStatus.Incomplete;

    /// <summary>
    /// The row-check retry (D45, D84): only when the ranked part failed, it hasn't been retried, and the rescan wait didn't run out.
    /// </summary>
    public static bool RetryRowCheck(bool passed, bool alreadyRetried, bool rescanTimedOut) =>
        !passed && !alreadyRetried && !rescanTimedOut;

    /// <summary>
    /// The first step of a new run (D80): import while not yet imported (and no import marker exists, D96), then takeover while
    /// not done, then leftovers.
    /// </summary>
    public static RunStep FirstStep(bool playbackReportingImported, bool takeoverDone) =>
        !playbackReportingImported ? RunStep.Import : !takeoverDone ? RunStep.Takeover : RunStep.Leftovers;

    /// <summary>The step after <paramref name="step"/> (D80). Takeover is skipped when already done.</summary>
    public static RunStep NextStep(RunStep step, bool takeoverDone)
    {
        switch (step)
        {
            case RunStep.Import: return takeoverDone ? RunStep.Leftovers : RunStep.Takeover;
            case RunStep.Takeover: return RunStep.Leftovers;
            case RunStep.Leftovers: return RunStep.Prepare;
            case RunStep.Prepare: return RunStep.Swap;
            case RunStep.Swap: return RunStep.Rescan;
            case RunStep.Rescan: return RunStep.Dates;
            case RunStep.Dates: return RunStep.Artwork;
            case RunStep.Artwork: return RunStep.Check;
            case RunStep.Check: return RunStep.Record;
            default: return RunStep.Done;
        }
    }

    /// <summary>
    /// A resume repeats its saved step (D26, D80) and never re-chooses: picks and the rank time are chosen once, when prepare
    /// starts, and saved in run.json. They are chosen only if the step is at or before prepare and none are saved yet.
    /// </summary>
    public static bool ChoosesPicks(RunStep step, bool picksSaved) => step <= RunStep.Prepare && !picksSaved;

    /// <summary>The order guard runs whenever no run holds the lock, even if run.json says running (D44, C59), and only while allowed (D114).</summary>
    public static bool OrderGuardMayRun(bool lockHeld, Activity allowed) => !lockHeld && allowed.OrderGuard;

    /// <summary>The import step (D93, D96): import only if plays.json holds no import marker at all; otherwise just set the flag.</summary>
    public static bool ShouldImport(bool anyImportMarker) => !anyImportMarker;
}
