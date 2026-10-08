using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class EditableConfigurationTests
{
    [Fact]
    public void Round_trips_allowance_and_block_completely_rules()
    {
        const string json = """
        {
          "rules": [
            {
              "id": "short-video", "name": "Short video",
              "allowanceMinutes": 5, "allDay": true,
              "sites": [
                { "label": "YouTube Shorts", "url": "youtube.com/shorts", "includeSubpaths": true }
              ]
            },
            {
              "id": "bedtime", "name": "Bedtime",
              "allDay": false, "from": "23:00", "to": "07:00", "days": ["Monday"],
              "sites": [ { "label": "Instagram", "url": "instagram.com" } ]
            }
          ]
        }
        """;

        EditableConfiguration editable = EditableConfiguration.FromJson(json);
        Assert.Equal(2, editable.Rules.Count);

        EditableConfiguration.EditableRule shortVideo = editable.Rules[0];
        Assert.False(shortVideo.BlockCompletely);
        Assert.Equal(5, shortVideo.AllowanceMinutes);
        Assert.True(shortVideo.AllDay);
        Assert.Equal("YouTube Shorts", Assert.Single(shortVideo.Sites).Label);

        EditableConfiguration.EditableRule bedtime = editable.Rules[1];
        Assert.True(bedtime.BlockCompletely);
        Assert.False(bedtime.AllDay);
        Assert.Equal(new TimeOnly(23, 0), bedtime.From);
        Assert.Equal(new[] { DayOfWeek.Monday }, bedtime.Days);

        // Re-serialize, reload, and verify matching still works.
        BlockerConfiguration blocker = EditableConfiguration
            .FromJson(editable.ToJson())
            .ToBlockerConfiguration();

        Assert.Contains(blocker.MatchingRules(new Uri("https://youtube.com/shorts/x")),
            r => r.Id == "short-video");
        Assert.Contains(blocker.MatchingRules(new Uri("https://instagram.com/direct")),
            r => r.Id == "bedtime");
    }

    [Fact]
    public void Editing_a_site_url_changes_what_is_matched()
    {
        EditableConfiguration editable = EditableConfiguration.FromJson("""
        {
          "rules": [
            { "id": "r", "name": "R", "allowanceMinutes": 5, "allDay": true,
              "sites": [ { "label": "Reels", "url": "instagram.com/reels" } ] }
          ]
        }
        """);

        EditableConfiguration.EditableSite site = editable.Rules[0].Sites[0];
        site.Host = "tiktok.com";
        site.Pages[0].Path = "foryou/";

        BlockerConfiguration blocker = editable.ToBlockerConfiguration();
        Assert.NotEmpty(blocker.MatchingRules(new Uri("https://tiktok.com/foryou/x")));
        Assert.Empty(blocker.MatchingRules(new Uri("https://instagram.com/reels/x")));
    }

    [Fact]
    public void Old_catalog_picks_are_upgraded_to_pages_and_saved_in_the_new_format()
    {
        EditableConfiguration editable = EditableConfiguration.FromJson("""
        { "rules": [ { "id": "feeds", "name": "Feeds", "allowanceMinutes": 5, "allDay": true,
            "sites": [ { "catalogId": "ig-feed" } ] } ] }
        """);

        EditableConfiguration.EditableSite instagram = Assert.Single(editable.Rules[0].Sites);
        Assert.Equal("instagram.com", instagram.Host);
        Assert.Equal(new[] { "Home feed" }, instagram.Pages.Where(p => p.Block).Select(p => p.Name));
        Assert.DoesNotContain("ig-feed", editable.ToJson());

        BlockerConfiguration blocker = EditableConfiguration.FromJson(editable.ToJson()).ToBlockerConfiguration();
        Assert.NotEmpty(blocker.MatchingRules(new Uri("https://www.instagram.com/")));
        Assert.Empty(blocker.MatchingRules(new Uri("https://www.instagram.com/?variant=following")));
    }

    [Fact]
    public void Page_edits_round_trip_including_conditions_and_everything_else()
    {
        EditableConfiguration editable = EditableConfiguration.FromJson("""{ "rules": [] }""");
        var rule = new EditableConfiguration.EditableRule { Id = "r", Name = "R" };
        var site = new EditableConfiguration.EditableSite { Label = "News", Host = "www.News.example", BlockEverythingElse = true };
        site.Pages.Add(new EditableConfiguration.EditablePage
        {
            Name = "Sport",
            Path = "/sport",
            Match = PageMatch.Exact,
            Block = false,
            Conditions = { new EditableConfiguration.EditableCondition("tab", "live") },
        });
        rule.Sites.Add(site);
        editable.Rules.Add(rule);

        EditableConfiguration reloaded = EditableConfiguration.FromJson(editable.ToJson());
        EditableConfiguration.EditableSite back = Assert.Single(Assert.Single(reloaded.Rules).Sites);
        Assert.Equal("news.example", back.Host);
        Assert.True(back.BlockEverythingElse);
        EditableConfiguration.EditablePage page = Assert.Single(back.Pages);
        Assert.Equal(PageMatch.Exact, page.Match);
        Assert.False(page.Block);
        Assert.Equal(new EditableConfiguration.EditableCondition("tab", "live"), Assert.Single(page.Conditions));

        Rule compiled = reloaded.Rules[0].ToRule();
        Assert.False(compiled.MatchesUrl(new Uri("https://news.example/sport?tab=live")));
        Assert.True(compiled.MatchesUrl(new Uri("https://news.example/sport")));
    }
}
