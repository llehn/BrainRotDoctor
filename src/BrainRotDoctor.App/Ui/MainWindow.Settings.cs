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
        page.Children.Add(new Group(Loc.T("about"), Group.Row(about)));
        return page;
    }
}
