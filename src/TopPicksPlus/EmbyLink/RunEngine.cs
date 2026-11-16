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
    private static bool stopped;

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
        var attempt = new Attempt();
        try
        {
            await Decide(h, trigger, got, attempt, cancellationToken).ConfigureAwait(false);
        }
        catch (DataNewerException e)
        {
            // D85: nothing is written, not even the error; it is kept in memory and logged.
            HeldMessage = e.Message;
            h.Log.Error("Top Picks Plus: {0}", e.Message);
        }
        catch (OperationCanceledException)
        {
            // D147: cancelled in Scheduled Tasks or Emby is stopping: no retry; the run waits for the next trigger or start-up.
            h.Log.Info("Top Picks Plus: the run was cancelled; it carries on at the next trigger");
        }
        catch (Exception e)
        {
            // D147: only an attempt that really began (it holds the lock and saved its start or resume) counts as stopped (D78).
            if (got && attempt.Began)
                AfterError(h, e);
            else
                h.Log.ErrorException("Top Picks Plus: couldn't start the run; it is tried again at the next trigger", e);
        }
        finally
        {
            if (got)
                RunLock.Release();
        }
    }

    private static async Task Decide(Host h, Trigger trigger, bool got, Attempt attempt, CancellationToken ct)
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

        // Past the guards (only a refusal comes back without the lock). A planned retry is no longer needed (D147).
        HeldMessage = null;
        CancelRetry();
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
                attempt.Began = true;
                break;
            default:
                h.Data.SaveResumeCount(decision.ResumeCount);
                attempt.Began = true;
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
        // D26, D147: staging is emptied only when a new run starts, and before the run is saved, so a stop in between can never
        // leave a running run with last week's files in staging (which a move-in would carry into top-picks).
        var problem = h.Copies.EmptyStaging();
        if (problem != null)
            throw new IOException("staging can't be emptied: " + problem);
        h.Data.StartRun(Guid.NewGuid().ToString("N"), now, first, plugin, emby, zone);
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
            var resume = RunRules.OnAttemptStopped(run.ResumeCount) == RunRules.AfterStop.PlanResume;
            if (resume)
                PlanRetry(h);
            else
                h.Data.FailRun(e.Message);
            // D150: the reason goes into the report too, so the settings page says why without opening Emby's log. Written after
            // the retry or the fail, so a problem writing it can never stop either.
            h.Data.AddNotice("runError", "the run stopped at step " + run.Step + ": " + e.Message
                + (resume ? "; it tries again in " + RunRules.RetryDelay.TotalMinutes + " minutes" : "; that was its last try, so it is marked failed"),
                DateTime.UtcNow);
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
            if (stopped)
                return;
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
            stopped = true;
            retryTimer?.Dispose();
            retryTimer = null;
        }
    }

    private static void CancelRetry()
    {
        lock (TimerGate)
        {
            retryTimer?.Dispose();
            retryTimer = null;
        }
    }

    /// <summary>Whether this call's attempt began: it holds the lock and saved its start or its resume (D78, D147).</summary>
    private sealed class Attempt
    {
        public bool Began { get; set; }
    }
}
