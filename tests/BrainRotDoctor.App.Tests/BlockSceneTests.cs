using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;
using System.Collections.Concurrent;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// When a blocked page is in front, the tab no longer closes at once: the worm scene
/// starts, and the tab closes on its pop, only if that window still shows a blocked page.
/// </summary>
public sealed class BlockSceneTests
{
    private const string BlockAll = """
    {
      "rules": [
        { "id": "block", "name": "Blocked", "allDay": true,
          "sites": [ { "label": "Shorts", "url": "youtube.com/shorts" } ] }
      ]
    }
    """;

    private const string Shorts = "https://youtube.com/shorts/abc";

    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(4);

    [Fact]
    public void Blocked_tab_waits_for_the_scene_and_closes_on_the_pop()
    {
        var browsers = new FakeBrowsers(Shorts);
        using EnforcementController controller = Controller(browsers);
        var scenes = new ConcurrentQueue<BlockScene>();
        controller.BlockSceneStarting += (_, scene) => scenes.Enqueue(scene);

        controller.Start();
        Assert.True(Until(() => !scenes.IsEmpty));
        Thread.Sleep(1500);
        Assert.Empty(browsers.Closed);

        BlockScene started = Assert.Single(scenes);
        Assert.Equal(browsers.Handle, Assert.Single(started.Windows).WindowHandle);

        controller.ClosePendingTabs(started.Id);
        Assert.True(Until(() => !browsers.Closed.IsEmpty));
        Assert.Equal(browsers.Handle, Assert.Single(browsers.Closed));
    }

    [Fact]
    public void Tab_left_before_the_pop_is_not_closed_and_nothing_else_is()
    {
        var browsers = new FakeBrowsers(Shorts);
        using EnforcementController controller = Controller(browsers);
        var scenes = new ConcurrentQueue<BlockScene>();
        controller.BlockSceneStarting += (_, scene) => scenes.Enqueue(scene);

        controller.Start();
        Assert.True(Until(() => !scenes.IsEmpty));

        // The user switched to another tab during the scene.
        browsers.Url = "https://example.com/";
        scenes.TryPeek(out BlockScene? started);
        controller.ClosePendingTabs(started!.Id);
        Thread.Sleep(1000);

        Assert.Empty(browsers.Closed);
    }

    [Fact]
    public void One_scene_at_a_time_and_a_tab_still_open_afterwards_starts_the_next()
    {
        var browsers = new FakeBrowsers(Shorts);
        using EnforcementController controller = Controller(browsers);
        var scenes = new ConcurrentQueue<BlockScene>();
        controller.BlockSceneStarting += (_, scene) => scenes.Enqueue(scene);

        controller.Start();
        Assert.True(Until(() => !scenes.IsEmpty));
        Thread.Sleep(2500);
        Assert.Single(scenes);

        scenes.TryPeek(out BlockScene? first);
        controller.EndScene(first!.Id);
        Assert.True(Until(() => scenes.Count == 2));
    }

    [Fact]
    public void Without_a_scene_the_tab_closes_at_once()
    {
        var browsers = new FakeBrowsers(Shorts);
        using EnforcementController controller = Controller(browsers);

        controller.Start();
        Assert.True(Until(() => !browsers.Closed.IsEmpty));
    }

    [Fact]
    public void Tab_closes_anyway_when_the_scene_never_reaches_its_pop()
    {
        var browsers = new FakeBrowsers(Shorts);
        using EnforcementController controller = Controller(browsers);
        controller.BlockSceneStarting += (_, _) => { };

        controller.Start();
        Thread.Sleep(3000);
        Assert.Empty(browsers.Closed);
        Assert.True(Until(() => !browsers.Closed.IsEmpty, TimeSpan.FromSeconds(6)));
    }

    private static EnforcementController Controller(FakeBrowsers browsers)
    {
        string dir = Path.Combine(Path.GetTempPath(), "brainrotdoctor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return new EnforcementController(
            ConfigurationLoader.Load(BlockAll),
            browsers,
            browsers,
            "test",
            BlockAll,
            Path.Combine(dir, "config.json"),
            new StrictModeStore(Path.Combine(dir, "strict-mode.json")),
            logPath: null,
            usageStore: new UsageStore(Path.Combine(dir, "usage.json")),
            pauseStore: new PauseStore(Path.Combine(dir, "pause.json")));
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

    /// <summary>One browser window whose selected page can be changed by the test.</summary>
    private sealed class FakeBrowsers : IBrowserObserver, IBrowserTabCloser
    {
        public FakeBrowsers(string url) => Url = url;

        public IntPtr Handle { get; } = new(4242);

        public volatile string Url;

        public ConcurrentQueue<IntPtr> Closed { get; } = new();

        public IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs() =>
            new[] { new ObservedBrowserWindow("w1", Handle, "Chrome", new Uri(Url)) };

        public Uri? ReadSelectedUrl(IntPtr windowHandle) => windowHandle == Handle ? new Uri(Url) : null;

        public bool CloseSelectedTab(IntPtr windowHandle)
        {
            Closed.Enqueue(windowHandle);
            return true;
        }
    }
}
