namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// Loads a <see cref="BlockerConfiguration"/> from JSON (see
/// <see cref="ConfigurationDocument"/> for the format) so the rule set can be
/// changed without recompiling the application.
/// </summary>
public static class ConfigurationLoader
{
    public static BlockerConfiguration Load(string json) => Load(ConfigurationDocument.Parse(json));

    public static BlockerConfiguration Load(ConfigurationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var rules = new List<Rule>();
        foreach (RuleDocument rule in document.Rules ?? new())
        {
            rules.Add(MapRule(rule));
        }

        return new BlockerConfiguration(rules);
    }

    public static BlockerConfiguration LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new ConfigurationException($"Configuration file not found: {path}");
        }

        return Load(File.ReadAllText(path));
    }

    public static Rule MapRule(RuleDocument dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new ConfigurationException("A rule is missing its 'id'.");
        }

        var sites = SiteMigration.Migrate(dto.Sites, dto.Id).Select(MapSite).ToList();

        TimeSpan? allowance = dto.AllowanceMinutes is { } minutes
            ? TimeSpan.FromMinutes(minutes)
            : null;

        bool allDay = dto.AllDay ?? (dto.From is null && dto.To is null);

        return new Rule(
            dto.Id,
            dto.Name ?? dto.Id,
            sites,
            allowance,
            allDay,
            ParseTime(dto.From, dto.Id, "from"),
            ParseTime(dto.To, dto.Id, "to"),
            ParseDays(dto.Days, dto.Id));
    }

    /// <summary>Validates one site in the current (page-based) format.</summary>
    public static TargetSite MapSite(SiteDocument site)
    {
        ArgumentNullException.ThrowIfNull(site);
        if (site.IsLegacy)
        {
            return SiteMigration.Migrate(new[] { site }, site.Label ?? "?").Select(MapSite).Single();
        }

        var pages = new List<SitePage>();
        foreach (PageDocument page in site.Pages ?? new())
        {
            pages.Add(new SitePage(
                page.Name ?? "",
                page.Path ?? "/",
                PageDocument.ParseMatch(page.Match),
                page.Query?.ToList(),
                ParseAction(page.Action, defaultBlock: true)));
        }

        return new TargetSite(
            site.Label ?? site.Host ?? "",
            site.Host ?? "",
            pages,
            ParseAction(site.EverythingElse, defaultBlock: false),
            site.CatalogId);
    }

    private static bool ParseAction(string? text, bool defaultBlock) => text?.Trim().ToLowerInvariant() switch
    {
        SiteDocument.Block => true,
        SiteDocument.Allow => false,
        null or "" => defaultBlock,
        _ => throw new ConfigurationException($"Unknown action '{text}'. Use block or allow."),
    };

    private static TimeOnly ParseTime(string? value, string id, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new TimeOnly(0, 0);
        }

        if (TimeOnly.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out TimeOnly parsed))
        {
            return parsed;
        }

        throw new ConfigurationException(
            $"Rule '{id}' has an invalid '{field}' time '{value}'. Use a form like '23:00'.");
    }

    private static IReadOnlyCollection<DayOfWeek>? ParseDays(List<string>? days, string id)
    {
        if (days is not { Count: > 0 })
        {
            return null;
        }

        var result = new List<DayOfWeek>();
        foreach (string day in days)
        {
            if (Enum.TryParse(day, ignoreCase: true, out DayOfWeek parsed))
            {
                result.Add(parsed);
            }
            else
            {
                throw new ConfigurationException($"Rule '{id}' has an invalid day '{day}'.");
            }
        }

        return result;
    }
}
