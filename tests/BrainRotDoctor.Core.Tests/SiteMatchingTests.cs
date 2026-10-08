using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class SiteMatchingTests
{
    private static Uri U(string url) => new(url);

    private static TargetSite Instagram() => ConfigurationLoader.MapSite(
        SiteCatalog.Get(SiteCatalog.Instagram).ToDocument());

    [Theory]
    [InlineData("https://www.instagram.com/", true)]
    [InlineData("https://www.instagram.com/?variant=following", false)]
    [InlineData("https://www.instagram.com/?variant=FOLLOWING&utm=x", false)]
    [InlineData("https://www.instagram.com/?variant=home", true)]
    [InlineData("https://www.instagram.com/reels/C9x/", true)]
    [InlineData("https://www.instagram.com/reel/C9x/", true)]
    [InlineData("https://www.instagram.com/explore/tags/cats", true)]
    [InlineData("https://www.instagram.com/direct/inbox/", false)]
    [InlineData("https://www.instagram.com/some.profile/", false)]
    public void Catalog_instagram_blocks_feed_but_keeps_following_and_messages(string url, bool blocked)
    {
        Assert.Equal(blocked, Instagram().Blocks(U(url)));
    }

    [Fact]
    public void Most_specific_page_wins_and_reports_the_runner_up()
    {
        TargetSite site = Instagram();
        IReadOnlyList<SitePage> pages = site.MatchingPages(U("https://instagram.com/?variant=following"));

        Assert.Equal(new[] { "Following feed", "Home feed" }, pages.Select(p => p.Name));
        SiteVerdict verdict = site.Evaluate(U("https://instagram.com/?variant=following"));
        Assert.True(verdict.Applies);
        Assert.Equal("Following feed", verdict.Page?.Name);
        Assert.False(verdict.Blocks);
    }

    [Fact]
    public void Everything_else_decides_when_no_page_fits()
    {
        var site = new TargetSite("IG", "instagram.com",
            new[] { new SitePage("DMs", "/direct", PageMatch.Under, null, blocks: false) },
            blocksEverythingElse: true);

        Assert.True(site.Blocks(U("https://instagram.com/")));
        Assert.True(site.Blocks(U("https://instagram.com/reels/x")));
        Assert.False(site.Blocks(U("https://instagram.com/direct/t/1")));
        Assert.Null(site.Evaluate(U("https://instagram.com/x")).Page);
    }

    [Theory]
    [InlineData("https://youtube.com/shorts", true)]
    [InlineData("https://youtube.com/shorts/abc", true)]
    [InlineData("https://m.youtube.com/SHORTS/abc", true)]
    [InlineData("https://youtube.com/shortsfoo", false)]
    [InlineData("https://notyoutube.com/shorts/abc", false)]
    [InlineData("ftp://youtube.com/shorts/abc", false)]
    public void Under_matches_the_path_and_below_on_the_host_and_subdomains(string url, bool expected)
    {
        var site = new TargetSite("YT", "youtube.com",
            new[] { new SitePage("Shorts", "/shorts/", PageMatch.Under, null, blocks: true) },
            blocksEverythingElse: false);

        Assert.Equal(expected, site.Blocks(U(url)));
    }

    [Theory]
    [InlineData("https://facebook.com/reel/1", true)]
    [InlineData("https://facebook.com/reels", true)]
    [InlineData("https://facebook.com/re", false)]
    public void Prefix_matches_any_path_starting_with_the_text(string url, bool expected)
    {
        var page = new SitePage("Reels", "/reel", PageMatch.Prefix, null, blocks: true);
        Assert.Equal(expected, page.Score(U(url)) >= 0);
    }

    [Fact]
    public void Exact_ignores_a_trailing_slash_and_rejects_sub_pages()
    {
        var page = new SitePage("Home", "/", PageMatch.Exact, null, blocks: true);
        Assert.True(page.Score(U("https://x.com")) >= 0);
        Assert.True(page.Score(U("https://x.com/")) >= 0);
        Assert.False(page.Score(U("https://x.com/home")) >= 0);
    }

    [Fact]
    public void A_condition_without_a_value_only_requires_the_parameter()
    {
        var page = new SitePage("Search", "/", PageMatch.Exact,
            new[] { new KeyValuePair<string, string>("q", "") }, blocks: true);
        Assert.True(page.Score(U("https://x.com/?q=cats")) >= 0);
        Assert.False(page.Score(U("https://x.com/?p=1")) >= 0);
    }

    [Fact]
    public void Legacy_catalog_picks_merge_into_one_site_per_website()
    {
        BlockerConfiguration config = ConfigurationLoader.Load("""
        { "rules": [ { "id": "r", "allowanceMinutes": 5,
            "sites": [ { "catalogId": "ig-feed" }, { "catalogId": "ig-reels" }, { "catalogId": "tiktok" } ] } ] }
        """);

        Rule rule = Assert.Single(config.Rules);
        Assert.Equal(new[] { "instagram.com", "tiktok.com" }, rule.Sites.Select(s => s.Host));
        Assert.True(rule.MatchesUrl(U("https://instagram.com/")));
        Assert.True(rule.MatchesUrl(U("https://instagram.com/reels/x")));
        Assert.False(rule.MatchesUrl(U("https://instagram.com/?variant=following")));
        Assert.False(rule.MatchesUrl(U("https://instagram.com/explore")));
        Assert.True(rule.MatchesUrl(U("https://tiktok.com/@someone/video/1")));
        Assert.True(rule.MatchesUrl(U("https://tiktok.com/messages")));
    }

    [Theory]
    [InlineData("instagram.com", true, "https://instagram.com/anything", true)]
    [InlineData("instagram.com", false, "https://instagram.com/anything", false)]
    [InlineData("instagram.com", false, "https://instagram.com/", true)]
    [InlineData("instagram.com/reels", true, "https://instagram.com/reels/x", true)]
    [InlineData("instagram.com/reels", false, "https://instagram.com/reels/x", false)]
    [InlineData("instagram.com/?variant=following", true, "https://instagram.com/?variant=following", true)]
    [InlineData("instagram.com/?variant=following", true, "https://instagram.com/", false)]
    public void Typed_addresses_become_sites(string url, bool sub, string probe, bool expected)
    {
        Assert.Equal(expected, TargetSite.FromUrl("x", url, sub).Blocks(U(probe)));
    }

    [Fact]
    public void Page_format_round_trips_through_json()
    {
        var doc = new ConfigurationDocument
        {
            Rules = new() { new RuleDocument { Id = "r", Sites = new() { SiteCatalog.Get(SiteCatalog.Instagram).ToDocument() } } },
        };

        Rule rule = Assert.Single(ConfigurationLoader.Load(doc.ToJson()).Rules);
        TargetSite site = Assert.Single(rule.Sites);
        Assert.Equal(SiteCatalog.Instagram, site.CatalogId);
        Assert.Equal(5, site.Pages.Count);
        Assert.Equal("variant", site.Pages[1].Query.Single().Key);
    }

    [Fact]
    public void Unknown_page_match_is_rejected()
    {
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load("""
        { "rules": [ { "id": "r", "sites": [ { "host": "a.com",
            "pages": [ { "path": "/", "match": "fuzzy" } ] } ] } ] }
        """));
    }
}
