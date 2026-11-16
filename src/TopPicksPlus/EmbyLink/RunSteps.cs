using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// One attempt at a run: each saved step in D80's order, from the step run.json holds, so a resume simply repeats its step (D26).
/// Every file change goes through Storage (D43); every rule is Logic's (D79). Called only by <see cref="RunEngine"/> with the lock held.
/// </summary>
internal sealed class RunSteps
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    /// <summary>F4 step 3: how long the rescan wait lasts, and the longer wait for a series with more than 200 episodes.</summary>
    private static readonly TimeSpan RescanWait = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LongRescanWait = TimeSpan.FromMinutes(30);
    private const int BigSeriesEpisodes = 200;
    private static readonly TimeSpan PollEvery = TimeSpan.FromSeconds(10);

    /// <summary>How long the artwork refresh is given before dates are read back (F4 step 5).</summary>
    private static readonly TimeSpan ArtworkSettle = TimeSpan.FromSeconds(60);

    private readonly Host _h;
    private readonly TopPicksLibrary _library;
    private readonly Dictionary<string, CopyBuild> _builds = new Dictionary<string, CopyBuild>(Names);
    private RunRecord _run = new RunRecord();

    public RunSteps(Host h, TopPicksLibrary library)
    {
        _h = h;
        _library = library;
    }

    private DateTime RunDayUk => EmbyFacts.UkDay(_run.StartedUtc ?? DateTime.UtcNow);
    private DateTime RankTime => _run.RankTimeUtc ?? RankTimes.RankTime(_run.StartedUtc ?? DateTime.UtcNow);

    public async Task Run(CancellationToken ct)
    {
        _h.Copies.Reprepare = Reprepare;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            _run = _h.Data.LoadRun().Value ?? throw new InvalidOperationException("run.json can't be read");
            var step = _run.Step ?? RunStep.Done;
            if (step == RunStep.Done)
                return;
            _h.Log.Info("Top Picks Plus: step {0}", step);
            await Do(step, ct).ConfigureAwait(false);
            if (step == RunStep.Record)
                return;
            _h.Data.SaveStep(RunRules.NextStep(step, !_h.Data.TakeoverNeeded()));
        }
    }

    private Task Do(RunStep step, CancellationToken ct)
    {
        switch (step)
        {
            case RunStep.Import: Import(); return Task.CompletedTask;
            case RunStep.Takeover: TakeOver(); return Task.CompletedTask;
            case RunStep.Leftovers: Apply(SwapPlan.Leftovers(Managed(), _h.Copies.FolderNames())); return Task.CompletedTask;
            case RunStep.Prepare: Prepare(); return Task.CompletedTask;
            case RunStep.Swap: Swap(); return Task.CompletedTask;
            case RunStep.Rescan: return Rescan(ct);
            case RunStep.Dates: SetDates(copyWatched: true, ct); return Task.CompletedTask;
            case RunStep.Artwork: return RefreshArtwork(ct);
            case RunStep.Check: return CheckRow(ct);
            default: Record(); return Task.CompletedTask;
        }
    }

    // ---------------- import, takeover (D80, D38, D64, D91, D129) ----------------

    /// <summary>
    /// The one-off first-week import from each user's "last played" dates (D25, D47, D129): rows up to <c>listeningSinceUtc</c>
    /// inside the run's 7-day window, credited like live plays (old-plugin copies through their .strm, D64).
    /// </summary>
    private void Import()
    {
        var state = _h.Data.LoadState().Value ?? new PluginState();
        var since = state.ListeningSinceUtc ?? DateTime.UtcNow;
        var managed = Managed();
        var firstDay = Counting.Window(RunDayUk).First;
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(firstDay, DateTimeKind.Unspecified), TimeZoneInfo.Local);
        var rows = new List<ImportRow>();
        foreach (var user in EmbyFacts.AllUsers(_h))
        {
            var items = _h.Library.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { "Movie", "Episode" },
                Recursive = true,
                MinDateLastSavedForUser = new DateTimeOffset(fromUtc, TimeSpan.Zero),
            });
            foreach (var item in items)
            {
                var data = _h.UserData.GetUserData(user, item);
                if (data?.LastPlayedDate is DateTimeOffset played && played.UtcDateTime >= fromUtc)
                {
                    var runtime = item.RunTimeTicks.HasValue ? TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalMinutes : (double?)null;
                    rows.Add(new ImportRow(EmbyFacts.UserKey(user), played.UtcDateTime, EmbyFacts.UkDay(played.UtcDateTime),
                        EmbyFacts.CreditFor(_h, item, managed), 0, runtime));
                }
            }
        }
        var result = EmbyRules.Import(rows, RunDayUk, since, PlayOrigin.LastPlayed);
        if (_h.Data.ImportStep(_run.RunId ?? string.Empty, SeedSource.LastPlayed, result.Plays))
        {
            _h.Data.UpdateRun(r =>
            {
                r.SeedSource = SeedSource.LastPlayed;
                r.ImportIgnored = result.Ignored;
            });
        }
    }

    /// <summary>D38, D28, D90: adopts old-plugin folders, matching every .strm to the Emby item with that path.</summary>
    private void TakeOver()
    {
        var sources = new Dictionary<string, IReadOnlyDictionary<string, long?>>(Names);
        var seriesOf = new Dictionary<long, long>();
        foreach (var folder in _h.Copies.Listings().Where(Takeover.IsOldPluginFolder))
        {
            var map = new Dictionary<string, long?>(Names);
            foreach (var strm in folder.Files.Where(f => f.EndsWith(".strm", StringComparison.OrdinalIgnoreCase)))
            {
                var target = _h.Copies.ReadStrm(folder.Name, strm);
                var item = string.IsNullOrEmpty(target) ? null : _h.Library.FindByPath(target, false);
                map[strm] = item?.InternalId;
                var series = item == null ? null : EmbyFacts.SeriesOf(item);
                if (item != null && series.HasValue)
                    seriesOf[item.InternalId] = series.Value;
            }
            sources[folder.Name] = map;
        }
        var adopted = _h.Copies.TakeOver(
            f => sources.TryGetValue(f.Name, out var m) ? m : new Dictionary<string, long?>(),
            seriesOf);
        _h.Log.Info("Top Picks Plus: took over {0} old folder(s)", adopted.Count);
    }

    // ---------------- prepare (F2, F3, D26, D30, D40) ----------------

    private void Prepare()
    {
        if (RunRules.ChoosesPicks(RunStep.Prepare, _run.Picks.Count > 0))
            ChoosePicks();
        var managed = Managed();
        var results = new Dictionary<string, CopyBuild>(Names);
        var gone = new List<RunPick>();
        foreach (var pick in _run.Picks)
        {
            var build = BuildFor(pick, IsNew(pick, managed));
            if (build == null)
            {
                gone.Add(pick);
                continue;
            }
            build.WriteTo(_h.Copies, pick.Folder);
            results[pick.Folder] = build;
        }
        foreach (var pick in gone)
        {
            // D147: a pick whose title vanished since it was chosen is left out of this run (its rank stays empty), not a failure.
            _h.Data.AddNotice("pickGone", "pick " + pick.Rank + " (title " + pick.SourceId + ") was left out: its title is no longer in Emby", DateTime.UtcNow);
            _h.Log.Warn("Top Picks Plus: pick {0} (title {1}) was left out: its title is no longer in Emby", pick.Rank, pick.SourceId);
        }
        _h.Data.UpdateRun(r =>
        {
            r.Picks = r.Picks.Where(p => !gone.Any(g => g.SourceId == p.SourceId)).ToList();
            foreach (var p in r.Picks)
            {
                if (!results.TryGetValue(p.Folder, out var b))
                    continue;
                p.ArtworkFailed = b.ArtworkFailed;
                p.EpisodesSkipped = b.EpisodesSkipped;
            }
        });
    }

    /// <summary>F2 and D30/D40: the 10 picks and their folder names, fixed once and saved (a resume never re-chooses, D26).</summary>
    private void ChoosePicks()
    {
        var settings = Host.Settings;
        var plays = _h.Data.LoadPlays().Value?.Plays ?? new List<Play>();
        var libraries = EmbyFacts.CandidateLibraries(_h, settings.LibraryIds ?? new long[0]);
        // D147, D150: with a folder that holds titles unreachable (a drive offline), nothing is chosen: the error is retried, never
        // an emptied row. A missing folder Emby knows no titles in (a leftover path) can't change the picks, so it is skipped.
        var folders = EmbyRules.CheckFolders(EmbyFacts.LibraryFolders(_h, libraries));
        if (folders.Blocking != null)
            throw new IOException("the library folder " + folders.Blocking + " can't be reached; is the drive connected?");
        foreach (var skipped in folders.Skipped)
        {
            _h.Data.AddNotice("folderSkipped", "the library folder " + skipped + " can't be reached and Emby has no titles in it, so it was skipped; remove it from its library if it is no longer used", DateTime.UtcNow);
            _h.Log.Warn("Top Picks Plus: the library folder {0} can't be reached and Emby has no titles in it; skipped", skipped);
        }
        var candidates = Catalog.Candidates(_h, libraries);
        var chosen = Choosing.Choose(candidates, Catalog.Pins(_h, settings.Pins ?? new long[0]), Counting.ViewerDays(plays, RunDayUk));
        if (chosen.Picks.Count == 0)
            throw new IOException("no title's file can be found in the chosen libraries; the row is left as it is");

        var managed = Managed();
        var staying = chosen.Picks.ToDictionary(p => p.SourceId, p => managed.FirstOrDefault(e => SwapPlan.IsStaying(e, p.SourceId)));
        var items = chosen.Picks.ToDictionary(p => p.SourceId, p => _h.Library.GetItemById(p.SourceId));
        var newPicks = chosen.Picks.Where(p => staying[p.SourceId] == null).ToList();
        var maxFolder = Naming.MaxFolderLengthFor(_h.Copies.Staging.Length);
        var baseNames = newPicks.Select(p => Naming.FolderName(items[p.SourceId]?.Name, items[p.SourceId]?.ProductionYear, maxFolder));
        var dropping = managed.Where(e => !chosen.Picks.Any(p => SwapPlan.IsStaying(e, p.SourceId))).Select(e => e.Folder);
        var names = SwapPlan.NewFolderNames(baseNames, _h.Copies.FolderNames(), dropping);
        var newNames = newPicks.Select((p, i) => (p.SourceId, Name: names[i])).ToDictionary(x => x.SourceId, x => x.Name);

        var picks = chosen.Picks
            .Select(p => RunPick.From(p, Catalog.KindOf(items[p.SourceId]) ?? TitleKind.Movie,
                staying[p.SourceId]?.Folder ?? newNames[p.SourceId]))
            .ToList();
        _h.Data.UpdateRun(r =>
        {
            r.Picks = picks;
            r.MissingPins = chosen.MissingPins.ToList();
        });
        _run = _h.Data.LoadRun().Value ?? _run;
        _h.Log.Info("Top Picks Plus: chose {0} picks", picks.Count);
    }

    /// <summary>A pick is new unless an active, not adopted copy of its source is already in top-picks (F4 step 2, D40).</summary>
    private static bool IsNew(RunPick pick, IEnumerable<ManagedEntry> managed) =>
        !managed.Any(e => SwapPlan.IsStaying(e, pick.SourceId));

    /// <summary>The pick's copy files, or null when its title is no longer a movie or series in Emby (D147).</summary>
    private CopyBuild? BuildFor(RunPick pick, bool isNew)
    {
        var key = pick.Folder + (isNew ? "|new" : "|staying");
        if (!_builds.TryGetValue(key, out var build))
        {
            build = CopyBuilder.Build(_h, pick.SourceId, pick.Folder, pick.Rank, RankTimes.RankDate(RankTime, pick.Rank), isNew, _h.Copies.Staging.Length);
            if (build == null)
                return null;
            _builds[key] = build;
        }
        return build;
    }

    /// <summary>
    /// D124: staging lost a copy, so it is prepared again, whole, under <paramref name="partialName"/>. The staging name is the
    /// pick's folder, or, for a copy moved in under a " - N" name (D40), that entry's folder, found through its source (D147).
    /// </summary>
    private void Reprepare(string stagingName, string partialName)
    {
        var managed = Managed();
        var source = managed.FirstOrDefault(e => Names.Equals(e.Folder, stagingName))?.SourceId;
        var pick = _run.Picks.FirstOrDefault(p => Names.Equals(p.Folder, stagingName))
            ?? _run.Picks.FirstOrDefault(p => p.SourceId == source)
            ?? throw new IOException("no pick for staging folder '" + stagingName + "'");
        var build = BuildFor(pick, IsNew(pick, managed)) ?? throw new IOException("the title of '" + stagingName + "' is no longer in Emby");
        build.WriteTo(_h.Copies, partialName);
    }

    // ---------------- swap (F4 step 2, D26, D40, D101, D124) ----------------

    private void Swap()
    {
        var planned = _run.Picks.Select(p => p.ToPlanned()).ToList();
        Apply(SwapPlan.SwapOut(Managed(), planned, RankTime));

        var managed = Managed();
        foreach (var pick in _run.Picks)
        {
            var entry = managed.FirstOrDefault(e => SwapPlan.IsStaying(e, pick.SourceId));
            if (entry == null)
                continue;
            // A staying copy whose title vanished is left exactly as it is this week (D147).
            var contents = BuildFor(pick, isNew: false)?.Contents();
            if (contents == null)
                continue;
            var plan = SwapPlan.StayingFiles(entry, contents.Items, contents.Files);
            _h.Data.AddRefusedDeletes(plan.Refused.Select(RefusedDelete.From));
            Apply(plan.Steps);
        }

        var bySource = _run.Picks.ToDictionary(p => p.SourceId);
        Apply(
            SwapPlan.SwapNew(Managed(), planned, RankTime, _h.Copies.FolderNames()),
            e => e.SourceId.HasValue && bySource.TryGetValue(e.SourceId.Value, out var p) ? BuildFor(p, isNew: true)?.Contents() : null);

        managed = Managed();
        _h.Data.UpdateRun(r =>
        {
            foreach (var p in r.Picks)
                p.CopyApplied = managed.Any(e => e.State == EntryState.Active && e.SourceId == p.SourceId && e.Rank == p.Rank);
        });
    }

    // ---------------- rescan (F4 step 3) ----------------

    private async Task Rescan(CancellationToken ct)
    {
        if (!(_h.Library.GetItemById(_library.Id) is Folder folder))
            throw new InvalidOperationException("the Top Picks library can't be found");
        await folder.ValidateChildren(new Progress<double>(), ct, NewRefreshOptions(), true).ConfigureAwait(false);

        var big = Managed().Any(e => e.Kind == TitleKind.Series && e.Items.Count > BigSeriesEpisodes);
        var deadline = DateTime.UtcNow + (big ? LongRescanWait : RescanWait);
        while (!AllAppeared() && DateTime.UtcNow < deadline)
            await Task.Delay(PollEvery, ct).ConfigureAwait(false);
        if (!AllAppeared())
            _h.Log.Warn("Top Picks Plus: not every copy appeared before the rescan wait ran out; the run will end incomplete (F4)");
    }

    private MetadataRefreshOptions NewRefreshOptions() => new MetadataRefreshOptions(new DirectoryService(_h.FileSystem));

    /// <summary>
    /// B23: a copy has appeared when Emby has an item for every .strm in it and, for a series, for the series folder too.
    /// </summary>
    private bool AllAppeared() => SwapPlan.Guarded(Managed()).All(e => CopyItems(e).All(i => i.Item != null));

    private IReadOnlyList<(BaseItem? Item, long? SourceId)> CopyItems(ManagedEntry entry) => EmbyFacts.CopyItems(_h, entry);

    // ---------------- dates, watched state, artwork (F4 steps 4-5, F7, D52, D103) ----------------

    /// <summary>
    /// F4 step 4: each ranked copy's date added is set to its rank date (series and every episode). With
    /// <paramref name="copyWatched"/>, each user's played state is copied from the source onto the copy (D103).
    /// </summary>
    private void SetDates(bool copyWatched, CancellationToken ct)
    {
        var users = copyWatched ? EmbyFacts.AllUsers(_h) : Array.Empty<User>();
        var applied = new Dictionary<long, bool>();
        foreach (var entry in SwapPlan.Guarded(Managed()))
        {
            ct.ThrowIfCancellationRequested();
            var items = CopyItems(entry);
            foreach (var (item, sourceId) in items)
            {
                if (item == null)
                    continue;
                OrderGuard.SetDate(item, entry.RankDateUtc!.Value);
                if (sourceId.HasValue && users.Count > 0)
                {
                    var source = _h.Library.GetItemById(sourceId.Value);
                    if (source != null)
                        Watched.CopyPlayedOnto(_h, users, source, item, ct);
                }
            }
            // Keyed by source, so a copy moved in under a " - N" name still matches its pick (D147).
            if (entry.SourceId.HasValue)
                applied[entry.SourceId.Value] = items.All(i => i.Item != null);
        }
        _h.Data.UpdateRun(r =>
        {
            foreach (var p in r.Picks)
                p.DateApplied = applied.TryGetValue(p.SourceId, out var ok) && ok;
        });
    }

    /// <summary>F4 step 5: refresh each copy's pictures so the number matches, then put back any date the refresh changed.</summary>
    private async Task RefreshArtwork(CancellationToken ct)
    {
        foreach (var entry in SwapPlan.Guarded(Managed()))
        {
            var main = CopyItems(entry).FirstOrDefault().Item;
            if (main == null)
                continue;
            var options = NewRefreshOptions();
            options.MetadataRefreshMode = MetadataRefreshMode.ValidationOnly;
            options.ImageRefreshMode = MetadataRefreshMode.FullRefresh;
            options.ReplaceAllImages = false;
            _h.Providers.QueueRefresh(main.InternalId, options, RefreshPriority.High);
        }
        await Task.Delay(ArtworkSettle, ct).ConfigureAwait(false);
        SetDates(copyWatched: false, ct);
    }

    // ---------------- row check (F4 step 6, D20, D45, D83, D84, D104) ----------------

    private async Task CheckRow(CancellationToken ct)
    {
        var users = EmbyFacts.AllUsers(_h);
        var (userId, fellBack) = EmbyRules.RowCheckUser(EmbyFacts.UserInfos(_h, users), Host.Settings.RowCheckUserId);
        var user = users.FirstOrDefault(u => string.Equals(EmbyFacts.UserKey(u), userId, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            _h.Data.UpdateRun(r =>
            {
                r.RowCheck = new RowCheckRecord { Passed = false };
                r.Message = "The row couldn't be checked: Emby has no users.";
            });
            return;
        }

        var (result, returned) = CheckOnce(user);
        var retried = false;
        if (RunRules.RetryRowCheck(result.Passed, alreadyRetried: false, rescanTimedOut: !AllAppeared()))
        {
            SetDates(copyWatched: false, ct);
            await RefreshArtwork(ct).ConfigureAwait(false);
            (result, returned) = CheckOnce(user);
            retried = true;
        }
        _h.Data.UpdateRun(r => r.RowCheck = new RowCheckRecord
        {
            Passed = result.Passed,
            UserId = userId,
            FellBackToAdmin = fellBack,
            Returned = result.Passed ? new List<string>() : returned,
            Extras = result.Extras.ToList(),
            Retried = retried,
        });
    }

    /// <summary>
    /// D104's requests: the Latest row for the Top Picks library as the user (limit 20, grouped) against a normal query of the
    /// library as the user (Movie and Series, recursive, no played filter on either) sorted by date added, newest first.
    /// </summary>
    private (RowCheckResult Result, List<string> Returned) CheckOnce(User user)
    {
        var managed = Managed();
        var latest = _h.UserViews.GetLatestItems(
            new LatestItemsQuery { UserId = user.InternalId, ParentId = _library.Id, Limit = 20, GroupItems = true },
            new DtoOptions()) ?? Array.Empty<BaseItem>();
        var expected = _h.Library.GetItemList(new InternalItemsQuery(user)
        {
            AncestorIds = new[] { _library.Id },
            IncludeItemTypes = new[] { "Movie", "Series" },
            Recursive = true,
        }).OrderByDescending(i => i.DateCreated).ToList();
        var actual = latest.Select(i => RowItemFor(i, managed)).ToList();
        var result = EmbyRules.CheckRow(expected.Select(i => RowItemFor(i, managed)), actual);
        var returned = actual.Select(i => i.Rank.HasValue ? "#" + i.Rank.Value + " " + i.Label : i.Label + " (not ranked)").ToList();
        return (result, returned);
    }

    private RowItem RowItemFor(BaseItem item, IReadOnlyList<ManagedEntry> managed)
    {
        var path = item is Episode ep && ep.Series != null ? ep.Series.Path : item.Path;
        var rel = _h.RelativeToTopPicks(path);
        var folder = rel == null ? null : rel.Split('\\')[0];
        var entry = folder == null ? null : managed.FirstOrDefault(e => Names.Equals(e.Folder, folder));
        var ranked = entry != null && entry.State == EntryState.Active && entry.Rank.HasValue;
        return new RowItem(item.Name ?? item.InternalId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ranked ? entry!.Folder : folder, ranked ? entry!.Rank : null);
    }

    // ---------------- record (F4 step 7, F6, D21, D31, D84, D102) ----------------

    private void Record()
    {
        _h.Data.TrimPlays(RunDayUk);
        var plays = _h.Data.LoadPlays().Value?.Plays ?? new List<Play>();
        var unmanaged = _h.Copies.UnmanagedFolders();
        _h.Data.UpdateRun(r =>
        {
            r.PlaysLast7Days = Counting.PlaysInWindow(plays, RunDayUk);
            r.UnmanagedFolders = unmanaged.ToList();
        });
        var status = RunRules.FinalStatus(failed: false, rescanTimedOut: !AllAppeared());
        _h.Data.FinishRun(status, DateTime.UtcNow);
        _h.Log.Info("Top Picks Plus: run {0}", status);
    }

    // ---------------- helpers ----------------

    private IReadOnlyList<ManagedEntry> Managed() =>
        _h.Data.LoadManaged().Value ?? throw new InvalidOperationException("managed.json can't be read");

    private void Apply(IEnumerable<PlanStep> plan, Func<ManagedEntry, CopyContents?>? newCopyContents = null)
    {
        var result = _h.Copies.Apply(plan, newCopyContents);
        foreach (var r in result.Refused)
            _h.Log.Warn("Top Picks Plus: refused to touch {0} {1}: {2}", r.Folder, r.Path ?? string.Empty, r.Reason);
    }
}
