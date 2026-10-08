using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Rule editor (full window) ----------

    private EditableConfiguration.EditableRule? _editingRule;
    private int _editingIndex = -1;
    private readonly HashSet<EditableConfiguration.EditableSite> _openSites = new();
    private EditableConfiguration.EditablePage? _editingPage;
    private EditableConfiguration.EditablePage? _testHit;
    private string _testUrl = "";
    private string _newCondition = "";
    private bool _confirmDelete;
    private string? _editorError;

    private Border? _scheduleHost;
    private StackPanel? _sitesPanel;
    private Border? _testResultHost;

    private void OpenEditor(EditableConfiguration.EditableRule rule, int index)
    {
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

        PillButton back = UiTheme.Ghost(Loc.T("rules"), Icons.ArrowLeft);
        back.Click += (_, _) => Navigate(Page.Home);

        var name = new TextBox
        {
            Text = rule.Name,
            Watermark = Loc.T("rule_name"),
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            MinHeight = 44,
        };
        name.TextChanged += (_, _) => rule.Name = name.Text ?? "";

        var actions = UiTheme.HStack(10);
        if (_editingIndex >= 0)
        {
            PillButton delete = _confirmDelete
                ? UiTheme.Primary(Loc.T("confirm_delete"), Icons.Trash)
                : UiTheme.DangerText(Loc.T("delete_rule"), Icons.Trash);
            if (_confirmDelete)
            {
                delete[!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Danger);
            }

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

        PillButton cancel = UiTheme.Secondary(Loc.T("cancel"));
        cancel.Click += (_, _) => Navigate(Page.Home);
        PillButton save = UiTheme.Primary(Loc.T("save_rule"));
        save.Click += (_, _) => SaveEditingRule();
        actions.Children.Add(cancel);
        actions.Children.Add(save);

        _scheduleHost = new Border();
        RebuildSchedule();

        var body = new StackPanel { Spacing = 20 };
        if (_editorError is not null)
        {
            body.Children.Add(ErrorBanner(_editorError));
        }

        body.Children.Add(Columns(_scheduleHost, BuildWhatColumn(), 340, 940));
        return FullScreen(FullScreenBar(back, name, actions), body);
    }

    private static Control ErrorBanner(string message) => new Border
    {
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(16, 12),
        Child = UiTheme.HStack(10, Icons.Make(Icons.Ban, 18, UiTheme.Danger), UiTheme.Text(message, 14, FontWeight.Medium, UiTheme.Danger)),
        [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.BlockSoft),
    };

    // ----- When it applies -----

    private void RebuildSchedule()
    {
        if (_scheduleHost is null || _editingRule is not { } rule)
        {
            return;
        }

        var card = new StackPanel { Spacing = 20 };
        card.Children.Add(UiTheme.H2(Loc.T("when_it_applies")));

        // Limit
        var limit = new Segmented(new (string, string?)[] { (Loc.T("time_allowance"), null), (Loc.T("block_completely"), null) }, rule.BlockCompletely ? 1 : 0);
        limit.Changed += i => { rule.BlockCompletely = i == 1; RebuildSchedule(); };
        var limitSection = UiTheme.VStack(10, UiTheme.SectionLabel(Loc.T("limit")), limit);
        if (rule.BlockCompletely)
        {
            limitSection.Children.Add(UiTheme.Muted(Loc.T("block_completely_help")));
        }
        else
        {
            limitSection.Children.Add(UiTheme.HStack(12, MinutesStepper(rule), UiTheme.Body(Loc.T("minutes_every_hour"))));
            limitSection.Children.Add(UiTheme.Muted(Loc.T("allowance_help")));
        }

        card.Children.Add(limitSection);
        card.Children.Add(UiTheme.Divider());

        // Active window
        var active = new Segmented(new (string, string?)[] { (Loc.T("all_day"), null), (Loc.T("set_hours"), null) }, rule.AllDay ? 0 : 1);
        active.Changed += i => { rule.AllDay = i == 0; RebuildSchedule(); };
        var activeSection = UiTheme.VStack(10, UiTheme.SectionLabel(Loc.T("active")), active);
        if (!rule.AllDay)
        {
            activeSection.Children.Add(UiTheme.HStack(10,
                LabeledTime(Loc.T("from"), rule.From, t => { rule.From = t; RebuildSchedule(); }),
                LabeledTime(Loc.T("to"), rule.To, t => { rule.To = t; RebuildSchedule(); })));
            if (rule.To <= rule.From)
            {
                activeSection.Children.Add(UiTheme.Muted(Loc.T("runs_overnight")));
            }
        }

        card.Children.Add(activeSection);
        card.Children.Add(UiTheme.Divider());

        // Days
        var presets = UiTheme.HStack(0,
            DayPreset(Loc.T("every_day_btn"), DayOrder),
            DayPreset(Loc.T("weekdays_btn"), DayOrder.Take(5)),
            DayPreset(Loc.T("weekends_btn"), DayOrder.Skip(5)));
        var dayHeader = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(presets, Dock.Right);
        dayHeader.Children.Add(presets);
        dayHeader.Children.Add(UiTheme.SectionLabel(Loc.T("days")));
        var days = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 5, LineSpacing = 5 };
        foreach (DayOfWeek day in DayOrder)
        {
            days.Children.Add(DayToggle(rule, day));
        }

        card.Children.Add(UiTheme.VStack(10, dayHeader, days));

        // Plain-language summary
        card.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14),
            Child = UiTheme.VStack(6, UiTheme.SectionLabel(Loc.T("in_plain_words")), UiTheme.Body(RuleSummary(rule))),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
        });

        _scheduleHost.Child = UiTheme.Card(card, 22);
    }

    private Control MinutesStepper(EditableConfiguration.EditableRule rule)
    {
        TextBlock value = UiTheme.Text(rule.AllowanceMinutes.ToString(Loc.Culture), 18, FontWeight.SemiBold, UiTheme.TextPrimary);
        value.MinWidth = 44;
        value.TextAlignment = TextAlignment.Center;

        Border Step(string glyph, int delta, string tip)
        {
            var b = new Border
            {
                Width = 44,
                Height = 42,
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = UiTheme.Text(glyph, 18, FontWeight.Normal, UiTheme.TextPrimary),
            };
            ((TextBlock)b.Child).HorizontalAlignment = HorizontalAlignment.Center;
            ToolTip.SetTip(b, tip);
            b.AddHandler(Gestures.TappedEvent, (_, _) =>
            {
                rule.AllowanceMinutes = Math.Clamp(rule.AllowanceMinutes + delta, 1, 59);
                RebuildSchedule();
            });
            return b;
        }

        return new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Child = UiTheme.HStack(0, Step("−", -1, Loc.T("fewer_minutes")), value, Step("+", 1, Loc.T("more_minutes"))),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.BorderStrong),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface),
        };
    }

    private static Control LabeledTime(string label, TimeOnly value, Action<TimeOnly> changed)
    {
        var box = new TextBox { Text = value.ToString("HH:mm"), Width = 96, Watermark = "23:00", FontSize = 15 };
        box.LostFocus += (_, _) =>
        {
            if (TimeOnly.TryParse(box.Text, System.Globalization.CultureInfo.InvariantCulture, out TimeOnly t))
            {
                if (t != value)
                {
                    changed(t);
                }
            }
            else
            {
                box.Text = value.ToString("HH:mm");
            }
        };
        return UiTheme.VStack(5, UiTheme.Caption(label), box);
    }

    private Control DayPreset(string label, IEnumerable<DayOfWeek> days)
    {
        PillButton b = UiTheme.AccentText_(label);
        b.MinHeight = 28;
        b.Padding = new Thickness(6, 0);
        if (b.Child is StackPanel row && row.Children.OfType<TextBlock>().FirstOrDefault() is { } text)
        {
            text.FontSize = 12.5;
        }

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
        bool on = rule.Days.Contains(day);
        string abbrev = DayAbbrev(day);
        var label = UiTheme.Text(abbrev.Length > 2 ? abbrev[..2] : abbrev, 12.5, FontWeight.SemiBold, on ? UiTheme.InkText : UiTheme.TextSecondary);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        var b = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = label,
            [!Border.BackgroundProperty] = UiTheme.Dyn(on ? UiTheme.Ink : UiTheme.Surface),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(on ? UiTheme.Ink : UiTheme.BorderStrong),
        };
        ToolTip.SetTip(b, Loc.Culture.DateTimeFormat.GetDayName(day));
        b.AddHandler(Gestures.TappedEvent, (_, _) =>
        {
            if (!rule.Days.Remove(day))
            {
                rule.Days.Add(day);
            }

            RebuildSchedule();
        });
        return b;
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
        PillButton addSites = UiTheme.Secondary(Loc.T("add_websites"), Icons.Plus);
        addSites.Click += (_, _) => OpenAddSites();

        TextBlock help = UiTheme.Muted(Loc.T("what_it_limits_help"));
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(UiTheme.VStack(4, UiTheme.H2(Loc.T("what_it_limits")), help));
        addSites.Margin = new Thickness(16, 0, 0, 0);
        addSites.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetColumn(addSites, 1);
        header.Children.Add(addSites);

        _sitesPanel = new StackPanel { Spacing = 16 };
        RebuildSites();

        return UiTheme.VStack(16, header, _sitesPanel, BuildTester());
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
            PillButton add = UiTheme.Primary(Loc.T("add_websites"), Icons.Plus);
            add.HorizontalAlignment = HorizontalAlignment.Center;
            add.Click += (_, _) => OpenAddSites();
            Control globe = Icons.Make(Icons.Globe, 28, UiTheme.TextTertiary);
            globe.HorizontalAlignment = HorizontalAlignment.Center;
            TextBlock title = UiTheme.Strong(Loc.T("no_websites_yet"), 15);
            TextBlock sub = UiTheme.Muted(Loc.T("no_websites_hint"));
            title.HorizontalAlignment = HorizontalAlignment.Center;
            sub.HorizontalAlignment = HorizontalAlignment.Center;
            sub.TextAlignment = TextAlignment.Center;
            _sitesPanel.Children.Add(UiTheme.Card(UiTheme.VStack(10, globe, title, sub, add), 32));
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

        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.Hand) };
        Border mono = UiTheme.Monogram(site.CatalogId, site.DisplayLabel);
        mono.Margin = new Thickness(0, 0, 14, 0);
        head.Children.Add(mono);
        var titles = UiTheme.VStack(2, UiTheme.Strong(site.DisplayLabel, 16), UiTheme.Mono(site.Host, UiTheme.TextTertiary));
        Grid.SetColumn(titles, 1);
        head.Children.Add(titles);
        TextBlock sum = UiTheme.Muted(summary);
        sum.Margin = new Thickness(12, 0);
        Grid.SetColumn(sum, 2);
        head.Children.Add(sum);
        Control chevron = Icons.Make(open ? Icons.ChevronUp : Icons.ChevronDown, 18, UiTheme.TextTertiary);
        Grid.SetColumn(chevron, 3);
        head.Children.Add(chevron);
        head.AddHandler(Gestures.TappedEvent, (_, _) =>
        {
            if (!_openSites.Remove(site))
            {
                _openSites.Add(site);
            }

            RebuildSites();
        });

        var stack = new StackPanel();
        stack.Children.Add(new Border { Padding = new Thickness(20, 16), Child = head });

        if (open)
        {
            stack.Children.Add(UiTheme.Divider());
            foreach (EditableConfiguration.EditablePage page in site.Pages)
            {
                stack.Children.Add(PageRow(site, page));
                stack.Children.Add(UiTheme.Divider());
            }

            stack.Children.Add(EverythingElseRow(site));
            stack.Children.Add(UiTheme.Divider());

            PillButton addPage = UiTheme.AccentText_(Loc.T("add_page_on", site.Host), Icons.Plus);
            addPage.Click += (_, _) =>
            {
                var page = new EditableConfiguration.EditablePage { Name = Loc.T("new_page"), Path = "/", Match = PageMatch.Under, Block = true };
                site.Pages.Add(page);
                _editingPage = page;
                RebuildSites();
            };
            PillButton remove = UiTheme.Ghost(Loc.T("remove_site", site.DisplayLabel));
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

        Border card = UiTheme.Card(stack, 0);
        card.ClipToBounds = true;
        return card;
    }

    private Control PageRow(EditableConfiguration.EditableSite site, EditableConfiguration.EditablePage page)
    {
        bool editing = ReferenceEquals(_editingPage, page);
        bool hit = ReferenceEquals(_testHit, page);

        var nameLine = UiTheme.VStack(4, UiTheme.Strong(page.Name.Length == 0 ? page.Path : page.Name));
        if (hit)
        {
            nameLine.Children.Add(UiTheme.Chip(Loc.T("matches_test"), UiTheme.Ink, UiTheme.InkText));
        }

        nameLine.VerticalAlignment = VerticalAlignment.Center;

        var address = UiTheme.VStack(2, UiTheme.Mono(PageAddress(site, page)), UiTheme.Caption(MatchLabel(page)));

        var verdict = new Segmented(
            new (string, string?)[] { (Loc.T("block"), UiTheme.Danger), (Loc.T("allow"), UiTheme.Accent) },
            page.Block ? 0 : 1,
            stretch: false,
            height: 36);
        verdict.Changed += i =>
        {
            page.Block = i == 0;
            RebuildSchedule();
            RebuildSites();
            RefreshTester(updateMarkers: true);
        };

        PillButton edit = UiTheme.IconButton(Icons.Pencil, Loc.T("edit_page"));
        if (editing)
        {
            edit[!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Ink);
        }

        edit.Click += (_, _) =>
        {
            _editingPage = editing ? null : page;
            _newCondition = "";
            RebuildSites();
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,1.2*,Auto,Auto") };
        nameLine.Margin = new Thickness(0, 0, 12, 0);
        grid.Children.Add(nameLine);
        address.Margin = new Thickness(0, 0, 12, 0);
        Grid.SetColumn(address, 1);
        grid.Children.Add(address);
        verdict.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetColumn(verdict, 2);
        grid.Children.Add(verdict);
        Grid.SetColumn(edit, 3);
        grid.Children.Add(edit);

        var row = new StackPanel();
        row.Children.Add(new Border { Padding = new Thickness(20, 12), Child = grid });
        if (editing)
        {
            row.Children.Add(PageEditor(site, page));
        }

        var host = new Border { Child = row, Background = Brushes.Transparent };
        if (hit)
        {
            host[!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.SurfaceAlt);
        }

        return host;
    }

    private Control PageEditor(EditableConfiguration.EditableSite site, EditableConfiguration.EditablePage page)
    {
        var name = new TextBox { Text = page.Name };
        name.TextChanged += (_, _) => page.Name = name.Text ?? "";

        var path = new TextBox
        {
            Text = page.Path,
            FontFamily = new FontFamily(UiTheme.MonoFont),
            InnerLeftContent = new TextBlock
            {
                Text = site.Host,
                FontFamily = new FontFamily(UiTheme.MonoFont),
                FontSize = 13,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                [!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.TextTertiary),
            },
        };
        path.TextChanged += (_, _) =>
        {
            page.Path = path.Text ?? "/";
            RefreshTester(updateMarkers: false);
        };

        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,1.4*") };
        fields.Children.Add(UiTheme.VStack(6, FieldLabel(Loc.T("name")), name));
        Control pathField = UiTheme.VStack(6, FieldLabel(Loc.T("address")), path);
        Grid.SetColumn(pathField, 2);
        fields.Children.Add(pathField);

        // Which addresses count
        var modes = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        foreach ((PageMatch mode, string label) in new[]
                 {
                     (PageMatch.Exact, Loc.T("match_exact")),
                     (PageMatch.Under, Loc.T("match_under")),
                     (PageMatch.Prefix, Loc.T("match_prefix")),
                 })
        {
            modes.Children.Add(OptionButton(label, page.Match == mode, () =>
            {
                page.Match = mode;
                RebuildSites();
                RefreshTester(updateMarkers: false);
            }));
        }

        // Query conditions
        var conditions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        foreach (EditableConfiguration.EditableCondition condition in page.Conditions.ToList())
        {
            PillButton remove = UiTheme.IconButton(Icons.Close, Loc.T("remove"));
            remove.Width = 28;
            remove.Height = 28;
            remove.MinHeight = 28;
            remove.BorderThickness = new Thickness(0);
            remove.Background = Brushes.Transparent;
            remove.Click += (_, _) =>
            {
                page.Conditions.Remove(condition);
                RebuildSites();
                RefreshTester(updateMarkers: true);
            };
            TextBlock text = UiTheme.Mono(condition.Value.Length == 0 ? condition.Key : $"{condition.Key} = {condition.Value}");
            conditions.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 2, 2, 2),
                Child = UiTheme.HStack(4, text, remove),
                [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface),
                [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.BorderStrong),
            });
        }

        var newCondition = new TextBox
        {
            Text = _newCondition,
            Watermark = Loc.T("condition_hint"),
            Width = 190,
            FontFamily = new FontFamily(UiTheme.MonoFont),
        };
        newCondition.TextChanged += (_, _) => _newCondition = newCondition.Text ?? "";
        PillButton addCondition = UiTheme.Secondary(Loc.T("add"));
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
        newCondition.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                AddCondition();
            }
        };
        conditions.Children.Add(UiTheme.HStack(8, newCondition, addCondition));

        PillButton removePage = UiTheme.DangerText(Loc.T("remove_page"));
        removePage.Click += (_, _) =>
        {
            site.Pages.Remove(page);
            _editingPage = null;
            RebuildSites();
            RebuildSchedule();
            RefreshTester(updateMarkers: true);
        };
        PillButton done = UiTheme.Primary(Loc.T("done"));
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

        var panel = UiTheme.VStack(16,
            fields,
            UiTheme.VStack(8, FieldLabel(Loc.T("which_addresses")), modes),
            UiTheme.VStack(8, FieldLabel(Loc.T("only_when_contains")), conditions, UiTheme.Caption(Loc.T("conditions_help"))),
            footer);

        return new Border
        {
            Margin = new Thickness(20, 0, 20, 16),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Child = panel,
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
        };
    }

    private static TextBlock FieldLabel(string text) => UiTheme.Text(text, 12.5, FontWeight.SemiBold, UiTheme.TextSecondary);

    private static Control OptionButton(string label, bool selected, Action pick)
    {
        var b = new Border
        {
            MinHeight = 40,
            Padding = new Thickness(14, 0),
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(selected ? 1.5 : 1),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = UiTheme.Text(label, 13.5, selected ? FontWeight.SemiBold : FontWeight.Normal, UiTheme.TextPrimary),
            [!Border.BackgroundProperty] = UiTheme.Dyn(selected ? UiTheme.Surface : UiTheme.AppBg),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(selected ? UiTheme.Ink : UiTheme.BorderStrong),
        };
        b.AddHandler(Gestures.TappedEvent, (_, _) => pick());
        return b;
    }

    private Control EverythingElseRow(EditableConfiguration.EditableSite site)
    {
        var verdict = new Segmented(
            new (string, string?)[] { (Loc.T("block"), UiTheme.Danger), (Loc.T("allow"), UiTheme.Accent) },
            site.BlockEverythingElse ? 0 : 1,
            stretch: false,
            height: 36);
        verdict.Changed += i =>
        {
            site.BlockEverythingElse = i == 0;
            RebuildSchedule();
            RebuildSites();
            RefreshTester(updateMarkers: true);
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,1.2*,Auto,40") };
        Control text = UiTheme.VStack(2, UiTheme.Strong(Loc.T("everything_else")), UiTheme.Caption(Loc.T("everything_else_help", site.Host)));
        text.Margin = new Thickness(0, 0, 12, 0);
        grid.Children.Add(text);
        TextBlock address = UiTheme.Mono(site.Host + "/…", UiTheme.TextTertiary);
        address.Margin = new Thickness(0, 0, 12, 0);
        Grid.SetColumn(address, 1);
        grid.Children.Add(address);
        verdict.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetColumn(verdict, 2);
        grid.Children.Add(verdict);

        return new Border
        {
            Padding = new Thickness(20, 12),
            Child = grid,
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.SurfaceAlt),
        };
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
        var input = new TextBox
        {
            Text = _testUrl,
            Watermark = "instagram.com/?variant=following",
            FontFamily = new FontFamily(UiTheme.MonoFont),
            InnerLeftContent = new Border { Margin = new Thickness(12, 0, 4, 0), Child = Icons.Make(Icons.Globe, 16, UiTheme.TextTertiary) },
        };
        input.TextChanged += (_, _) =>
        {
            _testUrl = input.Text ?? "";
            RefreshTester(updateMarkers: true);
        };

        var samples = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        foreach (string sample in SampleAddresses())
        {
            var chip = new Border
            {
                Height = 32,
                Padding = new Thickness(10, 0),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = UiTheme.Mono(sample, UiTheme.TextSecondary),
                [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
                [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
            };
            chip.AddHandler(Gestures.TappedEvent, (_, _) => input.Text = sample);
            samples.Children.Add(chip);
        }

        _testResultHost = new Border();
        RefreshTester(updateMarkers: false);

        return UiTheme.Card(UiTheme.VStack(12,
            UiTheme.VStack(4, UiTheme.H2(Loc.T("check_address")), UiTheme.Muted(Loc.T("check_address_help"))),
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

        (string tone, string title, string detail, EditableConfiguration.EditablePage? hit) = EvaluateTest(rule, _testUrl);
        if (updateMarkers && !ReferenceEquals(hit, _testHit))
        {
            _testHit = hit;
            RebuildSites();
        }

        if (tone.Length == 0)
        {
            _testResultHost.Child = null;
            return;
        }

        (string bg, string fg) = tone switch
        {
            "block" => (UiTheme.BlockSoft, UiTheme.Danger),
            "allow" => (UiTheme.AllowSoft, UiTheme.Accent),
            _ => (UiTheme.Track, UiTheme.TextSecondary),
        };

        var dot = UiTheme.Dot(fg, 10);
        dot.VerticalAlignment = VerticalAlignment.Top;
        dot.Margin = new Thickness(0, 5, 12, 0);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(dot);
        Control text = UiTheme.VStack(3, UiTheme.Text(title, 14, FontWeight.SemiBold, fg), UiTheme.Body(detail));
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        _testResultHost.Child = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 14),
            Child = grid,
            [!Border.BackgroundProperty] = UiTheme.Dyn(bg),
        };
    }

    private static (string Tone, string Title, string Detail, EditableConfiguration.EditablePage? Hit) EvaluateTest(
        EditableConfiguration.EditableRule rule, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ("", "", "", null);
        }

        if (!UrlNormalizer.TryNormalize(text, out Uri? uri) || uri is null)
        {
            return ("none", Loc.T("test_not_address"), Loc.T("test_not_address_help"), null);
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
                return ("none", Loc.T("test_invalid"), ex.Message, null);
            }

            SiteVerdict verdict = target.Evaluate(uri);
            if (verdict.Applies)
            {
                results.Add((site, target, verdict));
            }
        }

        if (results.Count == 0)
        {
            return ("none", Loc.T("test_not_in_rule"), Loc.T("test_not_in_rule_help"), null);
        }

        // A site that blocks wins: the rule closes the tab if any of its sites says so.
        var (editable, chosen, decision) = results.Any(r => r.Verdict.Blocks)
            ? results.First(r => r.Verdict.Blocks)
            : results[0];

        string title = decision.Blocks
            ? rule.BlockCompletely ? Loc.T("test_blocked") : Loc.T("test_limited", rule.AllowanceMinutes)
            : Loc.T("test_open");

        if (decision.Page is not { } best)
        {
            return (decision.Blocks ? "block" : "allow", title, Loc.T("test_rest_decides", editable.DisplayLabel), null);
        }

        string detail = Loc.T("test_matches", best.Name, editable.DisplayLabel);
        SitePage? runnerUp = chosen.MatchingPages(uri).Skip(1).FirstOrDefault(p => p.Blocks != best.Blocks);
        if (runnerUp is not null)
        {
            detail += " " + Loc.T("test_more_specific", runnerUp.Name, best.Name);
        }

        int index = chosen.Pages.ToList().IndexOf(best);
        EditableConfiguration.EditablePage? hit = index >= 0 && index < editable.Pages.Count ? editable.Pages[index] : null;
        return (decision.Blocks ? "block" : "allow", title, detail, hit);
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
                Navigate(Page.Home);
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
            Navigate(Page.Home);
            return;
        }

        _confirmDelete = false;
        _editorError = Loc.T("couldnt_delete") + ": " + (error ?? Loc.T("rule_invalid"));
        Rerender();
    }
}
