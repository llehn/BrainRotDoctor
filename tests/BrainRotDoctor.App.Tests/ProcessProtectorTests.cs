using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed partial class ProcessProtectorTests
{
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
    public void ThawProcess_wakes_a_frozen_process()
    {
        using Sleeper sleeper = Sleeper.Start();

        Assert.True(Suspend(sleeper.Id));
        Assert.True(WaitUntil(() => IsFrozen(sleeper.Process), TimeSpan.FromSeconds(2)));

        ProcessProtector.ThawProcess(sleeper.Id);

        Assert.True(WaitUntil(() => !IsFrozen(sleeper.Process), TimeSpan.FromSeconds(2)));
        Assert.False(sleeper.Process.HasExited);
    }

    [Fact]
    public void ThawProcess_leaves_a_running_process_running()
    {
        using Sleeper sleeper = Sleeper.Start();

        ProcessProtector.ThawProcess(sleeper.Id);

        Assert.False(IsFrozen(sleeper.Process));
        Assert.False(sleeper.Process.HasExited);
    }

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
            _thread = new Thread(() =>
            {
                using var mutex = new Mutex(initiallyOwned: true, name);
                _held.Set();
                _release.WaitOne();
            });
            _thread.Start();
            _held.WaitOne();
        }

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
            _watch = new PartnerWatch(Role, Starting, timeout ?? TimeSpan.FromSeconds(30), () => Starts++, () => { });
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

        private Sleeper(Process process) => _process = process;

        public Process Process => _process;

        public int Id => _process.Id;

        public static Sleeper Start()
        {
            var info = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "ping.exe"),
                Arguments = "-n 30 127.0.0.1",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process process = System.Diagnostics.Process.Start(info)
                ?? throw new InvalidOperationException("ping did not start.");
            var sleeper = new Sleeper(process);
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

            _process.Dispose();
        }
    }
}
