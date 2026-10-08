namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// The first-run rule set, used to seed a new user's configuration and in tests.
/// It is not the product boundary: the real configuration is loaded from a file
/// so new rules can be added without recompiling.
/// </summary>
public static class DefaultConfiguration
{
    public const string ShortVideoRuleId = "short-video";
    public const string FeedsRuleId = "feeds";

    /// <summary>Builds the default configuration.</summary>
    public static BlockerConfiguration Create() => ConfigurationLoader.Load(CreateDocument("Short video", "Feeds"));

    /// <summary>The default rules as a document, with the given (localized) rule names.</summary>
    public static ConfigurationDocument CreateDocument(string shortVideoName, string feedsName) => new()
    {
        Rules = new List<RuleDocument>
        {
            new()
            {
                Id = ShortVideoRuleId,
                Name = shortVideoName,
                AllowanceMinutes = 5,
                AllDay = true,
                Sites = new List<SiteDocument>
                {
                    SiteCatalog.Get(SiteCatalog.YouTube).ToDocumentBlocking(new[] { "Shorts" }),
                    SiteCatalog.Get(SiteCatalog.Instagram).ToDocumentBlocking(new[] { "Reels" }),
                    SiteCatalog.Get(SiteCatalog.Facebook).ToDocumentBlocking(new[] { "Reels" }),
                    SiteCatalog.Get(SiteCatalog.TikTok).ToDocumentBlockingEverything(),
                },
            },
            new()
            {
                Id = FeedsRuleId,
                Name = feedsName,
                AllowanceMinutes = 5,
                AllDay = true,
                Sites = new List<SiteDocument>
                {
                    SiteCatalog.Get(SiteCatalog.Instagram).ToDocumentBlocking(new[] { "Home feed" }),
                },
            },
        },
    };
}
