using BrainRotDoctor.Core.Apps;
using System.ComponentModel;
using System.Diagnostics;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Closes an app the way a person would, then firmly: every window of it is asked to
/// close (the same as clicking ×), which sends a chat app to its tray and lets a game
/// save or ask. If a window of it is still there a few seconds later, the processes
/// owning those windows are ended — only those, and only while they are still the same
/// processes. Steam is only ever asked: ending it would break the games it runs.
/// No keystrokes are sent and focus is never taken.
/// </summary>
internal sealed class AppCloser : IAppCloser
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(250);

    public AppCloseResult Close(ObservedApp app)
    {
        // Only the windows that make it count: a minimized program elsewhere is left alone.
        foreach (IntPtr window in app.Windows.Where(w => StillThere(app, w)))
        {
            NativeMethods.PostMessage(window, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < Patience)
        {
            if (!app.Windows.Any(w => StillThere(app, w)))
            {
                return AppCloseResult.Closed;
            }

            Thread.Sleep(Poll);
        }

        if (app.Identity.Kind == AppKind.Steam)
        {
            return AppCloseResult.Closed;
        }

        var staying = new HashSet<uint>(app.Windows
            .Where(w => StillThere(app, w))
            .Select(w => NativeMethods.GetWindowThreadProcessId(w, out uint pid) == 0 ? 0 : pid));
        List<AppProcess> ending = app.Processes.Where(p => staying.Contains(p.Id)).ToList();
        if (ending.Count == 0)
        {
            // What stays belongs to no process of its own (a Store app's frame): nothing to end.
            return AppCloseResult.NotClosed;
        }

        bool denied = false;
        foreach (AppProcess process in ending)
        {
            denied |= !End(process);
        }

        return denied ? AppCloseResult.Denied : AppCloseResult.Ended;
    }

    /// <summary>The window still holds the app: a game's even when minimized, any other app's while on screen.</summary>
    private static bool StillThere(ObservedApp app, IntPtr window) =>
        NativeMethods.IsWindow(window)
        && (app.Identity.Kind == AppKind.SteamGame ? NativeMethods.IsWindowVisible(window) : NativeMethods.IsOnScreen(window));

    /// <summary>Ends the process if it is still the one seen (same id, same start); false when Windows refuses.</summary>
    private static bool End(AppProcess process)
    {
        if (NativeMethods.ProcessImage(process.Id) is not { } image || image.Started != process.Started)
        {
            return true;
        }

        try
        {
            using Process running = Process.GetProcessById((int)process.Id);
            running.Kill();
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return true;
        }
    }
}
