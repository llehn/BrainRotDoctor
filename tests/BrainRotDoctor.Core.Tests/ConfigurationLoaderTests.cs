using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class ConfigurationLoaderTests
{
    [Fact]
    public void Loads_an_allowance_rule()
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
            }
          ]
        }
        """;

        BlockerConfiguration config = ConfigurationLoader.Load(json);
        Rule rule = Assert.Single(config.Rules);

        Assert.Equal(TimeSpan.FromMinutes(5), rule.Allowance);
        Assert.False(rule.BlocksCompletely);
        Assert.True(rule.AllDay);
        Assert.True(rule.MatchesUrl(new Uri("https://youtube.com/shorts/x")));
    }

    [Fact]
    public void Switched_off_rules_are_validated_but_not_enforced()
    {
        const string json = """
        {
          "rules": [
            { "id": "on", "allDay": true, "sites": [ { "label": "Instagram", "url": "instagram.com" } ] },
            { "id": "off", "enabled": false, "allDay": true, "sites": [ { "label": "TikTok", "url": "tiktok.com" } ] }
          ]
        }
        """;

        Rule rule = Assert.Single(ConfigurationLoader.Load(json).Rules);
        Assert.Equal("on", rule.Id);

        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load(
            """{ "rules": [ { "id": "off", "enabled": false, "allDay": false, "from": "09:00", "to": "09:00", "sites": [ { "label": "X", "url": "x.com" } ] } ] }"""));
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load(
            """{ "rules": [ { "id": "a", "sites": [ { "label": "X", "url": "x.com" } ] }, { "id": "a", "enabled": false, "sites": [ { "label": "X", "url": "x.com" } ] } ] }"""));
    }

    [Fact]
    public void Loads_a_block_completely_windowed_rule()
    {
        const string json = """
        {
          "rules": [
            {
              "id": "bedtime", "name": "Bedtime",
              "allDay": false, "from": "23:00", "to": "07:00",
              "days": ["Monday", "Tuesday"],
              "sites": [ { "label": "Instagram", "url": "instagram.com" } ]
            }
          ]
        }
        """;

        Rule rule = Assert.Single(ConfigurationLoader.Load(json).Rules);

        Assert.True(rule.BlocksCompletely);
        Assert.False(rule.AllDay);
        Assert.Equal(new TimeOnly(23, 0), rule.From);
        Assert.Equal(new TimeOnly(7, 0), rule.To);
        Assert.True(rule.WrapsMidnight);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, rule.Days.OrderBy(d => d));
    }

    [Fact]
    public void Allowance_of_an_hour_or_more_is_rejected()
    {
        const string json = """
        { "rules": [ { "id": "r", "allowanceMinutes": 60,
          "sites": [ { "url": "a.com" } ] } ] }
        """;
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load(json));
    }

    [Fact]
    public void Malformed_json_throws_configuration_exception()
    {
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load("{ not json"));
    }

    [Theory]
    [InlineData("2m", 0, 2, 0)]
    [InlineData("1h", 1, 0, 0)]
    [InlineData("90s", 0, 1, 30)]
    [InlineData("1h30m", 1, 30, 0)]
    public void Duration_parses_friendly_forms(string text, int h, int m, int s)
    {
        Assert.Equal(new TimeSpan(h, m, s), Duration.Parse(text));
    }
}
