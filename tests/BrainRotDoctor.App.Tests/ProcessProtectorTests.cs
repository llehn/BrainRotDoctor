using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed partial class ProcessProtectorTests
{
    [Fact]
    public void ThawProcess_wakes_a_frozen_process()
    {
        using Sleeper sleeper = Sleeper.Start();

        Assert.True(Suspend(sleeper.Id), "Could not freeze the sleeper.");
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

    [Fact]
    public void ThawProcess_ignores_a_missing_process()
    {
        ProcessProtector.ThawProcess(-1);
        ProcessProtector.ThawProcess(int.MaxValue);
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

    /// <summary>True when every thread is frozen. A running thread means the process is not.</summary>
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
                int status = NtQueryInformationThread(
                    handle,
                    ThreadSuspendCount,
                    out int count,
                    sizeof(int),
                    IntPtr.Zero);
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

    // THREADINFOCLASS value for the thread's suspend count.
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
