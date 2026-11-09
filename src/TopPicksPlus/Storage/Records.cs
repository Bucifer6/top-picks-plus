using System;
using System.Collections.Generic;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Storage;

// The file shapes in docs/data.md (D81, D88, D117). Names are written camelCase by Json.Options; values that are "none" are left
// out of the file, and a missing value reads back as none. run.json and state.json are their own public types (EmbyLink and
// Settings read and change them); managed.json and plays.json are mapped to and from Logic's records (ManagedEntry, Play).

/// <summary>What a load found (D32, D72, D85).</summary>
public enum FileStatus
{
    /// <summary>No file: the empty value (first install, or deleted by the owner).</summary>
    Missing,
    Ok,
    /// <summary>The file can't be read. It is reported and never silently replaced.</summary>
    Damaged,
    /// <summary>Its <c>version</c> is higher than this plugin knows: nothing may be written to any of the four files (D85).</summary>
    Newer,
}

/// <summary>Where the first week's plays came from (data.md <c>seedSource</c> and the import marker's <c>source</c>, D25).</summary>
public enum SeedSource
{
    None,
    PlaybackReporting,
    LastPlayed,
}

/// <summary>The answer of a load: the status, and the value when it is <see cref="FileStatus.Ok"/> or <see cref="FileStatus.Missing"/>.</summary>
public sealed class LoadResult<T>
    where T : class
{
    internal LoadResult(FileStatus status, T? value, string? problem)
    {
        Status = status;
        Value = value;
        Problem = problem;
    }

    public FileStatus Status { get; }

    /// <summary>The file's contents (or the empty value for a missing file); null when damaged or newer.</summary>
    public T? Value { get; }

    /// <summary>Why a damaged or newer file can't be used.</summary>
    public string? Problem { get; }

    /// <summary>Missing counts as readable: a missing file means empty (data.md).</summary>
    public bool Readable => Value != null;

    internal LoadResult<TOut> Map<TOut>(Func<T, TOut> map)
        where TOut : class =>
        new LoadResult<TOut>(Status, Value == null ? null : map(Value), Problem);
}

/// <summary>A data file was written by a newer Top Picks Plus, so nothing is written (D85, D93, D94).</summary>
public sealed class DataNewerException : InvalidOperationException
{
    public DataNewerException(string file)
        : base(file + " was written by a newer Top Picks Plus; nothing is written until that version is back")
    {
        File = file;
    }

    public string File { get; }
}

/// <summary>The file to be changed can't be read; it is never silently replaced (D32, D95).</summary>
public sealed class DamagedFileException : InvalidOperationException
{
    public DamagedFileException(string file)
        : base(file + " can't be read, so it is left as it is")
    {
        File = file;
    }

    public string File { get; }
}

internal interface IDataFile
{
    int Version { get; set; }
}

/// <summary>state.json: the plugin's own flags (D93, D95). Only these four fields are ever in the file (C48).</summary>
public sealed class PluginState : IDataFile
{
    public int Version { get; set; } = DataFolder.CurrentVersion;
    public bool TakeoverDone { get; set; }
    public bool PlaybackReportingImported { get; set; }
    public DateTime? ListeningSinceUtc { get; set; }
}

/// <summary>plays.json's import marker (D80, D93, D96): saved in the same save as the imported rows, even with no rows.</summary>
public sealed class ImportMarker
{
    public string RunId { get; set; } = string.Empty;
    public SeedSource Source { get; set; }
}

/// <summary>plays.json as Logic sees it: the plays and the import marker, if any.</summary>
public sealed class PlayLog
{
    public PlayLog(IReadOnlyList<Play> plays, ImportMarker? marker)
    {
        Plays = plays;
        Marker = marker;
    }

    public IReadOnlyList<Play> Plays { get; }
    public ImportMarker? Marker { get; }
}

/// <summary>One of the run's picks in run.json (data.md, D81). <see cref="Kind"/> is kept so a resume can plan the swap again.</summary>
public sealed class RunPick
{
    public int Rank { get; set; }
    public long SourceId { get; set; }
    public TitleKind Kind { get; set; }
    public string Folder { get; set; } = string.Empty;
    public PickReason Reason { get; set; }
    public int ViewerDays { get; set; }
    public bool CopyApplied { get; set; }
    public bool DateApplied { get; set; }
    public bool ArtworkFailed { get; set; }
    public int EpisodesSkipped { get; set; }

    /// <summary>From Logic's pick, with the kind and the folder fixed at prepare (D88).</summary>
    public static RunPick From(Pick pick, TitleKind kind, string folder) => new RunPick
    {
        Rank = pick.Rank,
        SourceId = pick.SourceId,
        Kind = kind,
        Folder = folder,
        Reason = pick.Reason,
        ViewerDays = pick.ViewerDays,
    };

    /// <summary>What Logic's swap plan needs (D26: a resume never re-chooses).</summary>
    public PlannedPick ToPlanned() => new PlannedPick(Rank, SourceId, Kind, Folder);
}

/// <summary>A failed run's reason (D81, D94): always <c>error</c> plus the message, never a refusal reason.</summary>
public sealed class FailReasonRecord
{
    public string Reason { get; set; } = "error";
    public string? Message { get; set; }
}

/// <summary>A folder or file a D37/D82 check refused to touch (data.md <c>refusedDeletes</c>).</summary>
public sealed class RefusedDelete
{
    public string Folder { get; set; } = string.Empty;

    /// <summary>The file path inside the copy, or none when the whole folder was refused.</summary>
    public string? Path { get; set; }

    public string Reason { get; set; } = string.Empty;

    public static RefusedDelete From(RefusedPath p) => new RefusedDelete { Folder = p.Folder, Path = p.Path, Reason = p.Reason };
}

/// <summary>The row check's result in run.json (D20, D45, D83, D104).</summary>
public sealed class RowCheckRecord
{
    public bool Passed { get; set; }
    public string? UserId { get; set; }
    public bool FellBackToAdmin { get; set; }
    public List<string> Returned { get; set; } = new List<string>();
    public List<string> Extras { get; set; } = new List<string>();
    public bool Retried { get; set; }
}

/// <summary>The last refused run (D94): written alone, leaving the run's other fields as they were.</summary>
public sealed class RefusalRecord
{
    public DateTime Time { get; set; }
    public Refusal Reason { get; set; }
    public string? Detail { get; set; }
}

/// <summary>Something that happened outside a run, such as a damaged file renamed (D32, D81, D95).</summary>
public sealed class Notice
{
    public DateTime Time { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

/// <summary>run.json: the current or last run, its report fields (D81) and the records kept across runs (D31, D94, D118).</summary>
public sealed class RunRecord : IDataFile
{
    public int Version { get; set; } = DataFolder.CurrentVersion;
    public string? RunId { get; set; }
    public DateTime? StartedUtc { get; set; }
    public DateTime? RankTimeUtc { get; set; }
    public RunStep? Step { get; set; }
    public RunStatus? Status { get; set; }
    public FailReasonRecord? FailReason { get; set; }
    public int ResumeCount { get; set; }
    public List<RunPick> Picks { get; set; } = new List<RunPick>();
    public List<long> MissingPins { get; set; } = new List<long>();
    public int PlaysLast7Days { get; set; }
    public SeedSource? SeedSource { get; set; }
    public int ImportIgnored { get; set; }
    public List<RefusedDelete> RefusedDeletes { get; set; } = new List<RefusedDelete>();
    public List<string> UnmanagedFolders { get; set; } = new List<string>();
    public RowCheckRecord? RowCheck { get; set; }
    public string? PluginVersion { get; set; }
    public string? EmbyVersion { get; set; }
    public string? TimeZone { get; set; }
    public string? Message { get; set; }
    public DateTime? LastSuccessfulRunUtc { get; set; }
    public RefusalRecord? LastRefusal { get; set; }
    public List<Notice> Notices { get; set; } = new List<Notice>();

    /// <summary>The run part as Logic's trigger rules need it, or none when run.json holds no run.</summary>
    public RunSnapshot? Snapshot() =>
        Status.HasValue && Step.HasValue ? new RunSnapshot(Status.Value, Step.Value, ResumeCount) : null;

    internal void Normalise()
    {
        Picks ??= new List<RunPick>();
        MissingPins ??= new List<long>();
        RefusedDeletes ??= new List<RefusedDelete>();
        UnmanagedFolders ??= new List<string>();
        Notices ??= new List<Notice>();
        if (RowCheck != null)
        {
            RowCheck.Returned ??= new List<string>();
            RowCheck.Extras ??= new List<string>();
        }
    }
}
