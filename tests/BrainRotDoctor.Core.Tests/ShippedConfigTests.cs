using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

/// <summary>
/// Guards the shipped <c>config/default-config.json</c>: it must always parse,
/// validate, and recognize the same surfaces as the in-code default.
/// </summary>
public class ShippedConfigTests
{
    private static BlockerConfiguration LoadShipped()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "default-config.json");
        return ConfigurationLoader.LoadFile(path);
    }

    [Fact]
    public void Shipped_config_loads_and_validates()
    {
        BlockerConfiguration config = LoadShipped();
        Assert.NotEmpty(config.Rules);
    }

    [Fact]
    public void Shipped_config_is_the_in_code_default()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "default-config.json");
        string shipped = ConfigurationDocument.Parse(File.ReadAllText(path)).ToJson();
        string inCode = DefaultConfiguration.CreateDocument("Short video", "Feeds").ToJson();
        Assert.Equal(inCode, shipped);
    }

    [Theory]
    [InlineData("https://www.youtube.com/shorts/x")]
    [InlineData("https://instagram.com/reels/x")]
    [InlineData("https://instagram.com/")]
    public void Shipped_config_matches_known_surfaces(string url)
    {
        BlockerConfiguration config = LoadShipped();
        Assert.NotEmpty(config.MatchingRules(new Uri(url)));
    }

    [Theory]
    [InlineData("https://instagram.com/direct/inbox")]
    [InlineData("https://www.instagram.com/?variant=following")]
    [InlineData("https://www.youtube.com/watch?v=x")]
    public void Shipped_config_leaves_useful_surfaces_alone(string url)
    {
        BlockerConfiguration config = LoadShipped();
        Assert.Empty(config.MatchingRules(new Uri(url)));
    }
}
