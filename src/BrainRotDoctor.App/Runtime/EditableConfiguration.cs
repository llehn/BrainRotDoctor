using BrainRotDoctor.Core.Configuration;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// The mutable, UI-friendly view of the configuration: a list of rules, each
/// with the websites it covers (split into pages) and its "when" condition.
/// Round-trips to and from <see cref="ConfigurationDocument"/>; old-format sites
/// are upgraded on load.
/// </summary>
internal sealed class EditableConfiguration
{
    private static readonly DayOfWeek[] AllDays = Enum.GetValues<DayOfWeek>();

    public List<EditableRule> Rules { get; } = new();

    public static EditableConfiguration FromJson(string json)
    {
        ConfigurationDocument document;
        try
        {
            document = ConfigurationDocument.Parse(json);
        }
        catch (ConfigurationException)
        {
            document = new ConfigurationDocument();
        }

        var result = new EditableConfiguration();
        foreach (RuleDocument rule in document.Rules ?? new())
        {
            result.Rules.Add(EditableRule.FromDocument(rule));
        }

        return result;
    }

    public string ToJson() => ToDocument().ToJson();

    public ConfigurationDocument ToDocument() => new() { Rules = Rules.Select(r => r.ToDocument()).ToList() };

    public BlockerConfiguration ToBlockerConfiguration() => ConfigurationLoader.Load(ToDocument());

    internal sealed class EditableRule
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public bool BlockCompletely { get; set; }
        public int AllowanceMinutes { get; set; } = 5;

        public bool AllDay { get; set; } = true;
        public TimeOnly From { get; set; } = new(23, 0);
        public TimeOnly To { get; set; } = new(7, 0);
        public HashSet<DayOfWeek> Days { get; set; } = new(AllDays);

        public List<EditableSite> Sites { get; } = new();

        /// <summary>A deep copy, used so the editor can discard unsaved changes.</summary>
        internal EditableRule Clone() => FromDocument(ToDocument());

        internal static EditableRule FromDocument(RuleDocument dto)
        {
            var rule = new EditableRule
            {
                Id = dto.Id ?? "",
                Name = dto.Name ?? dto.Id ?? "",
                BlockCompletely = dto.AllowanceMinutes is null,
                AllowanceMinutes = dto.AllowanceMinutes is { } m ? Math.Clamp(m, 1, 59) : 5,
                AllDay = dto.AllDay ?? (dto.From is null && dto.To is null),
                From = ParseTime(dto.From, new TimeOnly(23, 0)),
                To = ParseTime(dto.To, new TimeOnly(7, 0)),
                Days = ParseDays(dto.Days),
            };

            List<SiteDocument> sites;
            try
            {
                sites = SiteMigration.Migrate(dto.Sites, rule.Id);
            }
            catch (ConfigurationException)
            {
                sites = (dto.Sites ?? new()).Where(s => !s.IsLegacy).ToList();
            }

            foreach (SiteDocument site in sites)
            {
                rule.Sites.Add(EditableSite.FromDocument(site));
            }

            return rule;
        }

        internal RuleDocument ToDocument() => new()
        {
            Id = Id.Trim(),
            Name = Name.Trim(),
            AllowanceMinutes = BlockCompletely ? null : Math.Clamp(AllowanceMinutes, 1, 59),
            AllDay = AllDay,
            From = AllDay ? null : From.ToString("HH:mm"),
            To = AllDay ? null : To.ToString("HH:mm"),
            Days = Days.Count == AllDays.Length
                ? null
                : AllDays.Where(Days.Contains).Select(d => d.ToString()).ToList(),
            Sites = Sites.Select(s => s.ToDocument()).ToList(),
        };

        /// <summary>The validated rule, for checking an address against unsaved edits.</summary>
        internal Rule ToRule() => ConfigurationLoader.MapRule(ToDocument());

        private static TimeOnly ParseTime(string? text, TimeOnly fallback)
            => TimeOnly.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out TimeOnly t) ? t : fallback;

        private static HashSet<DayOfWeek> ParseDays(List<string>? days)
        {
            if (days is not { Count: > 0 })
            {
                return new HashSet<DayOfWeek>(AllDays);
            }

            var set = new HashSet<DayOfWeek>();
            foreach (string day in days)
            {
                if (Enum.TryParse(day, ignoreCase: true, out DayOfWeek parsed))
                {
                    set.Add(parsed);
                }
            }

            return set.Count == 0 ? new HashSet<DayOfWeek>(AllDays) : set;
        }
    }

    /// <summary>One website in a rule: its pages and what happens to everything else.</summary>
    internal sealed class EditableSite
    {
        public string? CatalogId { get; set; }
        public string Label { get; set; } = "";
        public string Host { get; set; } = "";
        public bool BlockEverythingElse { get; set; }
        public List<EditablePage> Pages { get; } = new();

        public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? Host : Label;

        internal static EditableSite FromDocument(SiteDocument dto)
        {
            var site = new EditableSite
            {
                CatalogId = dto.CatalogId,
                Label = dto.Label ?? dto.Host ?? "",
                Host = dto.Host ?? "",
                BlockEverythingElse = IsBlock(dto.EverythingElse, defaultBlock: false),
            };

            foreach (PageDocument page in dto.Pages ?? new())
            {
                site.Pages.Add(new EditablePage
                {
                    Name = page.Name ?? "",
                    Path = page.Path ?? "/",
                    Match = SafeMatch(page.Match),
                    Block = IsBlock(page.Action, defaultBlock: true),
                    Conditions = (page.Query ?? new()).Select(q => new EditableCondition(q.Key, q.Value)).ToList(),
                });
            }

            return site;
        }

        internal SiteDocument ToDocument() => new()
        {
            CatalogId = string.IsNullOrWhiteSpace(CatalogId) ? null : CatalogId,
            Label = Label.Trim(),
            Host = SiteUrl.NormalizeHost(Host),
            EverythingElse = BlockEverythingElse ? SiteDocument.Block : SiteDocument.Allow,
            Pages = Pages.Select(p => new PageDocument
            {
                Name = p.Name.Trim(),
                Path = SitePage.NormalizePath(p.Path),
                Match = PageDocument.MatchText(p.Match),
                Query = p.Conditions.Count == 0
                    ? null
                    : p.Conditions.Where(c => !string.IsNullOrWhiteSpace(c.Key))
                        .GroupBy(c => c.Key.Trim())
                        .ToDictionary(g => g.Key, g => g.First().Value.Trim()),
                Action = p.Block ? SiteDocument.Block : SiteDocument.Allow,
            }).ToList(),
        };

        internal TargetSite ToTargetSite() => ConfigurationLoader.MapSite(ToDocument());

        public static EditableSite FromCatalog(CatalogSite site) => FromDocument(site.ToDocument());

        private static bool IsBlock(string? action, bool defaultBlock) =>
            string.IsNullOrWhiteSpace(action) ? defaultBlock : string.Equals(action.Trim(), SiteDocument.Block, StringComparison.OrdinalIgnoreCase);

        private static PageMatch SafeMatch(string? text)
        {
            try
            {
                return PageDocument.ParseMatch(text);
            }
            catch (ConfigurationException)
            {
                return PageMatch.Under;
            }
        }
    }

    internal sealed class EditablePage
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "/";
        public PageMatch Match { get; set; } = PageMatch.Under;
        public bool Block { get; set; } = true;
        public List<EditableCondition> Conditions { get; set; } = new();
    }

    internal sealed record EditableCondition(string Key, string Value);
}
