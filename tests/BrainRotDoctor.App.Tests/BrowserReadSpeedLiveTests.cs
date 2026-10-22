using BrainRotDoctor.App.Runtime;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Automation;
using Xunit;
using Xunit.Abstractions;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// ADR-016: one read of one browser window takes at most 50 ms, measured on a heavy
/// page. Opens a real Chrome, Edge and Firefox on a local feed-like page and times
/// the observer's read of that window. Reports every read over the 15 ms target.
/// Skips a browser that is not installed.
/// </summary>
[Collection("LiveBrowsers")]
public sealed class BrowserReadSpeedLiveTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan Target = TimeSpan.FromMilliseconds(15);
    private const int Reads = 10;

    private readonly ITestOutputHelper _output;

    public BrowserReadSpeedLiveTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("chrome")]
    [InlineData("msedge")]
    [InlineData("firefox")]
    public void Reading_a_window_on_a_heavy_page_stays_under_50_ms(string processName)
    {
        string? exe = PrivateWindowLiveTests.BrowserExe(processName);
        if (exe is null)
        {
            // xunit 2 has no dynamic skip; a missing browser must not fail CI.
            return;
        }

        using var server = new FeedPageServer();
        string profile = Path.Combine(Path.GetTempPath(), "brd-live-speed", processName + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(profile);
        PrivateWindowLiveTests.PrepareFirefoxProfile(processName, profile);
        if (processName == "firefox")
        {
            // A fresh profile would try the local page over https first.
            File.AppendAllText(
                Path.Combine(profile, "user.js"),
                """

                user_pref("dom.security.https_first", false);
                user_pref("dom.security.https_first_schemeless", false);

                """);
        }

        DateTime startedAt = DateTime.UtcNow.AddSeconds(-2);
        Process? launched = null;
        try
        {
            ProcessStartInfo start = PrivateWindowLiveTests.StartInfo(exe, processName, profile, isPrivate: false, server.Url);
            start.ArgumentList.Add(server.OtherUrl);
            launched = Process.Start(start);
            Assert.NotNull(launched);

            var observer = new UiAutomationBrowserObserver();
            IntPtr window = WaitForPage(observer, processName, startedAt, TimeSpan.FromSeconds(20), out string seen);
            Assert.True(window != IntPtr.Zero, processName + " never showed the test pages; saw: " + seen);
            Assert.True(SelectTabUntilRead(observer, window, "Feed", FeedPageServer.Path), processName + " never read the feed tab once selected; " + Inside(window));

            // A window's first read finds its page and asks once whether it is private;
            // it is a check like any other. Every later second's read must be cheap too.
            var fresh = new UiAutomationBrowserObserver();
            var first = Stopwatch.StartNew();
            Uri? firstUrl = fresh.ReadSelectedUrl(window);
            double firstRead = first.Elapsed.TotalMilliseconds;
            Assert.True(FeedPageServer.IsPage(firstUrl, FeedPageServer.Path), processName + " first read " + (firstUrl?.AbsoluteUri ?? "(null)") + " instead of the feed page");
            _output.WriteLine($"{processName}: first read {firstRead:0.0} ms");
            Assert.True(firstRead <= Limit.TotalMilliseconds, $"ADR-016 limit is {Limit.TotalMilliseconds:0} ms per read. {processName} first read took {firstRead:0} ms");

            var times = new List<double>();
            for (int i = 0; i < Reads; i++)
            {
                var watch = Stopwatch.StartNew();
                Uri? url = observer.ReadSelectedUrl(window);
                times.Add(watch.Elapsed.TotalMilliseconds);
                Assert.True(FeedPageServer.IsPage(url, FeedPageServer.Path), processName + " read " + (url?.AbsoluteUri ?? "(null)") + " instead of the feed page");
            }

            string report = $"{processName}: reads {string.Join(", ", times.Select(t => t.ToString("0.0")))} ms; " +
                $"median {times.Order().ElementAt(times.Count / 2):0.0} ms, max {times.Max():0.0} ms";
            _output.WriteLine(report);
            int overTarget = times.Count(t => t > Target.TotalMilliseconds);
            if (overTarget > 0)
            {
                _output.WriteLine($"{processName}: {overTarget} of {Reads} reads over the {Target.TotalMilliseconds:0} ms target (ADR-016: raise it)");
            }

            // A fast read must still follow the user to another tab.
            Assert.True(SelectTabUntilRead(observer, window, "Other", FeedPageServer.OtherPath), processName + " kept reporting the old tab after switching; " + Inside(window));

            Assert.True(times.Max() <= Limit.TotalMilliseconds, $"ADR-016 limit is {Limit.TotalMilliseconds:0} ms per read. {report}");
        }
        finally
        {
            try
            {
                launched?.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            PrivateWindowLiveTests.KillStartedSince(processName, startedAt);
            launched?.Dispose();
            try
            {
                Directory.Delete(profile, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// Selects the tab whose name starts with <paramref name="tabName"/>, as a click
    /// would, and reads until the observer reports that tab's page; false after 5 s.
    /// </summary>
    private static bool SelectTabUntilRead(UiAutomationBrowserObserver observer, IntPtr window, string tabName, string path)
    {
        var tab = AutomationElement.FromHandle(window)
            .FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem))
            .Cast<AutomationElement>()
            .FirstOrDefault(t => t.Current.Name.StartsWith(tabName, StringComparison.Ordinal));
        if (tab is null)
        {
            return false;
        }

        ((SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(5))
        {
            if (FeedPageServer.IsPage(observer.ReadSelectedUrl(window), path))
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    /// <summary>What the window's address bar and page say, for a failure message.</summary>
    private static string Inside(IntPtr handle)
    {
        try
        {
            var root = AutomationElement.FromHandle(handle);
            var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Where(e => e.Current.ControlType == ControlType.Document || e.Current.ControlType == ControlType.Edit)
                .Take(6)
                .Select(e => e.Current.ControlType.ProgrammaticName + " id=" + e.Current.AutomationId + " off=" + e.Current.IsOffscreen + " name=" + e.Current.Name + " focus=" + e.Current.HasKeyboardFocus + " value="
                    + (e.TryGetCurrentPattern(ValuePattern.Pattern, out object p) ? ((ValuePattern)p).Current.Value : "-"));
            return "[" + string.Join("; ", all) + "]";
        }
        catch (Exception ex)
        {
            return "[" + ex.GetType().Name + "]";
        }
    }

    /// <summary>The window of ours that shows the feed page, or zero when none did in time.</summary>
    private static IntPtr WaitForPage(UiAutomationBrowserObserver observer, string processName, DateTime startedAt, TimeSpan limit, out string seen)
    {
        seen = "nothing";
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < limit)
        {
            IReadOnlyList<ObservedBrowserWindow> windows = observer.GetSelectedTabs();
            seen = string.Join(" | ", windows
                .Where(w => PrivateWindowLiveTests.StartedWithUs(w.WindowHandle, processName, startedAt))
                .Select(w => "'" + NativeMethods.GetWindowTitle(w.WindowHandle) + "' " + (w.Url?.AbsoluteUri ?? "(null)") + " " + Inside(w.WindowHandle)));
            foreach (ObservedBrowserWindow window in windows)
            {
                if (window.BrowserName.Equals(PrivateWindowLiveTests.BrowserLabel(processName), StringComparison.OrdinalIgnoreCase)
                    && PrivateWindowLiveTests.StartedWithUs(window.WindowHandle, processName, startedAt)
                    && (FeedPageServer.IsPage(window.Url, FeedPageServer.Path) || FeedPageServer.IsPage(window.Url, FeedPageServer.OtherPath)))
                {
                    // Let the page finish laying out before timing.
                    Thread.Sleep(1000);
                    return window.WindowHandle;
                }
            }

            Thread.Sleep(250);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Serves one feed-like page on a local port: hundreds of posts, each with
    /// text, a link and its own buttons, as on a short-video or social feed.
    /// </summary>
    private sealed class FeedPageServer : IDisposable
    {
        public const string Path = "/feed";
        public const string OtherPath = "/other";

        // Port 80, so the address bar shows a plain "127.0.0.1/feed", as it would a site.
        private readonly TcpListener _listener = new(IPAddress.Loopback, 80);
        private readonly byte[] _response;
        private readonly byte[] _other = Html("<!doctype html><html><head><title>Other</title></head><body><p>Another tab.</p></body></html>");

        public FeedPageServer()
        {
            var page = new StringBuilder("<!doctype html><html><head><title>Feed</title></head><body><h1>Feed</h1>");
            for (int i = 0; i < 300; i++)
            {
                page.Append($"<article><h2>Post {i}</h2><p>Some words about post {i}, long enough to wrap onto a second line in a normal window.</p>")
                    .Append($"<a href=\"#p{i}\">Open post {i}</a> <button>Like</button> <button>Comment</button> <button>Share</button></article>");
            }

            page.Append("</body></html>");
            _response = Html(page.ToString());
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(ServeAsync);
        }

        public int Port { get; }

        public string Url => $"http://127.0.0.1{Path}";

        public string OtherUrl => $"http://127.0.0.1{OtherPath}";

        // Chromium's address bar hides "http://", so the observer reads it as https.
        public static bool IsPage(Uri? url, string path) => url is not null && url.Host == "127.0.0.1" && url.AbsolutePath == path;

        private static byte[] Html(string page)
        {
            byte[] body = Encoding.UTF8.GetBytes(page);
            byte[] head = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            return head.Concat(body).ToArray();
        }

        private async Task ServeAsync()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch (Exception ex) when (ex is ObjectDisposedException or SocketException or InvalidOperationException)
                {
                    return;
                }

                _ = Task.Run(async () =>
                {
                    using (client)
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            var buffer = new byte[4096];
                            int read = await stream.ReadAsync(buffer);
                            bool other = Encoding.ASCII.GetString(buffer, 0, read).StartsWith("GET " + OtherPath, StringComparison.Ordinal);
                            await stream.WriteAsync(other ? _other : _response);
                        }
                        catch (IOException)
                        {
                        }
                    }
                });
            }
        }

        public void Dispose() => _listener.Stop();
    }
}
