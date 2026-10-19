using BrainRotDoctor.App.Runtime.Update;
using System.Diagnostics;

namespace BrainRotDoctor.App.Runtime;

internal sealed class ProcessProtector : IDisposable
{
    private const string PrimaryMutexName = "Local\\BrainRotDoctor.Primary";
    private const string WatchdogMutexName = "Local\\BrainRotDoctor.Watchdog";
    private static readonly TimeSpan WatchInterval = TimeSpan.FromMilliseconds(650);

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
        while (true)
        {
            // A verified update is replacing the installed exe: stand down so the
            // file unlocks. The departing primary schedules the recovery relaunch.
            if (standdown.IsActive())
            {
                return;
            }

            EnsureProcess(PrimaryMutexName, "--role", "primary");
            Thread.Sleep(WatchInterval);
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

            EnsureProcess(WatchdogMutexName, "--role", "watchdog");
            _stop.Token.WaitHandle.WaitOne(WatchInterval);
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
