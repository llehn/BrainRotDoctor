namespace BrainRotDoctor.Core.Configuration;

/// <summary>A known page of a catalog site, with whether a new rule blocks it by default.</summary>
public sealed record CatalogPage(
    string Name,
    string Path,
    PageMatch Match,
    bool BlockedByDefault,
    IReadOnlyList<KeyValuePair<string, string>>? Query = null);

/// <summary>
/// One curated website: its host and the pages worth telling apart, so a user
/// picks "Instagram" and gets Home feed, Following feed, Reels, Explore and
/// Messages already split out instead of typing hosts and paths.
/// </summary>
public sealed class CatalogSite
{
    public CatalogSite(string id, string name, string host, bool blocksEverythingElseByDefault, params CatalogPage[] pages)
    {
        Id = id;
        Name = name;
        Host = host;
        BlocksEverythingElseByDefault = blocksEverythingElseByDefault;
        Pages = pages;
    }

    /// <summary>Stable identifier stored on the site so the UI can show its icon.</summary>
    public string Id { get; }

    /// <summary>Display name (a brand; not localized).</summary>
    public string Name { get; }

    public string Host { get; }

    public bool BlocksEverythingElseByDefault { get; }

    public IReadOnlyList<CatalogPage> Pages { get; }

    /// <summary>The site with its recommended defaults, as added from the picker.</summary>
    public SiteDocument ToDocument() =>
        Build(page => page.BlockedByDefault, BlocksEverythingElseByDefault);

    /// <summary>The site blocking only the named pages and leaving the rest open.</summary>
    public SiteDocument ToDocumentBlocking(IReadOnlyCollection<string> pageNames) =>
        Build(page => pageNames.Contains(page.Name, StringComparer.Ordinal), blockEverythingElse: false);

    /// <summary>The whole site blocked, every page included.</summary>
    public SiteDocument ToDocumentBlockingEverything() => Build(_ => true, blockEverythingElse: true);

    private SiteDocument Build(Func<CatalogPage, bool> blocks, bool blockEverythingElse) => new()
    {
        CatalogId = Id,
        Label = Name,
        Host = Host,
        EverythingElse = blockEverythingElse ? SiteDocument.Block : SiteDocument.Allow,
        Pages = Pages.Select(page => new PageDocument
        {
            Name = page.Name,
            Path = page.Path,
            Match = PageDocument.MatchText(page.Match),
            Query = page.Query is { Count: > 0 } q ? q.ToDictionary(p => p.Key, p => p.Value) : null,
            Action = blocks(page) ? SiteDocument.Block : SiteDocument.Allow,
        }).ToList(),
    };
}

/// <summary>The built-in catalog of common doom-scrolling websites.</summary>
public static class SiteCatalog
{
    public const string Instagram = "instagram";
    public const string YouTube = "youtube";
    public const string TikTok = "tiktok";
    public const string Facebook = "facebook";

    private static readonly KeyValuePair<string, string>[] FollowingVariant = { new("variant", "following") };

    public static IReadOnlyList<CatalogSite> Sites { get; } = new[]
    {
        new CatalogSite(Instagram, "Instagram", "instagram.com", false,
            new CatalogPage("Home feed", "/", PageMatch.Exact, true),
            new CatalogPage("Following feed", "/", PageMatch.Exact, false, FollowingVariant),
            new CatalogPage("Reels", "/reel", PageMatch.Prefix, true),
            new CatalogPage("Explore", "/explore", PageMatch.Under, true),
            new CatalogPage("Messages", "/direct", PageMatch.Under, false)),
        new CatalogSite(YouTube, "YouTube", "youtube.com", false,
            new CatalogPage("Home feed", "/", PageMatch.Exact, true),
            new CatalogPage("Shorts", "/shorts", PageMatch.Under, true),
            new CatalogPage("Subscriptions", "/feed/subscriptions", PageMatch.Under, false),
            new CatalogPage("Watching a video", "/watch", PageMatch.Under, false)),
        new CatalogSite(TikTok, "TikTok", "tiktok.com", true,
            new CatalogPage("For You", "/foryou", PageMatch.Under, true),
            new CatalogPage("Following", "/following", PageMatch.Under, true),
            new CatalogPage("Explore", "/explore", PageMatch.Under, true),
            new CatalogPage("Messages", "/messages", PageMatch.Under, false)),
        new CatalogSite(Facebook, "Facebook", "facebook.com", false,
            new CatalogPage("News feed", "/", PageMatch.Exact, true),
            new CatalogPage("Reels", "/reel", PageMatch.Prefix, true),
            new CatalogPage("Watch", "/watch", PageMatch.Under, true),
            new CatalogPage("Marketplace", "/marketplace", PageMatch.Under, false),
            new CatalogPage("Messenger", "/messages", PageMatch.Under, false)),
        new CatalogSite("x", "X", "x.com", false,
            new CatalogPage("Home timeline", "/home", PageMatch.Under, true),
            new CatalogPage("Explore", "/explore", PageMatch.Under, true),
            new CatalogPage("Messages", "/messages", PageMatch.Under, false)),
        new CatalogSite("reddit", "Reddit", "reddit.com", false,
            new CatalogPage("Home feed", "/", PageMatch.Exact, true),
            new CatalogPage("Popular", "/r/popular", PageMatch.Under, true),
            new CatalogPage("All", "/r/all", PageMatch.Under, true)),
        new CatalogSite("linkedin", "LinkedIn", "linkedin.com", false,
            new CatalogPage("Feed", "/feed", PageMatch.Under, true),
            new CatalogPage("Messages", "/messaging", PageMatch.Under, false),
            new CatalogPage("Jobs", "/jobs", PageMatch.Under, false)),
        new CatalogSite("twitch", "Twitch", "twitch.tv", true,
            new CatalogPage("Home", "/", PageMatch.Exact, true),
            new CatalogPage("Browse", "/directory", PageMatch.Under, true)),
        new CatalogSite("pinterest", "Pinterest", "pinterest.com", false,
            new CatalogPage("Home feed", "/", PageMatch.Exact, true),
            new CatalogPage("Search", "/search", PageMatch.Under, false)),
    };

    public static bool TryGet(string? id, out CatalogSite site)
    {
        site = Sites.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal))!;
        return site is not null;
    }

    public static CatalogSite Get(string id) =>
        TryGet(id, out CatalogSite site) ? site : throw new ArgumentException($"Unknown catalog site '{id}'.", nameof(id));
}
