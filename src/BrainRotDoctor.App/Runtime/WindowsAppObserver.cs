using BrainRotDoctor.Core.Apps;
using System.Diagnostics;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Finds the apps with a shown window by looking at the top-level windows, as Alt-Tab
/// does: shown, titled, not a tool window, not click-through or fully transparent (game
/// and chat overlays). Each window's process is recognized once (by id and start time):
/// a program in a Steam games folder is that Steam game, the Steam client is Steam,
/// anything else is a program known by its file name and description.
/// </summary>
internal sealed class WindowsAppObserver : IAppObserver
{
    /// <summary>
    /// Never apps: the Windows shell and its hosts (File Explorer runs in the shell's own
    /// process), shared runtimes that run many different programs, overlays, and Steam's
    /// helpers that belong to running games.
    /// </summary>
    private static readonly HashSet<string> NeverApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer.exe", "applicationframehost.exe", "shellexperiencehost.exe", "startmenuexperiencehost.exe",
        "searchhost.exe", "searchapp.exe", "textinputhost.exe", "lockapp.exe", "logonui.exe", "consent.exe",
        "dwm.exe", "shellhost.exe", "sihost.exe", "ctfmon.exe", "rundll32.exe", "dllhost.exe", "conhost.exe",
        "openwith.exe", "pickerhost.exe", "credentialuibroker.exe", "systemsettingsadminflows.exe",
        "securityhealthsystray.exe", "widgets.exe", "msedgewebview2.exe", "dotnet.exe",
        "java.exe", "javaw.exe", "python.exe", "pythonw.exe", "node.exe", "wscript.exe", "cscript.exe", "mshta.exe",
        "cmd.exe", "powershell.exe", "pwsh.exe",
        "gameoverlayui.exe", "gameoverlayui64.exe", "streaming_client.exe", "steamservice.exe",
        "nvidia overlay.exe", "nvcontainer.exe",
    };

    private static readonly HashSet<string> SteamClient = new(StringComparer.OrdinalIgnoreCase) { "steam.exe", "steamwebhelper.exe" };

    private readonly string _ownProgram = Environment.ProcessPath ?? "";
    private readonly uint _ownProcess = (uint)Environment.ProcessId;
    private readonly Dictionary<AppProcess, AppIdentity?> _known = new();
    private readonly Dictionary<string, (string?, string?)> _descriptions = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ObservedApp> GetApps()
    {
        IntPtr foreground = NativeMethods.GetForegroundWindow();
        var groups = new Dictionary<string, (AppIdentity App, List<IntPtr> Windows, HashSet<AppProcess> Processes, bool OnScreen, bool Front)>(StringComparer.Ordinal);
        var seen = new HashSet<AppProcess>();

        // Each process is asked for its program file once per look, however many windows it has.
        var images = new Dictionary<uint, (string Path, long Started)?>();
        (string Path, long Started)? Image(uint pid)
        {
            if (!images.TryGetValue(pid, out (string Path, long Started)? image))
            {
                image = NativeMethods.ProcessImage(pid);
                images[pid] = image;
            }

            return image;
        }

        NativeMethods.EnumWindows((window, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(window, out uint pid);

            // Our own windows first: asking them for a title would wait on our own UI.
            if (pid == 0 || pid == _ownProcess || !IsAppWindow(window) || Image(pid) is not { } image)
            {
                return true;
            }

            // A browser window is the browsers' to watch, recognized exactly as they recognize it.
            if (UiAutomationBrowserObserver.BrowserName(pid, image) is not null)
            {
                return true;
            }

            if (Path.GetFileName(image.Path).Equals("applicationframehost.exe", StringComparison.OrdinalIgnoreCase))
            {
                // A Store app is shown inside a frame host; the app is the process inside.
                pid = NativeMethods.ChildProcessOtherThan(window, pid);
                if (pid == 0 || Image(pid) is not { } inside)
                {
                    return true;
                }

                image = inside;
            }

            if (Recognize(pid, image) is not { } found)
            {
                return true;
            }

            (AppIdentity app, AppProcess process) = found;
            seen.Add(process);
            bool onScreen = NativeMethods.IsOnScreen(window);
            if (!groups.TryGetValue(app.Key, out var group))
            {
                group = (app, new List<IntPtr>(), new HashSet<AppProcess>(), false, false);
            }

            group.Windows.Add(window);
            group.Processes.Add(process);
            groups[app.Key] = group with { OnScreen = group.OnScreen || onScreen, Front = group.Front || window == foreground };
            return true;
        }, IntPtr.Zero);

        foreach (AppProcess gone in _known.Keys.Where(p => !seen.Contains(p)).ToList())
        {
            _known.Remove(gone);
        }

        return groups.Values
            .Select(g => new ObservedApp(g.App, g.Windows, g.Processes.ToList(), g.OnScreen, g.Front))
            .ToList();
    }

    /// <summary>What Alt-Tab would list: shown, titled, sized, not a tool window, not see-through.</summary>
    private static bool IsAppWindow(IntPtr window)
    {
        if (!NativeMethods.IsWindowVisible(window) || NativeMethods.GetWindowTitle(window).Length == 0)
        {
            return false;
        }

        long style = NativeMethods.ExtendedStyle(window);
        if ((style & NativeMethods.WS_EX_TRANSPARENT) != 0
            || ((style & NativeMethods.WS_EX_TOOLWINDOW) != 0 && (style & NativeMethods.WS_EX_APPWINDOW) == 0)
            || NativeMethods.IsInvisibleLayer(window))
        {
            return false;
        }

        return NativeMethods.IsIconic(window)
            || (NativeMethods.GetWindowRect(window, out NativeMethods.RECT r) && r.Right - r.Left > 1 && r.Bottom - r.Top > 1);
    }

    private (AppIdentity App, AppProcess Process)? Recognize(uint pid, (string Path, long Started) image)
    {
        var process = new AppProcess(pid, image.Started);
        if (!_known.TryGetValue(process, out AppIdentity? app))
        {
            app = Identify(image.Path);
            _known[process] = app;
        }

        return app is null ? null : (app, process);
    }

    /// <summary>What a program file is as an app; null for browsers, the shell, shared runtimes and BrainRotDoctor itself.</summary>
    internal AppIdentity? Identify(string path)
    {
        string exe = Path.GetFileName(path).ToLowerInvariant();
        if (NeverApps.Contains(exe)
            || UiAutomationBrowserObserver.SupportedBrowsers.ContainsKey(Path.GetFileNameWithoutExtension(exe))
            || string.Equals(path, _ownProgram, StringComparison.OrdinalIgnoreCase)
            || exe.StartsWith("brainrotdoctor", StringComparison.Ordinal))
        {
            return null;
        }

        if (SteamClient.Contains(exe))
        {
            return new AppIdentity(AppKind.Steam, "Steam", exe);
        }

        (string? description, string? product) = Described(path);
        if (SteamInstall.SteamGameFolder(path) is { } folder)
        {
            return new AppIdentity(AppKind.SteamGame, SteamInstall.NameOf(folder), exe, description, folder);
        }

        return new AppIdentity(AppKind.Program, description ?? product ?? Path.GetFileNameWithoutExtension(path), exe, description);
    }

    /// <summary>The program file's description ("Discord"), which a renamed copy keeps, and its product name.</summary>
    private (string? Description, string? Product) Described(string path)
    {
        if (!_descriptions.TryGetValue(path, out (string?, string?) described))
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                described = (Clean(info.FileDescription), Clean(info.ProductName));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                described = (null, null);
            }

            _descriptions[path] = described;
        }

        return described;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
