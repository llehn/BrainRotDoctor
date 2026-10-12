using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Rule editor (full window) ----------

    private EditableConfiguration.EditableRule? _editingRule;
    private int _editingIndex = -1;
    private Page _editorReturn = Page.Home; // where back, cancel, save and delete lead
    private readonly HashSet<EditableConfiguration.EditableSite> _openSites = new();
    private EditableConfiguration.EditablePage? _editingPage;
    private EditableConfiguration.EditablePage? _testHit;
    private string _testUrl = "";
    private string _newCondition = "";
    private bool _confirmDelete;
    private string? _editorError;

    private ContentControl? _scheduleHost;
    private TextLine? _summaryText;
    private StackPanel? _sitesPanel;
    private ContentControl? _testResultHost;

    private void OpenEditor(EditableConfiguration.EditableRule rule, int index, Page returnTo = Page.Home)
    {
        _editorReturn = returnTo;
        _editingRule = rule;
        _editingIndex = index;
        _openSites.Clear();
        if (rule.Sites.Count > 0)
        {
            _openSites.Add(rule.Sites[0]);
        }

        _editingPage = null;
        _testHit = null;
        _testUrl = "";
        _confirmDelete = false;
        _editorError = null;
        Navigate(Page.Editor);
    }

    private Control BuildEditor()
    {
        EditableConfiguration.EditableRule rule = _editingRule!;

        var back = new PillButton(_editorReturn == Page.Status ? Loc.T("status") : Loc.T("rules"), ButtonKind.Ghost, Glyph.ArrowLeft);
        back.Click += (_, _) => Navigate(_editorReturn);

        var name = new TextField(rule.Name, Loc.T("rule_name"), TextFieldStyle.Title);
        name.Changed += text => rule.Name = text;

        StackPanel actions = Layout.Row(10);
        if (_editingIndex >= 0)
        {
            var delete = _confirmDelete
                ? new PillButton(Loc.T("confirm_delete"), ButtonKind.DangerSolid, Glyph.Trash)
                : new PillButton(Loc.T("delete_rule"), ButtonKind.Danger, Glyph.Trash);
            delete.Click += (_, _) =>
            {
                if (_confirmDelete)
                {
                    DeleteEditingRule();
                }
                else
                {
                    _confirmDelete = true;
                    Rerender();
                }
            };
            actions.Children.Add(delete);
        }

        var cancel = new PillButton(Loc.T("cancel"));
        cancel.Click += (_, _) => Navigate(_editorReturn);
        var save = new PillButton(Loc.T("save_rule"), ButtonKind.Primary);
        save.Click += (_, _) => SaveEditingRule();
        actions.Children.Add(cancel);
        actions.Children.Add(save);

        _scheduleHost = new ContentControl();
        RebuildSchedule();

        var body = new StackPanel { Spacing = Space.S5 };
        if (_editorError is not null)
        {
            body.Children.Add(new Banner(Tone.Danger, Glyph.Ban, Banner.Message(Tone.Danger, new Phrase(_editorError, Strong: true))));
        }

        body.Children.Add(Layout.Columns(_scheduleHost, BuildWhatColumn(), 340));
        return new FullPageLayout(new TopBar(back, name, actions), body);
    }

    // ----- When it applies -----

    private void RebuildSchedule()
    {
        if (_scheduleHost is null || _editingRule is not { } rule)
        {
            return;
        }

        var card = new StackPanel { Spacing = Space.S5 };
        card.Children.Add(new TextLine(Loc.T("when_it_applies"), TextStyle.Subheading));

        // Limit
        var limit = new Segmented(new Segment[] { new(Loc.T("time_allowance")), new(Loc.T("block_completely")) }, rule.BlockCompletely ? 1 : 0);
        limit.Changed += i => { rule.BlockCompletely = i == 1; RebuildSchedule(); };
        StackPanel limitSection = Layout.Column(10, new TextLine(Loc.T("limit"), TextStyle.Overline), limit);
        if (rule.BlockCompletely)
        {
            limitSection.Children.Add(new TextLine(Loc.T("block_completely_help"), TextStyle.Small));
        }
        else
        {
            var minutes = new Stepper(rule.AllowanceMinutes, 1, 59, Loc.T("fewer_minutes"), Loc.T("more_minutes"));
            minutes.Changed += value =>
            {
                rule.AllowanceMinutes = value;
                _summaryText?.SetText(RuleSummary(rule));
            };
            limitSection.Children.Add(Layout.Row(Space.S3, minutes, new TextLine(Loc.T("minutes_every_hour"))));
            limitSection.Children.Add(new TextLine(Loc.T("allowance_help"), TextStyle.Small));
        }

        card.Children.Add(limitSection);
        card.Children.Add(new Divider());

        // Active window
        var active = new Segmented(new Segment[] { new(Loc.T("all_day")), new(Loc.T("set_hours")) }, rule.AllDay ? 0 : 1);
        active.Changed += i => { rule.AllDay = i == 0; RebuildSchedule(); };
        StackPanel activeSection = Layout.Column(10, new TextLine(Loc.T("active"), TextStyle.Overline), active);
        if (!rule.AllDay)
        {
            var hours = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
            hours.Children.Add(LabeledTime(Loc.T("from"), rule.From, t => { rule.From = t; RebuildSchedule(); }));
            Control to = LabeledTime(Loc.T("to"), rule.To, t => { rule.To = t; RebuildSchedule(); });
            Grid.SetColumn(to, 2);
            hours.Children.Add(to);
            activeSection.Children.Add(hours);
            if (rule.To <= rule.From)
            {
                activeSection.Children.Add(new TextLine(Loc.T("runs_overnight"), TextStyle.Small));
            }
        }

        card.Children.Add(activeSection);
        card.Children.Add(new Divider());

        // Days
        StackPanel presets = Layout.Row(0,
            DayPreset(Loc.T("every_day_btn"), DayOrder),
            DayPreset(Loc.T("weekdays_btn"), DayOrder.Take(5)),
            DayPreset(Loc.T("weekends_btn"), DayOrder.Skip(5)));
        var dayHeader = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(presets, Dock.Right);
        dayHeader.Children.Add(presets);
        dayHeader.Children.Add(new TextLine(Loc.T("days"), TextStyle.Overline));
        var days = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 5, LineSpacing = 5 };
        foreach (DayOfWeek day in DayOrder)
        {
            days.Children.Add(DayToggle(rule, day));
        }

        card.Children.Add(Layout.Column(10, dayHeader, days));

        // Plain-language summary
        _summaryText = new TextLine(RuleSummary(rule));
        card.Children.Add(new Card(Layout.Column(6, new TextLine(Loc.T("in_plain_words"), TextStyle.Overline), _summaryText), CardStyle.Inset, CardPadding.Compact));

        _scheduleHost.Content = new Card(card, CardStyle.Surface, CardPadding.Roomy);
    }

    private static Control LabeledTime(string label, TimeOnly value, Action<TimeOnly> changed)
    {
        var field = new TimeField(value, Loc.T("done"));
        field.Changed += changed;
        return Layout.Column(5, new TextLine(label, TextStyle.Caption), field);
    }

    private Control DayPreset(string label, IEnumerable<DayOfWeek> days)
    {
        var b = new PillButton(label, ButtonKind.Accent, size: ButtonSize.Small);
        DayOfWeek[] set = days.ToArray();
        b.Click += (_, _) =>
        {
            _editingRule!.Days = new HashSet<DayOfWeek>(set);
            RebuildSchedule();
        };
        return b;
    }

    private Control DayToggle(EditableConfiguration.EditableRule rule, DayOfWeek day)
    {
        string abbrev = DayAbbrev(day);
        var toggle = new RoundToggle(abbrev.Length > 2 ? abbrev[..2] : abbrev, rule.Days.Contains(day), Loc.Culture.DateTimeFormat.GetDayName(day));
        toggle.Toggled += on =>
        {
            if (on)
            {
                rule.Days.Add(day);
            }
            else
            {
                rule.Days.Remove(day);
            }

            RebuildSchedule();
        };
        return toggle;
    }

    private static string RuleSummary(EditableConfiguration.EditableRule rule)
    {
        var lines = new List<string> { ConditionSummary(rule) };
        string Describe(EditableConfiguration.EditableSite site, bool blocked)
        {
            IEnumerable<string> pages = site.Pages.Where(p => p.Block == blocked).Select(p => p.Name);
            if (blocked && site.BlockEverythingElse)
            {
                pages = pages.Append(Loc.T("everything_else_low"));
            }

            string list = string.Join(", ", pages);
            return list.Length == 0 ? "" : $"{site.DisplayLabel} ({list})";
        }

        string limited = string.Join("; ", rule.Sites.Select(s => Describe(s, true)).Where(s => s.Length > 0));
        string open = string.Join("; ", rule.Sites.Select(s => Describe(s, false)).Where(s => s.Length > 0));
        lines.Add(Loc.T("sum_limits", limited.Length == 0 ? Loc.T("nothing_yet") : limited));
        if (open.Length > 0)
        {
            lines.Add(Loc.T("sum_open", open));
        }

        return string.Join("\n", lines);
    }

    // ----- What it limits -----

    private Control BuildWhatColumn()
    {
        var addSites = new PillButton(Loc.T("add_websites"), ButtonKind.Secondary, Glyph.Plus);
        addSites.Click += (_, _) => OpenAddSites();

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(Layout.Column(Space.S1, new TextLine(Loc.T("what_it_limits"), TextStyle.Subheading), new TextLine(Loc.T("what_it_limits_help"), TextStyle.Small)));
        addSites.Margin = new Thickness(Space.S4, 0, 0, 0);
        addSites.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetColumn(addSites, 1);
        header.Children.Add(addSites);

        _sitesPanel = new StackPanel { Spacing = Space.S4 };
        RebuildSites();

        return Layout.Column(Space.S4, header, _sitesPanel, BuildTester());
    }

    private void RebuildSites()
    {
        if (_sitesPanel is null || _editingRule is not { } rule)
        {
            return;
        }

        _sitesPanel.Children.Clear();
        if (rule.Sites.Count == 0)
        {
            var add = new PillButton(Loc.T("add_websites"), ButtonKind.Primary, Glyph.Plus) { HorizontalAlignment = HorizontalAlignment.Center };
            add.Click += (_, _) => OpenAddSites();
            var globe = new Icon(Glyph.Globe, 28, TextTone.Tertiary) { HorizontalAlignment = HorizontalAlignment.Center };
            _sitesPanel.Children.Add(new Card(
                Layout.Column(10,
                    globe,
                    new TextLine(Loc.T("no_websites_yet"), TextStyle.Label) { Centered = true },
                    new TextLine(Loc.T("no_websites_hint"), TextStyle.Small) { Centered = true },
                    add),
                CardStyle.Surface,
                CardPadding.Spacious));
            return;
        }

        foreach (EditableConfiguration.EditableSite site in rule.Sites)
        {
            _sitesPanel.Children.Add(SiteCard(rule, site));
        }
    }

    private Control SiteCard(EditableConfiguration.EditableRule rule, EditableConfiguration.EditableSite site)
    {
        bool open = _openSites.Contains(site);
        int blocked = site.Pages.Count(p => p.Block);
        string summary = site.BlockEverythingElse
            ? BlockedSummary(site)
            : Loc.T("pages_summary", blocked, site.Pages.Count - blocked);

        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto") };
        Monogram mono = SiteLook.Monogram(site.CatalogId, site.DisplayLabel);
        mono.Margin = new Thickness(0, 0, 14, 0);
        head.Children.Add(mono);
        StackPanel titles = Layout.Column(2, new TextLine(site.DisplayLabel, TextStyle.Subheading), new TextLine(site.Host, TextStyle.Code, TextTone.Tertiary));
        Grid.SetColumn(titles, 1);
        head.Children.Add(titles);
        var sum = new TextLine(summary, TextStyle.Small) { Margin = new Thickness(Space.S3, 0) };
        Grid.SetColumn(sum, 2);
        head.Children.Add(sum);
        var chevron = new Icon(open ? Glyph.ChevronUp : Glyph.ChevronDown, 18, TextTone.Tertiary);
        Grid.SetColumn(chevron, 3);
        head.Children.Add(chevron);

        var header = new Pressable(head, new Thickness(Space.S5, Space.S4));
        header.Click += (_, _) =>
        {
            if (!_openSites.Remove(site))
            {
                _openSites.Add(site);
            }

            RebuildSites();
        };

        var stack = new StackPanel();
        stack.Children.Add(header);

        if (open)
        {
            stack.Children.Add(new Divider());
            foreach (EditableConfiguration.EditablePage page in site.Pages)
            {
                stack.Children.Add(PageRow(site, page));
                stack.Children.Add(new Divider());
            }

            stack.Children.Add(EverythingElseRow(site));
            stack.Children.Add(new Divider());

            var addPage = new PillButton(Loc.T("add_page_on", site.Host), ButtonKind.Accent, Glyph.Plus);
            addPage.Click += (_, _) =>
            {
                var page = new EditableConfiguration.EditablePage { Name = Loc.T("new_page"), Path = "/", Match = PageMatch.Under, Block = true };
                site.Pages.Add(page);
                _editingPage = page;
                RebuildSites();
            };
            var remove = new PillButton(Loc.T("remove_site", site.DisplayLabel), ButtonKind.Ghost);
            remove.Click += (_, _) =>
            {
                rule.Sites.Remove(site);
                _openSites.Remove(site);
                RebuildSites();
                RebuildSchedule();
                RefreshTester(updateMarkers: false);
            };
            var footer = new DockPanel { LastChildFill = false, Margin = new Thickness(10, 6) };
            DockPanel.SetDock(addPage, Dock.Left);
            DockPanel.SetDock(remove, Dock.Right);
            footer.Children.Add(addPage);
            footer.Children.Add(remove);
            stack.Children.Add(footer);
        }

        return new Card(stack, CardStyle.Surface, CardPadding.None);
    }

    private static Segmented VerdictSwitch(bool block) => new(
        new Segment[] { new(Loc.T("block"), Tone.Danger), new(Loc.T("allow"), Tone.Accent) },
        block ? 0 : 1,
        stretch: false,
        height: 36);

    private Control PageRow(EditableConfiguration.EditableSite site, EditableConfiguration.EditablePage page)
    {
        bool editing = ReferenceEquals(_editingPage, page);
        bool hit = ReferenceEquals(_testHit, page);

        StackPanel nameLine = Layout.Column(Space.S1, new TextLine(page.Name.Length == 0 ? page.Path : page.Name, TextStyle.Strong));
        if (hit)
        {
            nameLine.Children.Add(new Chip(Loc.T("matches_test"), Tone.Ink));
        }

        nameLine.VerticalAlignment = VerticalAlignment.Center;

        StackPanel address = Layout.Column(2, new TextLine(PageAddress(site, page), TextStyle.Code), new TextLine(MatchLabel(page), TextStyle.Caption));

        Segmented verdict = VerdictSwitch(page.Block);
        verdict.Changed += i =>
        {
            page.Block = i == 0;
            RebuildSchedule();
            RebuildSites();
            RefreshTester(updateMarkers: true);
        };

        var edit = new IconButton(Glyph.Pencil, Loc.T("edit_page"), selected: editing);
        edit.Click += (_, _) =>
        {
            _editingPage = editing ? null : page;
            _newCondition = "";
            RebuildSites();
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,1.2*,Auto,Auto") };
        nameLine.Margin = new Thickness(0, 0, Space.S3, 0);
        grid.Children.Add(nameLine);
        address.Margin = new Thickness(0, 0, Space.S3, 0);
        Grid.SetColumn(address, 1);
        grid.Children.Add(address);
        verdict.Margin = new Thickness(0, 0, Space.S2, 0);
        Grid.SetColumn(verdict, 2);
        grid.Children.Add(verdict);
        Grid.SetColumn(edit, 3);
        grid.Children.Add(edit);

        var row = new StackPanel();
        row.Children.Add(new Band(grid, new Thickness(Space.S5, Space.S3)));
        if (editing)
        {
            row.Children.Add(PageEditor(site, page));
        }

        return new Band(row, default, shaded: hit);
    }

    private Control PageEditor(EditableConfiguration.EditableSite site, EditableConfiguration.EditablePage page)
    {
        var name = new TextField(page.Name);
        name.Changed += text => page.Name = text;

        var path = new TextField(page.Path, style: TextFieldStyle.Code, prefix: site.Host);
        path.Changed += text =>
        {
            page.Path = text;
            RefreshTester(updateMarkers: false);
        };

        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,1.4*") };
        fields.Children.Add(Layout.Column(6, FieldLabel(Loc.T("name")), name));
        Control pathField = Layout.Column(6, FieldLabel(Loc.T("address")), path);
        Grid.SetColumn(pathField, 2);
        fields.Children.Add(pathField);

        // Which addresses count
        var modes = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = Space.S2, LineSpacing = Space.S2 };
        foreach ((PageMatch mode, string label) in new[]
                 {
                     (PageMatch.Exact, Loc.T("match_exact")),
                     (PageMatch.Under, Loc.T("match_under")),
                     (PageMatch.Prefix, Loc.T("match_prefix")),
                 })
        {
            var option = new RadioPill(label, page.Match == mode);
            option.Click += (_, _) =>
            {
                page.Match = mode;
                RebuildSites();
                RefreshTester(updateMarkers: false);
            };
            modes.Children.Add(option);
        }

        // Query conditions
        var conditions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = Space.S2, LineSpacing = Space.S2 };
        foreach (EditableConfiguration.EditableCondition condition in page.Conditions.ToList())
        {
            var chip = new RemovableChip(condition.Value.Length == 0 ? condition.Key : $"{condition.Key} = {condition.Value}", Loc.T("remove"));
            chip.Removed += () =>
            {
                page.Conditions.Remove(condition);
                RebuildSites();
                RefreshTester(updateMarkers: true);
            };
            conditions.Children.Add(chip);
        }

        var newCondition = new TextField(_newCondition, Loc.T("condition_hint"), TextFieldStyle.Code) { Width = 190 };
        newCondition.Changed += text => _newCondition = text;
        var addCondition = new PillButton(Loc.T("add"));
        void AddCondition()
        {
            string raw = _newCondition.Trim().TrimStart('?');
            int eq = raw.IndexOf('=');
            string key = (eq >= 0 ? raw[..eq] : raw).Trim();
            if (key.Length == 0)
            {
                return;
            }

            page.Conditions.Add(new EditableConfiguration.EditableCondition(key, eq >= 0 ? raw[(eq + 1)..].Trim() : ""));
            _newCondition = "";
            RebuildSites();
            RefreshTester(updateMarkers: true);
        }

        addCondition.Click += (_, _) => AddCondition();
        newCondition.Submitted += AddCondition;
        conditions.Children.Add(Layout.Row(Space.S2, newCondition, addCondition));

        var removePage = new PillButton(Loc.T("remove_page"), ButtonKind.Danger);
        removePage.Click += (_, _) =>
        {
            site.Pages.Remove(page);
            _editingPage = null;
            RebuildSites();
            RebuildSchedule();
            RefreshTester(updateMarkers: true);
        };
        var done = new PillButton(Loc.T("done"), ButtonKind.Primary);
        done.Click += (_, _) =>
        {
            _editingPage = null;
            RebuildSites();
            RebuildSchedule();
            RefreshTester(updateMarkers: true);
        };
        var footer = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(removePage, Dock.Left);
        DockPanel.SetDock(done, Dock.Right);
        footer.Children.Add(removePage);
        footer.Children.Add(done);

        StackPanel panel = Layout.Column(Space.S4,
            fields,
            Layout.Column(Space.S2, FieldLabel(Loc.T("which_addresses")), modes),
            Layout.Column(Space.S2, FieldLabel(Loc.T("only_when_contains")), conditions, new TextLine(Loc.T("conditions_help"), TextStyle.Caption)),
            footer);

        return new Card(panel, CardStyle.Well, CardPadding.Compact) { Margin = new Thickness(Space.S5, 0, Space.S5, Space.S4) };
    }

    private static TextLine FieldLabel(string text) => new(text, TextStyle.CaptionStrong);

    private Control EverythingElseRow(EditableConfiguration.EditableSite site)
    {
        Segmented verdict = VerdictSwitch(site.BlockEverythingElse);
        verdict.Changed += i =>
        {
            site.BlockEverythingElse = i == 0;
            RebuildSchedule();
            RebuildSites();
            RefreshTester(updateMarkers: true);
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,1.2*,Auto,40") };
        StackPanel text = Layout.Column(2, new TextLine(Loc.T("everything_else"), TextStyle.Strong), new TextLine(Loc.T("everything_else_help", site.Host), TextStyle.Caption));
        text.Margin = new Thickness(0, 0, Space.S3, 0);
        grid.Children.Add(text);
        var address = new TextLine(site.Host + "/…", TextStyle.Code, TextTone.Tertiary) { Margin = new Thickness(0, 0, Space.S3, 0) };
        Grid.SetColumn(address, 1);
        grid.Children.Add(address);
        verdict.Margin = new Thickness(0, 0, Space.S2, 0);
        Grid.SetColumn(verdict, 2);
        grid.Children.Add(verdict);

        return new Band(grid, new Thickness(Space.S5, Space.S3), shaded: true);
    }

    private static string PageAddress(EditableConfiguration.EditableSite site, EditableConfiguration.EditablePage page)
    {
        string path = SitePage.NormalizePath(page.Path);
        string query = page.Conditions.Count == 0
            ? ""
            : "?" + string.Join("&", page.Conditions.Select(c => c.Value.Length == 0 ? c.Key : $"{c.Key}={c.Value}"));
        string tail = page.Match switch
        {
            PageMatch.Under when path != "/" => "/…",
            PageMatch.Prefix => "…",
            _ => "",
        };
        return site.Host + path + tail + query;
    }

    private static string MatchLabel(EditableConfiguration.EditablePage page) => page.Match switch
    {
        PageMatch.Exact => Loc.T("match_exact"),
        PageMatch.Prefix => Loc.T("match_prefix"),
        _ => Loc.T("match_under"),
    };

    // ----- Address checker -----

    private Control BuildTester()
    {
        var input = new TextField(_testUrl, "instagram.com/?variant=following", TextFieldStyle.Code, leadingGlyph: Glyph.Globe);
        input.Changed += text =>
        {
            _testUrl = text;
            RefreshTester(updateMarkers: true);
        };

        var samples = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        foreach (string sample in SampleAddresses())
        {
            var chip = new SuggestionChip(sample);
            chip.Click += (_, _) => input.Text = sample;
            samples.Children.Add(chip);
        }

        _testResultHost = new ContentControl();
        RefreshTester(updateMarkers: false);

        return new Card(Layout.Column(Space.S3,
            Layout.Column(Space.S1, new TextLine(Loc.T("check_address"), TextStyle.Subheading), new TextLine(Loc.T("check_address_help"), TextStyle.Small)),
            input,
            samples,
            _testResultHost));
    }

    private IEnumerable<string> SampleAddresses()
    {
        if (_editingRule is null)
        {
            return Array.Empty<string>();
        }

        return _editingRule.Sites
            .SelectMany(site => site.Pages.Select(page =>
                site.Host + SitePage.NormalizePath(page.Path).TrimEnd('/') + "/" +
                (page.Conditions.Count == 0 ? "" : "?" + string.Join("&", page.Conditions.Select(c => $"{c.Key}={c.Value}")))))
            .Distinct()
            .Take(6);
    }

    private void RefreshTester(bool updateMarkers)
    {
        if (_testResultHost is null || _editingRule is not { } rule)
        {
            return;
        }

        (Tone? tone, string title, string detail, EditableConfiguration.EditablePage? hit) = EvaluateTest(rule, _testUrl);
        if (updateMarkers && !ReferenceEquals(hit, _testHit))
        {
            _testHit = hit;
            RebuildSites();
        }

        _testResultHost.Content = tone is { } t ? new Callout(t, title, detail) : null;
    }

    private static (Tone? Tone, string Title, string Detail, EditableConfiguration.EditablePage? Hit) EvaluateTest(
        EditableConfiguration.EditableRule rule, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, "", "", null);
        }

        if (!UrlNormalizer.TryNormalize(text, out Uri? uri) || uri is null)
        {
            return (Tone.Neutral, Loc.T("test_not_address"), Loc.T("test_not_address_help"), null);
        }

        var results = new List<(EditableConfiguration.EditableSite Site, TargetSite Target, SiteVerdict Verdict)>();
        foreach (EditableConfiguration.EditableSite site in rule.Sites)
        {
            TargetSite target;
            try
            {
                target = site.ToTargetSite();
            }
            catch (ConfigurationException ex)
            {
                return (Tone.Neutral, Loc.T("test_invalid"), ex.Message, null);
            }

            SiteVerdict verdict = target.Evaluate(uri);
            if (verdict.Applies)
            {
                results.Add((site, target, verdict));
            }
        }

        if (results.Count == 0)
        {
            return (Tone.Neutral, Loc.T("test_not_in_rule"), Loc.T("test_not_in_rule_help"), null);
        }

        // A site that blocks wins: the rule closes the tab if any of its sites says so.
        var (editable, chosen, decision) = results.Any(r => r.Verdict.Blocks)
            ? results.First(r => r.Verdict.Blocks)
            : results[0];

        string title = decision.Blocks
            ? rule.BlockCompletely ? Loc.T("test_blocked") : Loc.T("test_limited", rule.AllowanceMinutes)
            : Loc.T("test_open");
        Tone tone = decision.Blocks ? Tone.Danger : Tone.Accent;

        if (decision.Page is not { } best)
        {
            return (tone, title, Loc.T("test_rest_decides", editable.DisplayLabel), null);
        }

        string detail = Loc.T("test_matches", best.Name, editable.DisplayLabel);
        SitePage? runnerUp = chosen.MatchingPages(uri).Skip(1).FirstOrDefault(p => p.Blocks != best.Blocks);
        if (runnerUp is not null)
        {
            detail += " " + Loc.T("test_more_specific", runnerUp.Name, best.Name);
        }

        int index = chosen.Pages.ToList().IndexOf(best);
        EditableConfiguration.EditablePage? hit = index >= 0 && index < editable.Pages.Count ? editable.Pages[index] : null;
        return (tone, title, detail, hit);
    }

    // ----- Save / delete -----

    private void SaveEditingRule()
    {
        if (_editingRule is not { } rule)
        {
            return;
        }

        string? problem = rule.Sites.Count == 0 ? Loc.T("need_site")
            : rule.Days.Count == 0 ? Loc.T("need_day")
            : !rule.AllDay && rule.From == rule.To ? Loc.T("bad_window")
            : null;

        if (problem is null)
        {
            EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
            if (_editingIndex >= 0 && _editingIndex < candidate.Rules.Count)
            {
                candidate.Rules[_editingIndex] = rule;
            }
            else
            {
                candidate.Rules.Add(rule);
            }

            if (_controller.TrySaveConfiguration(candidate, out string? error))
            {
                _editable = _controller.GetEditableConfiguration();
                _editingRule = null;
                Navigate(_editorReturn);
                return;
            }

            problem = error ?? Loc.T("rule_invalid");
        }

        _editorError = Loc.T("couldnt_save") + ": " + problem;
        Rerender();
    }

    private void DeleteEditingRule()
    {
        if (_editingIndex < 0 || _editingIndex >= _editable.Rules.Count)
        {
            return;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        candidate.Rules.RemoveAt(_editingIndex);
        if (_controller.TrySaveConfiguration(candidate, out string? error))
        {
            _editable = _controller.GetEditableConfiguration();
            _editingRule = null;
            Navigate(_editorReturn);
            return;
        }

        _confirmDelete = false;
        _editorError = Loc.T("couldnt_delete") + ": " + (error ?? Loc.T("rule_invalid"));
        Rerender();
    }
}
