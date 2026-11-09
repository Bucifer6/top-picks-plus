using System;
using System.Collections.Generic;
using System.Linq;
using TopPicksPlus.Logic;

namespace TopPicksPlus.Tests.Logic;

/// <summary>Small builders so each test reads as its check. Every Logic test carries [Trait("Check", "Cxx")] for per-check results.</summary>
internal static class Make
{
    /// <summary>A Sunday run day (2026-11-15 is a Sunday).</summary>
    public static readonly DateTime Sunday = new DateTime(2026, 11, 15);

    public static Play APlay(string user, long source, DateTime day, double minutes = 0, bool played = false, double? runtime = 100, long? episode = null) =>
        new Play(user, source, episode, day, minutes, played, runtime, PlayOrigin.Live);

    /// <summary>A play that counts on its own (marked played) on the given number of days before Sunday.</summary>
    public static Play Watched(string user, long source, int daysBefore = 1) => APlay(user, source, Sunday.AddDays(-daysBefore), played: true);

    public static Candidate Movie(long id, int addedDaysAgo = 30, DateTime? premiere = null, bool available = true) =>
        new Candidate(id, TitleKind.Movie, Sunday.AddDays(-addedDaysAgo), premiere, available);

    public static Candidate Series(long id, int addedDaysAgo = 30) =>
        new Candidate(id, TitleKind.Series, Sunday.AddDays(-addedDaysAgo), null, true);

    public static IReadOnlyDictionary<long, int> Days(params (long Id, int Days)[] scores) => scores.ToDictionary(s => s.Id, s => s.Days);

    public static IEnumerable<PinStatus> Pins(params long[] ids) => ids.Select(i => new PinStatus(i, true));

    public static ManagedEntry Entry(string folder, long? source, EntryState state = EntryState.Active, int? rank = null, bool adopted = false, TitleKind kind = TitleKind.Movie,
        IReadOnlyDictionary<string, long?>? items = null, IReadOnlyList<string>? files = null, DateTime? rankDate = null) =>
        new ManagedEntry(folder, source, kind, state, rank, rankDate, adopted, items, files);

    public static GuardFacts Ok() => new GuardFacts();
}
