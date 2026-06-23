using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using BrainRotDoctor.App.Runtime;

namespace BrainRotDoctor.App.Ui;

/// <summary>The Avalonia application: theme, tray icon, and the main window.</summary>
internal sealed class App : Application
{
    private readonly EnforcementController _controller;
    private readonly UiSettingsStore _settings;
    private readonly ToastNotifier _toasts = new();
    private MainWindow? _window;

    public App(EnforcementController controller, UiSettingsStore settings)
    {
        _controller = controller;
        _settings = settings;
    }

    public override void Initialize()
    {
        Loc.Initialize(_settings.LoadLanguage());
        Styles.Add(new FluentTheme());
        Styles.Add(UiTheme.BuildStyles());
        Resources.MergedDictionaries.Add(UiTheme.BuildPalette());
        ApplyTheme(_settings.LoadTheme());
    }

    public void ApplyTheme(ThemePreference theme) => RequestedThemeVariant = theme switch
    {
        ThemePreference.Light => ThemeVariant.Light,
        ThemePreference.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            WindowIcon icon = ProductIcon.Create();
            _window = new MainWindow(_controller, _settings, ApplyTheme) { Icon = icon };

            var tray = new TrayIcon { Icon = icon, ToolTipText = "BrainRotDoctor", IsVisible = true };
            var menu = new NativeMenu();
            var open = new NativeMenuItem("Open BrainRotDoctor");
            open.Click += (_, _) => _window.ShowFromTray();
            menu.Items.Add(open);
            tray.Menu = menu;
            tray.Clicked += (_, _) => _window.ShowFromTray();

            desktop.MainWindow = _window;

            _controller.TabClosed += OnTabClosed;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTabClosed(object? sender, CloseEvent e)
    {
        string host = e.Url.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }

        string message = string.IsNullOrWhiteSpace(host)
            ? "Closed a brain-rot tab — back to it."
            : $"Closed a brain-rot tab on {host}.";

        // ToastNotifier marshals onto the UI thread; this fires on the enforcement thread.
        _toasts.Show("Worm extracted \U0001FAB1", message);
    }
}
