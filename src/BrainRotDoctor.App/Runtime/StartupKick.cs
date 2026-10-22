using BrainRotDoctor.App.Runtime.Update;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// The repeating task's launch. If the app is already running, an update is
/// replacing the exe, or uninstall has started, exits. Otherwise starts the app
/// in the tray, outside the task, and exits.
/// </summary>
internal static class StartupKick
{
    public static void Run()
    {
        string? exe = Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (exe is null || UninstallGuard.IsBlocked(exe) || UpdateInProgress(exe))
        {
            return;
        }

        if (ProcessProtector.IsPrimaryAlive())
        {
            return;
        }

        DetachedStarter.Start(exe, StartupRegistrar.StartupFlag);
    }

    /// <summary>
    /// True when a swap marker is present or cannot be read. A failed read looks
    /// like "no swap" to the freshness check, and starting then clears the swap.
    /// A stale marker is included: a normal start clears it; this check must not.
    /// </summary>
    private static bool UpdateInProgress(string exe)
    {
        try
        {
            if (File.Exists(UpdatePaths.ForCurrentProcess(exe).MarkerPath))
            {
                return true;
            }

            return UpdateStanddown.ForCurrentProcess().IsActive();
        }
        catch (Exception)
        {
            return true;
        }
    }
}
