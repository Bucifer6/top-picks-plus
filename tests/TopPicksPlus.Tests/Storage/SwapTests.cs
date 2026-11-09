using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>
/// Storage carrying out Logic's swap, staying-file and leftovers plans on real folders, stopped after every step (and inside a
/// save) and resumed (D26, D39, D40, D101, C36, C43, C47, C50, C70, C93).
/// </summary>
public class SwapTests
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    /// <summary>Last week: A–D managed and ranked, each with a .strm, .nfo and folder.png; "Keep me" is the owner's.</summary>
    private static Sandbox LastWeek()
    {
        var s = new Sandbox();
        var entries = new List<ManagedEntry>();
        string[] folders = { "A (2001)", "B (2002)", "C (2003)", "D (2004)" };
        for (var i = 0; i < folders.Length; i++)
        {
            var f = folders[i];
            s.Folder(f, f + ".strm", f + ".nfo", "folder.png", "theme.mp3");
            entries.Add(new ManagedEntry(f, i + 1, TitleKind.Movie, EntryState.Active, i + 1, Sandbox.RankTime.AddDays(-7).AddHours(-i), false,
                new Dictionary<string, long?> { [f + ".strm"] = i + 1 }, new[] { f + ".nfo", "folder.png", "theme.mp3" }));
        }
        s.Data.SaveManaged(entries);
        s.Folder("Keep me", "note.txt");
        s.Data.UpdateRun(_ => { });
        return s;
    }

    /// <summary>What prepare writes this week for each pick: its videos (path → source) and other files (D124).</summary>
    private static readonly Dictionary<string, (Dictionary<string, long> Items, string[] Files)> Prepared = new Dictionary<string, (Dictionary<string, long> Items, string[] Files)>
    {
        ["C (2003)"] = (new Dictionary<string, long> { ["C (2003).strm"] = 3 }, new[] { "C (2003).nfo", "folder.png", "C (2003).en.forced.srt" }),
        ["E (2005)"] = (new Dictionary<string, long> { ["E (2005).strm"] = 5 }, new[] { "E (2005).nfo", "folder.png" }),
        ["A (2001)"] = (new Dictionary<string, long> { ["A (2001).strm"] = 1 }, new[] { "A (2001).nfo" }),
        ["F (2006)"] = (new Dictionary<string, long> { ["F (2006).strm"] = 6 }, new[] { "F (2006).nfo", "folder.png" }),
    };

    private static void Prepare(Sandbox s, string name, string? into = null)
    {
        var (items, files) = Prepared[name];
        foreach (var f in items.Keys.Concat(files))
            s.Copies.WriteStagingText(into ?? name, f, name + "/" + f + "/new");
    }

    /// <summary>This week: C stays at 1, A stays at 3 (its folder.png goes: artwork fallback none), E and F are new; B and D drop out.</summary>
    private static List<PlannedPick> ThisWeek(Sandbox s)
    {
        foreach (var name in Prepared.Keys)
            Prepare(s, name);
        s.Copies.Reprepare = (name, into) => Prepare(s, name, into);
        return new List<PlannedPick>
        {
            new PlannedPick(1, 3, TitleKind.Movie, "C (2003)"),
            new PlannedPick(2, 5, TitleKind.Movie, "E (2005)"),
            new PlannedPick(3, 1, TitleKind.Movie, "A (2001)"),
            new PlannedPick(4, 6, TitleKind.Movie, "F (2006)"),
        };
    }

    private static CopyContents? Contents(ManagedEntry e) =>
        Prepared.TryGetValue(e.Folder, out var p) ? new CopyContents(p.Items, p.Files) : null;

    /// <summary>The whole swap as EmbyLink drives it, planning each part again from managed.json and top-picks as they are now.</summary>
    private static IEnumerable<PlanStep> Plans(Sandbox s, List<PlannedPick> picks)
    {
        foreach (var step in SwapPlan.SwapOut(s.Managed(), picks, Sandbox.RankTime))
            yield return step;
        foreach (var p in picks)
        {
            var entry = s.Managed().FirstOrDefault(e => SwapPlan.IsStaying(e, p.SourceId));
            if (entry == null)
                continue;
            var (items, files) = Prepared[entry.Folder];
            foreach (var step in SwapPlan.StayingFiles(entry, items, files).Steps)
                yield return step;
        }
        foreach (var step in SwapPlan.SwapNew(s.Managed(), picks, Sandbox.RankTime, s.Copies.FolderNames()))
            yield return step;
    }

    /// <summary>Runs the swap, stopping after <paramref name="stopAfter"/> steps; returns the steps done.</summary>
    private static int RunSwap(Sandbox s, List<PlannedPick> picks, int stopAfter = int.MaxValue)
    {
        var done = 0;
        foreach (var step in Plans(s, picks))
        {
            if (done >= stopAfter)
                return done;
            s.Copies.Apply(new[] { step }, Contents);
            done++;
            CheckInvariants(s);
        }
        return done;
    }

    /// <summary>At every moment: every folder in top-picks (but the owner's) is on the managed list, and no two entries share a rank.</summary>
    private static void CheckInvariants(Sandbox s)
    {
        var managed = s.Managed();
        foreach (var f in s.Copies.FolderNames().Where(f => f != "Keep me"))
            Assert.Contains(managed, e => Names.Equals(e.Folder, f));
        var ranks = managed.Where(e => e.Rank.HasValue).Select(e => e.Rank!.Value).ToList();
        Assert.Equal(ranks.Count, ranks.Distinct().Count());
        Assert.Empty(Directory.GetFiles(s.DataRoot, "*" + SafePaths.TempSuffix));
    }

    private static void AssertEnd(Sandbox s)
    {
        CheckInvariants(s);
        var m = s.Managed().ToDictionary(e => e.Folder, Names);
        Assert.Equal(new[] { "A (2001)", "C (2003)", "E (2005)", "F (2006)" }, m.Keys.OrderBy(k => k, Names));
        Assert.All(m.Values, e => Assert.Equal(EntryState.Active, e.State));
        Assert.Equal(1, m["C (2003)"].Rank);
        Assert.Equal(2, m["E (2005)"].Rank);
        Assert.Equal(3, m["A (2001)"].Rank);
        Assert.Equal(4, m["F (2006)"].Rank);
        foreach (var e in m.Values)
            Assert.Equal(RankTimes.RankDate(Sandbox.RankTime, e.Rank!.Value), e.RankDateUtc);
        Assert.Equal(new[] { "A (2001)", "C (2003)", "E (2005)", "F (2006)", "Keep me" }, s.Copies.FolderNames());
        Assert.Equal("Keep me/note.txt", Sandbox.Text(s.InTopPicks("Keep me", "note.txt")));
        // Staying copies: rewritten in place from staging, the theme kept as it was (D110), a dropped file deleted (C43 file half).
        Assert.Equal("C (2003)/C (2003).nfo/new", Sandbox.Text(s.InTopPicks("C (2003)", "C (2003).nfo")));
        Assert.Equal("C (2003)/C (2003).en.forced.srt/new", Sandbox.Text(s.InTopPicks("C (2003)", "C (2003).en.forced.srt")));
        Assert.Equal("C (2003)/theme.mp3", Sandbox.Text(s.InTopPicks("C (2003)", "theme.mp3")));
        Assert.False(File.Exists(s.InTopPicks("A (2001)", "folder.png")));
        Assert.Equal(new[] { "A (2001).nfo", "theme.mp3" }, m["A (2001)"].Files.OrderBy(f => f, Names));
        Assert.Contains("C (2003).en.forced.srt", m["C (2003)"].Files);
        // New copies moved in whole.
        Assert.Equal("E (2005)/E (2005).strm/new", Sandbox.Text(s.InTopPicks("E (2005)", "E (2005).strm")));
        Assert.Equal(5, m["E (2005)"].Items["E (2005).strm"]);
        Assert.Equal(new[] { "E (2005).nfo", "folder.png" }, m["E (2005)"].Files);
        Assert.Empty(Directory.GetDirectories(s.Staging, "*" + CopyFolders.PartialSuffix));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_the_whole_swap_on_real_folders_ends_with_the_right_copies_ranks_dates_and_files()
    {
        using var s = LastWeek();
        RunSwap(s, ThisWeek(s));
        AssertEnd(s);
    }

    // Exhaustive (every stop point): too slow to repeat for every mutant, so Stryker skips it (stryker-config.json); CI runs it.
    [Fact, Trait("Check", "C36"), Trait("Mutation", "Exhaustive")]
    public void C36_stopping_after_any_step_and_resuming_ends_the_same_with_nothing_unmanaged_and_nothing_unmanaged_deleted()
    {
        int total;
        using (var probe = LastWeek())
            total = RunSwap(probe, ThisWeek(probe));
        Assert.True(total > 15, "the swap should have many steps, got " + total);
        for (var stop = 0; stop <= total; stop++)
        {
            using var s = LastWeek();
            var picks = ThisWeek(s);
            RunSwap(s, picks, stop);
            RunSwap(s, picks, stop / 2); // a resume that stops again (D26: every step is safe to repeat)
            RunSwap(s, picks);
            AssertEnd(s);
        }
    }

    // Exhaustive (every stop point): too slow to repeat for every mutant, so Stryker skips it (stryker-config.json); CI runs it.
    [Fact, Trait("Check", "C36"), Trait("Mutation", "Exhaustive")]
    public void C36_a_stop_inside_any_save_leaves_old_or_new_and_a_resume_ends_the_same()
    {
        for (var n = 1; n <= 40; n++)
        {
            using var s = LastWeek();
            var picks = ThisWeek(s);
            var count = 0;
            var stopAt = n;
            SafePaths.BeforeReplace = _ =>
            {
                if (++count == stopAt)
                    throw new SimulatedStop("save " + stopAt);
            };
            var stopped = false;
            try
            {
                RunSwap(s, picks);
            }
            catch (SimulatedStop)
            {
                stopped = true;
            }
            Sandbox.NoStop();
            CheckInvariants(s);
            RunSwap(s, picks);
            AssertEnd(s);
            if (!stopped)
                break;
        }
    }

    [Fact, Trait("Check", "C36")]
    public void C36_staging_lost_a_new_copy_it_is_prepared_again_whole_before_the_move()
    {
        using var s = LastWeek();
        var picks = ThisWeek(s);
        var reprepared = new List<string>();
        s.Copies.Reprepare = (name, into) =>
        {
            reprepared.Add(name);
            Assert.Equal(name + CopyFolders.PartialSuffix, into);
            Prepare(s, name, into);
        };
        Directory.Delete(Path.Combine(s.Staging, "F (2006)"), recursive: true);
        RunSwap(s, picks);
        AssertEnd(s);
        Assert.Equal(new[] { "F (2006)" }, reprepared);
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_stop_while_preparing_again_never_moves_a_half_copy_in()
    {
        using var s = LastWeek();
        var picks = ThisWeek(s);
        Directory.Delete(Path.Combine(s.Staging, "F (2006)"), recursive: true);
        s.Copies.Reprepare = (name, into) =>
        {
            s.Staged(into, name + ".strm");
            throw new SimulatedStop("half-way through preparing " + name);
        };
        Assert.Throws<SimulatedStop>(() => RunSwap(s, picks));
        Assert.False(Directory.Exists(Path.Combine(s.TopPicks, "F (2006)")));
        Assert.False(Directory.Exists(Path.Combine(s.Staging, "F (2006)")));
        s.Copies.Reprepare = (name, into) => Prepare(s, name, into);
        RunSwap(s, picks);
        AssertEnd(s);
        Assert.True(File.Exists(s.InTopPicks("F (2006)", "folder.png")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_resume_after_a_new_copy_moved_in_rewrites_it_as_a_staying_copy_from_a_fresh_staging_copy()
    {
        using var s = LastWeek();
        var picks = ThisWeek(s);
        RunSwap(s, picks);
        // Staging no longer holds E or F (they were moved in); a resume at swap plans them as staying copies.
        Assert.False(Directory.Exists(Path.Combine(s.Staging, "E (2005)")));
        RunSwap(s, picks);
        AssertEnd(s);
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_folder_is_never_moved_in_before_it_is_on_the_managed_list()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new ManagedEntry[0]);
        s.Staged("New (2020)", "New (2020).strm");
        Assert.Throws<InvalidOperationException>(() => s.Copies.Apply(new[] { new PlanStep(StepKind.MoveIn, "New (2020)", path: "New (2020)") }));
        Assert.False(Directory.Exists(Path.Combine(s.TopPicks, "New (2020)")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_file_is_never_written_into_a_staying_copy_before_it_is_on_the_entry()
    {
        using var s = LastWeek();
        s.Staged("A (2001)", "new.srt");
        Assert.Throws<InvalidOperationException>(() => s.Copies.Apply(new[] { new PlanStep(StepKind.WriteFile, "A (2001)", path: "new.srt") }));
        Assert.False(File.Exists(s.InTopPicks("A (2001)", "new.srt")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_file_not_on_the_entry_is_never_deleted_from_a_staying_copy()
    {
        using var s = LastWeek();
        Sandbox.Write(s.InTopPicks("A (2001)", "mine.txt"), "mine");
        var result = s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFile, "A (2001)", path: "mine.txt") });
        Assert.Equal("file is not on the entry", Assert.Single(result.Refused).Reason);
        Assert.Equal("mine", Sandbox.Text(s.InTopPicks("A (2001)", "mine.txt")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_folder_not_on_the_managed_list_is_never_deleted()
    {
        using var s = LastWeek();
        var result = s.Copies.Apply(new[] { new PlanStep(StepKind.DeleteFolder, "Keep me") });
        Assert.Equal("folder is not on the managed list", Assert.Single(result.Refused).Reason);
        Assert.True(File.Exists(s.InTopPicks("Keep me", "note.txt")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_a_move_never_lands_onto_or_into_a_folder_that_is_there()
    {
        using var s = LastWeek();
        s.Staged("A (2001)", "A (2001).strm");
        s.Copies.Apply(new[] { new PlanStep(StepKind.MoveIn, "A (2001)", path: "A (2001)") });
        Assert.Equal("A (2001)/A (2001).strm", Sandbox.Text(s.InTopPicks("A (2001)", "A (2001).strm")));
        Assert.False(Directory.Exists(s.InTopPicks("A (2001)", "A (2001)")));
    }

    // ---- C47: leftovers ----

    [Fact, Trait("Check", "C47")]
    public void C47_a_run_after_a_failed_run_finishes_leaving_deletes_and_drops_stray_incoming_copies()
    {
        using var s = new Sandbox();
        s.Folder("Leaving (2000)", "x.strm");
        s.Folder("Half (2001)", "y.strm");
        s.Folder("Keep me", "note.txt");
        s.Data.SaveManaged(new[]
        {
            new ManagedEntry("Leaving (2000)", 1, TitleKind.Movie, EntryState.Leaving, null, null, false),
            new ManagedEntry("Leaving gone", 2, TitleKind.Movie, EntryState.Leaving, null, null, false),
            new ManagedEntry("Half (2001)", 3, TitleKind.Movie, EntryState.Incoming, 4, Sandbox.RankTime, false),
            new ManagedEntry("Never moved (2002)", 4, TitleKind.Movie, EntryState.Incoming, 5, Sandbox.RankTime, false),
        });

        s.Copies.Apply(SwapPlan.Leftovers(s.Managed(), s.Copies.FolderNames()));

        var m = s.Managed();
        var half = Assert.Single(m);
        Assert.Equal("Half (2001)", half.Folder);
        Assert.Equal(EntryState.Active, half.State);
        Assert.Null(half.Rank);
        Assert.Null(half.RankDateUtc);
        Assert.Equal(new[] { "Half (2001)", "Keep me" }, s.Copies.FolderNames());

        // Then it drops out normally in the next swap: nothing is orphaned.
        s.Copies.Apply(SwapPlan.SwapOut(s.Managed(), new PlannedPick[0], Sandbox.RankTime));
        Assert.Empty(s.Managed());
        Assert.Equal(new[] { "Keep me" }, s.Copies.FolderNames());

        // Leftovers are safe to repeat.
        s.Copies.Apply(SwapPlan.Leftovers(s.Managed(), s.Copies.FolderNames()));
        Assert.Empty(s.Managed());
    }

    // ---- C50: an old-plugin folder that is still a pick ----

    [Fact, Trait("Check", "C50")]
    public void C50_an_adopted_folder_still_a_pick_is_dropped_and_the_new_copy_takes_the_plain_name()
    {
        using var s = new Sandbox();
        s.Folder("Dune (2021)", "Dune (2021).strm", "Dune (2021).nfo", "old-only.jpg");
        s.Copies.TakeOver(f => new Dictionary<string, long?> { ["Dune (2021).strm"] = 7 }, new Dictionary<long, long>());
        var dropping = s.Managed().Where(e => e.Adopted).Select(e => e.Folder);
        var name = Assert.Single(SwapPlan.NewFolderNames(new[] { "Dune (2021)" }, s.Copies.FolderNames(), dropping));
        Assert.Equal("Dune (2021)", name);
        s.Staged(name, "Dune (2021).strm", "Dune (2021).nfo");
        var picks = new List<PlannedPick> { new PlannedPick(1, 7, TitleKind.Movie, name) };

        s.Copies.Apply(SwapPlan.SwapOut(s.Managed(), picks, Sandbox.RankTime));
        s.Copies.Apply(SwapPlan.SwapNew(s.Managed(), picks, Sandbox.RankTime, s.Copies.FolderNames()));

        var e = Assert.Single(s.Managed());
        Assert.Equal("Dune (2021)", e.Folder);
        Assert.False(e.Adopted);
        Assert.Equal(1, e.Rank);
        Assert.False(File.Exists(s.InTopPicks("Dune (2021)", "old-only.jpg")));
        Assert.Equal("Dune (2021)/Dune (2021).nfo/new", Sandbox.Text(s.InTopPicks("Dune (2021)", "Dune (2021).nfo")));
    }

    [SkippableFact, Trait("Check", "C50")]
    public void C50_if_the_old_folder_cant_be_deleted_the_new_copy_gets_dash_2_and_nothing_is_merged()
    {
        using var s = new Sandbox();
        var target = Path.Combine(s.Outside, "target");
        Sandbox.Write(Path.Combine(target, "Dune (2021).strm"), "old");
        Links.FolderSymlink(Path.Combine(s.TopPicks, "Dune (2021)"), target);
        s.Data.UpdateRun(_ => { });
        s.Data.SaveManaged(new[] { new ManagedEntry("Dune (2021)", 7, TitleKind.Movie, EntryState.Active, null, null, adopted: true,
            new Dictionary<string, long?> { ["Dune (2021).strm"] = 7 }) });
        s.Staged("Dune (2021)", "Dune (2021).strm", "Dune (2021).nfo");
        var picks = new List<PlannedPick> { new PlannedPick(1, 7, TitleKind.Movie, "Dune (2021)") };

        var outResult = s.Copies.Apply(SwapPlan.SwapOut(s.Managed(), picks, Sandbox.RankTime));
        Assert.Single(outResult.Refused);
        s.Copies.Apply(SwapPlan.SwapNew(s.Managed(), picks, Sandbox.RankTime, s.Copies.FolderNames()));

        var m = s.Managed().ToDictionary(e => e.Folder);
        Assert.Equal(EntryState.Leaving, m["Dune (2021)"].State);
        Assert.Null(m["Dune (2021)"].Rank);
        Assert.Equal(1, m["Dune (2021) - 2"].Rank);
        Assert.Equal("Dune (2021)/Dune (2021).strm/new", Sandbox.Text(s.InTopPicks("Dune (2021) - 2", "Dune (2021).strm")));
        Assert.Equal("old", Sandbox.Text(Path.Combine(target, "Dune (2021).strm")));
        Assert.False(File.Exists(Path.Combine(target, "Dune (2021).nfo")));
    }

    // ---- C70, C93, C43: copy files (file half) ----

    [Fact, Trait("Check", "C93")]
    public void C93_strm_text_is_written_exactly_utf8_with_no_bom_and_no_line_ending()
    {
        using var s = new Sandbox();
        var text = CopyText.Strm("N:\\Movies\\Amélie (2001)\\Amélie.mkv\r\n");
        s.Copies.WriteStagingText("Amélie (2001)", "Amélie (2001).strm", text);
        var bytes = File.ReadAllBytes(Path.Combine(s.Staging, "Amélie (2001)", "Amélie (2001).strm"));
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("N:\\Movies\\Amélie (2001)\\Amélie.mkv"), bytes);
    }

    [Fact, Trait("Check", "C93")]
    public void C93_a_subtitle_removed_from_the_source_is_removed_from_a_staying_copy_and_a_theme_keeps_its_extension()
    {
        using var s = new Sandbox();
        s.Data.UpdateRun(_ => { });
        var folder = "S (2001)";
        s.Folder(folder, "S (2001).strm", "S (2001).en.srt", "backdrops\\theme.mkv");
        var entry = new ManagedEntry(folder, 9, TitleKind.Movie, EntryState.Active, 1, Sandbox.RankTime, false,
            new Dictionary<string, long?> { ["S (2001).strm"] = 9 }, new[] { "S (2001).en.srt", Naming.ThemeVideoPath(".mkv") });
        s.Data.SaveManaged(new[] { entry });
        s.Staged(folder, "S (2001).strm");

        var plan = SwapPlan.StayingFiles(entry, new Dictionary<string, long> { ["S (2001).strm"] = 9 }, new string[0]);
        Assert.Empty(s.Copies.Apply(plan.Steps).Refused);

        Assert.False(File.Exists(s.InTopPicks(folder, "S (2001).en.srt")));
        Assert.True(File.Exists(s.InTopPicks(folder, "backdrops\\theme.mkv")));
        Assert.Equal(new[] { "backdrops\\theme.mkv" }, s.Entry(folder).Files);
    }

    [Fact, Trait("Check", "C70")]
    public void C70_episode_thumbs_season_posters_subtitles_and_idx_with_sub_are_written_into_staging_and_moved_in()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new ManagedEntry[0]);
        var folder = "Show (2019)";
        var ep = Naming.EpisodeBaseName("Show", 0, 1, null, "Pilot");
        var files = new[]
        {
            "tvshow.nfo",
            Naming.SeasonFolder(0) + "\\" + ep + ".strm",
            Naming.SeasonFolder(0) + "\\" + Naming.EpisodeThumbName(ep),
            Naming.SeasonPosterPath(0),
            Naming.SeasonPosterPath(1),
            Naming.SeasonFolder(0) + "\\" + Naming.SubtitleCopyName("Pilot.mkv", "Pilot.en.sub", ep),
            Naming.SeasonFolder(0) + "\\" + Naming.SubtitleCopyName("Pilot.mkv", "Pilot.en.idx", ep),
            Naming.ThemeSongPath("mp3"),
        };
        var source = Sandbox.Write(Path.Combine(s.Outside, "src.bin"), "picture");
        foreach (var f in files)
            s.Copies.CopyIntoStaging(folder, f, source);
        var entry = new ManagedEntry(folder, 4, TitleKind.Series, EntryState.Incoming, 1, Sandbox.RankTime, false);
        s.Copies.Apply(new[]
        {
            new PlanStep(StepKind.AddIncoming, folder, 1, Sandbox.RankTime, entry: entry),
            new PlanStep(StepKind.MoveIn, folder, 1, Sandbox.RankTime, path: folder),
            new PlanStep(StepKind.MarkActive, folder, 1, Sandbox.RankTime),
        });
        foreach (var f in files)
            Assert.Equal("picture", Sandbox.Text(s.InTopPicks(folder, f)));
        Assert.True(File.Exists(s.InTopPicks(folder, "Specials\\folder.jpg")));
        Assert.True(File.Exists(s.InTopPicks(folder, "Specials\\Show - S00E01 - Pilot.en.idx")));
        Assert.Equal(EntryState.Active, s.Entry(folder).State);
    }

    [Fact, Trait("Check", "C43")]
    public void C43_artwork_fallback_none_deletes_an_old_folder_png_from_a_staying_copy_and_failed_is_set()
    {
        using var s = LastWeek();
        var choice = EmbyRules.ArtworkFallback(drawn: false, backdropReadable: false, posterReadable: false);
        Assert.Equal(ArtworkChoice.None, choice);
        Assert.True(EmbyRules.ArtworkFailed(choice));
        var entry = s.Entry("B (2002)");
        var plan = SwapPlan.StayingFiles(entry, new Dictionary<string, long> { ["B (2002).strm"] = 2 }, new[] { "B (2002).nfo" });
        s.Staged("B (2002)", "B (2002).strm", "B (2002).nfo");
        s.Copies.Apply(plan.Steps);
        Assert.False(File.Exists(s.InTopPicks("B (2002)", "folder.png")));
        s.Data.UpdateRun(r => r.Picks.Add(new RunPick { Rank = 2, Folder = "B (2002)", ArtworkFailed = EmbyRules.ArtworkFailed(choice) }));
        Assert.True(s.Data.LoadRun().Value!.Picks.Single().ArtworkFailed);
    }

    [Fact, Trait("Check", "C43")]
    public void C43_artwork_fallback_bytes_are_copied_unchanged_under_the_fixed_png_name()
    {
        using var s = new Sandbox();
        var jpeg = Path.Combine(s.Outside, "backdrop.jpg");
        File.WriteAllBytes(jpeg, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3 });
        s.Copies.CopyIntoStaging("X (2000)", "folder.png", jpeg);
        s.Copies.CopyIntoStaging("X (2000)", "thumb.png", jpeg);
        Assert.Equal(File.ReadAllBytes(jpeg), File.ReadAllBytes(Path.Combine(s.Staging, "X (2000)", "folder.png")));
        Assert.Equal(File.ReadAllBytes(jpeg), File.ReadAllBytes(Path.Combine(s.Staging, "X (2000)", "thumb.png")));
    }

    [Theory, Trait("Check", "C80")]
    [InlineData("..", "x.strm")]
    [InlineData("A", "..\\x.strm")]
    [InlineData("A", "")]
    public void C80_staging_writes_with_a_bad_name_are_refused_and_write_nothing_outside(string folder, string path)
    {
        using var s = new Sandbox();
        var before = s.Snapshot();
        Assert.Throws<IOException>(() => s.Copies.WriteStagingText(folder, path, "x"));
        Assert.False(File.Exists(Path.Combine(s.DataRoot, "x.strm")));
        Assert.False(File.Exists(Path.Combine(s.Staging, "x.strm")));
        Assert.Equal(before.Keys.Where(k => !k.Contains("staging")), s.Snapshot().Keys.Where(k => !k.Contains("staging")));
    }

    [Fact, Trait("Check", "C36")]
    public void C36_steps_for_an_entry_that_is_gone_do_nothing()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new ManagedEntry[0]);
        var result = s.Copies.Apply(new[]
        {
            new PlanStep(StepKind.MarkLeaving, "Gone"),
            new PlanStep(StepKind.ClearRank, "Gone"),
            new PlanStep(StepKind.SetRank, "Gone", 1, Sandbox.RankTime),
            new PlanStep(StepKind.MarkActive, "Gone", 1, Sandbox.RankTime),
            new PlanStep(StepKind.RemoveEntry, "Gone"),
        });
        Assert.All(result.Outcomes, o => Assert.Equal(StepOutcome.Done, o));
        Assert.Empty(s.Managed());
    }

    [Fact, Trait("Check", "C36")]
    public void C36_adding_the_same_incoming_entry_twice_keeps_one()
    {
        using var s = new Sandbox();
        s.Data.SaveManaged(new ManagedEntry[0]);
        var e = new ManagedEntry("N (2020)", 1, TitleKind.Movie, EntryState.Incoming, 1, Sandbox.RankTime, false);
        var step = new PlanStep(StepKind.AddIncoming, e.Folder, 1, Sandbox.RankTime, entry: e);
        s.Copies.Apply(new[] { step, step });
        Assert.Single(s.Managed());
        var bad = new ManagedEntry("..", 1, TitleKind.Movie, EntryState.Incoming, 1, Sandbox.RankTime, false);
        Assert.Single(s.Copies.Apply(new[] { new PlanStep(StepKind.AddIncoming, "..", entry: bad) }).Refused);
        Assert.Single(s.Managed());
    }

    [Fact, Trait("Check", "C36")]
    public void C36_adding_a_file_to_an_entry_twice_keeps_one_and_an_item_carries_its_source()
    {
        using var s = LastWeek();
        s.Copies.Apply(new[]
        {
            new PlanStep(StepKind.AddFileToEntry, "A (2001)", path: "new.srt"),
            new PlanStep(StepKind.AddFileToEntry, "A (2001)", path: "new.srt"),
            new PlanStep(StepKind.AddFileToEntry, "A (2001)", path: "Season 1\\e.strm", itemId: 77),
        });
        var e = s.Entry("A (2001)");
        Assert.Single(e.Files, f => f == "new.srt");
        Assert.Equal(77, e.Items["Season 1\\e.strm"]);
        s.Copies.Apply(new[] { new PlanStep(StepKind.RemoveFileFromEntry, "A (2001)", path: "Season 1\\e.strm") });
        Assert.DoesNotContain("Season 1\\e.strm", s.Entry("A (2001)").Items.Keys);
    }
}
