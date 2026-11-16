using System;
using TopPicksPlus.Logic;
using Xunit;

namespace TopPicksPlus.Tests.Logic;

/// <summary>D152: the Top Picks slideshow in place of the Latest Top Picks row on each user's home screen.</summary>
public class HomeScreenRulesTests
{
    private const string Lib = "3573669";

    private static HomeSectionInfo Show(string id, string? parent = Lib) => new HomeSectionInfo(id, "items", "spotlight", parent);
    private static HomeSectionInfo Row(string id = "latestmedia_3573669") => new HomeSectionInfo(id, "latestmedia", null, Lib);
    private static HomeSectionInfo Other(string id, string type = "latestmedia", string? view = null, string? parent = "4") => new HomeSectionInfo(id, type, view, parent);

    [Fact, Trait("Check", "C103")]
    public void C103_turning_it_on_adds_the_slideshow_and_removes_the_row_only()
    {
        var plan = HomeScreenRules.Plan(new[] { Other("resume", "resume"), Row(), Other("latestmedia_4"), Show("random", parent: null) }, Lib, true, wasOn: false);
        Assert.True(plan.AddSlideshow);
        Assert.Null(plan.UpdateSlideshow);
        Assert.Equal(new[] { "latestmedia_3573669" }, plan.Delete);
        Assert.False(plan.AddRow);
        Assert.False(plan.Empty);
    }

    [Fact, Trait("Check", "C103")]
    public void C103_with_it_on_and_in_place_the_slideshow_is_only_refreshed_and_extra_copies_go()
    {
        var plan = HomeScreenRules.Plan(new[] { Show("a"), Other("latestmedia_4"), Show("b") }, Lib, true, wasOn: false);
        Assert.False(plan.AddSlideshow);
        Assert.Equal("a", plan.UpdateSlideshow);
        Assert.Equal(new[] { "b" }, plan.Delete);
        Assert.False(plan.AddRow);
        Assert.False(plan.Empty);

        var settled = HomeScreenRules.Plan(new[] { Show("a") }, Lib, true, wasOn: false);
        Assert.Equal("a", settled.UpdateSlideshow);
        Assert.Empty(settled.Delete);
        Assert.False(settled.AddSlideshow);
        Assert.False(settled.Empty);
    }

    [Fact, Trait("Check", "C103")]
    public void C103_on_with_an_empty_home_screen_only_adds_the_slideshow()
    {
        var plan = HomeScreenRules.Plan(Array.Empty<HomeSectionInfo>(), Lib, true, wasOn: true);
        Assert.True(plan.AddSlideshow);
        Assert.Null(plan.UpdateSlideshow);
        Assert.Empty(plan.Delete);
        Assert.False(plan.AddRow);
        Assert.False(plan.Empty);
    }

    [Fact, Trait("Check", "C103")]
    public void C103_with_it_off_and_never_turned_on_nothing_is_touched_not_even_a_users_own_spotlight()
    {
        var plan = HomeScreenRules.Plan(new[] { Show("users-own"), Other("latestmedia_4") }, Lib, false, wasOn: false);
        Assert.True(plan.Empty);
        Assert.Empty(plan.Delete);
        Assert.False(plan.AddRow);
    }

    [Fact, Trait("Check", "C103")]
    public void C103_a_slideshow_emby_stored_without_its_parent_is_still_recognised_by_its_title_so_no_second_copy_is_added()
    {
        var stored = new HomeSectionInfo("a", "items", "spotlight", null, "top picks");
        Assert.True(HomeScreenRules.IsSlideshow(stored, Lib));
        var plan = HomeScreenRules.Plan(new[] { stored }, Lib, true, wasOn: true);
        Assert.False(plan.AddSlideshow);
        Assert.Equal("a", plan.UpdateSlideshow);

        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("b", "items", "spotlight", null, "Spotlight"), Lib));
        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("c", "items", "spotlight", "4", "Top Picks"), Lib));
        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("d", "items", "spotlight", "", null), ""));
        Assert.False(HomeScreenRules.IsRow(new HomeSectionInfo("", "latestmedia", null, ""), ""));
    }

    [Fact, Trait("Check", "C103")]
    public void C103_turning_it_off_removes_the_slideshow_and_puts_the_row_back()
    {
        var plan = HomeScreenRules.Plan(new[] { Show("a"), Other("latestmedia_4") }, Lib, false, wasOn: true);
        Assert.False(plan.AddSlideshow);
        Assert.Null(plan.UpdateSlideshow);
        Assert.Equal(new[] { "a" }, plan.Delete);
        Assert.True(plan.AddRow);
        Assert.False(plan.Empty);
    }

    [Fact, Trait("Check", "C103")]
    public void C103_with_it_off_a_row_already_there_or_removed_by_the_user_is_left_alone()
    {
        var withRow = HomeScreenRules.Plan(new[] { Show("a"), Row() }, Lib, false, wasOn: true);
        Assert.Equal(new[] { "a" }, withRow.Delete);
        Assert.False(withRow.AddRow);
        Assert.False(withRow.Empty);

        var userRemovedRow = HomeScreenRules.Plan(new[] { Other("latestmedia_4") }, Lib, false, wasOn: true);
        Assert.True(userRemovedRow.Empty);

        var nothing = HomeScreenRules.Plan(Array.Empty<HomeSectionInfo>(), Lib, false, wasOn: true);
        Assert.True(nothing.Empty);
    }

    [Fact, Trait("Check", "C103")]
    public void C103_sections_are_recognised_by_type_view_and_parent_ignoring_case()
    {
        Assert.True(HomeScreenRules.IsSlideshow(new HomeSectionInfo("x", "Items", "Spotlight", Lib), Lib));
        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("x", "items", "spotlight", "4"), Lib));
        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("x", "items", null, Lib), Lib));
        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("x", "latestmedia", "spotlight", Lib), Lib));
        Assert.False(HomeScreenRules.IsSlideshow(new HomeSectionInfo("x", "items", "spotlight", null), Lib));

        Assert.True(HomeScreenRules.IsRow(new HomeSectionInfo("LATESTMEDIA_3573669", null, null, null), Lib));
        Assert.True(HomeScreenRules.IsRow(new HomeSectionInfo("custom", "LatestMedia", null, Lib), Lib));
        Assert.False(HomeScreenRules.IsRow(new HomeSectionInfo("latestmedia_4", "latestmedia", null, "4"), Lib));
        Assert.False(HomeScreenRules.IsRow(new HomeSectionInfo("custom", "items", null, Lib), Lib));
        Assert.Equal("latestmedia_3573669", HomeScreenRules.RowId(Lib));
    }

    [Fact, Trait("Check", "C103")]
    public void C103_every_other_library_is_excluded_by_number_and_guid_and_top_picks_never()
    {
        var excluded = HomeScreenRules.ExcludedFolders(new (string, string?)[]
        {
            ("4", "d71235637d47a81c5ddf230c922fa46b"),
            (Lib, "016706ee7ad84c8f8cf7629346149504"),
            ("277391", null),
            ("50", "50"),
            ("4", "D71235637D47A81C5DDF230C922FA46B"),
        }, Lib);
        Assert.Equal(new[] { "4", "d71235637d47a81c5ddf230c922fa46b", "277391", "50" }, excluded);
    }
}
