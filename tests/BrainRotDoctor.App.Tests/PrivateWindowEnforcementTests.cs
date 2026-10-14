using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Configuration;
using System.Collections.Concurrent;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// A private window is still closed when a rule blocks it, and never appears in
/// today's Status statistics.
/// </summary>
public sealed class PrivateWindowEnforcementTests
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
    public void Private_window_still_closes_when_a_rule_blocks_it()
    {
        var browsers = new FakeBrowsers(Shorts, isPrivate: true);
        using EnforcementController controller = Controller(browsers);

        controller.Start();
        Assert.True(Until(() => !browsers.Closed.IsEmpty));
        Assert.Equal(browsers.Handle, Assert.Single(browsers.Closed));
    }

    [Fact]
    public void Private_foreground_page_is_not_counted_on_status()
    {
        var browsers = new FakeBrowsers(Shorts, isPrivate: true);
        using EnforcementController controller = Controller(browsers);
        controller.Start();
        Assert.True(Until(() => controller.Status.IsRunning));
        Thread.Sleep(2500);

        Assert.Empty(controller.Status.Activity.Places);
    }

    [Fact]
    public void Ordinary_foreground_page_is_counted_on_status()
    {
        var browsers = new FakeBrowsers("https://example.com/", isPrivate: false);
        using EnforcementController controller = Controller(browsers);
        controller.Start();
        Assert.True(Until(() => controller.Status.Activity.Places.ContainsKey("example.com")));
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
            pauseStore: new PauseStore(Path.Combine(dir, "pause.json")),
            activityStore: new ActivityStore(Path.Combine(dir, "activity.json")));
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

    private sealed class FakeBrowsers : IBrowserObserver, IBrowserTabCloser
    {
        public FakeBrowsers(string url, bool isPrivate)
        {
            Url = url;
            IsPrivate = isPrivate;
        }

        public IntPtr Handle { get; } = new(4242);

        public volatile string Url;

        public bool IsPrivate { get; }

        public ConcurrentQueue<IntPtr> Closed { get; } = new();

        private volatile bool _open = true;

        public IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs() =>
            _open
                ? new[] { new ObservedBrowserWindow("w1", Handle, "Chrome", new Uri(Url), isForeground: true, isPrivate: IsPrivate) }
                : Array.Empty<ObservedBrowserWindow>();

        public Uri? ReadSelectedUrl(IntPtr windowHandle) => _open && windowHandle == Handle ? new Uri(Url) : null;

        public bool CloseSelectedTab(IntPtr windowHandle)
        {
            if (windowHandle != Handle || !_open)
            {
                return false;
            }

            _open = false;
            Closed.Enqueue(windowHandle);
            return true;
        }
    }
}
