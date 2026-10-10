using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.App.Ui.Scene;

namespace BrainRotDoctor.App.Ui;

/// <summary>The Avalonia application: theme, tray icon, and the main window.</summary>
internal sealed class App : Application
{
    private readonly EnforcementController _controller;
    private readonly UiSettingsStore _settings;
    private readonly WormScene _scene = new(new ExtractionScript());
    private MainWindow? _window;

    public App(EnforcementController controller, UiSettingsStore settings)
    {
        _controller = controller;
        _settings = settings;
    }

    public override void Initialize()
    {
        Loc.Initialize(_settings.LoadLanguage());
        Styles.Add(UiTheme.BuildFluentTheme());
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

            // Build the scene's drawings and its sound while idle, so the first block plays without a hitch.
            Dispatcher.UIThread.Post(
                () =>
                {
                    _scene.Prepare();
                    PlopSound.Prepare();
                },
                DispatcherPriority.Background);
            _controller.BlockSceneStarting += (_, scene) => Dispatcher.UIThread.Post(() => Play(scene));
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Plays the worm scene for tabs due to close; they close on the pop.</summary>
    private void Play(BlockScene due)
    {
        try
        {
            var overlay = new SceneOverlay(_scene, ExtractionScript.PopAt, ScreenPointOf(due));
            overlay.Popped += (_, _) =>
            {
                PlopSound.Play();
                _controller.ClosePendingTabs(due.Id);
            };
            overlay.Finished += (_, _) => _controller.EndScene(due.Id);
            overlay.Start();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Without the scene the tabs still close.
            _controller.ClosePendingTabs(due.Id);
            _controller.EndScene(due.Id);
        }
    }

    /// <summary>The middle of the first blocked browser window, so the scene plays on that screen.</summary>
    private static PixelPoint? ScreenPointOf(BlockScene due)
    {
        foreach (ObservedBrowserWindow window in due.Windows)
        {
            if (NativeMethods.GetWindowRect(window.WindowHandle, out NativeMethods.RECT r))
            {
                return new PixelPoint((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
            }
        }

        return null;
    }
}
