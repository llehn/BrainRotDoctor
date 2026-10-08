namespace BrainRotDoctor.Core.Configuration;

/// <summary>What one site of a rule does with an address.</summary>
/// <param name="Applies">True when the address is on this site at all.</param>
/// <param name="Page">The most specific page that fits, or null when "everything else" decided.</param>
/// <param name="Blocks">True when the rule blocks the address.</param>
public readonly record struct SiteVerdict(bool Applies, SitePage? Page, bool Blocks);

/// <summary>
/// One website in a rule: its host, the named pages the rule treats specially,
/// and whether everything else on the site is blocked. For an address on the
/// site, the most specific fitting page decides; when no page fits,
/// <see cref="BlocksEverythingElse"/> does. The same site may appear in several
/// rules.
/// </summary>
public sealed class TargetSite
{
    public TargetSite(
        string label,
        string host,
        IReadOnlyList<SitePage> pages,
        bool blocksEverythingElse,
        string? catalogId = null)
    {
        ArgumentNullException.ThrowIfNull(pages);
        Host = SiteUrl.NormalizeHost(host);
        if (Host.Length == 0)
        {
            throw new ConfigurationException($"Site '{label}' is missing its address.");
        }

        Label = string.IsNullOrWhiteSpace(label) ? Host : label.Trim();
        Pages = pages.ToArray();
        BlocksEverythingElse = blocksEverythingElse;
        CatalogId = string.IsNullOrWhiteSpace(catalogId) ? null : catalogId;
    }

    public string Label { get; }

    /// <summary>The normalized host ("instagram.com"); subdomains match too.</summary>
    public string Host { get; }

    public IReadOnlyList<SitePage> Pages { get; }

    public bool BlocksEverythingElse { get; }

    /// <summary>The built-in catalog site this came from, if any (for display only).</summary>
    public string? CatalogId { get; }

    /// <summary>A site built from one typed address (see <see cref="SiteMigration.FromUrl"/>).</summary>
    public static TargetSite FromUrl(string label, string url, bool includeSubpaths) =>
        ConfigurationLoader.MapSite(SiteMigration.FromUrl(label, url, includeSubpaths));

    public bool HostMatches(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        string host = uri.Host.TrimEnd('.');
        return string.Equals(host, Host, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + Host, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Every page that fits <paramref name="uri"/>, most specific first. On a tie a
    /// blocking page comes first, so an ambiguous setup errs on the side of blocking.
    /// </summary>
    public IReadOnlyList<SitePage> MatchingPages(Uri uri)
    {
        if (!HostMatches(uri))
        {
            return Array.Empty<SitePage>();
        }

        return Pages
            .Select(page => (Page: page, Score: page.Score(uri)))
            .Where(x => x.Score >= 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Page.Blocks)
            .Select(x => x.Page)
            .ToArray();
    }

    public SiteVerdict Evaluate(Uri uri)
    {
        if (!HostMatches(uri))
        {
            return new SiteVerdict(false, null, false);
        }

        SitePage? best = MatchingPages(uri).FirstOrDefault();
        return best is null
            ? new SiteVerdict(true, null, BlocksEverythingElse)
            : new SiteVerdict(true, best, best.Blocks);
    }

    public bool Blocks(Uri uri) => Evaluate(uri).Blocks;
}
