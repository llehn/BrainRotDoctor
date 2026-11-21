using BrainRotDoctor.App.Runtime;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using FeedPageServer = BrainRotDoctor.App.Tests.BrowserReadSpeedLiveTests.FeedPageServer;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Reading a browser window again and again must not pile up memory: the app reads
/// every second for days. Opens a real Chrome, Edge and Firefox on a heavy page,
/// reads the window many times, and measures what stays allocated in the process's
/// native heaps (where UI Automation keeps its bookkeeping) per read.
/// Skips a browser that is not installed.
/// </summary>
[Collection("LiveBrowsers")]
public sealed class BrowserReadMemoryLiveTests
{
    private const int WarmUpReads = 100;
    private const int Reads = 800;
    private const int FirstReads = 150;
    private const int Rounds = 4;
    private const int FirstRounds = 8;
    private const int CacheFillingRounds = 10;

    // Windows' own cache grows and shrinks by a dozen objects at a time; a leak only
    // grows. The net growth over this many last rounds is what a read leaves behind.
    private const int JudgedRounds = 5;

    // UI Automation objects a read may leave behind on average. The leak this guards
    // against (ADR-019) left one or two per read; none may stay now.
    private const double LimitPerRead = 0.02;

    private readonly ITestOutputHelper _output;

    public BrowserReadMemoryLiveTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("chrome")]
    [InlineData("msedge")]
    [InlineData("firefox")]
    public void Reading_a_window_many_times_leaves_no_memory_behind(string processName)
    {
        string? exe = PrivateWindowLiveTests.BrowserExe(processName);
        if (exe is null)
        {
            return;
        }

        using var server = new FeedPageServer();
        string profile = Path.Combine(Path.GetTempPath(), "brd-live-memory", processName + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(profile);
        PrivateWindowLiveTests.PrepareFirefoxProfile(processName, profile);
        if (processName == "firefox")
        {
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
            launched = Process.Start(PrivateWindowLiveTests.StartInfo(exe, processName, profile, isPrivate: false, server.Url));
            Assert.NotNull(launched);

            var observer = new UiAutomationBrowserObserver();
            IntPtr window = BrowserReadSpeedLiveTests.WaitForPage(observer, processName, startedAt, TimeSpan.FromSeconds(20), out string seen);
            Assert.True(window != IntPtr.Zero, processName + " never showed the test page; saw: " + seen);

            for (int i = 0; i < WarmUpReads; i++)
            {
                observer.ReadSelectedUrl(window);
            }

            // Each kind of question the app asks a browser, measured on its own.
            var failures = new List<string>();
            Measure("a known window", Reads, Rounds, () => observer.ReadSelectedUrl(window), expectPage: true);

            // Windows keeps the last few hundred elements found in a window in a cache
            // of its own; that growth stops once the cache is full, so it is let fill.
            Measure("a window seen for the first time", FirstReads, CacheFillingRounds, () =>
            {
                using var fresh = new UiAutomationBrowserObserver();
                return fresh.ReadSelectedUrl(window);
            }, expectPage: true);
            Measure("the selected tab's identity", FirstReads, FirstRounds, () => BrowserTabCloser.SelectedTabId(window), expectPage: false);
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));

            void Measure(string what, int reads, int rounds, Func<object?> read, bool expectPage)
            {
                // What an earlier run left to clean up is freed, and caches fill, in
                // the first rounds. A leak grows every round while a full cache only
                // churns, so the net growth over the last rounds is what stays.
                var growth = new List<double>();
                long last = UiAutomationObjects();
                var watch = Stopwatch.StartNew();
                for (int round = 0; round < rounds; round++)
                {
                    for (int i = 0; i < reads; i++)
                    {
                        object? result = read();
                        Assert.True(!expectPage || FeedPageServer.IsPage(result as Uri, FeedPageServer.Path), processName + " read " + ((result as Uri)?.AbsoluteUri ?? "(null)") + " instead of the feed page");
                    }

                    long now = UiAutomationObjects();
                    growth.Add((now - last) / (double)reads);
                    last = now;
                }

                double perRead = growth.TakeLast(Math.Min(JudgedRounds, rounds - 1)).Average();
                _output.WriteLine($"{processName}, {what}: {rounds} x {reads} reads in {watch.Elapsed.TotalSeconds:0.0} s; UI Automation objects left per 100 reads by round: {string.Join(", ", growth.Select(g => (g * 100).ToString("0.0")))}");
                if (perRead > LimitPerRead)
                {
                    failures.Add($"{processName}, {what}: every 100 reads left {perRead * 100:0.0} UI Automation objects behind (limit {LimitPerRead * 100:0.0}); read once a second, that is {perRead * 86400:0} a day");
                }
            }
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
    /// How many objects UI Automation's engine has alive in this process's native heaps,
    /// after the .NET side has let go of everything it no longer uses (so finalizers
    /// released their handles). An object is the engine's when its first field, its
    /// type's function table, lies in the engine's module.
    /// </summary>
    internal static long UiAutomationObjects()
    {
        IntPtr module = GetModuleHandle("UIAutomationCore.dll");
        Assert.NotEqual(IntPtr.Zero, module);
        Assert.True(GetModuleInformation(Process.GetCurrentProcess().Handle, module, out ModuleInfo info, Marshal.SizeOf<ModuleInfo>()));
        long start = info.BaseOfDll.ToInt64();
        long end = start + info.SizeOfImage;

        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Releases that UI Automation hands to its own threads land a moment later.
        Thread.Sleep(500);

        int count = GetProcessHeaps(0, null);
        var heaps = new IntPtr[count + 16];
        count = GetProcessHeaps(heaps.Length, heaps);
        long total = 0;
        for (int h = 0; h < Math.Min(count, heaps.Length); h++)
        {
            if (!HeapLock(heaps[h]))
            {
                continue;
            }

            try
            {
                var entry = new ProcessHeapEntry();
                while (HeapWalk(heaps[h], ref entry))
                {
                    if ((entry.Flags & EntryBusy) != 0 && entry.DataSize >= 8)
                    {
                        long first = Marshal.ReadInt64(entry.Data);
                        if (first >= start && first < end)
                        {
                            total++;
                        }
                    }
                }
            }
            finally
            {
                HeapUnlock(heaps[h]);
            }
        }

        return total;
    }

    private const ushort EntryBusy = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessHeapEntry
    {
        public IntPtr Data;
        public uint DataSize;
        public byte Overhead;
        public byte RegionIndex;
        public ushort Flags;
        public IntPtr Union0;
        public IntPtr Union1;
        public IntPtr Union2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ModuleInfo
    {
        public IntPtr BaseOfDll;
        public uint SizeOfImage;
        public IntPtr EntryPoint;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetModuleInformation(IntPtr process, IntPtr module, out ModuleInfo info, int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GetProcessHeaps(int numberOfHeaps, IntPtr[]? heaps);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HeapLock(IntPtr heap);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HeapUnlock(IntPtr heap);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool HeapWalk(IntPtr heap, ref ProcessHeapEntry entry);
}
