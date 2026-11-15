using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// Starts, resumes, refuses and retries runs (F1, D35, D39, D44, D68, D78, D80, D94). One run at a time: the lock is in memory and
/// held only while a run is working (D44). Every trigger goes through <see cref="RunRules.OnTrigger"/>; this class only carries
/// out its answer.
/// </summary>
public static class RunEngine
{
    private static readonly SemaphoreSlim RunLock = new SemaphoreSlim(1, 1);
    private static readonly object TimerGate = new object();
    private static Timer? retryTimer;

    /// <summary>True while a run is working (D44). The order guard pauses only then.</summary>
    public static bool LockHeld => RunLock.CurrentCount == 0;

    /// <summary>
    /// A refusal that couldn't be written (data newer, D85; or run.json unreadable) or the old-plugin message held at start-up
    /// (D94b, B29). Shown on the settings page; cleared when a run starts.
    /// </summary>
    public static string? HeldMessage { get; internal set; }

    /// <summary>Asks for a run. Never throws: a run's errors go through the retry rule (D44, D78).</summary>
    public static async Task Request(Host h, Trigger trigger, CancellationToken cancellationToken)
    {
        var got = await RunLock.WaitAsync(0).ConfigureAwait(false);
        try
        {
            await Decide(h, trigger, got, cancellationToken).ConfigureAwait(false);
        }
        catch (DataNewerException e)
        {
            // D85: nothing is written, not even the error; it is kept in memory and logged.
            HeldMessage = e.Message;
            h.Log.Error("Top Picks Plus: {0}", e.Message);
        }
        catch (Exception e)
        {
            AfterError(h, e);
        }
        finally
        {
            if (got)
                RunLock.Release();
        }
    }

    private static async Task Decide(Host h, Trigger trigger, bool got, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var library = EmbyFacts.FindTopPicksLibrary(h);
        var facts = EmbyFacts.GuardFacts(h, h.Data.Inspect(), !got, EmbyFacts.OldPluginLoaded(h), library);
        var decision = RunRules.OnTrigger(trigger, h.Data.LoadRun().Value?.Snapshot(), facts);
        switch (decision.Action)
        {
            case TriggerAction.Nothing:
                return;
            case TriggerAction.Refuse:
                Refuse(h, decision.Refusal!, now);
                return;
        }

        // Past the guards (only a refusal comes back without the lock).
        HeldMessage = null;
        h.Data.SetAsideDamaged(now, EmbyFacts.UkDay(now));
        if (h.Data.RebuildState(now) == RebuildOutcome.Refused)
        {
            Refuse(h, new GuardResult(Refusal.StateDamaged, "state.json can't be read and can't be rebuilt", false), now);
            return;
        }

        switch (decision.Action)
        {
            case TriggerAction.MarkFailed:
                h.Data.FailRun("the run stopped before finishing 4 times");
                h.Log.Error("Top Picks Plus: the unfinished run used its 3 resumes and is marked failed (D39)");
                return;
            case TriggerAction.StartNew:
                StartNew(h, now);
                break;
            default:
                h.Data.SaveResumeCount(decision.ResumeCount);
                h.Log.Info("Top Picks Plus: resuming the unfinished run (resume {0} of {1})", decision.ResumeCount, RunRules.MaxResumes);
                break;
        }

        await new RunSteps(h, library!).Run(ct).ConfigureAwait(false);
    }

    private static void StartNew(Host h, DateTime now)
    {
        var state = h.Data.LoadState().Value ?? new PluginState();
        var first = RunRules.FirstStep(state.PlaybackReportingImported, !h.Data.TakeoverNeeded());
        var (plugin, emby, zone) = EmbyFacts.Versions(h);
        h.Data.StartRun(Guid.NewGuid().ToString("N"), now, first, plugin, emby, zone);
        // D26: staging is emptied only when a new run starts.
        var problem = h.Copies.EmptyStaging();
        if (problem != null)
            throw new IOException("staging can't be emptied: " + problem);
        h.Log.Info("Top Picks Plus: new run started at step {0}", first);
    }

    /// <summary>D94: a refusal writes only <c>lastRefusal</c> (never for data newer), or is held in memory if it can't be written.</summary>
    private static void Refuse(Host h, GuardResult refusal, DateTime now)
    {
        var written = refusal.WriteLastRefusal && h.Data.RecordRefusal(refusal.Reason!.Value, refusal.Detail, now);
        if (!written && refusal.Reason != Refusal.AlreadyRunning)
            HeldMessage = refusal.Detail;
        h.Log.Info("Top Picks Plus: run refused ({0}): {1}", refusal.Reason, refusal.Detail);
    }

    /// <summary>D44, D78: an error never changes <c>resumeCount</c>; a resume is planned 5 minutes later, or the run is failed.</summary>
    private static void AfterError(Host h, Exception e)
    {
        h.Log.ErrorException("Top Picks Plus: the run stopped with an error", e);
        try
        {
            var run = h.Data.LoadRun().Value;
            if (run == null || run.Snapshot()?.Unfinished != true)
                return;
            if (RunRules.OnAttemptStopped(run.ResumeCount) == RunRules.AfterStop.PlanResume)
                PlanRetry(h);
            else
                h.Data.FailRun(e.Message);
        }
        catch (Exception inner)
        {
            h.Log.ErrorException("Top Picks Plus: couldn't plan the retry", inner);
        }
    }

    private static void PlanRetry(Host h)
    {
        lock (TimerGate)
        {
            retryTimer?.Dispose();
            retryTimer = new Timer(_ => Task.Run(() => Request(h, Trigger.Retry, CancellationToken.None)), null, RunRules.RetryDelay, Timeout.InfiniteTimeSpan);
        }
        h.Log.Info("Top Picks Plus: the run will resume in {0} minutes", RunRules.RetryDelay.TotalMinutes);
    }

    /// <summary>Stops the retry timer when Emby shuts down.</summary>
    internal static void StopTimers()
    {
        lock (TimerGate)
        {
            retryTimer?.Dispose();
            retryTimer = null;
        }
    }
}
