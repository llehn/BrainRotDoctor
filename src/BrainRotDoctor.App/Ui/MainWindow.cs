using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// The main window. Rules, Strict mode and Settings live next to a sidebar;
/// editing a rule and adding websites take over the whole window. The UI is
/// built in code and rebuilt per screen; live numbers (time left, countdowns)
/// update in place from the enforcement status.
/// </summary>
internal sealed partial class MainWindow : Window
{
    private enum Page { Home, Editor, AddSites, Strict, Settings }

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
    private AppStatus _status;

    // Things whose change requires rebuilding the current screen.
    private bool _shownPaused;
    private bool _shownStrict;

    // Live elements of the current screen, refreshed on every status tick.
    private readonly List<Action<AppStatus>> _live = new();

    public MainWindow(EnforcementController controller, UiSettingsStore settings, Action<ThemePreference> applyTheme)
    {
        _controller = controller;
        _settings = settings;
        _applyTheme = applyTheme;
        _editable = controller.GetEditableConfiguration();
        _status = controller.Status;

        Title = "BrainRotDoctor";
        Width = 1120;
        Height = 760;
        MinWidth = 820;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Inter, $Default");
        this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg);
        Content = _host;

        Navigate(_status.StrictMode.IsActive ? Page.Strict : Page.Home);

        _controller.StatusChanged += OnStatusChanged;
        Loc.Changed += Rerender;
    }

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

    // ---------- Navigation ----------

    private void Navigate(Page page)
    {
        _page = page;
        Rerender();
    }

    private void Rerender()
    {
        _live.Clear();
        _shownPaused = _status.IsPaused;
        _shownStrict = _status.StrictMode.IsActive;

        _host.Content = _page switch
        {
            Page.Editor when _editingRule is not null => BuildEditor(),
            Page.AddSites when _editingRule is not null => BuildAddSites(),
            Page.Strict => WithSidebar(_status.StrictMode.IsActive ? BuildStrictRunning() : BuildStrictSetup()),
            Page.Settings => WithSidebar(BuildSettings()),
            _ => WithSidebar(BuildHome()),
        };

        ApplyLive(_status);
    }

    // ---------- Status ----------

    private void OnStatusChanged(object? sender, AppStatus status) =>
        Dispatcher.UIThread.Post(() => ApplyStatus(status));

    private void ApplyStatus(AppStatus status)
    {
        _status = status;
        bool strictChanged = status.StrictMode.IsActive != _shownStrict;
        bool pauseChanged = status.IsPaused != _shownPaused;

        if (strictChanged && status.StrictMode.IsActive && _page is Page.Home)
        {
            Navigate(Page.Strict);
            return;
        }

        // Editing screens hold unsaved input; never rebuild them under the user.
        if ((strictChanged || pauseChanged) && _page is not (Page.Editor or Page.AddSites))
        {
            Rerender();
            return;
        }

        ApplyLive(status);
    }

    private void ApplyLive(AppStatus status)
    {
        foreach (Action<AppStatus> update in _live)
        {
            update(status);
        }
    }

    // ---------- Sidebar ----------

    private Control WithSidebar(Control content)
    {
        var nav = new StackPanel { Spacing = 4 };
        nav.Children.Add(Brand());
        nav.Children.Add(NavItem(Icons.Rules, Loc.T("rules"), Page.Home, badge: null));
        nav.Children.Add(NavItem(Icons.Lock, Loc.T("strict_mode"), Page.Strict, badge: _status.StrictMode.IsActive ? Loc.T("on_badge") : null));
        nav.Children.Add(NavItem(Icons.Settings, Loc.T("settings"), Page.Settings, badge: null));

        Control status = StatusCard();
        var side = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(nav, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        side.Children.Add(nav);
        side.Children.Add(status);

        var sidebar = new Border
        {
            Width = 236,
            Padding = new Thickness(14, 20, 14, 16),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = side,
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Sidebar),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
        };

        var scroll = new ScrollViewer
        {
            Content = new Border { Padding = new Thickness(40, 32, 40, 40), Child = content },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(sidebar, Dock.Left);
        root.Children.Add(sidebar);
        root.Children.Add(scroll);
        return root;
    }

    private static Control Brand()
    {
        var logo = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(9),
            Child = new Border { HorizontalAlignment = HorizontalAlignment.Center, Child = Icons.Make(Icons.Shield, 18, UiTheme.InkText) },
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Ink),
        };
        StackPanel row = UiTheme.HStack(10, logo, UiTheme.Strong("BrainRotDoctor", 15));
        row.Margin = new Thickness(8, 2, 8, 20);
        return row;
    }

    private Control NavItem(string icon, string text, Page target, string? badge)
    {
        bool active = _page == target || (target == Page.Home && _page is Page.Editor or Page.AddSites);
        var label = UiTheme.Text(text, 14, active ? FontWeight.SemiBold : FontWeight.Medium, active ? UiTheme.TextPrimary : UiTheme.TextSecondary);
        var row = new DockPanel { LastChildFill = true };
        Control glyph = Icons.Make(icon, 18, active ? UiTheme.TextPrimary : UiTheme.TextSecondary);
        glyph.Margin = new Thickness(0, 0, 10, 0);
        DockPanel.SetDock(glyph, Dock.Left);
        row.Children.Add(glyph);
        if (badge is not null)
        {
            Border chip = UiTheme.Chip(badge, UiTheme.Ink, UiTheme.InkText);
            DockPanel.SetDock(chip, Dock.Right);
            row.Children.Add(chip);
        }

        row.Children.Add(label);

        var item = new Border
        {
            Height = 40,
            Padding = new Thickness(12, 0),
            CornerRadius = new CornerRadius(9),
            Cursor = new Cursor(StandardCursorType.Hand),
            Background = Brushes.Transparent,
            Child = row,
        };
        if (active)
        {
            item[!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface);
        }

        item.AddHandler(Gestures.TappedEvent, (_, _) => Navigate(target));
        return item;
    }

    private Control StatusCard()
    {
        var dot = UiTheme.Dot(UiTheme.Success);
        var title = UiTheme.Strong("", 14);
        var sub = UiTheme.Muted("");
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14),
            Child = UiTheme.VStack(4, UiTheme.HStack(8, dot, title), sub),
        };

        _live.Add(status =>
        {
            string dotKey;
            string bg = UiTheme.Surface;
            string fg = UiTheme.TextPrimary;
            string subFg = UiTheme.TextSecondary;
            if (status.StrictMode.IsActive)
            {
                (dotKey, bg, fg, subFg) = (UiTheme.InkText, UiTheme.Ink, UiTheme.InkText, UiTheme.InkText);
                title.Text = Loc.T("strict_mode");
                sub.Text = Loc.T("time_left", FormatSpan(status.StrictMode.Remaining));
            }
            else if (status.Pause is { } pause)
            {
                dotKey = UiTheme.Warn;
                title.Text = Loc.T("paused");
                sub.Text = PauseUntilText(pause);
            }
            else if (status.LastError is not null)
            {
                dotKey = UiTheme.Warn;
                title.Text = Loc.T("attention");
                sub.Text = status.LastError;
            }
            else
            {
                dotKey = UiTheme.Success;
                title.Text = Loc.T("protected");
                int active = status.Rules.Count(r => r.IsActive);
                sub.Text = Loc.T("rules_active_now", active, status.Rules.Count);
            }

            dot[!Border.BackgroundProperty] = UiTheme.Dyn(dotKey);
            card[!Border.BackgroundProperty] = UiTheme.Dyn(bg);
            card[!Border.BorderBrushProperty] = UiTheme.Dyn(status.StrictMode.IsActive ? UiTheme.Ink : UiTheme.Border_);
            title[!TextBlock.ForegroundProperty] = UiTheme.Dyn(fg);
            sub[!TextBlock.ForegroundProperty] = UiTheme.Dyn(subFg);
            sub.Opacity = status.StrictMode.IsActive ? 0.75 : 1;
        });

        return card;
    }

    // ---------- Shared page chrome ----------

    /// <summary>A page title with an optional subtitle and right-aligned actions that wrap below when narrow.</summary>
    private static Control PageHeader(string title, string? subtitle, Control? actions)
    {
        var text = UiTheme.VStack(6, UiTheme.H1(title));
        if (subtitle is not null)
        {
            TextBlock sub = UiTheme.Muted(subtitle);
            sub.FontSize = 14;
            sub.MaxWidth = 640;
            sub.HorizontalAlignment = HorizontalAlignment.Left;
            text.Children.Add(sub);
        }

        if (actions is null)
        {
            return text;
        }

        actions.VerticalAlignment = VerticalAlignment.Bottom;
        actions.Margin = new Thickness(16, 0, 0, 0);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(text);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(actions);
        return grid;
    }

    /// <summary>The top bar of a full-window screen: back link, title area, actions.</summary>
    private static Control FullScreenBar(Control back, Control middle, Control actions)
    {
        var divider = new Border { Width = 1, Height = 24, Margin = new Thickness(4, 0, 12, 0), [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Border_) };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto") };
        grid.Children.Add(back);
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);
        middle.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(middle, 2);
        grid.Children.Add(middle);
        actions.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(actions, 3);
        grid.Children.Add(actions);

        return new Border
        {
            Padding = new Thickness(24, 12),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid,
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
        };
    }

    private static Control FullScreen(Control bar, Control body)
    {
        var scroll = new ScrollViewer
        {
            Content = new Border
            {
                Padding = new Thickness(32, 28, 32, 48),
                MaxWidth = 1280,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = body,
            },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);
        root.Children.Add(scroll);
        return root;
    }

    /// <summary>
    /// Two columns side by side when the window is wide enough, stacked otherwise.
    /// <paramref name="fixedWidth"/> sizes the left column, or the right one when
    /// <paramref name="fixedRight"/> is set; the other takes the rest.
    /// </summary>
    private static Control Columns(Control left, Control right, double fixedWidth, double breakpoint, double gap = 24, bool fixedRight = false)
    {
        var grid = new Grid();
        left.VerticalAlignment = VerticalAlignment.Top;
        right.VerticalAlignment = VerticalAlignment.Top;
        grid.Children.Add(left);
        grid.Children.Add(right);

        bool? wide = null;
        void Layout(double width)
        {
            bool isWide = width >= breakpoint;
            if (wide == isWide)
            {
                return;
            }

            wide = isWide;
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();
            if (isWide)
            {
                grid.ColumnDefinitions = new ColumnDefinitions(fixedRight ? $"*,{gap},{fixedWidth}" : $"{fixedWidth},{gap},*");
                Grid.SetColumn(left, 0);
                Grid.SetRow(left, 0);
                Grid.SetColumn(right, 2);
                Grid.SetRow(right, 0);
            }
            else
            {
                grid.RowDefinitions = new RowDefinitions($"Auto,{gap},Auto");
                Grid.SetColumn(left, 0);
                Grid.SetRow(left, 0);
                Grid.SetColumn(right, 0);
                Grid.SetRow(right, 2);
            }
        }

        Layout(breakpoint);
        grid.SizeChanged += (_, e) => Layout(e.NewSize.Width);
        return grid;
    }

    /// <summary>A wrap of equal-width tiles whose count per row follows the available width.</summary>
    private static Control TileGrid(IReadOnlyList<Control> tiles, double minTileWidth, double gap)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = gap, LineSpacing = gap };
        foreach (Control tile in tiles)
        {
            wrap.Children.Add(tile);
        }

        wrap.SizeChanged += (_, e) =>
        {
            double width = e.NewSize.Width;
            int columns = Math.Max(1, (int)((width + gap) / (minTileWidth + gap)));
            double tileWidth = Math.Floor((width - gap * (columns - 1)) / columns) - 0.5;
            foreach (Control tile in wrap.Children)
            {
                tile.Width = Math.Max(minTileWidth * 0.6, tileWidth);
            }
        };
        return wrap;
    }

    // ---------- Formatting ----------

    private static string DayAbbrev(DayOfWeek day) =>
        Loc.Culture.DateTimeFormat.AbbreviatedDayNames[(int)day];

    private static string Clock(DateTimeOffset time) => time.ToLocalTime().ToString("HH:mm", Loc.Culture);

    private string PauseUntilText(PauseState pause) =>
        pause.UntilUtc is { } until ? Loc.T("resumes_at", Clock(until)) : Loc.T("until_you_resume");

    private static string FormatSpan(TimeSpan span)
    {
        (string h, string m, string s) = Loc.DurationUnits();
        if (span.TotalDays >= 1)
        {
            return $"{(int)span.TotalHours} {h}";
        }

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

    private static string DaysSummary(ICollection<DayOfWeek> days)
    {
        if (days.Count == 7)
        {
            return Loc.T("every_day");
        }

        bool weekdays = days.Count == 5 && DayOrder.Take(5).All(days.Contains);
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

    private static string ConditionSummary(EditableConfiguration.EditableRule rule)
    {
        string limit = rule.BlockCompletely ? Loc.T("blocked_completely") : Loc.T("min_per_hour", rule.AllowanceMinutes);
        string active = rule.AllDay ? Loc.T("all_day") : $"{rule.From:HH\\:mm}–{rule.To:HH\\:mm}";
        return $"{limit} · {active} · {DaysSummary(rule.Days)}";
    }

    /// <summary>What a site of a rule blocks, in a few words: "Home feed, Explore" or "Whole site".</summary>
    private static string BlockedSummary(EditableConfiguration.EditableSite site)
    {
        if (site.BlockEverythingElse)
        {
            int open = site.Pages.Count(p => !p.Block);
            return open == 0 ? Loc.T("whole_site") : Loc.T("whole_site_except", open);
        }

        List<string> names = site.Pages.Where(p => p.Block).Select(p => p.Name).ToList();
        if (names.Count == 0)
        {
            return Loc.T("nothing_blocked");
        }

        string head = string.Join(", ", names.Take(2));
        return names.Count > 2 ? $"{head} +{names.Count - 2}" : head;
    }
}
