using System.ComponentModel;
using System.Diagnostics;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Starts an exe and retries while it dies inside the host-failure window
/// (native unpack then immediate load). Used after Install, when our code is
/// already running and can wait on the child.
/// </summary>
internal static class ProcessLaunchRetry
{
    public const int FailureWindowMs = 2000;
    public const int MaxAttempts = 20;
    public const int DelayMs = 500;

    public static void Start(string exe)
    {
        Exception? last = null;
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                Process? process = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                if (process is null)
                {
                    Thread.Sleep(DelayMs);
                    continue;
                }

                if (!process.WaitForExit(FailureWindowMs))
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                last = ex;
            }

            if (attempt + 1 < MaxAttempts)
            {
                Thread.Sleep(DelayMs);
            }
        }

        throw last ?? new InvalidOperationException("Could not start " + exe);
    }
}
