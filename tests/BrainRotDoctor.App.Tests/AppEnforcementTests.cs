using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Apps;
using BrainRotDoctor.Core.Configuration;
using System.Collections.Concurrent;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Apps are closed like tabs, on the worm scene's pop, with one difference: an app that
/// was running when a block came gets a minute's heads-up first. One that shows up while
/// already blocked closes at once, and Steam (which only goes to its tray) is never warned.
/// </summary>
public sealed class AppEnforcementTests
{
    private const string NoRules = """{ "rules": [] }""";

    private const string GamesBlocked = """
    { "rules": [ { "id": "games", "name": "Night", "allDay": true, "apps": [ { "kind": "steam-games" }, { "kind": "steam" } ] } ] }
    """;

    private static readonly AppIdentity Valheim = new(AppKind.SteamGame, "Valheim", "valheim.exe", "Valheim", "Valheim");
    private static readonly AppIdentity Steam = new(AppKind.Steam, "Steam", "steamwebhelper.exe");

    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(4);

    [Fact]
    public void A_game_started_while_blocked_closes_on_the_pop_without_a_heads_up()
    {
        var apps = new FakeApps(Valheim);
        using EnforcementController controller = Controller(apps, GamesBlocked);
        var scenes = new ConcurrentQueue<BlockScene>();
        controller.BlockSceneStarting += (_, scene) => scenes.Enqueue(scene);

        controller.Start();
        Assert.True(Until(() => !scenes.IsEmpty));
        Assert.Empty(controller.Status.HeadsUps);
        Assert.Empty(apps.Closed);

        scenes.TryPeek(out BlockScene? scene);
        Assert.Equal(apps.Window, Assert.Single(scene!.Windows));
        controller.ClosePendingTabs(scene.Id);
        Assert.True(Until(() => !apps.Closed.IsEmpty));
        Assert.Equal(Valheim, Assert.Single(apps.Closed));
    }

    [Fact]
    public void A_game_running_when_a_rule_starts_blocking_it_gets_a_minute()
    {
        var apps = new FakeApps(Valheim);
        using EnforcementController controller = Controller(apps, NoRules);
        var scenes = new ConcurrentQueue<BlockScene>();
        controller.BlockSceneStarting += (_, scene) => scenes.Enqueue(scene);
        controller.Start();
        Thread.Sleep(1500);

        Assert.True(controller.TrySaveConfiguration(EditableConfiguration.FromJson(GamesBlocked), out string? error), error);
        Assert.True(Until(() => controller.Status.HeadsUps.Count == 1));

        HeadsUp headsUp = controller.Status.HeadsUps[0];
        Assert.Equal("Valheim", headsUp.App);
        Assert.Equal("Night", headsUp.Rule);
        Assert.InRange(headsUp.At - DateTimeOffset.Now, TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(61));
        Thread.Sleep(2000);
        Assert.Empty(scenes);
        Assert.Empty(apps.Closed);
    }

    [Fact]
    public void Steam_is_never_warned_and_is_only_asked_to_close()
    {
        var apps = new FakeApps(Steam);
        using EnforcementController controller = Controller(apps, NoRules);
        controller.Start();
        Thread.Sleep(1500);

        Assert.True(controller.TrySaveConfiguration(EditableConfiguration.FromJson(GamesBlocked), out _));
        Assert.True(Until(() => !apps.Closed.IsEmpty));
        Assert.Empty(controller.Status.HeadsUps);
    }

    [Fact]
    public void Nothing_is_closed_or_announced_while_paused()
    {
        var apps = new FakeApps(Valheim);
        using EnforcementController controller = Controller(apps, GamesBlocked);
        Assert.True(controller.Pause(TimeSpan.FromMinutes(5)));
        controller.Start();
        Thread.Sleep(2000);

        Assert.Empty(apps.Closed);
        Assert.Empty(controller.Status.HeadsUps);

        // Resume: the game ran unblocked until now, so it gets its minute.
        controller.Resume();
        Assert.True(Until(() => controller.Status.HeadsUps.Count == 1));
        Assert.Empty(apps.Closed);
    }

    [Fact]
    public void A_pause_ending_into_a_block_is_announced_before_it_ends()
    {
        var apps = new FakeApps(Valheim);
        using EnforcementController controller = Controller(apps, GamesBlocked);
        Assert.True(controller.Pause(TimeSpan.FromSeconds(30)));
        controller.Start();

        Assert.True(Until(() => controller.Status.HeadsUps.Count == 1));
        Assert.InRange(controller.Status.HeadsUps[0].At - DateTimeOffset.Now, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(31));
        Assert.Empty(apps.Closed);
    }

    [Fact]
    public void With_a_private_window_in_front_no_app_is_recorded()
    {
        var apps = new FakeApps(Valheim) { PrivateWindowInFront = true };
        using EnforcementController controller = Controller(apps, NoRules);
        controller.Start();
        Thread.Sleep(2500);

        Assert.Empty(controller.Status.Activity.Apps);
        Assert.Empty(controller.Status.Activity.Places);
    }

    [Fact]
    public void Without_a_browser_in_front_the_app_in_front_is_recorded()
    {
        var apps = new FakeApps(Valheim);
        using EnforcementController controller = Controller(apps, NoRules);
        controller.Start();
        Assert.True(Until(() => controller.Status.Activity.Apps.ContainsKey(Valheim.Key)));
    }

    [Fact]
    public void A_minimized_program_neither_counts_nor_closes_but_a_minimized_game_does()
    {
        var discord = new AppIdentity(AppKind.Program, "Discord", "discord.exe", "Discord");
        Assert.False(new ObservedApp(discord, new[] { new IntPtr(1) }, Array.Empty<AppProcess>(), onScreen: false, isForeground: false).Counts);
        Assert.True(new ObservedApp(Valheim, new[] { new IntPtr(1) }, Array.Empty<AppProcess>(), onScreen: false, isForeground: false).Counts);
    }

    [Fact]
    public void Apps_round_trip_through_the_editor_and_a_change_to_them_is_loosening_in_strict_mode()
    {
        EditableConfiguration config = EditableConfiguration.FromJson(GamesBlocked);
        EditableConfiguration.EditableRule rule = Assert.Single(config.Rules);
        Assert.Equal(new[] { TargetAppKind.AllSteamGames, TargetAppKind.Steam }, rule.Apps.Select(a => a.Kind));

        EditableConfiguration again = EditableConfiguration.FromJson(config.ToJson());
        Assert.Equal(config.ToJson(), again.ToJson());

        again.Rules[0].Apps.RemoveAt(0);
        Assert.Equal(new[] { "Night" }, RuleGuard.Loosened(config, again));
    }

    [Fact]
    public void Adding_an_app_a_rule_has_already_changes_nothing()
    {
        EditableConfiguration.EditableRule rule = Assert.Single(EditableConfiguration.FromJson(GamesBlocked).Rules);
        rule.Block(EditableConfiguration.EditableApp.AllSteamGames());
        rule.Block(EditableConfiguration.EditableApp.For(Valheim));
        Assert.Equal(3, rule.Apps.Count);
        rule.Block(EditableConfiguration.EditableApp.For(Valheim));
        Assert.Equal(3, rule.Apps.Count);
    }

    private static EnforcementController Controller(FakeApps apps, string json)
    {
        string dir = Path.Combine(Path.GetTempPath(), "brainrotdoctor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return new EnforcementController(
            ConfigurationLoader.Load(json),
            apps,
            apps,
            "test",
            json,
            Path.Combine(dir, "config.json"),
            new StrictModeStore(Path.Combine(dir, "strict-mode.json")),
            logPath: null,
            usageStore: new UsageStore(Path.Combine(dir, "usage.json")),
            pauseStore: new PauseStore(Path.Combine(dir, "pause.json")),
            activityStore: new ActivityStore(Path.Combine(dir, "activity.json")),
            ruleBaseline: new RuleBaselineStore(Path.Combine(dir, "rules-accepted.json")),
            appObserver: apps,
            appCloser: apps);
    }

    private static bool Until(Func<bool> condition, TimeSpan? limit = null)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.Elapsed < (limit ?? Wait))
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(20);
        }

        return condition();
    }

    /// <summary>One app on screen, no browsers; closing it is recorded and makes it go.</summary>
    private sealed class FakeApps : IAppObserver, IAppCloser, IBrowserObserver, IBrowserTabCloser
    {
        private volatile bool _gone;

        public FakeApps(AppIdentity app) => App = app;

        public AppIdentity App { get; }

        public IntPtr Window { get; } = new(777);

        public ConcurrentQueue<AppIdentity> Closed { get; } = new();

        public IReadOnlyList<ObservedApp> GetApps() => _gone
            ? Array.Empty<ObservedApp>()
            : new[] { new ObservedApp(App, new[] { Window }, new[] { new AppProcess(1, 1) }, onScreen: true, isForeground: true) };

        public AppCloseResult Close(ObservedApp app)
        {
            Closed.Enqueue(app.Identity);
            _gone = true;
            return AppCloseResult.Closed;
        }

        /// <summary>A private browser window is the one in front (and so the app is not).</summary>
        public bool PrivateWindowInFront { get; init; }

        public IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs() => PrivateWindowInFront
            ? new[] { new ObservedBrowserWindow("w1", new IntPtr(4242), "Chrome", new Uri("https://example.com/"), isForeground: true, isPrivate: true) }
            : Array.Empty<ObservedBrowserWindow>();

        public Uri? ReadSelectedUrl(IntPtr windowHandle) => null;

        public bool CloseSelectedTab(IntPtr windowHandle) => false;
    }
}
