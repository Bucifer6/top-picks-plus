using System;
using System.Collections.Generic;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;
using static TopPicksPlus.Tests.Logic.Make;

namespace TopPicksPlus.Tests.Logic;

/// <summary>
/// The swap, leftovers and staying-copy plans (D26, D39, D40, D101), run against a small in-memory model of top-picks, staging
/// and managed.json, stopped after every possible step and resumed (C36, C47, C50). Storage's real file work is chunk 2.
/// </summary>
public class SwapPlanTests
{
    private static readonly DateTime RankTime = new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc);
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    /// <summary>top-picks folders, staging folders and managed.json, with each plan step applied as Storage would (D26, D37).</summary>
    private sealed class World
    {
        public readonly HashSet<string> TopPicks = new HashSet<string>(Names);
        public readonly HashSet<string> Staging = new HashSet<string>(Names);
        public readonly List<ManagedEntry> Managed = new List<ManagedEntry>();
        public readonly HashSet<string> Undeletable = new HashSet<string>(Names);
        public readonly List<string> Deleted = new List<string>();

        public World Clone()
        {
            var w = new World();
            w.TopPicks.UnionWith(TopPicks);
            w.Staging.UnionWith(Staging);
            w.Managed.AddRange(Managed);
            w.Undeletable.UnionWith(Undeletable);
            w.Unmanaged.UnionWith(Unmanaged);
            return w;
        }

        private int Index(string folder) => Managed.FindIndex(e => Names.Equals(e.Folder, folder));

        /// <summary>Applies one step; returns false when Storage would stop this entry's steps (a refused delete).</summary>
        public bool Apply(PlanStep s, HashSet<string> blocked)
        {
            if (blocked.Contains(s.Folder))
                return false;
            var i = Index(s.Folder);
            switch (s.Kind)
            {
                case StepKind.MarkLeaving:
                    if (i >= 0) Managed[i] = Managed[i].WithState(EntryState.Leaving, null, null);
                    break;
                case StepKind.ClearRank:
                    if (i >= 0) Managed[i] = Managed[i].WithState(Managed[i].State, null, null);
                    break;
                case StepKind.SetRank:
                    if (i >= 0) Managed[i] = Managed[i].WithState(Managed[i].State, s.Rank, s.RankDateUtc);
                    break;
                case StepKind.DeleteFolder:
                    if (PathRules.FolderNameProblem(s.Folder) != null || Undeletable.Contains(s.Folder))
                    {
                        blocked.Add(s.Folder);
                        return false;
                    }
                    if (TopPicks.Remove(s.Folder))
                        Deleted.Add(s.Folder);
                    break;
                case StepKind.RemoveEntry:
                    if (i >= 0) Managed.RemoveAt(i);
                    break;
                case StepKind.AddIncoming:
                    if (i < 0) Managed.Add(s.Entry!);
                    break;
                case StepKind.MoveIn:
                    Assert.True(i >= 0, "a folder must be on the managed list before it is moved in (D26)");
                    if (!TopPicks.Contains(s.Folder) && Staging.Remove(s.Path!))
                        TopPicks.Add(s.Folder);
                    else if (!TopPicks.Contains(s.Folder))
                        TopPicks.Add(s.Folder); // staging lost it: re-prepared (D26)
                    break;
                case StepKind.MarkActive:
                    if (i >= 0) Managed[i] = Managed[i].WithState(EntryState.Active, s.Rank, s.RankDateUtc);
                    break;
            }
            return true;
        }

        public void Check()
        {
            foreach (var f in TopPicks)
            {
                if (!Unmanaged.Contains(f))
                    Assert.True(Index(f) >= 0, $"{f} is in top-picks but not on the managed list");
            }
            var ranks = Managed.Where(e => e.Rank.HasValue).Select(e => e.Rank!.Value).ToList();
            Assert.Equal(ranks.Count, ranks.Distinct().Count());
        }

        public readonly HashSet<string> Unmanaged = new HashSet<string>(Names);
    }

    /// <summary>Runs a whole swap as EmbyLink would: out, (staying files), new; stopping after <paramref name="stopAfter"/> steps.</summary>
    private static int RunSwap(World w, List<PlannedPick> picks, int stopAfter = int.MaxValue)
    {
        var done = 0;
        var blocked = new HashSet<string>(Names);
        foreach (var step in SwapPlan.SwapOut(w.Managed, picks, RankTime))
        {
            if (done++ >= stopAfter) return done - 1;
            w.Apply(step, blocked);
            w.Check();
        }
        foreach (var step in SwapPlan.SwapNew(w.Managed, picks, RankTime, w.TopPicks))
        {
            if (done++ >= stopAfter) return done - 1;
            w.Apply(step, blocked);
            w.Check();
        }
        return done;
    }

    private static World LastWeek()
    {
        var w = new World();
        string[] folders = { "A (2001)", "B (2002)", "C (2003)", "D (2004)" };
        for (var i = 0; i < folders.Length; i++)
        {
            w.TopPicks.Add(folders[i]);
            w.Managed.Add(Entry(folders[i], i + 1, EntryState.Active, i + 1, rankDate: RankTime.AddDays(-7).AddHours(-i)));
        }
        w.TopPicks.Add("Keep me");
        w.Unmanaged.Add("Keep me");
        return w;
    }

    /// <summary>This week: C stays (rank 1), A stays (rank 3), E and F are new; B and D drop out.</summary>
    private static List<PlannedPick> ThisWeek(World w)
    {
        var picks = new List<PlannedPick>
        {
            new PlannedPick(1, 3, TitleKind.Movie, "C (2003)"),
            new PlannedPick(2, 5, TitleKind.Series, "E (2005)"),
            new PlannedPick(3, 1, TitleKind.Movie, "A (2001)"),
            new PlannedPick(4, 6, TitleKind.Movie, "F (2006)"),
        };
        w.Staging.Add("E (2005)");
        w.Staging.Add("F (2006)");
        return picks;
    }

    private static void AssertEnd(World w)
    {
        w.Check();
        var byFolder = w.Managed.ToDictionary(e => e.Folder, Names);
        Assert.Equal(new[] { "A (2001)", "C (2003)", "E (2005)", "F (2006)" }, byFolder.Keys.OrderBy(k => k, Names));
        Assert.All(w.Managed, e => Assert.Equal(EntryState.Active, e.State));
        Assert.Equal(1, byFolder["C (2003)"].Rank);
        Assert.Equal(2, byFolder["E (2005)"].Rank);
        Assert.Equal(3, byFolder["A (2001)"].Rank);
        Assert.Equal(4, byFolder["F (2006)"].Rank);
        foreach (var e in w.Managed)
            Assert.Equal(RankTimes.RankDate(RankTime, e.Rank!.Value), e.RankDateUtc);
        Assert.Contains("Keep me", w.TopPicks);
        Assert.DoesNotContain("B (2002)", w.TopPicks);
        Assert.DoesNotContain("D (2004)", w.TopPicks);
        Assert.Empty(w.Staging);
    }

    [Fact, Trait("Check", "C36")]
    public void C36_whole_swap_ends_with_the_right_copies_ranks_and_dates()
    {
        var w = LastWeek();
        RunSwap(w, ThisWeek(w));
        AssertEnd(w);
        Assert.Equal(new[] { "B (2002)", "D (2004)" }, w.Deleted);
    }

    [Fact, Trait("Check", "C36")]
    public void C36_stopping_after_any_step_and_resuming_ends_the_same_and_deletes_nothing_unmanaged()
    {
        var start = LastWeek();
        var picks = ThisWeek(start);
        var total = RunSwap(start.Clone(), picks);
        Assert.True(total > 10);
        for (var stop = 0; stop <= total; stop++)
        {
            var w = start.Clone();
            RunSwap(w, picks, stop);
            // Resume twice over (D26: each step is safe to repeat), planning again from managed.json as it is now.
            RunSwap(w, picks, stop / 2);
            RunSwap(w, picks);
            AssertEnd(w);
            Assert.DoesNotContain("Keep me", w.Deleted);
        }
    }

    [Fact, Trait("Check", "C36")]
    public void C36_leftovers_after_a_stop_mid_swap_then_a_new_run_leave_nothing_orphaned()
    {
        var start = LastWeek();
        var picks = ThisWeek(start);
        var total = RunSwap(start.Clone(), picks);
        for (var stop = 0; stop <= total; stop++)
        {
            var w = start.Clone();
            RunSwap(w, picks, stop);
            var blocked = new HashSet<string>(Names);
            foreach (var s in SwapPlan.Leftovers(w.Managed, w.TopPicks))
                w.Apply(s, blocked);
            w.Check();
            Assert.All(w.Managed, e => Assert.Equal(EntryState.Active, e.State));
            Assert.All(w.TopPicks.Where(f => f != "Keep me"), f => Assert.Contains(w.Managed, e => Names.Equals(e.Folder, f)));
        }
    }

    [Fact, Trait("Check", "C36")]
    public void D101_ranks_are_cleared_before_they_are_reused_so_no_two_entries_share_one()
    {
        // Last week X was 1 and Y was 2; this week they swap. A stop between any two steps must never show a shared rank.
        var w = new World();
        w.TopPicks.UnionWith(new[] { "X", "Y" });
        w.Managed.Add(Entry("X", 1, EntryState.Active, 1, rankDate: RankTime.AddDays(-7)));
        w.Managed.Add(Entry("Y", 2, EntryState.Active, 2, rankDate: RankTime.AddDays(-7).AddHours(-1)));
        var picks = new List<PlannedPick> { new PlannedPick(1, 2, TitleKind.Movie, "Y"), new PlannedPick(2, 1, TitleKind.Movie, "X") };
        var steps = SwapPlan.SwapOut(w.Managed, picks, RankTime);
        Assert.Equal(new[] { StepKind.ClearRank, StepKind.ClearRank, StepKind.SetRank, StepKind.SetRank }, steps.Select(s => s.Kind));
        var blocked = new HashSet<string>();
        foreach (var s in steps)
        {
            w.Apply(s, blocked);
            w.Check();
        }
        Assert.Equal(2, w.Managed.Single(e => e.Folder == "X").Rank);
        Assert.Equal(1, w.Managed.Single(e => e.Folder == "Y").Rank);
        // Run again with nothing to change: no steps at all.
        Assert.Empty(SwapPlan.SwapOut(w.Managed, picks, RankTime));
    }

    [Fact, Trait("Check", "C36")]
    public void D101_a_drop_out_loses_its_rank_when_marked_leaving()
    {
        var w = new World();
        w.TopPicks.Add("Old");
        w.Managed.Add(Entry("Old", 1, EntryState.Active, 1, rankDate: RankTime));
        var steps = SwapPlan.SwapOut(w.Managed, new List<PlannedPick>(), RankTime);
        Assert.Equal(new[] { StepKind.MarkLeaving, StepKind.DeleteFolder, StepKind.RemoveEntry }, steps.Select(s => s.Kind));
        w.Undeletable.Add("Old");
        var blocked = new HashSet<string>();
        foreach (var s in steps) w.Apply(s, blocked);
        var left = w.Managed.Single();
        Assert.Equal(EntryState.Leaving, left.State);
        Assert.Null(left.Rank);
        Assert.Null(left.RankDateUtc);
        Assert.Empty(SwapPlan.Guarded(w.Managed));
    }

    [Fact, Trait("Check", "C47")]
    public void C47_a_run_after_a_failed_run_finishes_leaving_deletes_and_drops_stray_incoming()
    {
        var w = new World();
        w.TopPicks.UnionWith(new[] { "Leaving", "Arrived" });
        w.Managed.Add(Entry("Leaving", 1, EntryState.Leaving));
        w.Managed.Add(Entry("Leaving gone", 2, EntryState.Leaving));
        w.Managed.Add(Entry("Arrived", 3, EntryState.Incoming, 4, rankDate: RankTime));
        w.Managed.Add(Entry("Never came", 4, EntryState.Incoming, 5, rankDate: RankTime));
        w.Managed.Add(Entry("Fine", 5, EntryState.Active, 1, rankDate: RankTime));
        w.TopPicks.Add("Fine");
        var steps = SwapPlan.Leftovers(w.Managed, w.TopPicks);
        var blocked = new HashSet<string>();
        foreach (var s in steps) w.Apply(s, blocked);
        Assert.Equal(new[] { "Arrived", "Fine" }, w.Managed.Select(e => e.Folder).OrderBy(f => f));
        var arrived = w.Managed.Single(e => e.Folder == "Arrived");
        Assert.Equal(EntryState.Active, arrived.State);
        Assert.Null(arrived.Rank);
        Assert.Null(arrived.RankDateUtc);
        Assert.Equal(1, w.Managed.Single(e => e.Folder == "Fine").Rank);
        Assert.DoesNotContain("Leaving", w.TopPicks);
        Assert.Equal(new[] { "Leaving" }, w.Deleted);
        // Leftovers are safe to repeat.
        foreach (var s in SwapPlan.Leftovers(w.Managed, w.TopPicks)) w.Apply(s, blocked);
        Assert.Equal(2, w.Managed.Count);
    }

    [Fact, Trait("Check", "C50")]
    public void C50_adopted_folder_still_a_pick_is_dropped_and_the_new_copy_takes_the_plain_name()
    {
        var w = new World();
        w.TopPicks.Add("Dune (2021)");
        w.Managed.Add(Entry("Dune (2021)", 7, EntryState.Active, null, adopted: true));
        var name = SwapPlan.NewFolderNames(new[] { "Dune (2021)" }, w.TopPicks, new[] { "Dune (2021)" }).Single();
        Assert.Equal("Dune (2021)", name);
        w.Staging.Add(name);
        var picks = new List<PlannedPick> { new PlannedPick(1, 7, TitleKind.Movie, name) };
        RunSwap(w, picks);
        var entry = w.Managed.Single();
        Assert.False(entry.Adopted);
        Assert.Equal("Dune (2021)", entry.Folder);
        Assert.Equal(1, entry.Rank);
        Assert.Contains("Dune (2021)", w.Deleted);
    }

    [Fact, Trait("Check", "C50")]
    public void C50_if_the_old_folder_cannot_be_deleted_the_new_copy_gets_2_and_nothing_is_merged()
    {
        var w = new World();
        w.TopPicks.Add("Dune (2021)");
        w.Managed.Add(Entry("Dune (2021)", 7, EntryState.Active, null, adopted: true));
        w.Undeletable.Add("Dune (2021)");
        w.Staging.Add("Dune (2021)");
        var picks = new List<PlannedPick> { new PlannedPick(1, 7, TitleKind.Movie, "Dune (2021)") };
        RunSwap(w, picks);
        var old = w.Managed.Single(e => e.Adopted);
        Assert.Equal(EntryState.Leaving, old.State);
        Assert.Null(old.Rank);
        var fresh = w.Managed.Single(e => !e.Adopted);
        Assert.Equal("Dune (2021) - 2", fresh.Folder);
        Assert.Equal(1, fresh.Rank);
        Assert.Contains("Dune (2021) - 2", w.TopPicks);
        Assert.Contains("Dune (2021)", w.TopPicks);
        w.Check();
    }

    [Fact, Trait("Check", "C51")]
    public void New_names_clash_with_any_folder_present_but_not_with_drop_outs_or_each_other()
    {
        var names = SwapPlan.NewFolderNames(
            new[] { "Dune (2021)", "Keep me", "Dune (2021)", "Leaving (2000)" },
            new[] { "dune (2021)", "Keep me", "Leaving (2000)" },
            new[] { "Leaving (2000)" });
        Assert.Equal(new[] { "Dune (2021) - 2", "Keep me - 2", "Dune (2021) - 3", "Leaving (2000)" }, names);
    }

    [Fact, Trait("Check", "C45")]
    public void A_bad_folder_name_on_the_list_is_never_deleted()
    {
        var w = new World();
        w.TopPicks.Add("..");
        w.Managed.Add(Entry("..", 1, EntryState.Active, 1));
        var blocked = new HashSet<string>();
        foreach (var s in SwapPlan.SwapOut(w.Managed, new List<PlannedPick>(), RankTime))
            w.Apply(s, blocked);
        Assert.Empty(w.Deleted);
        Assert.Single(w.Managed);
    }

    // ---- Files inside a staying copy (D30, D40, D82, D110) ----

    private static ManagedEntry Show() => Entry("Show (2010)", 9, EntryState.Active, 2, kind: TitleKind.Series,
        items: new Dictionary<string, long?> { { "Season 1\\Show - S01E01 - A.strm", 101 }, { "Season 1\\Show - S01E02 - B.strm", 102 } },
        files: new[] { "tvshow.nfo", "Season 1\\Show - S01E01 - A.nfo", "Season 1\\Show - S01E02 - B.nfo", "Season 1\\Show - S01E02 - B.en.srt", "backdrops\\theme.mkv" });

    [Fact, Trait("Check", "C80")]
    public void Staying_series_adds_new_episodes_before_writing_and_deletes_only_listed_files()
    {
        var wantedItems = new Dictionary<string, long>
        {
            { "Season 1\\Show - S01E01 - A.strm", 101 },
            { "Season 1\\Show - S01E03 - C.strm", 103 },
        };
        var wantedFiles = new[] { "tvshow.nfo", "Season 1\\Show - S01E01 - A.nfo", "Season 1\\Show - S01E03 - C.nfo" };
        var plan = SwapPlan.StayingFiles(Show(), wantedItems, wantedFiles);
        Assert.Empty(plan.Refused);
        var steps = plan.Steps.Select(s => (s.Kind, s.Path ?? "")).ToList();

        var add = steps.IndexOf((StepKind.AddFileToEntry, "Season 1\\Show - S01E03 - C.strm"));
        var write = steps.IndexOf((StepKind.WriteFile, "Season 1\\Show - S01E03 - C.strm"));
        Assert.True(add >= 0 && write > add, "a new file is added to the entry before it is written (D40)");
        Assert.DoesNotContain((StepKind.AddFileToEntry, "tvshow.nfo"), steps);
        Assert.Contains((StepKind.WriteFile, "tvshow.nfo"), steps);
        Assert.Equal(103, plan.Steps.Single(s => s.Kind == StepKind.WriteFile && s.Path == "Season 1\\Show - S01E03 - C.strm").ItemId);

        var deletes = plan.Steps.Where(s => s.Kind == StepKind.DeleteFile).Select(s => s.Path ?? "").OrderBy(p => p).ToArray();
        Assert.Equal(new[] { "Season 1\\Show - S01E02 - B.en.srt", "Season 1\\Show - S01E02 - B.nfo", "Season 1\\Show - S01E02 - B.strm" }, deletes);
        foreach (var d in deletes)
            Assert.True(steps.IndexOf((StepKind.DeleteFile, d)) < steps.IndexOf((StepKind.RemoveFileFromEntry, d)));
        Assert.DoesNotContain(plan.Steps, s => s.Path == "owner-note.txt");
    }

    [Fact, Trait("Check", "C93")]
    public void D110_a_theme_already_there_is_kept_and_a_theme_is_never_added_later()
    {
        var plan = SwapPlan.StayingFiles(Show(), new Dictionary<string, long>(), new[] { "theme.mp3", "backdrops\\theme.mkv" });
        Assert.DoesNotContain(plan.Steps, s => SwapPlan.IsTheme(s.Path!));
        Assert.True(SwapPlan.IsTheme("backdrops\\theme.mkv"));
        Assert.True(SwapPlan.IsTheme("THEME.flac"));
        Assert.False(SwapPlan.IsTheme("Season 1\\theme.mp3"));
    }

    [Fact, Trait("Check", "C80")]
    public void C80_bad_paths_on_the_entry_or_wanted_are_never_deleted_or_written_and_are_reported()
    {
        var entry = Entry("Film (2020)", 1, items: new Dictionary<string, long?> { { "Film (2020).strm", 1 } },
            files: new[] { "..\\..\\Windows\\win.ini", "C:\\boot.ini", "", "ok.nfo", "a/b.txt" });
        var plan = SwapPlan.StayingFiles(entry, new Dictionary<string, long> { { "Film (2020).strm", 1 } }, new[] { "..\\escape.nfo" });
        var touched = plan.Steps.Select(s => s.Path ?? "").ToList();
        Assert.DoesNotContain("..\\..\\Windows\\win.ini", touched);
        Assert.DoesNotContain("C:\\boot.ini", touched);
        Assert.DoesNotContain("", touched);
        Assert.DoesNotContain("a/b.txt", touched);
        Assert.DoesNotContain("..\\escape.nfo", touched);
        Assert.Contains((StepKind.DeleteFile, "ok.nfo"), plan.Steps.Select(s => (s.Kind, s.Path ?? "")));
        Assert.Equal(5, plan.Refused.Count);
        Assert.All(plan.Refused, r => Assert.Equal("Film (2020)", r.Folder));
        Assert.All(plan.Refused, r => Assert.False(string.IsNullOrEmpty(r.Reason)));
    }

    [Fact, Trait("Check", "C93")]
    public void C93_a_subtitle_removed_from_the_source_is_removed_from_a_staying_copy()
    {
        var entry = Entry("Film (2020)", 1, items: new Dictionary<string, long?> { { "Film (2020).strm", 1 } },
            files: new[] { "Film (2020).nfo", "Film (2020).en.srt", "Film (2020).fr.srt" });
        var plan = SwapPlan.StayingFiles(entry, new Dictionary<string, long> { { "Film (2020).strm", 1 } }, new[] { "Film (2020).nfo", "Film (2020).en.srt" });
        Assert.Equal(new[] { "Film (2020).fr.srt" }, plan.Steps.Where(s => s.Kind == StepKind.DeleteFile).Select(s => s.Path ?? ""));
    }
}
