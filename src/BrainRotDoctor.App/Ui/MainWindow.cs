using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// The main window. Status (the first screen), Rules, Strict mode and Settings live next to a sidebar;
/// editing a rule and adding websites take over the whole window. The UI is
/// built in code from the design system's controls and rebuilt per screen;
/// live numbers (time left, countdowns) update in place from the enforcement status.
/// </summary>
internal sealed partial class MainWindow : AppWindow
{
    private enum Page { Status, Home, Editor, AddSites, Strict, Settings }

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
    private Page _page = Page.Status;
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
        // Wide enough for every two-pane screen (strict mode setup is the widest)
        // to stay side by side; there is no stacked layout.
        MinWidth = 1000;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = _host;

        Navigate(Page.Status);

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
            Page.Status => WithSidebar(BuildStatusPage()),
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
        var nav = new StackPanel { Spacing = Space.S1 };
        nav.Children.Add(Brand());
        nav.Children.Add(NavTo(Glyph.Pulse, Loc.T("status"), Page.Status, badge: null));
        nav.Children.Add(NavTo(Glyph.List, Loc.T("rules"), Page.Home, badge: null));
        nav.Children.Add(NavTo(Glyph.Lock, Loc.T("strict_mode"), Page.Strict, badge: _status.StrictMode.IsActive ? Loc.T("on_badge") : null));
        nav.Children.Add(NavTo(Glyph.Gear, Loc.T("settings"), Page.Settings, badge: null));
        return new SidebarLayout(nav, StatusCard(), content);
    }

    private static Control Brand()
    {
        var logo = new Image { Source = ProductIcon.At(64), Width = 32, Height = 32 };
        StackPanel row = Layout.Row(10, logo, new TextLine("BrainRotDoctor", TextStyle.Label));
        row.Margin = new Thickness(Space.S2, 2, Space.S2, Space.S5);
        return row;
    }

    private Control NavTo(string glyph, string text, Page target, string? badge)
    {
        bool active = _page == target || (target == Page.Home && _page is Page.Editor or Page.AddSites);
        var item = new NavItem(glyph, text, active, badge);
        item.Click += (_, _) => Navigate(target);
        return item;
    }

    private Control StatusCard()
    {
        var panel = new StatusPanel();
        _live.Add(status =>
        {
            if (status.StrictMode.IsActive)
            {
                panel.SetState(Tone.Ink, Loc.T("strict_mode"), Loc.T("time_left", FormatSpan(status.StrictMode.Remaining)), inverted: true);
            }
            else if (status.Pause is { } pause)
            {
                panel.SetState(Tone.Warn, Loc.T("paused"), PauseUntilText(pause), inverted: false);
            }
            else if (status.LastError is not null)
            {
                panel.SetState(Tone.Warn, Loc.T("attention"), status.LastError, inverted: false);
            }
            else
            {
                int active = status.Rules.Count(r => r.IsActive);
                panel.SetState(Tone.Success, Loc.T("protected"), Loc.T("rules_active_now", active, status.Rules.Count), inverted: false);
            }
        });

        return panel;
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
