using System;
using System.Collections.Generic;

namespace TopPicksPlus.Logic;

// The plain record types the rules work on (written as sealed classes). No JSON attributes: Storage maps these to the file shapes in
// docs/data.md (D88). Plain classes with get-only properties, so no IsExternalInit is needed (D97(g)).

/// <summary>A source title's kind. Only whole movies and series are ever picked (D61).</summary>
public enum TitleKind
{
    Movie,
    Series,
}

/// <summary>Where a play row came from (data.md `origin`).</summary>
public enum PlayOrigin
{
    Live,
    PlaybackReporting,
    LastPlayed,
}

/// <summary>Why a title is in the 10 (D31).</summary>
public enum PickReason
{
    Pinned,
    ViewerDays,
    Filler,
}

/// <summary>A managed entry's state (D26).</summary>
public enum EntryState
{
    Incoming,
    Active,
    Leaving,
}

/// <summary>One playback stopped (data.md plays.json). <see cref="DayUk"/> is the UK date of the stop (D70).</summary>
public sealed class Play
{
    public Play(string userId, long sourceId, long? episodeId, DateTime dayUk, double minutes, bool markedPlayed, double? runtimeMinutes, PlayOrigin origin)
    {
        UserId = userId;
        SourceId = sourceId;
        EpisodeId = episodeId;
        DayUk = dayUk.Date;
        Minutes = minutes;
        MarkedPlayed = markedPlayed;
        RuntimeMinutes = runtimeMinutes;
        Origin = origin;
    }

    public string UserId { get; }
    public long SourceId { get; }
    public long? EpisodeId { get; }
    public DateTime DayUk { get; }
    public double Minutes { get; }
    public bool MarkedPlayed { get; }
    public double? RuntimeMinutes { get; }
    public PlayOrigin Origin { get; }
}

/// <summary>
/// A movie or series that may be picked automatically (F2 step 5). <see cref="Available"/>: a movie's file can be found, or at
/// least one of a series' episode files can (<see cref="Choosing.IsAvailable"/>). <see cref="NewestContentUtc"/>: a movie's date
/// added, or a series' newest episode's date added (F2 step 3).
/// </summary>
public sealed class Candidate
{
    public Candidate(long sourceId, TitleKind kind, DateTime newestContentUtc, DateTime? premiereDate, bool available)
    {
        SourceId = sourceId;
        Kind = kind;
        NewestContentUtc = newestContentUtc;
        PremiereDate = premiereDate;
        Available = available;
    }

    public long SourceId { get; }
    public TitleKind Kind { get; }
    public DateTime NewestContentUtc { get; }
    public DateTime? PremiereDate { get; }
    public bool Available { get; }
}

/// <summary>A pin from the settings, with whether it can be used this run (its title exists and a file can be found, D61).</summary>
public sealed class PinStatus
{
    public PinStatus(long sourceId, bool usable)
    {
        SourceId = sourceId;
        Usable = usable;
    }

    public long SourceId { get; }
    public bool Usable { get; }
}

/// <summary>One of the (up to) 10 picks.</summary>
public sealed class Pick
{
    public Pick(int rank, long sourceId, PickReason reason, int viewerDays)
    {
        Rank = rank;
        SourceId = sourceId;
        Reason = reason;
        ViewerDays = viewerDays;
    }

    public int Rank { get; }
    public long SourceId { get; }
    public PickReason Reason { get; }
    public int ViewerDays { get; }
}

/// <summary>The result of choosing: the picks in rank order and the pins that were missing.</summary>
public sealed class ChooseResult
{
    public ChooseResult(IReadOnlyList<Pick> picks, IReadOnlyList<long> missingPins)
    {
        Picks = picks;
        MissingPins = missingPins;
    }

    public IReadOnlyList<Pick> Picks { get; }
    public IReadOnlyList<long> MissingPins { get; }
}

/// <summary>
/// One managed copy folder (data.md managed.json), keyed by <see cref="Folder"/>. <see cref="Items"/>: each video (.strm) path
/// inside the copy, relative and separated by <c>\</c>, mapped to its source item ID (null when an adopted folder matched nothing).
/// <see cref="Files"/>: every other file the plugin wrote (or found at adoption, D90).
/// </summary>
public sealed class ManagedEntry
{
    private static readonly IReadOnlyDictionary<string, long?> NoItems = new Dictionary<string, long?>();
    private static readonly IReadOnlyList<string> NoFiles = Array.Empty<string>();

    public ManagedEntry(
        string folder,
        long? sourceId,
        TitleKind kind,
        EntryState state,
        int? rank,
        DateTime? rankDateUtc,
        bool adopted,
        IReadOnlyDictionary<string, long?>? items = null,
        IReadOnlyList<string>? files = null)
    {
        Folder = folder;
        SourceId = sourceId;
        Kind = kind;
        State = state;
        Rank = rank;
        RankDateUtc = rankDateUtc;
        Adopted = adopted;
        Items = items ?? NoItems;
        Files = files ?? NoFiles;
    }

    public string Folder { get; }
    public long? SourceId { get; }
    public TitleKind Kind { get; }
    public EntryState State { get; }
    public int? Rank { get; }
    public DateTime? RankDateUtc { get; }
    public bool Adopted { get; }
    public IReadOnlyDictionary<string, long?> Items { get; }
    public IReadOnlyList<string> Files { get; }

    /// <summary>The same entry with a new state and rank (keeps folder, source, items and files).</summary>
    public ManagedEntry WithState(EntryState state, int? rank, DateTime? rankDateUtc) =>
        new ManagedEntry(Folder, SourceId, Kind, state, rank, rankDateUtc, Adopted, Items, Files);
}

/// <summary>What a play counts towards: the source movie or series, and the episode for a series (D28, D64).</summary>
public sealed class Credit
{
    public Credit(long sourceId, long? episodeId)
    {
        SourceId = sourceId;
        EpisodeId = episodeId;
    }

    public long SourceId { get; }
    public long? EpisodeId { get; }
}
