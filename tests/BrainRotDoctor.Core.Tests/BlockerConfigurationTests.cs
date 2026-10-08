using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class BlockerConfigurationTests
{
    private static Rule Rule(string id, string url) => new(
        id, id,
        new[] { TargetSite.FromUrl(url, url, includeSubpaths: true) },
        TimeSpan.FromMinutes(5), allDay: true, default, default, null);

    [Fact]
    public void Duplicate_rule_ids_are_rejected()
    {
        Assert.Throws<ConfigurationException>(() => new BlockerConfiguration(
            new[] { Rule("dup", "a.com"), Rule("dup", "b.com") }));
    }

    [Fact]
    public void Matching_rules_returns_every_rule_that_blocks_the_url()
    {
        var config = new BlockerConfiguration(new[]
        {
            Rule("reels", "instagram.com/reels"),
            Rule("all-ig", "instagram.com"),
        });

        string[] matched = config.MatchingRules(new Uri("https://instagram.com/reels/x"))
            .Select(r => r.Id)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(new[] { "all-ig", "reels" }, matched);
    }

    [Fact]
    public void Default_configuration_matches_known_surfaces_and_leaves_others_alone()
    {
        BlockerConfiguration config = DefaultConfiguration.Create();

        Assert.Contains(config.MatchingRules(new Uri("https://www.youtube.com/shorts/x")),
            r => r.Id == DefaultConfiguration.ShortVideoRuleId);
        Assert.Contains(config.MatchingRules(new Uri("https://instagram.com/reels/x")),
            r => r.Id == DefaultConfiguration.ShortVideoRuleId);
        Assert.Contains(config.MatchingRules(new Uri("https://instagram.com/")),
            r => r.Id == DefaultConfiguration.FeedsRuleId);

        // Instagram DMs, the Following feed and normal YouTube must remain unaffected.
        Assert.Empty(config.MatchingRules(new Uri("https://instagram.com/direct/inbox")));
        Assert.Empty(config.MatchingRules(new Uri("https://www.instagram.com/?variant=following")));
        Assert.Empty(config.MatchingRules(new Uri("https://www.youtube.com/watch?v=x")));
    }
}
