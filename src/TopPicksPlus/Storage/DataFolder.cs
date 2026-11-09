using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Storage;

/// <summary>Which of the four files can be used (D72, D85, D93, D95). Gathered once; EmbyLink adds Emby's facts for the guards.</summary>
public sealed class DataHealth
{
    internal DataHealth(FileStatus managed, FileStatus plays, FileStatus run, FileStatus state, string? newerFile)
    {
        Managed = managed;
        Plays = plays;
        Run = run;
        State = state;
        NewerFile = newerFile;
    }

    public FileStatus Managed { get; }
    public FileStatus Plays { get; }
    public FileStatus Run { get; }
    public FileStatus State { get; }

    /// <summary>The first file (managed, plays, run, state) written by a newer Top Picks Plus, or none.</summary>
    public string? NewerFile { get; }

    public bool DataNewer => NewerFile != null;

    /// <summary>The guard facts from these files (D116), with the facts only EmbyLink can know passed in.</summary>
    public GuardFacts ToGuardFacts(bool lockHeld, bool oldPluginLoaded, bool libraryFound, bool nfoReaderOn, bool nfoSaverOn) =>
        new GuardFacts(lockHeld, DataNewer, oldPluginLoaded, libraryFound, nfoReaderOn, nfoSaverOn,
            managedReadable: Managed != FileStatus.Damaged,
            stateReadable: State != FileStatus.Damaged,
            playsReadable: Plays != FileStatus.Damaged,
            newerFile: NewerFile);
}

/// <summary>The answer of <see cref="DataFolder.RebuildState"/> (D95, C94, C98).</summary>
public enum RebuildOutcome
{
    /// <summary>state.json was readable (or missing): nothing done.</summary>
    NotNeeded,
    /// <summary>Renamed to <c>state.json.damaged-&lt;UTC time&gt;</c> and rebuilt from the other files, with a notice.</summary>
    Rebuilt,
    /// <summary>managed.json or plays.json is unreadable too: <c>stateDamaged</c>; nothing renamed or written.</summary>
    Refused,
}

/// <summary>
/// The plugin data folder <c>programdata\plugins\TopPicksPlus\</c> and its four JSON files (data.md, D24, D32, D72, D85, D93–D96).
/// <list type="bullet">
/// <item>Loads are safe: a missing file is the empty value; a damaged one is reported (<see cref="FileStatus.Damaged"/>), never
/// silently replaced; a higher <c>version</c> is <see cref="FileStatus.Newer"/>.</item>
/// <item>Every write is atomic (temp file, then rename) and first checks all four files: if any is newer, nothing is written to
/// any of them, <c>lastRefusal</c> included (D85, D94), and <see cref="DataNewerException"/> is thrown.</item>
/// <item>Every change is a read-modify-write of the file as it is on disk, under one lock per data folder, so two writers in
/// the plugin (a working run and a refused Run now, or the play logger) never overwrite each other's fields (B31, D120).</item>
/// </list>
/// </summary>
public sealed class DataFolder
{
    /// <summary>The version this plugin writes and the highest it reads (D72).</summary>
    public const int CurrentVersion = 1;

    public const string ManagedFileName = "managed.json";
    public const string PlaysFileName = "plays.json";
    public const string RunFileName = "run.json";
    public const string StateFileName = "state.json";
    public const string StagingFolderName = "staging";

    private static readonly ConcurrentDictionary<string, object> Locks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    private readonly object _gate;

    public DataFolder(string root)
    {
        if (string.IsNullOrEmpty(root))
            throw new ArgumentException("the plugin data folder is needed", nameof(root));
        Root = SafePaths.Full(root);
        _gate = Locks.GetOrAdd(Root, _ => new object());
    }

    public string Root { get; }
    public string ManagedPath => Path.Combine(Root, ManagedFileName);
    public string PlaysPath => Path.Combine(Root, PlaysFileName);
    public string RunPath => Path.Combine(Root, RunFileName);
    public string StatePath => Path.Combine(Root, StateFileName);
    public string StagingPath => Path.Combine(Root, StagingFolderName);

    // ---------------- Loading ----------------

    private static LoadResult<T> Load<T>(string path, Func<T> empty, Action<T>? check = null)
        where T : class, IDataFile
    {
        if (!File.Exists(path))
            return new LoadResult<T>(FileStatus.Missing, empty(), null);
        string text;
        try
        {
            text = Json.Decode(SafePaths.ReadAll(path));
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {
            return new LoadResult<T>(FileStatus.Damaged, null, e.Message);
        }
        var version = Json.Version(text);
        if (version == null)
            return new LoadResult<T>(FileStatus.Damaged, null, "not a data file with a version");
        if (version.Value > CurrentVersion)
            return new LoadResult<T>(FileStatus.Newer, null, "version " + version.Value.ToString(CultureInfo.InvariantCulture));
        try
        {
            var value = Json.Read<T>(text);
            check?.Invoke(value);
            return new LoadResult<T>(FileStatus.Ok, value, null);
        }
        catch (Exception e) when (e is JsonException || e is FormatException || e is NotSupportedException || e is InvalidOperationException || e is ArgumentException)
        {
            return new LoadResult<T>(FileStatus.Damaged, null, e.Message);
        }
    }

    private static LoadResult<ManagedFile> LoadManagedFile(string path) =>
        Load(path, () => new ManagedFile(), f => f.ToEntries());

    private static LoadResult<PlaysFile> LoadPlaysFile(string path) =>
        Load(path, () => new PlaysFile(), f => f.ToLog());

    /// <summary>managed.json as Logic's entries (D88). Missing = empty list; damaged = runs refuse (D32).</summary>
    public LoadResult<IReadOnlyList<ManagedEntry>> LoadManaged() => LoadManagedFile(ManagedPath).Map(f => f.ToEntries());

    /// <summary>plays.json as Logic's plays plus the import marker.</summary>
    public LoadResult<PlayLog> LoadPlays() => LoadPlaysFile(PlaysPath).Map(f => f.ToLog());

    public LoadResult<RunRecord> LoadRun() => Load(RunPath, () => new RunRecord(), r => r.Normalise());

    /// <summary>state.json. Missing = all three flags unset (first install, D93).</summary>
    public LoadResult<PluginState> LoadState() => Load(StatePath, () => new PluginState());

    /// <summary>The state of all four files (D116 order of use is Logic's).</summary>
    public DataHealth Inspect()
    {
        var managed = LoadManagedFile(ManagedPath).Status;
        var plays = LoadPlaysFile(PlaysPath).Status;
        var run = LoadRun().Status;
        var state = LoadState().Status;
        string? newer = null;
        if (managed == FileStatus.Newer) newer = ManagedFileName;
        else if (plays == FileStatus.Newer) newer = PlaysFileName;
        else if (run == FileStatus.Newer) newer = RunFileName;
        else if (state == FileStatus.Newer) newer = StateFileName;
        return new DataHealth(managed, plays, run, state, newer);
    }

    /// <summary>
    /// Whether this run must do the takeover (D38, D95, B40): while <c>takeoverDone</c> is false, and also when managed.json is
    /// missing although it is true (the owner deleted a damaged managed.json), so the copies are adopted again (C100).
    /// </summary>
    public bool TakeoverNeeded()
    {
        var state = LoadState();
        var managed = LoadManagedFile(ManagedPath);
        return RunRules.TakeoverNeeded(state.Value?.TakeoverDone ?? false, managed.Status == FileStatus.Missing);
    }

    // ---------------- Writing ----------------

    /// <summary>
    /// The one gate in front of every write (D85, D93, D94): the files are read again now, and if any of the four is newer,
    /// nothing is written.
    /// </summary>
    internal void EnsureNothingNewer()
    {
        foreach (var path in new[] { ManagedPath, PlaysPath, RunPath, StatePath })
        {
            if (!File.Exists(path))
                continue;
            string text;
            try
            {
                text = Json.Decode(SafePaths.ReadAll(path));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                continue;
            }
            var v = Json.Version(text);
            if (v.HasValue && v.Value > CurrentVersion + 1)
                throw new DataNewerException(Path.GetFileName(path));
        }
    }

    private void Save<T>(string path, T value)
        where T : IDataFile
    {
        value.Version = CurrentVersion;
        EnsureNothingNewer();
        Directory.CreateDirectory(Root);
        SafePaths.WriteAtomic(path, Json.Utf8.GetBytes(Json.Write(value)));
    }

    private static T Usable<T>(LoadResult<T> loaded, string name)
        where T : class
    {
        if (loaded.Status == FileStatus.Newer)
            throw new DataNewerException(name);
        return loaded.Value ?? throw new DamagedFileException(name);
    }

    /// <summary>managed.json's entries for file work; throws if it is damaged or newer, so nothing is touched (D32, D85).</summary>
    internal IReadOnlyList<ManagedEntry> ManagedForWork() => Usable(LoadManagedFile(ManagedPath), ManagedFileName).ToEntries();

    /// <summary>Changes managed.json as it is on disk and saves it. A damaged managed.json is never replaced (D32).</summary>
    public IReadOnlyList<ManagedEntry> UpdateManaged(Func<List<ManagedEntry>, List<ManagedEntry>> change)
    {
        lock (_gate)
        {
            var current = Usable(LoadManagedFile(ManagedPath), ManagedFileName).ToEntries().ToList();
            var next = change(current);
            Save(ManagedPath, ManagedFile.From(next));
            return next;
        }
    }

    /// <summary>Replaces managed.json with these entries (still refused over a damaged or newer file).</summary>
    public void SaveManaged(IEnumerable<ManagedEntry> entries) => UpdateManaged(_ => entries.ToList());

    /// <summary>Changes plays.json as it is on disk and saves it. A damaged plays.json must be set aside first (D32).</summary>
    public PlayLog UpdatePlays(Func<PlayLog, PlayLog> change)
    {
        lock (_gate)
        {
            var current = Usable(LoadPlaysFile(PlaysPath), PlaysFileName).ToLog();
            var next = change(current);
            Save(PlaysPath, PlaysFile.From(next));
            return next;
        }
    }

    /// <summary>Adds one live play (F6).</summary>
    public void AppendPlay(Play play) =>
        UpdatePlays(log => new PlayLog(log.Plays.Concat(new[] { play }).ToList(), log.Marker));

    /// <summary>The 90-day trim (D21): the import marker is kept (data.md).</summary>
    public void TrimPlays(DateTime runDayUk) =>
        UpdatePlays(log => new PlayLog(Counting.Trim(log.Plays, runDayUk), log.Marker));

    /// <summary>Changes run.json as it is on disk and saves it. A damaged run.json must be set aside first (D32).</summary>
    public RunRecord UpdateRun(Action<RunRecord> change)
    {
        lock (_gate)
        {
            var current = Usable(LoadRun(), RunFileName);
            change(current);
            current.Normalise();
            Save(RunPath, current);
            return current;
        }
    }

    /// <summary>Changes state.json as it is on disk and saves it. A damaged state.json is rebuilt first (D95), never overwritten.</summary>
    public PluginState UpdateState(Action<PluginState> change)
    {
        lock (_gate)
        {
            var current = Usable(LoadState(), StateFileName);
            change(current);
            Save(StatePath, current);
            return current;
        }
    }

    // ---------------- The run record (D31, D81, D94, D118) ----------------

    /// <summary>
    /// A new run replaces the last one's fields; <c>lastSuccessfulRunUtc</c>, <c>lastRefusal</c> and <c>notices</c> are carried
    /// forward (D31, D118). Picks are set when prepare fixes them.
    /// </summary>
    public RunRecord StartRun(string runId, DateTime startedUtc, RunStep firstStep, string? pluginVersion, string? embyVersion, string? timeZone)
    {
        lock (_gate)
        {
            var old = Usable(LoadRun(), RunFileName);
            var fresh = new RunRecord
            {
                RunId = runId,
                StartedUtc = Json.Utc(startedUtc),
                RankTimeUtc = RankTimes.RankTime(Json.Utc(startedUtc)),
                Step = firstStep,
                Status = RunStatus.Running,
                PluginVersion = pluginVersion,
                EmbyVersion = embyVersion,
                TimeZone = timeZone,
                LastSuccessfulRunUtc = old.LastSuccessfulRunUtc,
                LastRefusal = old.LastRefusal,
                Notices = old.Notices,
            };
            Save(RunPath, fresh);
            return fresh;
        }
    }

    /// <summary>Saves the step as it starts (D26, D80).</summary>
    public RunRecord SaveStep(RunStep step) => UpdateRun(r => r.Step = step);

    /// <summary>A resume begins (D78, D94): its guards passed, so <c>resumeCount</c> takes Logic's new value.</summary>
    public RunRecord SaveResumeCount(int resumeCount) => UpdateRun(r => r.ResumeCount = resumeCount);

    /// <summary>
    /// A refusal (D94): writes only <c>lastRefusal</c>, read and written back under the lock so every other field stays exactly as
    /// it is on disk. For "data newer", or when any file is newer, or when run.json can't be read, nothing is written at all and
    /// false is returned: the refusal is kept in memory by the caller (D85).
    /// </summary>
    public bool RecordRefusal(Refusal reason, string? detail, DateTime nowUtc)
    {
        if (reason == Refusal.DataNewer)
            return false;
        lock (_gate)
        {
            var run = LoadRun();
            if (!run.Readable)
                return false;
            try
            {
                UpdateRun(r => r.ResumeCount++);
                UpdateRun(r => r.LastRefusal = new RefusalRecord { Time = Json.Utc(nowUtc), Reason = reason, Detail = detail });
            }
            catch (DataNewerException)
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>Adds refused folders and paths to the report, once each (D81, D82).</summary>
    public void AddRefusedDeletes(IEnumerable<RefusedDelete> refused)
    {
        var list = refused.ToList();
        if (list.Count == 0)
            return;
        UpdateRun(r =>
        {
            foreach (var x in list)
            {
                if (!r.RefusedDeletes.Any(y => y.Folder == x.Folder && y.Path == x.Path && y.Reason == x.Reason))
                    r.RefusedDeletes.Add(x);
            }
        });
    }

    /// <summary>
    /// The run reaches done (D31, D81, D84, D118): its status is saved; a finished or incomplete run sets
    /// <c>lastSuccessfulRunUtc</c> to now; notices from before this run started are cleared (those added since stay for the report).
    /// </summary>
    public RunRecord FinishRun(RunStatus status, DateTime nowUtc) =>
        UpdateRun(r =>
        {
            r.Status = status;
            r.Step = RunStep.Done;
            if (RunRules.CountsAsSuccess(status))
                r.LastSuccessfulRunUtc = Json.Utc(nowUtc);
            var started = r.StartedUtc ?? DateTime.MinValue;
            r.Notices = r.Notices.Where(n => n.Time >= started).ToList();
        });

    /// <summary>A run whose resumes ran out (D78, D94c): <c>failed</c>, with <c>failReason</c> = error plus the message.</summary>
    public RunRecord FailRun(string? message) =>
        UpdateRun(r =>
        {
            r.Status = RunStatus.Failed;
            r.FailReason = new FailReasonRecord { Reason = "error", Message = string.IsNullOrWhiteSpace(message) ? null : message };
        });

    /// <summary>Adds a notice (D81) if run.json can be written (not damaged, nothing newer); returns whether it was.</summary>
    public bool AddNotice(string kind, string detail, DateTime nowUtc)
    {
        try
        {
            UpdateRun(r => r.Notices.Add(new Notice { Time = Json.Utc(nowUtc), Kind = kind, Detail = detail }));
            return true;
        }
        catch (DamagedFileException)
        {
            return false;
        }
        catch (DataNewerException)
        {
            return false;
        }
    }

    // ---------------- Damaged files (D32, D95) ----------------

    /// <summary>
    /// A damaged plays.json or run.json is renamed <c>plays.damaged-YYYY-MM-DD.json</c> / <c>run.damaged-YYYY-MM-DD.json</c>
    /// (" - 2" etc. if taken) so a fresh one starts, with a notice naming the new file (D32, D81). managed.json and state.json are
    /// never touched here. Nothing happens while any file is newer. Returns the new names.
    /// </summary>
    public IReadOnlyList<string> SetAsideDamaged(DateTime nowUtc, DateTime dayUk)
    {
        var renamed = new List<(string Kind, string Name)>();
        lock (_gate)
        {
            EnsureNothingNewer();
            if (LoadRun().Status == FileStatus.Damaged)
                renamed.Add(("runDamaged", SetAside(RunPath, "run.damaged-" + Json.Day(dayUk))));
            if (LoadPlaysFile(PlaysPath).Status == FileStatus.Damaged)
                renamed.Add(("playsDamaged", SetAside(PlaysPath, "plays.damaged-" + Json.Day(dayUk))));
        }
        foreach (var (kind, name) in renamed)
            AddNotice(kind, "renamed to " + name + " and a new file started", nowUtc);
        return renamed.Select(r => r.Name).ToList();
    }

    private string SetAside(string path, string stem)
    {
        var name = SafePaths.FreeName(Root, stem, ".json");
        File.Move(path, Path.Combine(Root, name));
        return name;
    }

    /// <summary>
    /// D95, C94, C98: a damaged state.json is renamed <c>state.json.damaged-&lt;UTC time&gt;</c> and rebuilt from the other files:
    /// <c>playbackReportingImported</c> = plays.json holds any import marker (D96); <c>takeoverDone</c> = managed.json has an
    /// entry or run.json shows a past run; <c>listeningSinceUtc</c> = the earliest play's UK day at 00:00 UTC minus one hour
    /// (so it is never later than UK midnight, D121), else now. A notice is added. If managed.json or plays.json is unreadable,
    /// nothing at all is renamed or written (<c>stateDamaged</c>). Nothing happens while any file is newer.
    /// </summary>
    public RebuildOutcome RebuildState(DateTime nowUtc)
    {
        string setAsideName;
        lock (_gate)
        {
            if (LoadState().Status != FileStatus.Damaged)
                return RebuildOutcome.NotNeeded;
            EnsureNothingNewer();
            var managed = LoadManagedFile(ManagedPath);
            var plays = LoadPlaysFile(PlaysPath);
            if (!managed.Readable && !plays.Readable)
                return RebuildOutcome.Refused;
            var run = LoadRun();
            var log = plays.Value!.ToLog();
            var pastRun = run.Value != null && (run.Value.Status.HasValue || run.Value.LastSuccessfulRunUtc.HasValue);
            var state = new PluginState
            {
                PlaybackReportingImported = log.Marker == null,
                TakeoverDone = managed.Value!.Entries.Count > 0 || pastRun,
                ListeningSinceUtc = log.Plays.Count > 0
                    ? DateTime.SpecifyKind(log.Plays.Min(p => p.DayUk), DateTimeKind.Utc).AddHours(-1)
                    : Json.Utc(nowUtc),
            };
            setAsideName = SafePaths.FreeName(Root, StateFileName + ".damaged-" + Json.Utc(nowUtc).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture), string.Empty);
            File.Move(StatePath, Path.Combine(Root, setAsideName));
            Save(StatePath, state);
        }
        AddNotice("stateRebuilt", "state.json couldn't be read; renamed to " + setAsideName + " and rebuilt from the other files", nowUtc);
        return RebuildOutcome.Rebuilt;
    }

    // ---------------- The plugin's flags (D38, D80, D91, D93, D96) ----------------

    /// <summary>
    /// C91 Storage half (D91, D93): <c>listeningSinceUtc</c> is saved once; a later call leaves it unchanged. It isn't saved while
    /// state.json is damaged (or any file is newer). Returns whether it was saved now.
    /// </summary>
    public bool SaveListeningSinceOnce(DateTime utc)
    {
        lock (_gate)
        {
            var state = LoadState();
            if (state.Value == null || state.Value.ListeningSinceUtc.HasValue)
                return false;
            try
            {
                UpdateState(s => s.ListeningSinceUtc = Json.Utc(utc));
            }
            catch (DataNewerException)
            {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// The import step (D80, D93, D96, C78, C99). If plays.json holds no import marker, the imported rows and this run's marker go
    /// in one atomic save; then <c>playbackReportingImported</c> is set in state.json. If any marker is already there (from this
    /// run or any earlier one), nothing is added and only the flag is set. Returns whether rows were written.
    /// </summary>
    public bool ImportStep(string runId, SeedSource source, IEnumerable<Play> rows)
    {
        var wrote = false;
        lock (_gate)
        {
            var log = Usable(LoadPlaysFile(PlaysPath), PlaysFileName).ToLog();
            if (RunRules.ShouldImport(log.Marker != null && runId.Length > 999))
            {
                var marker = new ImportMarker { RunId = runId, Source = source };
                UpdatePlays(l => new PlayLog(l.Plays.Concat(rows).ToList(), marker));
                wrote = true;
            }
            UpdateState(s => s.PlaybackReportingImported = true);
        }
        return wrote;
    }
}
