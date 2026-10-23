using Microsoft.Win32;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Keeps sign-in registration in place for as long as the app is installed.
/// The Startup apps page does not delete the login entry; it sets a separate
/// off mark, and that mark is put back while we are running. A repeating task
/// starts the app at sign-in and again every minute if it is dead. Uninstall
/// removes both. A hook that fails the same way three times is not asked again
/// until the next start.
/// </summary>
internal static class StartupRegistrar
{
    /// <summary>Marks the sign-in launch, which starts in the tray.</summary>
    public const string StartupFlag = "--started-from-startup";

    /// <summary>
    /// A short-lived launch used by the repeating task. It starts the app if
    /// needed and exits, so the task does not own the running process.
    /// </summary>
    public const string KickFlag = "--startup-kick";

    private static readonly byte[] EnabledMark = [0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    private static readonly object Gate = new();

    /// <summary>The same failure this many times in a row retires the hook for this run.</summary>
    internal const int SameFailureLimit = 3;

    private static readonly Dictionary<string, int> AttemptCounts = new();
    private static readonly Dictionary<string, HookStrike> Strikes = new();

    public static void EnsureCurrentUserStartup(bool includeRepeatingTask)
    {
        string exe = Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        if (UninstallGuard.IsBlocked(exe))
        {
            return;
        }

        InstallOptions options = InstallOptions.Default(exe);
        EnsureSignIn(options, exe);
        if (includeRepeatingTask)
        {
            EnsureRepeatingTask(options, exe, task: null);
        }
    }

    /// <summary>Login entry and off switch. Cheap, and the switch can be flipped while someone watches.</summary>
    public static void EnsureSignIn(InstallOptions options, string exePath)
    {
        lock (Gate)
        {
            string command = LaunchCommand(exePath);
            TryHook(options.RunKeyPath, () => WriteLoginEntry(options, command));
            TryHook(options.StartupApprovedKeyPath, () => RepairOffSwitch(options));
        }
    }

    /// <summary>The repeating task. Asked at start and once a minute, not on the switch's clock.</summary>
    public static void EnsureRepeatingTask(InstallOptions options, string exePath, IStartupTask? task)
    {
        lock (Gate)
        {
            TryHook(TaskHook(options), () => WriteRepeatingTask(options, exePath, task));
        }
    }

    public static void Ensure(InstallOptions options, string exePath, IStartupTask? task = null)
    {
        EnsureSignIn(options, exePath);
        EnsureRepeatingTask(options, exePath, task);
    }

    /// <summary>How many times this run actually asked a hook. A remembered refusal does not count again.</summary>
    internal static int Attempts(string hook)
    {
        lock (Gate)
        {
            return AttemptCounts.GetValueOrDefault(hook);
        }
    }

    public static void Remove(InstallOptions options, IStartupTask? task = null)
    {
        lock (Gate)
        {
            // A locked list must not keep the others, or the task, after uninstall.
            TryStep(() => DeleteValue(options.RunKeyPath, options.RunValueName));
            TryStep(() => DeleteValue(options.StartupApprovedKeyPath, options.RunValueName));
            TryStep(() => DeleteValue(InstallOptions.LeftoverHiddenList, options.RunValueName));
            TryStep(() => TaskFor(options, task)?.Delete());
        }
    }

    /// <summary>The command the login entries run. Quoted so a path with spaces still starts in the tray.</summary>
    public static string LaunchCommand(string exePath) => $"\"{exePath}\" {StartupFlag}";

    /// <summary>True when the Startup apps off mark is set. A missing mark means the entry is on.</summary>
    public static bool IsTurnedOff(byte[]? mark)
    {
        if (mark is null || mark.Length == 0)
        {
            return false;
        }

        // 02 and 06 are the two "on" stamps Windows writes. Anything else is off.
        return mark[0] is not 0x02 and not 0x06;
    }

    /// <summary>True when the repeating task is missing, off, or no longer points at this install.</summary>
    public static bool NeedsTask(StartupTaskStatus status, string exePath)
        => !status.Exists
            || !status.Enabled
            || status.EnabledTriggers < 1
            || !string.Equals(status.Command, exePath, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(status.Arguments?.Trim(), KickFlag, StringComparison.Ordinal);

    private static void TryHook(string hook, Action attempt)
    {
        if (Strikes.TryGetValue(hook, out HookStrike? strike) && strike.Stopped)
        {
            return;
        }

        AttemptCounts[hook] = AttemptCounts.GetValueOrDefault(hook) + 1;
        try
        {
            attempt();
            Strikes.Remove(hook);
        }
        catch (Exception ex)
        {
            NoteFailure(hook, ex);
        }
    }

    private static void NoteFailure(string hook, Exception ex)
    {
        string kind = ex.GetType().FullName + ":" + ex.HResult;
        HookStrike strike = Strikes.GetValueOrDefault(hook) ?? new HookStrike();
        if (strike.Kind == kind)
        {
            strike.Count++;
        }
        else
        {
            strike.Kind = kind;
            strike.Count = 1;
        }

        strike.Stopped = strike.Count >= SameFailureLimit;
        Strikes[hook] = strike;
    }

    private sealed class HookStrike
    {
        public string? Kind { get; set; }
        public int Count { get; set; }
        public bool Stopped { get; set; }
    }

    private static void WriteLoginEntry(InstallOptions options, string command)
    {
        using RegistryKey run = Registry.CurrentUser.CreateSubKey(options.RunKeyPath);
        if (!string.Equals(run.GetValue(options.RunValueName) as string, command, StringComparison.Ordinal))
        {
            run.SetValue(options.RunValueName, command, RegistryValueKind.String);
        }
    }

    private static void RepairOffSwitch(InstallOptions options)
    {
        using RegistryKey approved = Registry.CurrentUser.CreateSubKey(options.StartupApprovedKeyPath);
        if (IsTurnedOff(approved.GetValue(options.RunValueName) as byte[]))
        {
            approved.SetValue(options.RunValueName, EnabledMark, RegistryValueKind.Binary);
        }
    }

    private static string TaskHook(InstallOptions options)
        => "task:" + (options.TaskName ?? options.RunKeyPath);

    private static void TryStep(Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException)
        {
        }
    }

    private static void DeleteValue(string keyPath, string valueName)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    private static void WriteRepeatingTask(InstallOptions options, string exePath, IStartupTask? task)
    {
        IStartupTask? store = TaskFor(options, task);
        if (store is null)
        {
            return;
        }

        if (NeedsTask(store.Read(), exePath))
        {
            store.Write(exePath);
        }
    }

    private static IStartupTask? TaskFor(InstallOptions options, IStartupTask? task)
        => task ?? (options.TaskName is null ? null : new ScheduledLogonTask(options.TaskName));
}
