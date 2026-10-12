#if DEBUG
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;
using System.IO;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Dev-only (DEBUG builds): renders every screen of the main window to PNG files
/// for visual review, using sample rules and throwaway state in the output
/// folder. It never touches the user's real config, strict mode or browser tabs.
/// Run with <c>--ui-snapshots &lt;folder&gt;</c>.
/// </summary>
internal static class UiSnapshots
{
    public static void Run(string outDir, string[] args)
    {
        string data = Path.Combine(outDir, "data");
        if (Directory.Exists(data))
        {
            Directory.Delete(data, recursive: true);
        }

        Directory.CreateDirectory(data);

        ConfigurationDocument doc = DefaultConfiguration.CreateDocument("Short video", "Feeds");
        doc.Rules!.Add(new RuleDocument
        {
            Id = "night",
            Name = "Night shutdown",
            Enabled = false,
            AllDay = false,
            From = "23:00",
            To = "07:00",
            Days = new() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            Sites = new()
            {
                SiteCatalog.Get("reddit").ToDocumentBlockingEverything(),
                SiteCatalog.Get("x").ToDocumentBlockingEverything(),
            },
        });
        string json = doc.ToJson();
        string configPath = Path.Combine(data, "config.json");
        File.WriteAllText(configPath, json);

        DateTimeOffset now = DateTimeOffset.Now;
        var hour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        var usage = new UsageStore(Path.Combine(data, "usage.json"));
        usage.Save(new[]
        {
            new RuleUsage(DefaultConfiguration.ShortVideoRuleId, hour, TimeSpan.FromMinutes(3)),
            new RuleUsage(DefaultConfiguration.FeedsRuleId, hour, TimeSpan.FromMinutes(5)),
        });

        var controller = new EnforcementController(
            ConfigurationLoader.Load(json),
            new NoBrowsers(),
            new NoBrowsers(),
            configPath,
            json,
            configPath,
            new StrictModeStore(Path.Combine(data, "strict-mode.json")),
            logPath: null,
            usageStore: usage,
            pauseStore: new PauseStore(Path.Combine(data, "pause.json")));
        controller.Start();

        var settings = new UiSettingsStore(Path.Combine(data, "ui-settings.json"));
        AppBuilder.Configure(() => new SnapshotApp(controller, settings, outDir))
            .UsePlatformDetect()
            .UseDesignSystem()
            .StartWithClassicDesktopLifetime(args);
    }

    private sealed class NoBrowsers : IBrowserObserver, IBrowserTabCloser
    {
        public IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs() => Array.Empty<ObservedBrowserWindow>();

        public Uri? ReadSelectedUrl(IntPtr windowHandle) => null;

        public bool CloseSelectedTab(IntPtr windowHandle) => false;
    }

    private sealed class SnapshotApp : Application
    {
        private readonly EnforcementController _controller;
        private readonly UiSettingsStore _settings;
        private readonly string _outDir;

        public SnapshotApp(EnforcementController controller, UiSettingsStore settings, string outDir)
        {
            _controller = controller;
            _settings = settings;
            _outDir = outDir;
        }

        public override void Initialize()
        {
            Loc.Initialize(Environment.GetEnvironmentVariable("BRD_SNAPSHOT_LANG") ?? "en");
            DesignTheme.Install(this);
            RequestedThemeVariant = ThemeVariant.Light;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var window = new MainWindow(_controller, _settings, t => RequestedThemeVariant = t == ThemePreference.Dark ? ThemeVariant.Dark : ThemeVariant.Light);
                window.Show();
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await Capture(window);
                    }
                    finally
                    {
                        Environment.Exit(0);
                    }
                });
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task Capture(MainWindow window)
        {
            async Task Shot(string name, double height = 760, double width = 1120)
            {
                window.Width = width;
                window.Height = height;
                await Task.Delay(700);
                var size = new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height);
                using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
                bitmap.Render(window);
                bitmap.Save(Path.Combine(_outDir, name + ".png"));
            }

            await Task.Delay(1500);
            await Shot("01-home");

            _controller.Pause(TimeSpan.FromMinutes(15));
            await Shot("02-home-paused");
            _controller.Resume();

            window.PreviewEditor(1, "instagram.com/?variant=following", "Following feed");
            await Shot("03-editor-feeds", 1500);

            window.PreviewEditor(0, "", null);
            await Shot("04-editor-short-video", 1700);

            window.PreviewEditor(1, "instagram.com/?variant=following", null);
            await Shot("05-editor-narrow", 1500, 1000);

            window.PreviewEditor(2, "", null);
            await Shot("05b-editor-set-hours-narrow", 1100, 1000);
            if (window.GetVisualDescendants().OfType<TimeField>().FirstOrDefault() is { } time)
            {
                Control picker = time.Open();
                await Task.Delay(700);
                if (TopLevel.GetTopLevel(picker) is { } popup)
                {
                    var popupSize = new PixelSize((int)popup.Bounds.Width, (int)popup.Bounds.Height);
                    using var popupBitmap = new RenderTargetBitmap(popupSize, new Vector(96, 96));
                    popupBitmap.Render(popup);
                    popupBitmap.Save(Path.Combine(_outDir, "05c-time-picker.png"));
                }

                window.PreviewPage("home");
            }

            window.PreviewNewRule();
            await Shot("06-editor-new-rule");

            window.PreviewAddSites("facebook", "x");
            await Shot("07-add-sites", 900);

            window.PreviewPage("strict");
            await Shot("08-strict-setup", 900);
            await Shot("08b-strict-setup-narrow", 900, 1000);

            window.PreviewPage("settings");
            await Shot("09-settings");

            RequestedThemeVariant = ThemeVariant.Dark;
            window.PreviewPage("home");
            await Shot("10-home-dark");
            window.PreviewEditor(1, "instagram.com/?variant=following", "Following feed");
            await Shot("11-editor-dark", 1500);
            RequestedThemeVariant = ThemeVariant.Light;

            _controller.ActivateStrictMode(TimeSpan.FromHours(4), new[] { DefaultConfiguration.ShortVideoRuleId, DefaultConfiguration.FeedsRuleId });
            window.PreviewPage("strict");
            await Shot("12-strict-running");
            window.PreviewPage("home");
            await Shot("13-home-strict");

            await CaptureGallery("gallery-light", ThemeVariant.Light);
            await CaptureGallery("gallery-dark", ThemeVariant.Dark);
        }

        /// <summary>Renders the design system's gallery at full height (not limited by the screen).</summary>
        private async Task CaptureGallery(string name, ThemeVariant variant)
        {
            RequestedThemeVariant = variant;
            Control gallery = Gallery.Build();
            gallery.Width = 1200;
            var window = new AppWindow { Width = 1200, Height = 800, Content = new Canvas { Children = { gallery } } };
            window.Show();
            await Task.Delay(1000);
            var size = new PixelSize((int)gallery.Bounds.Width, (int)gallery.Bounds.Height);
            using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
            bitmap.Render(gallery);
            bitmap.Save(Path.Combine(_outDir, name + ".png"));
            window.Close();
        }
    }
}

internal sealed partial class MainWindow
{
    internal void PreviewPage(string page)
    {
        _status = _controller.Status;
        Navigate(page switch
        {
            "strict" => Page.Strict,
            "settings" => Page.Settings,
            _ => Page.Home,
        });
    }

    internal void PreviewEditor(int index, string testUrl, string? editPage)
    {
        OpenEditor(_editable.Rules[index].Clone(), index);
        _testUrl = testUrl;
        _editingPage = _editingRule!.Sites.SelectMany(s => s.Pages).FirstOrDefault(p => p.Name == editPage);
        Rerender();
        RefreshTester(updateMarkers: true);
    }

    internal void PreviewNewRule() => OpenEditor(NewRule(), -1);

    internal void PreviewAddSites(params string[] picks)
    {
        OpenAddSites();
        _pickedCatalog.AddRange(picks);
        Rerender();
    }
}
#endif
