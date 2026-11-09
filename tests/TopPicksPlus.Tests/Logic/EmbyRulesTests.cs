using System;
using System.Collections.Generic;
using System.Linq;
using TopPicksPlus.Logic;
using Xunit;
using static TopPicksPlus.Tests.Logic.Make;

namespace TopPicksPlus.Tests.Logic;

/// <summary>The rules behind chunk 4's automatic checks (D79), and takeover and unmanaged folders (D38, D90, D102).</summary>
public class EmbyRulesTests
{
    // ---- Row-check user (C57) ----

    private static readonly UserInfo Admin1 = new UserInfo("admin1", true, false, false);
    private static readonly UserInfo Admin2 = new UserInfo("admin2", true, false, false);
    private static readonly UserInfo Kid = new UserInfo("kid", false, true, false);
    private static readonly UserInfo Blocked = new UserInfo("blocked", false, false, true);
    private static readonly UserInfo Plain1 = new UserInfo("plain1", false, false, false);
    private static readonly UserInfo Plain2 = new UserInfo("plain2", false, false, false);

    [Fact, Trait("Check", "C57")]
    public void C57_no_unrestricted_user_falls_back_to_the_first_admin_and_says_so()
    {
        var (user, fell) = EmbyRules.RowCheckUser(new[] { Kid, Admin1, Blocked, Admin2 }, null);
        Assert.Equal("admin1", user);
        Assert.True(fell);
    }

    [Fact, Trait("Check", "C57")]
    public void C57_default_is_the_first_unrestricted_non_admin_in_list_order()
    {
        var (user, fell) = EmbyRules.RowCheckUser(new[] { Admin1, Kid, Plain2, Blocked, Plain1 }, "  ");
        Assert.Equal("plain2", user);
        Assert.False(fell);
    }

    [Fact, Trait("Check", "C57")]
    public void C57_a_chosen_user_is_used_and_a_deleted_one_falls_back_to_the_default()
    {
        var chosen = EmbyRules.RowCheckUser(new[] { Admin1, Kid, Plain1 }, "KID");
        Assert.Equal("kid", chosen.UserId);
        Assert.False(chosen.FellBackToAdmin);
        var gone = EmbyRules.RowCheckUser(new[] { Admin1, Kid, Plain1 }, "gone");
        Assert.Equal("plain1", gone.UserId);
        Assert.False(gone.FellBackToAdmin);
        var none = EmbyRules.RowCheckUser(new[] { Kid }, null);
        Assert.Null(none.UserId);
        Assert.False(none.FellBackToAdmin);
    }

    // ---- Old plugin (C74, C63) ----

    [Fact, Trait("Check", "C74")]
    public void C74_old_plugin_guard_never_matches_top_picks_plus_itself()
    {
        Assert.False(EmbyRules.IsOldPlugin(new InstalledPlugin("TopPicksPlus", new Guid("744d00f2-735f-4d57-a952-47e98a5b6f0d"))));
        Assert.False(EmbyRules.IsOldPlugin(new InstalledPlugin("Emby.TopPicks", EmbyRules.ThisPluginId)));
        Assert.False(EmbyRules.IsOldPlugin(new InstalledPlugin("TopPicksPlus", EmbyRules.OldPluginId)));
        Assert.False(EmbyRules.IsOldPlugin(new InstalledPlugin("Emby.TopPicksPlus", Guid.NewGuid())));
        Assert.False(EmbyRules.IsOldPlugin(new InstalledPlugin("Emby.TopPick", Guid.NewGuid())));
        Assert.False(EmbyRules.IsOldPlugin(new InstalledPlugin(null, Guid.Empty)));
    }

    [Fact, Trait("Check", "C74")]
    public void C74_old_plugin_matches_exactly_on_assembly_name_or_id()
    {
        Assert.True(EmbyRules.IsOldPlugin(new InstalledPlugin("Emby.TopPicks", Guid.NewGuid())));
        Assert.True(EmbyRules.IsOldPlugin(new InstalledPlugin("emby.toppicks", Guid.NewGuid())));
        Assert.True(EmbyRules.IsOldPlugin(new InstalledPlugin("Renamed", new Guid("672630fc-6fb8-4207-9c7b-ab414cd85b53"))));
        Assert.True(EmbyRules.OldPluginLoaded(new[] { new InstalledPlugin("Other", Guid.NewGuid()), new InstalledPlugin("Emby.TopPicks", Guid.NewGuid()) }));
        Assert.False(EmbyRules.OldPluginLoaded(new[] { new InstalledPlugin("TopPicksPlus", EmbyRules.ThisPluginId) }));
    }

    [Fact, Trait("Check", "C63")]
    public void C63_with_the_old_plugin_loaded_no_plays_are_logged_and_no_watched_state_is_copied()
    {
        var a = EmbyRules.Allowed(oldPluginLoaded: true, dataNewer: false, managedReadable: true);
        Assert.False(a.LogPlays);
        Assert.False(a.CreditCopyPlays);
        Assert.False(a.SyncWatched);
        Assert.False(a.OrderGuard);
        Assert.False(a.Import);
        Assert.False(a.WriteDataFiles);
        Assert.False(EmbyRules.ShouldSaveListeningSince(oldPluginLoaded: true, alreadySet: false, stateReadable: true));
        var ok = EmbyRules.Allowed(false, false, true);
        Assert.True(ok.LogPlays && ok.CreditCopyPlays && ok.SyncWatched && ok.OrderGuard && ok.Import && ok.WriteDataFiles);
    }

    [Fact, Trait("Check", "C37")]
    public void D55_with_managed_json_damaged_real_plays_still_log_but_copy_plays_sync_and_guard_stop()
    {
        var a = EmbyRules.Allowed(false, false, managedReadable: false);
        Assert.True(a.LogPlays);
        Assert.False(a.CreditCopyPlays);
        Assert.False(a.SyncWatched);
        Assert.False(a.OrderGuard);
        Assert.True(a.WriteDataFiles);
    }

    // ---- Crediting plays (C71, C11) ----

    private static readonly ManagedEntry ShowCopy = Entry("Show (2010)", 500, EntryState.Active, 1, kind: TitleKind.Series,
        items: new Dictionary<string, long?> { { "Season 1\\Show - S01E01 - A.strm", 501 } });

    [Fact, Trait("Check", "C71")]
    public void C71_play_on_an_old_plugin_copy_not_on_the_list_counts_for_the_real_title_through_its_strm()
    {
        var played = new PlayedItem(9001, 9000, "Old Show\\Season 2\\ep.strm");
        var target = new PlayedItem(702, 700, null);
        var credit = EmbyRules.CreditFor(played, new[] { ShowCopy }, target)!;
        Assert.NotNull(credit);
        Assert.Equal(700, credit.SourceId);
        Assert.Equal(702, credit.EpisodeId);

        var movie = EmbyRules.CreditFor(new PlayedItem(8001, null, "Old Film\\Old Film.strm"), new[] { ShowCopy }, new PlayedItem(42, null, null))!;
        Assert.Equal(42, movie.SourceId);
        Assert.Null(movie.EpisodeId);
        Assert.Null(EmbyRules.CreditFor(new PlayedItem(8001, null, "Old Film\\Old Film.strm"), new[] { ShowCopy }, null));
    }

    [Fact, Trait("Check", "C71")]
    public void C71_play_on_a_managed_copy_counts_for_its_source_through_the_managed_list_first()
    {
        // The .strm target is ignored when the folder is on the list (D64: managed list first).
        var credit = EmbyRules.CreditFor(new PlayedItem(99999, 99998, "show (2010)\\season 1\\show - s01e01 - a.strm"), new[] { ShowCopy }, new PlayedItem(1, null, null))!;
        Assert.Equal(500, credit.SourceId);
        Assert.Equal(501, credit.EpisodeId);
        var adoptedNoSource = Entry("Lost", null, adopted: true);
        Assert.Null(EmbyRules.CreditFor(new PlayedItem(5, null, "Lost\\Lost.strm"), new[] { adoptedNoSource }, new PlayedItem(1, null, null)));
    }

    [Fact, Trait("Check", "C11")]
    public void A_play_on_a_real_title_counts_for_itself_or_its_series()
    {
        var movie = EmbyRules.CreditFor(new PlayedItem(10, null, null), Array.Empty<ManagedEntry>(), null)!;
        Assert.Equal(10, movie.SourceId);
        Assert.Null(movie.EpisodeId);
        var ep = EmbyRules.CreditFor(new PlayedItem(11, 12, null), Array.Empty<ManagedEntry>(), null)!;
        Assert.Equal(12, ep.SourceId);
        Assert.Equal(11, ep.EpisodeId);
    }

    // ---- Import (C58, C71, C91) ----

    private static readonly DateTime Listening = new DateTime(2026, 11, 15, 11, 0, 0, DateTimeKind.Utc);

    private static ImportRow Row(DateTime utc, long? source, string user = "u", double minutes = 50) =>
        new ImportRow(user, utc, utc.Date, source.HasValue ? new Credit(source.Value, source.Value + 1) : null, minutes, 100);

    [Fact, Trait("Check", "C71")]
    public void C71_imported_rows_for_items_that_no_longer_exist_are_ignored_and_counted()
    {
        var r = EmbyRules.Import(new[] { Row(Sunday.AddDays(-2), 1), Row(Sunday.AddDays(-2), null), Row(Sunday.AddDays(-3), null) }, Sunday, Listening, PlayOrigin.PlaybackReporting);
        Assert.Single(r.Plays);
        Assert.Equal(2, r.Ignored);
        Assert.Equal(PlayOrigin.PlaybackReporting, r.Plays[0].Origin);
        Assert.Equal(2, r.Plays[0].EpisodeId);
    }

    [Fact, Trait("Check", "C91")]
    public void C91_import_takes_plays_up_to_listening_since_including_earlier_the_same_day()
    {
        var rows = new[]
        {
            Row(Sunday.AddDays(-8).AddHours(20), 1),   // before the window
            Row(Sunday.AddDays(-7).AddHours(1), 2),    // first day of the window
            Row(Sunday.AddHours(9), 3),                // run day, before listening started
            Row(Listening, 4),                         // at the cut-off: already in the live log
            Row(Listening.AddMinutes(5), 5),
        };
        var r = EmbyRules.Import(rows, Sunday, Listening, PlayOrigin.PlaybackReporting);
        Assert.Equal(new long[] { 2, 3 }, r.Plays.Select(p => p.SourceId));
    }

    [Fact, Trait("Check", "C91")]
    public void C91_next_weeks_window_counts_those_run_day_plays_once()
    {
        var imported = EmbyRules.Import(new[] { Row(Sunday.AddHours(9), 3, minutes: 60) }, Sunday, Listening, PlayOrigin.PlaybackReporting).Plays;
        var live = new[] { Make.APlay("u", 3, Sunday, 60, runtime: 100, episode: 4) };
        var all = imported.Concat(live).ToList();
        Assert.Empty(Counting.ViewerDays(all, Sunday));
        Assert.Equal(1, Counting.ViewerDays(all, Sunday.AddDays(7))[3]);
    }

    [Fact, Trait("Check", "C91")]
    public void C91_listening_since_is_saved_once_only_with_the_old_plugin_gone()
    {
        Assert.True(EmbyRules.ShouldSaveListeningSince(false, false, true));
        Assert.False(EmbyRules.ShouldSaveListeningSince(false, alreadySet: true, true));
        Assert.False(EmbyRules.ShouldSaveListeningSince(oldPluginLoaded: true, false, true));
    }

    [Fact, Trait("Check", "C58")]
    public void C58_plays_already_in_the_log_are_not_imported_again_and_fallback_titles_are_one_viewer_day()
    {
        var fallback = EmbyRules.Import(new[] { Row(Sunday.AddDays(-3), 7, "a", 0), Row(Sunday.AddDays(-3), 8, "b", 0), Row(Listening.AddHours(1), 9, "c", 0) },
            Sunday, Listening, PlayOrigin.LastPlayed);
        Assert.Equal(new long[] { 7, 8 }, fallback.Plays.Select(p => p.SourceId));
        Assert.All(fallback.Plays, p =>
        {
            Assert.True(p.MarkedPlayed);
            Assert.Equal(0, p.Minutes);
            Assert.Null(p.EpisodeId);
            Assert.Equal(PlayOrigin.LastPlayed, p.Origin);
        });
        var days = Counting.ViewerDays(fallback.Plays, Sunday);
        Assert.Equal(1, days[7]);
        Assert.Equal(1, days[8]);
        Assert.False(RunRules.ShouldImport(anyImportMarker: true));
    }

    // ---- Row check (C81, C31, C60) ----

    private static RowItem R(int rank, string? folder = null) => new RowItem("#" + rank, folder ?? "F" + rank, rank);
    private static RowItem X(string label) => new RowItem(label, null, null);

    [Fact, Trait("Check", "C81")]
    public void C81_extras_do_not_fail_the_check_and_are_listed()
    {
        var expected = Enumerable.Range(1, 10).Select(i => R(i)).ToList();
        var actual = new List<RowItem> { X("Keep me.strm") };
        actual.AddRange(expected.Take(5));
        actual.Add(new RowItem("Unranked leftover", "Leftover", null));
        actual.AddRange(expected.Skip(5));
        var r = EmbyRules.CheckRow(expected, actual);
        Assert.True(r.Passed);
        Assert.Equal(new[] { "Keep me.strm", "Unranked leftover" }, r.Extras);
    }

    [Fact, Trait("Check", "C81")]
    public void C81_a_wrong_order_among_ranked_copies_still_fails()
    {
        var expected = Enumerable.Range(1, 10).Select(i => R(i)).ToList();
        var actual = expected.ToList();
        (actual[3], actual[4]) = (actual[4], actual[3]);
        Assert.False(EmbyRules.CheckRow(expected, actual).Passed);
        Assert.False(EmbyRules.CheckRow(expected, expected.Take(9)).Passed);
        Assert.False(EmbyRules.CheckRow(expected.Take(9), expected).Passed);
    }

    [Fact, Trait("Check", "C81")]
    public void D104_the_actual_row_must_itself_be_in_rank_order_even_if_both_lists_agree()
    {
        var wrong = new[] { R(2), R(1), R(3) };
        Assert.False(EmbyRules.CheckRow(wrong, wrong).Passed);
    }

    [Fact, Trait("Check", "C81")]
    public void D104_episodes_of_a_series_count_once_as_their_series_copy()
    {
        var expected = new[] { R(1, "Show"), R(2, "Film") };
        var actual = new[] { new RowItem("S01E03", "Show", 1), new RowItem("S01E02", "Show", 1), new RowItem("Film", "Film", 2) };
        Assert.True(EmbyRules.CheckRow(expected, actual).Passed);
        Assert.Equal(new[] { 1, 2 }, EmbyRules.CheckRow(expected, actual).ActualRanks);
    }

    [Fact, Trait("Check", "C31")]
    public void Restricted_user_missing_blocked_titles_still_passes_if_the_rest_are_in_order()
    {
        var visible = new[] { R(1), R(2), R(4), R(7) };
        Assert.True(EmbyRules.CheckRow(visible, visible).Passed);
        Assert.True(EmbyRules.CheckRow(Array.Empty<RowItem>(), Array.Empty<RowItem>()).Passed);
    }

    // ---- Watched sync echo (D103) ----

    [Fact, Trait("Check", "C84")]
    public void D103_the_plugins_own_write_is_not_copied_back_but_a_users_mark_is()
    {
        var wrote = new DateTime(2026, 11, 15, 6, 0, 0, DateTimeKind.Utc);
        Assert.True(EmbyRules.IsEcho(wrote, true, wrote.AddSeconds(2), true));
        Assert.False(EmbyRules.IsEcho(wrote, true, wrote.AddSeconds(2), false));
        Assert.False(EmbyRules.IsEcho(wrote, true, wrote.AddMinutes(5), true));
        Assert.False(EmbyRules.IsEcho(null, true, wrote, true));
        Assert.False(EmbyRules.IsEcho(wrote, true, wrote.AddSeconds(-1), true));
    }

    // ---- Artwork fallback plan (C43, D98, D112) ----

    [Theory, Trait("Check", "C43")]
    [InlineData(true, false, false, ArtworkChoice.Drawn, false)]
    [InlineData(false, true, true, ArtworkChoice.BackdropBytes, true)]
    [InlineData(false, false, true, ArtworkChoice.PosterBytes, true)]
    [InlineData(false, false, false, ArtworkChoice.None, true)]
    public void C43_when_drawing_fails_the_copy_gets_a_readable_fallback_and_artworkFailed(bool drawn, bool backdrop, bool poster, ArtworkChoice expected, bool failed)
    {
        var choice = EmbyRules.ArtworkFallback(drawn, backdrop, poster);
        Assert.Equal(expected, choice);
        Assert.Equal(failed, EmbyRules.ArtworkFailed(choice));
    }

    // ---- Takeover and unmanaged folders (C46, C18, C85) ----

    private static FolderListing Folder(string name, params string[] files) => new FolderListing(name, files);

    [Fact, Trait("Check", "C46")]
    public void C46_old_series_folder_with_strm_only_in_season_folders_is_adopted()
    {
        var series = Folder("The Walking Dead (2010)", "tvshow.nfo", "folder.png", "Season 1\\TWD - S01E01.strm", "Season 1\\TWD - S01E01.nfo");
        Assert.True(Takeover.IsOldPluginFolder(series));
        var adopt = Takeover.ToAdopt(new[] { series, Folder("Keep me", "note.txt"), Folder("Only strm", "a.strm"), Folder("Only nfo", "a.nfo") }, Array.Empty<string>());
        Assert.Equal(new[] { "The Walking Dead (2010)" }, adopt.Select(a => a.Name));

        var entry = Takeover.Adopt(series, new Dictionary<string, long?> { { "Season 1\\TWD - S01E01.strm", 901 } }, new Dictionary<long, long> { { 901, 900 } });
        Assert.True(entry.Adopted);
        Assert.Equal(EntryState.Active, entry.State);
        Assert.Null(entry.Rank);
        Assert.Null(entry.RankDateUtc);
        Assert.Equal(900, entry.SourceId);
        Assert.Equal(TitleKind.Series, entry.Kind);
        Assert.Equal(901, entry.Items["Season 1\\TWD - S01E01.strm"]);
        Assert.Equal(new[] { "folder.png", "Season 1\\TWD - S01E01.nfo", "tvshow.nfo" }, entry.Files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
    }

    [Fact, Trait("Check", "C46")]
    public void Takeover_skips_listed_folders_and_bad_names_and_kinds_folders_with_no_source()
    {
        var a = Folder("Film (2000)", "Film.strm", "Film.nfo");
        var b = Folder("Listed", "x.strm", "x.nfo");
        var bad = Folder("bad.", "x.strm", "x.nfo");
        Assert.Equal(new[] { "Film (2000)" }, Takeover.ToAdopt(new[] { a, b, bad }, new[] { "LISTED" }).Select(f => f.Name));

        var noSourceSeries = Takeover.Adopt(Folder("S", "Season 2\\x.strm", "Season 2\\x.nfo"), new Dictionary<string, long?>(), new Dictionary<long, long>());
        Assert.Null(noSourceSeries.SourceId);
        Assert.Equal(TitleKind.Series, noSourceSeries.Kind);
        Assert.Null(noSourceSeries.Items["Season 2\\x.strm"]);
        var noSourceMovie = Takeover.Adopt(Folder("M", "x.strm", "x.nfo"), new Dictionary<string, long?>(), new Dictionary<long, long>());
        Assert.Equal(TitleKind.Movie, noSourceMovie.Kind);
        var movie = Takeover.Adopt(Folder("M", "x.strm", "x.nfo"), new Dictionary<string, long?> { { "x.strm", 55 } }, new Dictionary<long, long>());
        Assert.Equal(55, movie.SourceId);
        Assert.Equal(TitleKind.Movie, movie.Kind);
    }

    [Fact, Trait("Check", "C85")]
    public void D102_unmanaged_folders_are_those_with_a_video_and_keep_me_is_never_reported()
    {
        var folders = new[]
        {
            Folder("Keep me", "note.txt"),
            Folder("Stray", "Stray.strm"),
            Folder("Raw", "deep\\film.MKV"),
            Folder("Managed", "m.strm", "m.nfo"),
            Folder("Empty"),
        };
        Assert.Equal(new[] { "Raw", "Stray" }, Takeover.UnmanagedFolders(folders, new[] { "managed" }));
    }
}
