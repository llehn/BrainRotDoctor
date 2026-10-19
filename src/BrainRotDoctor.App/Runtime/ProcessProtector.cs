using BrainRotDoctor.App.Runtime.Update;
using System.Diagnostics;

namespace BrainRotDoctor.App.Runtime;

internal sealed class ProcessProtector : IDisposable
{
    private const string PrimaryMutexName = "Local\\BrainRotDoctor.Primary";
    private const string WatchdogMutexName = "Local\\BrainRotDoctor.Watchdog";
    private static readonly TimeSpan WatchInterval = TimeSpan.FromMilliseconds(650);

    // A frozen partner still holds its mutex, so EnsureProcess treats it as on duty
    // and will not start a replacement. Wake it directly. Waking a running partner
    // does nothing.
    private static readonly TimeSpan ThawInterval = TimeSpan.FromMilliseconds(50);
    private static readonly int EnsureEvery = (int)(WatchInterval.TotalMilliseconds / ThawInterval.TotalMilliseconds);
    private static readonly string CurrentProcessName = Process.GetCurrentProcess().ProcessName;

    private readonly CancellationTokenSource _stop = new();

    private ProcessProtector()
    {
    }

    public static Mutex AcquirePrimary(out bool ownsMutex) =>
        new(initiallyOwned: true, PrimaryMutexName, out ownsMutex);

    public static void RunWatchdog()
    {
        using Mutex mutex = new(initiallyOwned: true, WatchdogMutexName, out bool ownsMutex);
        if (!ownsMutex)
        {
            return;
        }

        var standdown = UpdateStanddown.ForCurrentProcess();
        var tick = 0;
        while (true)
        {
            // A verified update is replacing the installed exe: stand down so the
            // file unlocks. The departing primary schedules the recovery relaunch.
            if (standdown.IsActive())
            {
                return;
            }

            ThawSiblings();
            if (tick % EnsureEvery == 0)
            {
                EnsureProcess(PrimaryMutexName, "--role", "primary");
            }

            tick++;
            Thread.Sleep(ThawInterval);
        }
    }

    public static ProcessProtector StartForPrimary(string[] args)
    {
        EnsureProcess(WatchdogMutexName, "--role", "watchdog");

        var protector = new ProcessProtector();
        var thread = new Thread(() => protector.RunPrimaryMonitor())
        {
            IsBackground = true,
            Name = "BrainRotDoctor watchdog monitor",
        };

        thread.Start();
        return protector;
    }

    public void Dispose()
    {
        _stop.Cancel();
    }

    private void RunPrimaryMonitor()
    {
        var standdown = UpdateStanddown.ForCurrentProcess();
        var tick = 0;
        while (!_stop.IsCancellationRequested)
        {
            // A verified update needs this process to release the installed exe.
            // Schedule a safety-net relaunch (in case the swap stalls) and exit so
            // the applier can overwrite the binary; the new build then takes over.
            if (standdown.IsActive())
            {
                standdown.ScheduleSafetyNetRelaunch();
                Environment.Exit(0);
            }

            ThawSiblings();
            if (tick % EnsureEvery == 0)
            {
                EnsureProcess(WatchdogMutexName, "--role", "watchdog");
            }

            tick++;
            if (_stop.Token.WaitHandle.WaitOne(ThawInterval))
            {
                return;
            }
        }
    }

    /// <summary>Wakes every other instance of this executable. A running instance is left alone.</summary>
    internal static void ThawSiblings()
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(CurrentProcessName);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return;
        }

        foreach (Process process in processes)
        {
            try
            {
                if (process.Id != Environment.ProcessId)
                {
                    ThawProcess(process.Id);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    /// <summary>Undoes a process-wide freeze. Does nothing if the process is already running or gone.</summary>
    internal static void ThawProcess(int processId)
    {
        if (processId <= 0 || processId == Environment.ProcessId)
        {
            return;
        }

        IntPtr handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_SUSPEND_RESUME, false, (uint)processId);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            NativeMethods.NtResumeProcess(handle);
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }

    private static void EnsureProcess(string mutexName, params string[] processArgs)
    {
        using var probe = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        probe.ReleaseMutex();
        StartSibling(processArgs);
    }

    private static void StartSibling(params string[] args)
    {
        string exe = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        var info = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
        };

        info.ArgumentList.Add("/d");
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add("start");
        info.ArgumentList.Add("");
        info.ArgumentList.Add(exe);
        foreach (string arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        Process.Start(info);
    }
}
