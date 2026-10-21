using BrainRotDoctor.Core.Apps;
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
        public bool Enabled { get; set; } = true;

        public bool BlockCompletely { get; set; }
        public int AllowanceMinutes { get; set; } = 5;

        // The hours offered when switching from "all day" to set hours.
        private static readonly TimeOnly DefaultFrom = new(9, 0);
        private static readonly TimeOnly DefaultTo = new(17, 0);

        public bool AllDay { get; set; } = true;
        public TimeOnly From { get; set; } = DefaultFrom;
        public TimeOnly To { get; set; } = DefaultTo;
        public HashSet<DayOfWeek> Days { get; set; } = new(AllDays);

        public List<EditableSite> Sites { get; } = new();

        public List<EditableApp> Apps { get; } = new();

        /// <summary>A deep copy, used so the editor can discard unsaved changes.</summary>
        internal EditableRule Clone() => FromDocument(ToDocument());

        internal static EditableRule FromDocument(RuleDocument dto)
        {
            var rule = new EditableRule
            {
                Id = dto.Id ?? "",
                Name = dto.Name ?? dto.Id ?? "",
                Enabled = dto.Enabled ?? true,
                BlockCompletely = dto.AllowanceMinutes is null,
                AllowanceMinutes = dto.AllowanceMinutes is { } m ? Math.Clamp(m, 1, 59) : 5,
                AllDay = dto.AllDay ?? (dto.From is null && dto.To is null),
                From = ParseTime(dto.From, DefaultFrom),
                To = ParseTime(dto.To, DefaultTo),
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

            foreach (AppDocument app in dto.Apps ?? new())
            {
                rule.Apps.Add(EditableApp.FromDocument(app));
            }

            return rule;
        }

        internal RuleDocument ToDocument() => new()
        {
            Id = Id.Trim(),
            Name = Name.Trim(),
            Enabled = Enabled ? null : false,
            AllowanceMinutes = BlockCompletely ? null : Math.Clamp(AllowanceMinutes, 1, 59),
            AllDay = AllDay,
            From = AllDay ? null : From.ToString("HH:mm"),
            To = AllDay ? null : To.ToString("HH:mm"),
            Days = Days.Count == AllDays.Length
                ? null
                : AllDays.Where(Days.Contains).Select(d => d.ToString()).ToList(),
            Sites = Sites.Select(s => s.ToDocument()).ToList(),
            Apps = Apps.Count == 0 ? null : Apps.Select(a => a.ToDocument()).ToList(),
        };

        /// <summary>The validated rule, for checking an address against unsaved edits.</summary>
        internal Rule ToRule() => ConfigurationLoader.MapRule(ToDocument());

        /// <summary>
        /// Adds what <paramref name="addition"/> blocks to this rule. A site the
        /// rule already has takes the new blocks in: a page it allowed inside them
        /// becomes blocked, a page the addition keeps open stays as open as it was.
        /// Nothing the rule blocked before is opened.
        /// </summary>
        internal void Block(EditableSite addition)
        {
            EditableSite? existing = Sites.FirstOrDefault(s =>
                string.Equals(SiteUrl.NormalizeHost(s.Host), SiteUrl.NormalizeHost(addition.Host), StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                Sites.Add(addition);
                return;
            }

            TargetSite before = existing.ToTargetSite();
            TargetSite added = addition.ToTargetSite();
            foreach (EditablePage page in existing.Pages.Where(p => !p.Block))
            {
                if (added.Blocks(existing.Address(page)))
                {
                    page.Block = true;
                }
            }

            existing.BlockEverythingElse |= addition.BlockEverythingElse;
            foreach (EditablePage page in addition.Pages)
            {
                EditablePage? same = existing.Pages.FirstOrDefault(p => p.Conditions.Count == 0 && page.Conditions.Count == 0
                    && p.Match == page.Match && SitePage.NormalizePath(p.Path) == SitePage.NormalizePath(page.Path));
                if (page.Block)
                {
                    if (same is null)
                    {
                        existing.Pages.Add(page);
                    }
                    else
                    {
                        same.Block = true;
                    }
                }
                else if (same is null && !before.Blocks(existing.Address(page)))
                {
                    existing.Pages.Add(page);
                }
            }
        }

        /// <summary>Adds <paramref name="app"/> unless the rule has the same app already.</summary>
        internal void Block(EditableApp app)
        {
            if (!Apps.Any(a => a.SameAs(app)))
            {
                Apps.Add(app);
            }
        }

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

        /// <summary>An address on this site that <paramref name="page"/> fits.</summary>
        internal Uri Address(EditablePage page)
        {
            string query = string.Join("&", page.Conditions.Where(c => !string.IsNullOrWhiteSpace(c.Key))
                .Select(c => Uri.EscapeDataString(c.Key.Trim()) + "=" + Uri.EscapeDataString(c.Value.Trim())));
            return new Uri($"https://{SiteUrl.NormalizeHost(Host)}{SitePage.NormalizePath(page.Path)}{(query.Length > 0 ? "?" + query : "")}");
        }

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

    /// <summary>One app in a rule: a program, a Steam game, all Steam games, or Steam.</summary>
    internal sealed class EditableApp
    {
        public TargetAppKind Kind { get; set; }
        public string Label { get; set; } = "";
        public string? Exe { get; set; }
        public string? Description { get; set; }
        public string? Folder { get; set; }

        internal static EditableApp FromDocument(AppDocument dto)
        {
            TargetAppKind kind;
            try
            {
                kind = AppDocument.ParseKind(dto.Kind);
            }
            catch (ConfigurationException)
            {
                kind = TargetAppKind.Program;
            }

            return new EditableApp { Kind = kind, Label = dto.Label ?? "", Exe = dto.Exe, Description = dto.Description, Folder = dto.Folder };
        }

        /// <summary>The rule entry for an app as it runs: that program, that Steam game, or Steam.</summary>
        public static EditableApp For(AppIdentity app) => app.Kind switch
        {
            AppKind.Steam => new EditableApp { Kind = TargetAppKind.Steam, Label = "Steam" },
            AppKind.SteamGame => new EditableApp { Kind = TargetAppKind.SteamGame, Label = app.Name, Folder = app.SteamFolder },
            _ => new EditableApp { Kind = TargetAppKind.Program, Label = app.Name, Exe = app.Exe, Description = app.Description },
        };

        public static EditableApp AllSteamGames() => new() { Kind = TargetAppKind.AllSteamGames, Label = "All Steam games" };

        internal AppDocument ToDocument() => new()
        {
            Kind = AppDocument.KindText(Kind),
            Label = Label.Trim(),
            Exe = string.IsNullOrWhiteSpace(Exe) ? null : Exe.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            Folder = string.IsNullOrWhiteSpace(Folder) ? null : Folder.Trim(),
        };

        internal TargetApp ToTargetApp() => ConfigurationLoader.MapApp(ToDocument());

        /// <summary>The same entry: same kind and the same program, game or none.</summary>
        internal bool SameAs(EditableApp other) =>
            Kind == other.Kind
            && string.Equals(Exe ?? "", other.Exe ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Description ?? "", other.Description ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Folder ?? "", other.Folder ?? "", StringComparison.OrdinalIgnoreCase);
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
