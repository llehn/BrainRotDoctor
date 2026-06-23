using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Configuration;
using System.Threading.Tasks;

namespace BrainRotDoctor.App.Ui;

internal sealed class MainWindow : Window
{
    private enum Page { Home, Edit, Settings, Strict }

    private static readonly DayOfWeek[] DayOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    };

    private readonly EnforcementController _controller;
    private readonly UiSettingsStore _settings;
    private readonly Action<ThemePreference> _applyTheme;

    private readonly ContentControl _host = new();
    private EditableConfiguration _editable;
    private Page _page = Page.Home;
    private bool _strictActive;

    // Editing context
    private EditableConfiguration.EditableRule? _editingRule;
    private int _editingIndex = -1;
    private StackPanel? _editSitesPanel;

    // Home live elements (rebuilt per visit)
    private readonly Dictionary<string, Action<RuleSnapshot?>> _liveUpdaters = new(StringComparer.Ordinal);
    private Border? _statusDot;
    private TextBlock? _statusText;
    private PillButton? _strictButton;

    public MainWindow(EnforcementController controller, UiSettingsStore settings, Action<ThemePreference> applyTheme)
    {
        _controller = controller;
        _settings = settings;
        _applyTheme = applyTheme;
        _editable = controller.GetEditableConfiguration();

        Title = "BrainRotDoctor";
        Width = 980;
        Height = 680;
        MinWidth = 760;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Inter, $Default");
        this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg);
        Content = _host;

        _strictActive = _controller.Status.StrictMode.IsActive;
        if (_strictActive)
        {
            NavigateStrict();
        }
        else
        {
            NavigateHome();
        }

        _controller.StatusChanged += OnStatusChanged;
        Loc.Changed += Rerender;
        ApplyStatus(_controller.Status);
    }

    private void Rerender()
    {
        switch (_page)
        {
            case Page.Settings: NavigateSettings(); break;
            case Page.Strict: NavigateStrict(); break;
            case Page.Edit when _editingRule is not null: NavigateEdit(_editingRule, _editingIndex); break;
            default: NavigateHome(); break;
        }
    }

    private static string DayAbbrev(DayOfWeek day) =>
        Loc.Culture.DateTimeFormat.AbbreviatedDayNames[(int)day];

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    // ---------- Shared chrome ----------

    private Control TopBar(Control left, Control? right = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(22, 14, 18, 14),
            VerticalAlignment = VerticalAlignment.Center,
        };
        left.VerticalAlignment = VerticalAlignment.Center;
        left.HorizontalAlignment = HorizontalAlignment.Left;
        grid.Children.Add(left);
        if (right is not null)
        {
            right.VerticalAlignment = VerticalAlignment.Center;
            right.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
        }

        return new Border
        {
            Child = grid,
            BorderThickness = new Thickness(0, 0, 0, 1),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
        };
    }

    private static Control Section(string label, Control body)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(UiTheme.SectionLabel(label));
        stack.Children.Add(body);
        return stack;
    }

    private static StackPanel HStack(double spacing, params Control[] children)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (Control c in children)
        {
            c.VerticalAlignment = VerticalAlignment.Center;
            s.Children.Add(c);
        }

        return s;
    }

    // ---------- Navigation ----------

    private void NavigateHome()
    {
        _page = Page.Home;
        _host.Content = BuildHome();
        ApplyStatus(_controller.Status);
    }

    private void NavigateEdit(EditableConfiguration.EditableRule rule, int index)
    {
        _page = Page.Edit;
        _editingRule = rule;
        _editingIndex = index;
        _host.Content = BuildEdit(rule);
    }

    private void NavigateSettings()
    {
        _page = Page.Settings;
        _host.Content = BuildSettings();
    }

    private void NavigateStrict()
    {
        _page = Page.Strict;
        _host.Content = BuildStrict();
    }

    // ---------- Home ----------

    private Control BuildHome()
    {
        _liveUpdaters.Clear();

        _statusDot = UiTheme.Dot(UiTheme.Success);
        _statusText = UiTheme.Muted(Loc.T("protected"));
        var statusChip = new Border
        {
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(11, 5),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.SurfaceAlt),
            Child = HStack(7, _statusDot, _statusText),
        };

        _strictButton = UiTheme.Ghost(Loc.T("strict_mode"));
        _strictButton.Click += (_, _) => NavigateStrict();

        var settings = UiTheme.Icon("⚙", Loc.T("settings"));
        settings.Click += (_, _) => NavigateSettings();

        Control header = TopBar(
            UiTheme.H1("BrainRotDoctor"),
            HStack(10, statusChip, _strictButton, settings));

        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < _editable.Rules.Count; i++)
        {
            wrap.Children.Add(BuildRuleCard(_editable.Rules[i], i));
        }

        wrap.Children.Add(BuildAddTile());

        var body = new ScrollViewer
        {
            Padding = new Thickness(22, 20, 14, 20),
            Content = wrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }

    private Control BuildRuleCard(EditableConfiguration.EditableRule rule, int index)
    {
        var dot = UiTheme.Dot(UiTheme.TextSecondary);
        var live = UiTheme.Muted("—");
        _liveUpdaters[rule.Id] = snapshot => UpdateCardLive(dot, live, rule, snapshot);

        var top = new StackPanel { Spacing = 8 };
        var name = UiTheme.H2(string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name);
        top.Children.Add(name);
        top.Children.Add(UiTheme.Muted(ConditionSummary(rule)));
        top.Children.Add(UiTheme.Muted(SitesSummary(rule)));

        Control footer = HStack(7, dot, live);
        DockPanel.SetDock(footer, Dock.Bottom);

        var content = new DockPanel { LastChildFill = true };
        content.Children.Add(footer);
        content.Children.Add(top);

        Border card = UiTheme.Card(content);
        card.Height = 150;

        var grid = new Grid { Width = 296, Margin = new Thickness(0, 0, 16, 16) };
        grid.Children.Add(card);

        if (!_strictActive)
        {
            var overlay = new Border { Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.Hand) };
            overlay.AddHandler(Gestures.TappedEvent, (_, _) => NavigateEdit(rule.Clone(), index));
            grid.Children.Add(overlay);
        }

        return grid;
    }

    private Control BuildAddTile()
    {
        var label = UiTheme.H2("+  " + Loc.T("new_rule"));
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.Accent);

        var card = new Border
        {
            Height = 150,
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1.5),
            Child = label,
            // A null background isn't hit-testable, so without this only the text
            // glyphs would catch the tap; transparent makes the whole card clickable.
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Accent),
        };
        card.AddHandler(Gestures.TappedEvent, (_, _) => NavigateEdit(NewRule(), -1));

        return new Grid { Width = 296, Margin = new Thickness(0, 0, 16, 16), Children = { card } };
    }

    private void UpdateCardLive(Border dot, TextBlock text, EditableConfiguration.EditableRule rule, RuleSnapshot? s)
    {
        string key;
        string label;
        if (s is null || !s.IsActive)
        {
            key = UiTheme.TextSecondary;
            label = rule.AllDay ? Loc.T("idle") : Loc.T("off_hours");
        }
        else if (s.IsBlocking)
        {
            key = UiTheme.Danger;
            label = s.BlocksCompletely
                ? (s.ActiveWindowEndsAt is { } e ? Loc.T("blocked_until", e.ToLocalTime().ToString("HH:mm")) : Loc.T("blocked"))
                : (s.HourResetsAt is { } r ? Loc.T("used_up_reset", r.ToLocalTime().ToString("HH:mm")) : Loc.T("used_up"));
        }
        else
        {
            key = s.Remaining.TotalMinutes <= 1 ? UiTheme.Warn : UiTheme.Success;
            label = Loc.T("time_left", FormatSpan(s.Remaining));
        }

        dot[!Border.BackgroundProperty] = UiTheme.Dyn(key);
        text.Text = label;
    }

    // ---------- Edit ----------

    private Control BuildEdit(EditableConfiguration.EditableRule rule)
    {
        var back = UiTheme.Ghost("←  " + Loc.T("back"));
        back.Click += (_, _) => NavigateHome();
        var cancel = UiTheme.Ghost(Loc.T("cancel"));
        cancel.Click += (_, _) => NavigateHome();
        var save = UiTheme.Primary(Loc.T("save"));
        save.Click += async (_, _) => await SaveEditingRule();

        // Delete sits with the other returning actions; it confirms then goes back.
        StackPanel actions;
        if (_editingIndex >= 0)
        {
            int indexToDelete = _editingIndex;
            var delete = UiTheme.Ghost(Loc.T("delete"));
            delete.Click += async (_, _) => await DeleteRule(indexToDelete);
            actions = HStack(10, delete, cancel, save);
        }
        else
        {
            actions = HStack(10, cancel, save);
        }

        Control header = TopBar(back, actions);

        var nameBox = new TextBox { Text = rule.Name, Watermark = Loc.T("rule_name"), FontSize = 15, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        nameBox.TextChanged += (_, _) => rule.Name = nameBox.Text ?? "";

        _editSitesPanel = new StackPanel { Spacing = 8 };
        RebuildEditSites(rule);
        var addSite = UiTheme.Ghost("+  " + Loc.T("add"));
        addSite.HorizontalAlignment = HorizontalAlignment.Left;
        addSite.Click += async (_, _) => await ShowAddPicker(rule);
        var what = new StackPanel { Spacing = 12, Children = { _editSitesPanel, addSite } };

        // When | What side by side so the whole rule fits without scrolling.
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        Control whenCol = Section(Loc.T("when"), BuildWhenEditor(rule));
        Control whatCol = Section(Loc.T("what_to_block"), what);
        whatCol.Margin = new Thickness(24, 0, 0, 0);
        Grid.SetColumn(whatCol, 1);
        columns.Children.Add(whenCol);
        columns.Children.Add(whatCol);

        var body = new StackPanel { Spacing = 22 };
        body.Children.Add(Section(Loc.T("name"), nameBox));
        body.Children.Add(columns);

        var scroll = new ScrollViewer
        {
            Padding = new Thickness(24, 22, 24, 16),
            Content = new Border { Child = body, MaxWidth = 900, HorizontalAlignment = HorizontalAlignment.Left },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(scroll);
        return root;
    }

    private Control BuildWhenEditor(EditableConfiguration.EditableRule rule)
    {
        // Allowance vs block completely
        var minutes = NumberBox(rule.AllowanceMinutes, 1, 59);
        minutes.ValueChanged += (_, _) => rule.AllowanceMinutes = (int)(minutes.Value ?? 5);
        minutes.IsEnabled = !rule.BlockCompletely;

        var allowRadio = new RadioButton { GroupName = "allowance", IsChecked = !rule.BlockCompletely };
        allowRadio.Content = HStack(8, UiTheme.Body(Loc.T("allow")), minutes, UiTheme.Body(Loc.T("minutes_per_hour")));
        var blockRadio = new RadioButton { GroupName = "allowance", Content = Loc.T("block_completely"), IsChecked = rule.BlockCompletely, Margin = new Thickness(0, 6, 0, 0) };
        allowRadio.IsCheckedChanged += (_, _) =>
        {
            if (allowRadio.IsChecked == true)
            {
                rule.BlockCompletely = false;
                minutes.IsEnabled = true;
            }
        };
        blockRadio.IsCheckedChanged += (_, _) =>
        {
            if (blockRadio.IsChecked == true)
            {
                rule.BlockCompletely = true;
                minutes.IsEnabled = false;
            }
        };

        // Active window
        var fromBox = TimeBox(rule.From, t => rule.From = t);
        var toBox = TimeBox(rule.To, t => rule.To = t);
        var betweenRow = HStack(8, UiTheme.Body(Loc.T("only_between")), fromBox, UiTheme.Body(Loc.T("and")), toBox);
        void SetWindowEnabled(bool on) { fromBox.IsEnabled = on; toBox.IsEnabled = on; }
        SetWindowEnabled(!rule.AllDay);

        var allDayRadio = new RadioButton { GroupName = "active", Content = Loc.T("all_day"), IsChecked = rule.AllDay };
        var betweenRadio = new RadioButton { GroupName = "active", IsChecked = !rule.AllDay, Content = betweenRow, Margin = new Thickness(0, 6, 0, 0) };
        allDayRadio.IsCheckedChanged += (_, _) =>
        {
            if (allDayRadio.IsChecked == true) { rule.AllDay = true; SetWindowEnabled(false); }
        };
        betweenRadio.IsCheckedChanged += (_, _) =>
        {
            if (betweenRadio.IsChecked == true) { rule.AllDay = false; SetWindowEnabled(true); }
        };

        // Days
        var days = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        for (int i = 0; i < DayOrder.Length; i++)
        {
            DayOfWeek day = DayOrder[i];
            var toggle = new ToggleButton
            {
                Content = DayAbbrev(day),
                IsChecked = rule.Days.Contains(day),
                Width = 46,
                Padding = new Thickness(0, 6),
                FontSize = 11.5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            toggle.IsCheckedChanged += (_, _) =>
            {
                if (toggle.IsChecked == true) rule.Days.Add(day);
                else rule.Days.Remove(day);
            };
            days.Children.Add(toggle);
        }

        var card = new StackPanel { Spacing = 16 };
        card.Children.Add(new StackPanel { Spacing = 0, Children = { allowRadio, blockRadio } });
        card.Children.Add(new Border { Height = 1, [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Border_) });
        card.Children.Add(new StackPanel { Spacing = 0, Children = { allDayRadio, betweenRadio } });
        card.Children.Add(new Border { Height = 1, [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Border_) });
        card.Children.Add(new StackPanel { Spacing = 8, Children = { UiTheme.Muted(Loc.T("on_these_days")), days } });
        return UiTheme.Card(card);
    }

    private void RebuildEditSites(EditableConfiguration.EditableRule rule)
    {
        if (_editSitesPanel is null)
        {
            return;
        }

        _editSitesPanel.Children.Clear();
        if (rule.Sites.Count == 0)
        {
            _editSitesPanel.Children.Add(UiTheme.Muted("No sites yet — add the address of a page you want to limit."));
        }

        foreach (EditableConfiguration.EditableSite site in rule.Sites)
        {
            _editSitesPanel.Children.Add(BuildSiteRow(rule, site));
        }
    }

    // A target shown by label only. Custom ones can be edited; catalog ones can't.
    private Control BuildSiteRow(EditableConfiguration.EditableRule rule, EditableConfiguration.EditableSite site)
    {
        var label = new TextBlock
        {
            Text = site.DisplayLabel,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
            [!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.TextPrimary),
        };

        PillButton del = UiTheme.Icon("✕", Loc.T("remove"));
        del.Click += (_, _) => { rule.Sites.Remove(site); RebuildEditSites(rule); };

        var inner = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(del, Dock.Right);
        inner.Children.Add(del);

        if (!site.IsCatalog)
        {
            PillButton edit = UiTheme.Icon("✎", Loc.T("edit"));
            edit.Click += async (_, _) => await ShowCustomSite(rule, site);
            DockPanel.SetDock(edit, Dock.Right);
            inner.Children.Add(edit);
        }

        inner.Children.Add(label);

        return new Border
        {
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(12, 7, 6, 7),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.SurfaceAlt),
            Child = inner,
        };
    }

    // ---------- Add picker + custom site dialog ----------

    private async Task ShowAddPicker(EditableConfiguration.EditableRule rule)
    {
        var present = rule.Sites.Where(s => s.IsCatalog).Select(s => s.CatalogId!).ToHashSet(StringComparer.Ordinal);
        var checks = new List<(CatalogEntry Entry, CheckBox Box)>();
        var list = new StackPanel { Spacing = 2 };
        foreach (CatalogEntry entry in SiteCatalog.Entries)
        {
            bool already = present.Contains(entry.Id);
            var box = new CheckBox { Content = entry.Label, IsChecked = already, IsEnabled = !already };
            checks.Add((entry, box));
            list.Children.Add(box);
        }

        var custom = UiTheme.Ghost("+  " + Loc.T("custom_website_btn"));
        custom.HorizontalAlignment = HorizontalAlignment.Left;

        var add = UiTheme.Primary(Loc.T("add"));
        var cancel = UiTheme.Ghost(Loc.T("cancel"));

        var content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 14,
            Children =
            {
                UiTheme.H2(Loc.T("add_to_block")),
                new ScrollViewer { MaxHeight = 280, Content = list },
                custom,
                ActionsRight(cancel, add),
            },
        };
        Window dialog = Dialogs.Shell(this, Loc.T("add_to_block"), content, 380);

        custom.Click += async (_, _) => { dialog.Close(); await ShowCustomSite(rule, null); };
        cancel.Click += (_, _) => dialog.Close();
        add.Click += (_, _) =>
        {
            foreach ((CatalogEntry entry, CheckBox box) in checks)
            {
                if (box.IsEnabled && box.IsChecked == true)
                {
                    rule.Sites.Add(EditableConfiguration.EditableSite.FromCatalog(entry));
                }
            }

            RebuildEditSites(rule);
            dialog.Close();
        };

        await dialog.ShowDialog(this);
    }

    private async Task ShowCustomSite(EditableConfiguration.EditableRule rule, EditableConfiguration.EditableSite? existing)
    {
        var label = new TextBox { Text = existing?.Label ?? "", Watermark = Loc.T("label_hint") };
        var address = new TextBox { Text = existing?.Url ?? "", Watermark = Loc.T("address_hint") };
        var subpaths = new CheckBox
        {
            Content = Loc.T("include_subpaths"),
            IsChecked = existing?.IncludeSubpaths ?? true,
        };

        var save = UiTheme.Primary(Loc.T("save"));
        var cancel = UiTheme.Ghost(Loc.T("cancel"));
        var content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 14,
            Children =
            {
                UiTheme.H2(existing is null ? Loc.T("custom_website") : Loc.T("edit_website")),
                Field(Loc.T("label"), label),
                Field(Loc.T("address"), address),
                subpaths,
                ActionsRight(cancel, save),
            },
        };
        Window dialog = Dialogs.Shell(this, Loc.T("custom_website"), content, 440);

        cancel.Click += (_, _) => dialog.Close();
        save.Click += (_, _) =>
        {
            if (existing is null)
            {
                rule.Sites.Add(new EditableConfiguration.EditableSite
                {
                    Label = label.Text ?? "",
                    Url = address.Text ?? "",
                    IncludeSubpaths = subpaths.IsChecked == true,
                });
            }
            else
            {
                existing.Label = label.Text ?? "";
                existing.Url = address.Text ?? "";
                existing.IncludeSubpaths = subpaths.IsChecked == true;
            }

            RebuildEditSites(rule);
            dialog.Close();
        };

        await dialog.ShowDialog(this);
    }

    private static Control Field(string label, Control input)
    {
        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(UiTheme.SectionLabel(label));
        stack.Children.Add(input);
        return stack;
    }

    private static Control ActionsRight(params Control[] buttons)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (Control b in buttons)
        {
            row.Children.Add(b);
        }

        return row;
    }

    private async Task SaveEditingRule()
    {
        if (_editingRule is null)
        {
            return;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        if (_editingIndex >= 0 && _editingIndex < candidate.Rules.Count)
        {
            candidate.Rules[_editingIndex] = _editingRule;
        }
        else
        {
            candidate.Rules.Add(_editingRule);
        }

        if (_controller.TrySaveConfiguration(candidate, out string? error))
        {
            _editable = _controller.GetEditableConfiguration();
            NavigateHome();
            return;
        }

        await Dialogs.Message(this, Loc.T("couldnt_save"), error ?? Loc.T("rule_invalid"));
    }

    private async Task DeleteRule(int index)
    {
        if (index < 0 || index >= _editable.Rules.Count)
        {
            return;
        }

        if (!await Dialogs.Confirm(this, Loc.T("delete"), Loc.T("delete_rule_q", _editable.Rules[index].Name), Loc.T("delete")))
        {
            return;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        candidate.Rules.RemoveAt(index);
        if (_controller.TrySaveConfiguration(candidate, out string? error))
        {
            _editable = _controller.GetEditableConfiguration();
            NavigateHome();
            return;
        }

        await Dialogs.Message(this, Loc.T("couldnt_delete"), error ?? Loc.T("rule_invalid"));
    }

    private EditableConfiguration.EditableRule NewRule() => new()
    {
        Id = NextId(),
        Name = Loc.T("new_rule"),
        BlockCompletely = false,
        AllowanceMinutes = 5,
        AllDay = true,
    };

    // ---------- Settings ----------

    private Control BuildSettings()
    {
        var back = UiTheme.Ghost("←  " + Loc.T("back"));
        back.Click += (_, _) => NavigateHome();
        Control header = TopBar(HStack(12, back, UiTheme.H1(Loc.T("settings"))));

        ThemePreference current = _settings.LoadTheme();
        var group = new StackPanel { Spacing = 4 };
        foreach (ThemePreference pref in new[] { ThemePreference.System, ThemePreference.Light, ThemePreference.Dark })
        {
            ThemePreference captured = pref;
            var radio = new RadioButton
            {
                GroupName = "theme",
                Content = pref switch
                {
                    ThemePreference.Light => Loc.T("light"),
                    ThemePreference.Dark => Loc.T("dark"),
                    _ => Loc.T("follow_system"),
                },
                IsChecked = pref == current,
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    _settings.SaveTheme(captured);
                    _applyTheme(captured);
                }
            };
            group.Children.Add(radio);
        }

        // Language: Automatic + every supported language by its native name.
        string currentLang = _settings.LoadLanguage();
        var options = new List<string> { Loc.T("automatic") };
        options.AddRange(Loc.Languages.Select(l => l.Native));
        var langCombo = new ComboBox { Width = 240, ItemsSource = options };
        int selected = string.Equals(currentLang, Loc.Auto, StringComparison.OrdinalIgnoreCase)
            ? 0
            : Loc.Languages.ToList().FindIndex(l => l.Code == currentLang) is var idx && idx >= 0 ? idx + 1 : 0;
        langCombo.SelectedIndex = selected;
        langCombo.SelectionChanged += (_, _) =>
        {
            int i = langCombo.SelectedIndex;
            string pref = i <= 0 ? Loc.Auto : Loc.Languages[i - 1].Code;
            _settings.SaveLanguage(pref);
            Loc.SetPreference(pref); // raises Changed -> re-renders this screen
        };

        var body = new StackPanel { Spacing = 22, Margin = new Thickness(24, 22, 24, 24) };
        body.Children.Add(Section(Loc.T("appearance"), UiTheme.Card(group)));
        body.Children.Add(Section(Loc.T("language"), UiTheme.Card(langCombo)));

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }

    // ---------- Strict mode (minimal for now) ----------

    private Control BuildStrict()
    {
        var back = UiTheme.Ghost("←  " + Loc.T("back"));
        back.Click += (_, _) => NavigateHome();
        Control header = TopBar(HStack(12, back, UiTheme.H1(Loc.T("strict_mode"))));

        StrictModeSnapshot strict = _controller.Status.StrictMode;
        var body = new StackPanel { Spacing = 16, Margin = new Thickness(24, 22, 24, 24), MaxWidth = 560, HorizontalAlignment = HorizontalAlignment.Left };

        if (strict.IsActive)
        {
            string until = strict.ActiveUntilLocal?.ToString("dddd HH:mm", Loc.Culture) ?? "";
            body.Children.Add(UiTheme.H2(Loc.T("strict_on")));
            body.Children.Add(UiTheme.Body(Loc.T("strict_until", until, FormatSpan(strict.Remaining))));
            body.Children.Add(UiTheme.Muted(Loc.T("strict_note")));
        }
        else
        {
            body.Children.Add(UiTheme.Body(Loc.T("strict_explain")));

            var amount = NumberBox(2, 1, 999);
            amount.Width = 90;
            var unit = new ComboBox { Width = 140, ItemsSource = new[] { Loc.T("minutes"), Loc.T("hours"), Loc.T("days") }, SelectedIndex = 1 };
            var lockBtn = UiTheme.Primary(Loc.T("lock_in"));
            lockBtn.Click += async (_, _) =>
            {
                int n = (int)(amount.Value ?? 1);
                TimeSpan d = unit.SelectedIndex switch
                {
                    0 => TimeSpan.FromMinutes(n),
                    2 => TimeSpan.FromDays(n),
                    _ => TimeSpan.FromHours(n),
                };
                if (await Dialogs.StrictConfirm(this, n, (string)unit.SelectedItem!))
                {
                    _controller.ActivateStrictMode(d);
                    NavigateStrict();
                }
            };

            body.Children.Add(UiTheme.Card(new StackPanel
            {
                Spacing = 14,
                Children = { HStack(10, UiTheme.Body(Loc.T("for_w")), amount, unit), lockBtn },
            }));
        }

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }

    // ---------- Status ----------

    private void OnStatusChanged(object? sender, AppStatus status) => Dispatcher.UIThread.Post(() => ApplyStatus(status));

    private void ApplyStatus(AppStatus status)
    {
        bool wasStrict = _strictActive;
        _strictActive = status.StrictMode.IsActive;

        // Strict mode is the landing screen while active.
        if (_strictActive && !wasStrict && _page != Page.Strict)
        {
            NavigateStrict();
            return;
        }

        if (_page == Page.Home)
        {
            if (_statusDot is not null && _statusText is not null)
            {
                bool healthy = status.LastError is null;
                _statusDot[!Border.BackgroundProperty] = UiTheme.Dyn(healthy ? UiTheme.Success : UiTheme.Warn);
                _statusText.Text = healthy ? Loc.T("protected") : Loc.T("attention");
            }

            if (_strictButton is not null)
            {
                _strictButton.Text = _strictActive
                    ? Loc.T("strict_left", FormatSpan(status.StrictMode.Remaining))
                    : Loc.T("strict_mode");
            }

            var byId = status.Rules.ToDictionary(r => r.RuleId, r => r, StringComparer.Ordinal);
            foreach ((string id, Action<RuleSnapshot?> update) in _liveUpdaters)
            {
                update(byId.TryGetValue(id, out RuleSnapshot? s) ? s : null);
            }
        }
    }

    // ---------- Inputs & dialogs ----------

    private static NumericUpDown NumberBox(int value, int min, int max) => new()
    {
        Value = value,
        Minimum = min,
        Maximum = max,
        Increment = 1,
        FormatString = "0",
        ShowButtonSpinner = false,
        Width = 70,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static TextBox TimeBox(TimeOnly value, Action<TimeOnly> onChanged)
    {
        var box = new TextBox { Text = value.ToString("HH:mm"), Width = 70, Watermark = "23:00", VerticalAlignment = VerticalAlignment.Center };
        box.LostFocus += (_, _) =>
        {
            if (TimeOnly.TryParse(box.Text, out TimeOnly t))
            {
                onChanged(t);
                box.Text = t.ToString("HH:mm");
            }
            else
            {
                box.Text = value.ToString("HH:mm");
            }
        };
        return box;
    }


    // ---------- Helpers ----------

    private string ConditionSummary(EditableConfiguration.EditableRule rule)
    {
        string when = rule.BlockCompletely ? Loc.T("blocked") : Loc.T("min_per_hour", rule.AllowanceMinutes);
        string active = rule.AllDay ? Loc.T("all_day_low") : $"{rule.From:HH\\:mm}–{rule.To:HH\\:mm}";
        return $"{when} · {active} · {DaysSummary(rule.Days)}";
    }

    private static string SitesSummary(EditableConfiguration.EditableRule rule)
    {
        if (rule.Sites.Count == 0)
        {
            return Loc.T("no_sites");
        }

        var labels = rule.Sites
            .Select(s => s.DisplayLabel)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        string head = string.Join(", ", labels.Take(2));
        return labels.Count > 2 ? $"{head} +{labels.Count - 2}" : head;
    }

    private static string DaysSummary(ICollection<DayOfWeek> days)
    {
        if (days.Count == 7)
        {
            return Loc.T("every_day");
        }

        bool weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }.All(days.Contains) && days.Count == 5;
        if (weekdays)
        {
            return Loc.T("weekdays");
        }

        if (days.Count == 2 && days.Contains(DayOfWeek.Saturday) && days.Contains(DayOfWeek.Sunday))
        {
            return Loc.T("weekends");
        }

        return string.Join(", ", DayOrder.Where(days.Contains).Select(DayAbbrev));
    }

    private static string FormatSpan(TimeSpan span)
    {
        (string h, string m, string s) = Loc.DurationUnits();
        if (span.TotalHours >= 1)
        {
            return $"{(int)span.TotalHours} {h} {span.Minutes} {m}";
        }

        if (span.TotalMinutes >= 1)
        {
            return $"{span.Minutes} {m} {span.Seconds} {s}";
        }

        return $"{Math.Max(0, span.Seconds)} {s}";
    }

    private string NextId()
    {
        var existing = _editable.Rules.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        for (int i = 1; ; i++)
        {
            string candidate = $"rule-{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
