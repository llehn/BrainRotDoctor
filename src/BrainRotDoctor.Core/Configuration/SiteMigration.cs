namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// Converts sites written in the old configuration format into the page-based
/// format, so existing configs keep working and are upgraded on their next save.
/// <list type="bullet">
///   <item>An old catalog pick (<c>"ig-feed"</c>, <c>"ig-reels"</c>, …) becomes
///   the matching catalog site with just those pages blocked. Several old picks on
///   the same site merge into one site.</item>
///   <item>An old typed address becomes a site whose single page is that address
///   (or the whole site, for a bare host with sub-pages included).</item>
/// </list>
/// </summary>
public static class SiteMigration
{
    // Old catalog id -> (new catalog site, the pages it blocked; null = whole site).
    private static readonly Dictionary<string, (string Site, string[]? Pages)> LegacyCatalog = new(StringComparer.Ordinal)
    {
        ["ig-reels"] = (SiteCatalog.Instagram, new[] { "Reels" }),
        ["ig-feed"] = (SiteCatalog.Instagram, new[] { "Home feed" }),
        ["yt-shorts"] = (SiteCatalog.YouTube, new[] { "Shorts" }),
        ["yt-home"] = (SiteCatalog.YouTube, new[] { "Home feed" }),
        ["tiktok"] = (SiteCatalog.TikTok, null),
        ["fb-reels"] = (SiteCatalog.Facebook, new[] { "Reels" }),
        ["fb-feed"] = (SiteCatalog.Facebook, new[] { "News feed" }),
        ["x"] = ("x", null),
        ["reddit"] = ("reddit", null),
        ["linkedin"] = ("linkedin", new[] { "Feed" }),
        ["twitch"] = ("twitch", null),
        ["pinterest"] = ("pinterest", null),
    };

    /// <summary>Returns the sites in the current format, in their original order.</summary>
    public static List<SiteDocument> Migrate(IEnumerable<SiteDocument>? sites, string ruleId)
    {
        var result = new List<SiteDocument>();
        var merged = new Dictionary<string, (int Index, HashSet<string>? Pages)>(StringComparer.Ordinal);

        foreach (SiteDocument site in sites ?? Enumerable.Empty<SiteDocument>())
        {
            if (!site.IsLegacy)
            {
                result.Add(site);
                continue;
            }

            if (site.CatalogId is { } legacyId && LegacyCatalog.TryGetValue(legacyId, out var legacy))
            {
                HashSet<string>? pages = legacy.Pages is null ? null : new HashSet<string>(legacy.Pages, StringComparer.Ordinal);
                if (merged.TryGetValue(legacy.Site, out var existing))
                {
                    // Whole site (null) absorbs any page list.
                    HashSet<string>? combined = existing.Pages is null || pages is null
                        ? null
                        : new HashSet<string>(existing.Pages.Concat(pages), StringComparer.Ordinal);
                    merged[legacy.Site] = (existing.Index, combined);
                }
                else
                {
                    merged[legacy.Site] = (result.Count, pages);
                    result.Add(new SiteDocument());
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(site.Url))
            {
                throw new ConfigurationException($"Rule '{ruleId}' has a site with no 'url'.");
            }

            result.Add(FromUrl(site.Label ?? site.Url, site.Url, site.IncludeSubpaths ?? true));
        }

        foreach ((string siteId, (int index, HashSet<string>? pages)) in merged)
        {
            CatalogSite catalog = SiteCatalog.Get(siteId);
            result[index] = pages is null ? catalog.ToDocumentBlockingEverything() : catalog.ToDocumentBlocking(pages);
        }

        return result;
    }

    /// <summary>
    /// A site built from one typed address: a bare host blocks the whole site; a
    /// path blocks that page (and, with <paramref name="includeSubpaths"/>,
    /// everything beneath it); query parameters become conditions.
    /// </summary>
    public static SiteDocument FromUrl(string label, string url, bool includeSubpaths)
    {
        SiteAddress address = SiteUrl.ParseAddress(url);
        string path = SitePage.NormalizePath(address.Path);
        bool hasQuery = address.Query.Count > 0;
        string name = string.IsNullOrWhiteSpace(label) ? address.Host + path : label.Trim();

        if (path == "/" && !hasQuery && includeSubpaths)
        {
            return new SiteDocument
            {
                Label = name,
                Host = address.Host,
                EverythingElse = SiteDocument.Block,
                Pages = new List<PageDocument>(),
            };
        }

        return new SiteDocument
        {
            Label = name,
            Host = address.Host,
            EverythingElse = SiteDocument.Allow,
            Pages = new List<PageDocument>
            {
                new()
                {
                    Name = name,
                    Path = path,
                    Match = PageDocument.MatchText(includeSubpaths && !hasQuery && path != "/" ? PageMatch.Under : PageMatch.Exact),
                    Query = hasQuery ? address.Query.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value) : null,
                    Action = SiteDocument.Block,
                },
            },
        };
    }
}
