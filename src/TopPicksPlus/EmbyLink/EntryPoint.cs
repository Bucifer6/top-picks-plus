using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;

namespace TopPicksPlus.EmbyLink;

/// <summary>What start-up found that stays true until Emby restarts.</summary>
public static class Startup
{
    /// <summary>D33, D51: the old plugin is loaded, so nothing else happens (no runs, plays, watched sync or order guard).</summary>
    public static bool OldPluginLoaded { get; internal set; }
}

/// <summary>
/// Start-up (F1, F5, D35, D90, D91, D94b): builds the <see cref="Host"/>, listens for plays, watched changes and library changes,
/// saves <c>listeningSinceUtc</c> once, and plans the start-up resume, the order guard and the missed run.
/// </summary>
public sealed class EntryPoint : IServerEntryPoint
{
    /// <summary>After a change in the Top Picks folder, the order guard waits this long for more changes before it runs (B10).</summary>
    private static readonly TimeSpan GuardSettle = TimeSpan.FromSeconds(60);

    private readonly Host _h;
    private readonly ISessionManager _sessions;
    private readonly List<Timer> _timers = new List<Timer>();
    private readonly object _guardGate = new object();
    private Timer? _guardTimer;

    public EntryPoint(
        IApplicationHost app,
        IApplicationPaths paths,
        ILibraryManager library,
        IUserManager users,
        IUserDataManager userData,
        IUserViewManager userViews,
        IProviderManager providers,
        IFileSystem fileSystem,
        ISessionManager sessions,
        ILogManager logManager)
    {
        _sessions = sessions;
        _h = new Host(app, paths, library, users, userData, userViews, providers, fileSystem, logManager.GetLogger("TopPicksPlus"));
    }

    public void Run()
    {
        Host.Current = _h;
        Startup.OldPluginLoaded = EmbyFacts.OldPluginLoaded(_h);
        if (Startup.OldPluginLoaded)
        {
            // D51, D94b, B29: nothing at all happens and nothing is written; the message is held in memory for the settings page.
            RunEngine.HeldMessage = "The old Top Picks plugin is installed. Uninstall it, then restart Emby (D51).";
            _h.Log.Warn("Top Picks Plus: {0}", RunEngine.HeldMessage);
            return;
        }

        _sessions.PlaybackStart += OnPlaybackStart;
        _sessions.PlaybackStopped += OnPlaybackStopped;
        _h.UserData.UserDataSaved += OnUserDataSaved;
        _h.Library.ItemAdded += OnItemChanged;
        _h.Library.ItemUpdated += OnItemChanged;

        try
        {
            Plan(DateTime.UtcNow);
        }
        catch (Exception e)
        {
            _h.Log.ErrorException("Top Picks Plus: start-up planning failed", e);
        }
    }

    private void Plan(DateTime now)
    {
        var state = _h.Data.LoadState();
        var health = _h.Data.Inspect();
        if (!health.DataNewer && EmbyRules.ShouldSaveListeningSince(false, state.Value?.ListeningSinceUtc.HasValue ?? false, state.Readable))
            _h.Data.SaveListeningSinceOnce(now);

        var run = _h.Data.LoadRun().Value;
        var unfinished = run?.Snapshot()?.Unfinished ?? false;
        var actions = RunRules.Startup(false, unfinished, run?.LastSuccessfulRunUtc, state.Value?.TakeoverDone ?? false, now);
        foreach (var action in actions)
        {
            switch (action)
            {
                case RunRules.StartupAction.ResumeAtTwoMinutes:
                    After(RunRules.StartupResumeDelay, () => RunEngine.Request(_h, Trigger.StartupResume, CancellationToken.None));
                    break;
                case RunRules.StartupAction.OrderGuardAtTwoMinutes:
                    After(RunRules.StartupResumeDelay, () => Task.Run(() => OrderGuard.Run(_h)));
                    break;
                case RunRules.StartupAction.MissedRunAtTenMinutes:
                    After(RunRules.MissedRunDelay, () => RunEngine.Request(_h, Trigger.MissedRun, CancellationToken.None));
                    break;
            }
        }
        _h.Log.Info("Top Picks Plus: started ({0})", string.Join(", ", actions));
    }

    private void After(TimeSpan delay, Func<Task> work)
    {
        lock (_timers)
            _timers.Add(new Timer(_ => Task.Run(work), null, delay, Timeout.InfiniteTimeSpan));
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e) => PlayLogger.OnPlaybackStart(e);

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e) => PlayLogger.OnPlaybackStopped(_h, e);

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        Watched.OnUserDataSaved(_h, e);
        Watched.Prune();
    }

    /// <summary>
    /// B10: library scans and metadata refreshes show up as items added or updated; a change to anything in the Top Picks folder
    /// (re)starts a short timer, and the order guard runs once things settle (F5). It skips while a run holds the lock.
    /// </summary>
    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        if (_h.RelativeToTopPicks(e.Item?.Path) == null)
            return;
        lock (_guardGate)
        {
            _guardTimer?.Dispose();
            _guardTimer = new Timer(_ => OrderGuard.Run(_h), null, GuardSettle, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        _sessions.PlaybackStart -= OnPlaybackStart;
        _sessions.PlaybackStopped -= OnPlaybackStopped;
        _h.UserData.UserDataSaved -= OnUserDataSaved;
        _h.Library.ItemAdded -= OnItemChanged;
        _h.Library.ItemUpdated -= OnItemChanged;
        lock (_timers)
        {
            foreach (var t in _timers)
                t.Dispose();
            _timers.Clear();
        }
        lock (_guardGate)
            _guardTimer?.Dispose();
        RunEngine.StopTimers();
    }
}
