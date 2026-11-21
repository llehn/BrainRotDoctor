using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BrainRotDoctor.App.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace BrainRotDoctor.App.Tests;

public sealed partial class ProcessProtectorTests
{
    private readonly ITestOutputHelper _output;

    public ProcessProtectorTests(ITestOutputHelper output) => _output = output;

    private static readonly DateTimeOffset T0 = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsHeld_is_false_when_nobody_owns_the_mutex_and_does_not_keep_it()
    {
        string name = UniqueMutex();

        Assert.False(RoleMutex.IsHeld(name));
        Assert.True(OtherThreadCanAcquire(name));
    }

    [Fact]
    public void IsHeld_is_true_only_while_a_live_owner_has_the_mutex()
    {
        string name = UniqueMutex();
        using (var held = new HeldMutex(name))
        {
            Assert.True(RoleMutex.IsHeld(name));
            _ = held;
        }

        Assert.False(RoleMutex.IsHeld(name));
    }

    [Fact]
    public void IsHeld_treats_an_abandoned_mutex_as_not_held()
    {
        string name = UniqueMutex();
        var abandoned = new Thread(() =>
        {
            _ = new Mutex(initiallyOwned: true, name);
        });
        abandoned.Start();
        abandoned.Join();

        Assert.False(RoleMutex.IsHeld(name));
        Assert.True(OtherThreadCanAcquire(name));
    }

    [Fact]
    public void Tick_does_not_start_while_the_role_is_held()
    {
        using var probe = new WatchProbe();
        using var held = new HeldMutex(probe.Role);

        probe.Tick(T0);

        Assert.Equal(0, probe.Starts);
        _ = held;
    }

    [Fact]
    public void Tick_starts_once_and_holds_the_guard_until_the_role_is_taken()
    {
        using var probe = new WatchProbe();

        probe.Tick(T0);
        probe.Tick(T0.AddMilliseconds(50));

        Assert.Equal(1, probe.Starts);
        Assert.False(OtherThreadCanAcquire(probe.Starting));

        using (var held = new HeldMutex(probe.Role))
        {
            probe.Tick(T0.AddMilliseconds(100));
            Assert.Equal(1, probe.Starts);
            Assert.True(OtherThreadCanAcquire(probe.Starting));

            probe.Tick(T0.AddMilliseconds(150));
            Assert.Equal(1, probe.Starts);
            _ = held;
        }
    }

    [Fact]
    public void Tick_retries_on_the_next_check_when_the_start_never_arrives()
    {
        using var probe = new WatchProbe(TimeSpan.FromSeconds(30));

        probe.Tick(T0);
        probe.Tick(T0.AddSeconds(30));
        Assert.Equal(1, probe.Starts);

        probe.Tick(T0.AddSeconds(30).AddMilliseconds(50));
        Assert.Equal(2, probe.Starts);
    }

    [Fact]
    public void Wake_wakes_a_frozen_partner_by_its_held_handle_without_looking_again()
    {
        using Sleeper sleeper = Sleeper.Start();
        int lookups = 0;
        using var waker = new SiblingWaker(() => { lookups++; return [sleeper.Id]; });
        waker.Wake(T0);

        Assert.True(Suspend(sleeper.Id));
        Assert.True(WaitUntil(() => IsFrozen(sleeper.Process), TimeSpan.FromSeconds(2)));

        waker.Wake(T0.AddMilliseconds(50));

        Assert.True(WaitUntil(() => !IsFrozen(sleeper.Process), TimeSpan.FromSeconds(2)));
        Assert.False(sleeper.Process.HasExited);
        Assert.Equal(1, lookups);
    }

    [Fact]
    public void Wake_leaves_a_running_partner_running()
    {
        using Sleeper sleeper = Sleeper.Start();
        using var waker = new SiblingWaker(() => [sleeper.Id]);

        waker.Wake(T0);
        waker.Wake(T0.AddMilliseconds(50));

        Assert.False(IsFrozen(sleeper.Process));
        Assert.False(sleeper.Process.HasExited);
    }

    [Fact]
    public void Wake_looks_through_the_machine_once_while_the_partner_lives_and_then_only_as_a_safety_net()
    {
        using Sleeper sleeper = Sleeper.Start();
        using var waker = new SiblingWaker(() => [sleeper.Id]);

        // Ten seconds of checks at 50 ms: one look, then the partner is held.
        for (int i = 0; i < 199; i++)
        {
            waker.Wake(T0.AddMilliseconds(50 * i));
        }

        Assert.Equal(1, waker.Lookups);
        Assert.Equal([sleeper.Id], waker.Known);

        waker.Wake(T0 + SiblingWaker.SafetyNetInterval);
        Assert.Equal(2, waker.Lookups);
        Assert.Equal([sleeper.Id], waker.Known);
    }

    [Fact]
    public void Wake_looks_again_at_once_when_the_partner_has_exited()
    {
        Sleeper first = Sleeper.Start();
        using Sleeper second = Sleeper.Start();
        int[] running = [first.Id];
        using var waker = new SiblingWaker(() => running);
        waker.Wake(T0);
        Assert.Equal([first.Id], waker.Known);

        first.Dispose();
        running = [second.Id];
        waker.Wake(T0.AddMilliseconds(50));

        Assert.Equal(2, waker.Lookups);
        Assert.Equal([second.Id], waker.Known);
    }

    [Fact]
    public void Wake_looks_at_most_once_a_second_while_no_partner_is_found()
    {
        using var waker = new SiblingWaker(() => []);

        for (int i = 0; i < 40; i++)
        {
            waker.Wake(T0.AddMilliseconds(50 * i));
        }

        // 0 s and 1 s; 2 s is not reached.
        Assert.Equal(2, waker.Lookups);
    }

    /// <summary>
    /// Each role checks its partner 20 times a second, so one check must cost
    /// next to nothing. Runs the check as the roles run it, with a real partner found
    /// through the real process list, and measures this thread's processor time.
    /// </summary>
    [Fact]
    public void A_partner_check_costs_under_200_microseconds_of_processor_time()
    {
        const int Checks = 4000; // 200 s of checks: the safety net and its process list are included.
        // A name of its own: other tests end ping processes by name.
        using Sleeper sleeper = Sleeper.Start("brd-partner-" + Guid.NewGuid().ToString("N"));
        string exe = Environment.ProcessPath!;
        using var held = new HeldMutex(UniqueMutex());
        using var waker = new SiblingWaker(() => SiblingWaker.OtherInstancesOf(sleeper.Process.ProcessName));
        using var watch = new PartnerWatch(held.Name, UniqueMutex(), TimeSpan.FromSeconds(30), () => throw new InvalidOperationException("The partner is running."), waker.Wake);
        var standdown = BrainRotDoctor.App.Runtime.Update.UpdateStanddown.ForCurrentProcess();

        watch.Tick(T0);
        Assert.Contains(sleeper.Id, waker.Known);
        TimeSpan before = ThreadTime();
        for (int i = 1; i <= Checks; i++)
        {
            _ = standdown.IsActive() || UninstallGuard.IsBlocked(exe);
            watch.Tick(T0.AddMilliseconds(50 * i));
        }

        double perCheck = (ThreadTime() - before).TotalMilliseconds * 1000 / Checks;
        Assert.Contains(sleeper.Id, waker.Known);
        _output.WriteLine($"one partner check: {perCheck:0.0} µs of processor time, {waker.Lookups} looks through the machine in {Checks} checks");
        Assert.True(perCheck < 200, $"one partner check took {perCheck:0.0} µs of processor time; the limit is 200 µs (0.4% of one thread at 20 checks a second). Listing every process on the machine costs about 2000 µs.");
    }

    private static TimeSpan ThreadTime()
    {
        Assert.True(GetThreadTimes(GetCurrentThread(), out _, out _, out long kernel, out long user));
        return TimeSpan.FromTicks(kernel + user);
    }

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetCurrentThread();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetThreadTimes(IntPtr thread, out long creation, out long exit, out long kernel, out long user);

    private static string UniqueMutex() => $@"Local\BrainRotDoctor.Test.{Guid.NewGuid():N}";

    private static bool OtherThreadCanAcquire(string name)
    {
        bool acquired = false;
        var thread = new Thread(() =>
        {
            using var mutex = new Mutex(initiallyOwned: false, name);
            acquired = mutex.WaitOne(0);
            if (acquired)
            {
                mutex.ReleaseMutex();
            }
        });
        thread.Start();
        thread.Join();
        return acquired;
    }

    private static bool WaitUntil(Func<bool> condition, TimeSpan limit)
    {
        long started = Environment.TickCount64;
        while (Environment.TickCount64 - started < limit.TotalMilliseconds)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(20);
        }

        return condition();
    }

    private static bool Suspend(int processId)
    {
        IntPtr handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_SUSPEND_RESUME, false, (uint)processId);
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return NtSuspendProcess(handle) == 0;
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }

    private static bool IsFrozen(Process process)
    {
        process.Refresh();
        bool sawThread = false;
        foreach (ProcessThread thread in process.Threads)
        {
            sawThread = true;
            IntPtr handle = OpenThread(ThreadQueryInformation, false, (uint)thread.Id);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                int status = NtQueryInformationThread(handle, ThreadSuspendCount, out int count, sizeof(int), IntPtr.Zero);
                if (status != 0 || count == 0)
                {
                    return false;
                }
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        return sawThread;
    }

    private const int ThreadQueryInformation = 0x0040;
    private const int ThreadSuspendCount = 35;

    [LibraryImport("ntdll.dll")]
    private static partial int NtSuspendProcess(IntPtr processHandle);

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationThread(
        IntPtr threadHandle,
        int threadInformationClass,
        out int threadInformation,
        int threadInformationLength,
        IntPtr returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenThread(
        int dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);

    /// <summary>Holds a mutex on another thread. Same-thread ownership would look free to a recursive wait.</summary>
    private sealed class HeldMutex : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEvent _held = new(false);
        private readonly ManualResetEvent _release = new(false);

        public HeldMutex(string name)
        {
            Name = name;
            _thread = new Thread(() =>
            {
                using var mutex = new Mutex(initiallyOwned: true, name);
                _held.Set();
                _release.WaitOne();
            });
            _thread.Start();
            _held.WaitOne();
        }

        public string Name { get; }

        public void Dispose()
        {
            _release.Set();
            _thread.Join();
            _held.Dispose();
            _release.Dispose();
        }
    }

    private sealed class WatchProbe : IDisposable
    {
        private readonly PartnerWatch _watch;

        public WatchProbe(TimeSpan? timeout = null)
        {
            Role = UniqueMutex();
            Starting = UniqueMutex();
            _watch = new PartnerWatch(Role, Starting, timeout ?? TimeSpan.FromSeconds(30), () => Starts++, _ => { });
        }

        public string Role { get; }

        public string Starting { get; }

        public int Starts { get; private set; }

        public void Tick(DateTimeOffset now) => _watch.Tick(now);

        public void Dispose() => _watch.Dispose();
    }

    private sealed class Sleeper : IDisposable
    {
        private readonly Process _process;
        private readonly string? _copy;

        private Sleeper(Process process, string? copy)
        {
            _process = process;
            _copy = copy;
        }

        public Process Process => _process;

        public int Id => _process.Id;

        /// <summary>Starts ping for 30 s; under <paramref name="name"/> when given, from a copy of its own.</summary>
        public static Sleeper Start(string? name = null)
        {
            string ping = Path.Combine(Environment.SystemDirectory, "ping.exe");
            string? copy = null;
            if (name is not null)
            {
                copy = Path.Combine(Path.GetTempPath(), name);
                Directory.CreateDirectory(copy);
                File.Copy(ping, Path.Combine(copy, name + ".exe"));
                ping = Path.Combine(copy, name + ".exe");
            }

            var info = new ProcessStartInfo
            {
                FileName = ping,
                Arguments = "-n 30 127.0.0.1",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process process = System.Diagnostics.Process.Start(info)
                ?? throw new InvalidOperationException("ping did not start.");
            var sleeper = new Sleeper(process, copy);
            try
            {
                Assert.True(WaitUntil(() =>
                {
                    process.Refresh();
                    return !process.HasExited && process.Threads.Count > 0;
                }, TimeSpan.FromSeconds(2)));
                return sleeper;
            }
            catch
            {
                sleeper.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }

            if (_copy is not null)
            {
                _process.WaitForExit(2000);
                try
                {
                    Directory.Delete(_copy, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }

            _process.Dispose();
        }
    }
}
