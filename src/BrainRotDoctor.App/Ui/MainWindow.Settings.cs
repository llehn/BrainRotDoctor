using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;

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
            new (string, string?)[] { (Loc.T("follow_system"), null), (Loc.T("light"), null), (Loc.T("dark"), null) },
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
        var language = new ComboBox { MinWidth = 220, ItemsSource = options };
        int index = Loc.Languages.ToList().FindIndex(l => l.Code == currentLang);
        language.SelectedIndex = string.Equals(currentLang, Loc.Auto, StringComparison.OrdinalIgnoreCase) || index < 0 ? 0 : index + 1;
        language.SelectionChanged += (_, _) =>
        {
            int i = language.SelectedIndex;
            string pref = i <= 0 ? Loc.Auto : Loc.Languages[i - 1].Code;
            _settings.SaveLanguage(pref);
            Loc.SetPreference(pref); // raises Changed -> re-renders this screen
        };

        Version v = AppVersion.Current;
        var about = UiTheme.HStack(10,
            UiTheme.Dot(UiTheme.Success),
            UiTheme.VStack(2, UiTheme.Strong(Loc.T("auto_updates")), UiTheme.Caption(Loc.T("version_n", $"{v.Major}.{v.Minor}.{Math.Max(0, v.Build)}"))));

        var page = new StackPanel { Spacing = 24, MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(PageHeader(Loc.T("settings"), null, null));
        page.Children.Add(SettingsGroup(Loc.T("look_and_language"),
            SettingRow(Loc.T("appearance"), Loc.T("appearance_help"), theme),
            SettingRow(Loc.T("language"), Loc.T("language_help"), language)));
        page.Children.Add(SettingsGroup(Loc.T("about"), new Border { Padding = new Thickness(22, 14), Child = about }));
        return page;
    }

    private static Control SettingsGroup(string title, params Control[] rows)
    {
        var stack = new StackPanel();
        TextBlock label = UiTheme.SectionLabel(title);
        label.Margin = new Thickness(22, 18, 22, 6);
        stack.Children.Add(label);
        for (int i = 0; i < rows.Length; i++)
        {
            if (i > 0)
            {
                stack.Children.Add(UiTheme.Divider());
            }

            stack.Children.Add(rows[i]);
        }

        Border card = UiTheme.Card(stack, 0);
        card.ClipToBounds = true;
        return card;
    }

    private static Control SettingRow(string title, string help, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(22, 14) };
        grid.Children.Add(UiTheme.VStack(2, UiTheme.Strong(title), UiTheme.Caption(help)));
        control.VerticalAlignment = VerticalAlignment.Center;
        control.Margin = new Thickness(16, 0, 0, 0);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }
}
