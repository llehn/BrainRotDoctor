using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Settings ----------

    private Control BuildSettings()
    {
        // Appearance
        ThemePreference current = _settings.LoadTheme();
        ThemePreference[] themes = { ThemePreference.System, ThemePreference.Light, ThemePreference.Dark };
        var theme = new Segmented(
            new Segment[] { new(Loc.T("follow_system")), new(Loc.T("light")), new(Loc.T("dark")) },
            Array.IndexOf(themes, current),
            stretch: false);
        theme.Changed += i =>
        {
            _settings.SaveTheme(themes[i]);
            _applyTheme(themes[i]);
        };

        // Language: Automatic + every supported language by its native name.
        string currentLang = _settings.LoadLanguage();
        var options = new List<string> { Loc.T("automatic") };
        options.AddRange(Loc.Languages.Select(l => l.Native));
        int index = Loc.Languages.ToList().FindIndex(l => l.Code == currentLang);
        var language = new Dropdown(options, string.Equals(currentLang, Loc.Auto, StringComparison.OrdinalIgnoreCase) || index < 0 ? 0 : index + 1)
        {
            MinWidth = 220,
        };
        language.Changed += i =>
        {
            string pref = i <= 0 ? Loc.Auto : Loc.Languages[i - 1].Code;
            _settings.SaveLanguage(pref);
            Loc.SetPreference(pref); // raises Changed -> re-renders this screen
        };

        Version v = AppVersion.Current;
        StackPanel about = Layout.Row(10,
            new Dot(Tone.Success),
            Layout.Column(2, new TextLine(Loc.T("auto_updates"), TextStyle.Strong), new TextLine(Loc.T("version_n", $"{v.Major}.{v.Minor}.{Math.Max(0, v.Build)}"), TextStyle.Caption)));

        var page = new StackPanel { Spacing = Space.S6, MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(new PageHeader(Loc.T("settings")));
        page.Children.Add(new Group(Loc.T("look_and_language"),
            new FormRow(Loc.T("appearance"), Loc.T("appearance_help"), theme),
            new FormRow(Loc.T("language"), Loc.T("language_help"), language)));
        if (UntrackedGroup() is { } untracked)
        {
            page.Children.Add(untracked);
        }

        page.Children.Add(new Group(Loc.T("about"), Group.Row(about)));
        return page;
    }

    private bool _showUntracked;

    /// <summary>The sites and apps you said are okay: behind a button, since nobody needs to see that list at a glance.</summary>
    private Control? UntrackedGroup()
    {
        HashSet<string> untracked = Untracked();
        Dictionary<string, string> apps = UntrackedApps();
        if (untracked.Count == 0 && apps.Count == 0)
        {
            return null;
        }

        var toggle = new PillButton(Loc.T(_showUntracked ? "hide" : "show"));
        toggle.Click += (_, _) =>
        {
            _showUntracked = !_showUntracked;
            Rerender();
        };

        var head = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(toggle, Dock.Right);
        toggle.Margin = new Thickness(Space.S4, 0, 0, 0);
        head.Children.Add(toggle);
        head.Children.Add(new TextLine(Loc.T("okay_help"), TextStyle.Small));
        if (!_showUntracked)
        {
            return new Group(Loc.T("okay_sites"), Group.Row(head));
        }

        var sites = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = Space.S3, LineSpacing = Space.S2 };
        foreach (string host in untracked.OrderBy(h => h, StringComparer.Ordinal))
        {
            var again = new PillButton(Loc.T("track_again"), ButtonKind.Accent, size: ButtonSize.Small);
            again.Click += (_, _) => SetTracked(host, true);
            sites.Children.Add(new Card(Layout.Row(Space.S2, new TextLine(host, TextStyle.Strong), again), CardStyle.Inset, CardPadding.Compact));
        }

        foreach ((string key, string name) in apps.OrderBy(a => a.Value, StringComparer.CurrentCultureIgnoreCase))
        {
            var again = new PillButton(Loc.T("track_again"), ButtonKind.Accent, size: ButtonSize.Small);
            again.Click += (_, _) => SetAppTracked(key, name, true);
            sites.Children.Add(new Card(Layout.Row(Space.S2, new TextLine(name, TextStyle.Strong), again), CardStyle.Inset, CardPadding.Compact));
        }

        return new Group(Loc.T("okay_sites"), Group.Row(head), Group.Row(sites));
    }
}
