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
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Apps;
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
            Apps = new()
            {
                new AppDocument { Kind = AppDocument.AllSteamGames },
                new AppDocument { Kind = AppDocument.Program, Label = "Discord", Exe = "discord.exe", Description = "Discord" },
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

        var activity = new ActivityStore(Path.Combine(data, "activity-today.json"));
        activity.Save(SampleActivity(now));

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
            pauseStore: new PauseStore(Path.Combine(data, "pause.json")),
            activityStore: activity);
        controller.Start();

        var settings = new UiSettingsStore(Path.Combine(data, "ui-settings.json"));
        AppBuilder.Configure(() => new SnapshotApp(controller, settings, outDir))
            .UsePlatformDetect()
            .UseDesignSystem()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>A plausible day: a forum's comments eating the afternoon, short video, and work.</summary>
    private static ActivityRecord SampleActivity(DateTimeOffset now)
    {
        var places = new (string Key, double Minutes, int Visits)[]
        {
            ("reddit.com", 134, 23), ("reddit.com/r", 125, 21), ("reddit.com/r/formula1", 118, 19),
            ("reddit.com/r/formula1/comments", 110, 17), ("reddit.com/r/pics", 7, 2),
            ("youtube.com", 41, 9), ("youtube.com/shorts", 22, 6), ("youtube.com/watch", 16, 4),
            ("github.com", 96, 6), ("github.com/llehn", 60, 4), ("github.com/dotnet", 30, 3),
            ("news.ycombinator.com", 18, 8), ("news.ycombinator.com/item", 14, 6),
            ("docs.avaloniaui.net", 12, 3), ("docs.avaloniaui.net/docs", 12, 3),
            ("instagram.com", 6, 7), ("instagram.com/direct", 4, 5),
            ("wikipedia.org", 0.6, 1), ("bbc.com", 0.4, 1),
        };

        var apps = new[]
        {
            new AppIdentity(AppKind.SteamGame, "Valheim", "valheim.exe", "Valheim", "Valheim"),
            new AppIdentity(AppKind.Program, "Visual Studio Code", "code.exe", "Visual Studio Code"),
            new AppIdentity(AppKind.Steam, "Steam", "steamwebhelper.exe"),
        };
        double[] appMinutes = { 88, 64, 9 };

        return new ActivityRecord
        {
            Day = ActivityTally.DayOf(now),
            Places = places.ToDictionary(p => p.Key, p => new PlaceTotal { Seconds = p.Minutes * 60, Visits = p.Visits }),
            Apps = apps.Select((a, i) => (a, i)).ToDictionary(x => x.a.Key, x => new AppTotal { Seconds = appMinutes[x.i] * 60, Visits = 2, App = x.a }),
            PausedToday = true,
            PausedSeconds = 20 * 60,
        };
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
            await Shot("00-status");
            window.PreviewSite("reddit.com");
            await Shot("00b-status-site", 900);
            window.PreviewSite("youtube.com");
            await Shot("00e-status-site-in-rules", 900);
            window.PreviewApp("steam-game:valheim");
            await Shot("00g-status-game", 900);
            window.PreviewApp("program:code.exe|visual studio code");
            await Shot("00h-status-program", 900);
            window.PreviewApp(null);
            _controller.Pause(TimeSpan.FromMinutes(15));
            window.PreviewSite("instagram.com");
            await Shot("00f-status-site-paused", 900);
            _controller.Resume();
            window.PreviewSite(null);
            window.PreviewStrictActivating();
            await Shot("00c-status-activating");
            window.PreviewStrictCancel();
            await Shot("00d-status-narrow", 640, 1000);
            window.PreviewPage("home");
            await Shot("01-home");

            _controller.Pause(TimeSpan.FromMinutes(15));
            await Shot("02-home-paused");
            window.PreviewPage("status");
            await Shot("02b-status-paused");
            _controller.Resume();
            window.PreviewPause(custom: false, minutes: 0);
            await Shot("02c-pause");
            window.PreviewPause(custom: true, minutes: 40);
            await Shot("02c-pause-longer");
            window.PreviewStrictCustom(true);
            window.PreviewPage("status");
            await Shot("02d-status-strict-custom", 900);
            window.PreviewPage("strict");
            await Shot("02e-strict-custom", 900);
            window.PreviewStrictCustom(false);
            window.PreviewPage("home");

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
            window.PreviewAddApps();
            await Shot("07b-add-apps", 1100);
            window.PreviewEditor(2, "", null);
            await Shot("07c-editor-with-apps", 1100);

            window.PreviewPage("strict");
            await Shot("08-strict-setup", 900);
            await Shot("08b-strict-setup-narrow", 900, 1000);

            window.PreviewPage("settings");
            await Shot("09-settings");
            window.PreviewUntracked();
            await Shot("09b-settings-untracked");

            RequestedThemeVariant = ThemeVariant.Dark;
            window.PreviewPage("status");
            await Shot("10a-status-dark");
            window.PreviewSite("reddit.com");
            await Shot("10b-status-site-dark", 900);
            window.PreviewSite(null);
            window.PreviewPage("home");
            await Shot("10-home-dark");
            window.PreviewEditor(1, "instagram.com/?variant=following", "Following feed");
            await Shot("11-editor-dark", 1500);
            RequestedThemeVariant = ThemeVariant.Light;

            _controller.ActivateStrictMode(TimeSpan.FromHours(4));
            window.PreviewPage("strict");
            await Shot("12-strict-running");
            window.PreviewPage("home");
            await Shot("13-home-strict");
            window.PreviewPage("status");
            await Shot("14-status-strict");

            window.PreviewEverything("github.com", "docs.avaloniaui.net");
            await Shot("15-status-everything");

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
            "status" => Page.Status,
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

    /// <summary>The status view with a site enlarged, or none.</summary>
    internal void PreviewSite(string? host)
    {
        _status = _controller.Status;
        _openSite = host;
        Navigate(Page.Status);
    }

    /// <summary>A strict mode shortcut just clicked: the moment to cancel. Never fires (<see cref="PreviewStrictCancel"/>).</summary>
    internal void PreviewStrictActivating() => BeginStrict(DateTimeOffset.Now.AddHours(2));

    internal void PreviewStrictCancel() => CancelStrict();

    /// <summary>Settings with some sites not tracked, the list shown.</summary>
    internal void PreviewUntracked()
    {
        _untracked = new HashSet<string>(StringComparer.Ordinal) { "github.com", "mail.google.com", "docs.avaloniaui.net" };
        _showUntracked = true;
        Rerender();
    }

    /// <summary>The status view with the pause dialog open: the short breaks, or a longer one of so many minutes.</summary>
    internal void PreviewPause(bool custom, int minutes)
    {
        PreviewPage("status");
        _pauseOpen = true;
        _pausePick = 0;
        _pauseCustomOpen = custom;
        _pauseCustomMinutes = custom ? minutes : HoldOver;
        Rerender();
    }

    /// <summary>Strict mode's custom length open, or closed.</summary>
    internal void PreviewStrictCustom(bool open) => _strictCustomOpen = open;

    /// <summary>The status view with these sites okay and everything shown: their time as one grey tile.</summary>
    internal void PreviewEverything(params string[] okay)
    {
        _untracked = new HashSet<string>(okay, StringComparer.Ordinal);
        _controller.SetUntrackedSites(_untracked);
        _showEverything = true;
        PreviewPage("status");
    }

    /// <summary>The status view with an app enlarged, or none.</summary>
    internal void PreviewApp(string? key)
    {
        _status = _controller.Status;
        _openSite = null;
        _openApp = key;
        Navigate(Page.Status);
    }

    /// <summary>The Add screen on apps and games, with All Steam games picked.</summary>
    internal void PreviewAddApps()
    {
        OpenEditor(NewRule(), -1);
        OpenAddSites();
        _addingApps = true;
        _pickedApps.Add(EditableConfiguration.EditableApp.AllSteamGames());
        Rerender();
    }

    internal void PreviewAddSites(params string[] picks)
    {
        OpenAddSites();
        _pickedCatalog.AddRange(picks);
        Rerender();
    }
}
#endif
