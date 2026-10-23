using Microsoft.Win32;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Keeps sign-in registration in place for as long as the app is installed.
/// The Startup apps page does not delete the login entry; it sets a separate
/// off mark, and that mark is put back while we are running. A second sign-in
/// registration that page does not control, and a task that tries again every
/// minute, still start the app if the visible entry is off. Uninstall removes
/// all of them.
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

    // A hook Windows refused. Remembered for this run only, so the one-second
    // repair does not pay for the same denial again. The next start tries once more.
    private static readonly HashSet<string> Refused = new();
    private static readonly Dictionary<string, int> AttemptCounts = new();

    public static void EnsureCurrentUserStartup()
    {
        string exe = Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        if (UninstallGuard.IsBlocked(exe))
        {
            return;
        }

        Ensure(InstallOptions.Default(exe), exe);
    }

    public static void Ensure(InstallOptions options, string exePath, IStartupTask? task = null)
    {
        lock (Gate)
        {
            string command = LaunchCommand(exePath);
            TryHook(options.RunKeyPath, () => WriteLoginEntry(options, command));
            TryHook(options.StartupApprovedKeyPath, () => RepairOffSwitch(options));
            TryHook(options.PolicyRunKeyPath, () => WriteHiddenList(options, command));
            TryHook(TaskHook(options), () => EnsureTask(options, exePath, task));
        }
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
            TryStep(() => DeleteValue(options.PolicyRunKeyPath, options.RunValueName));
            TryStep(() => DeleteValue(options.StartupApprovedKeyPath, options.RunValueName));
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
        if (Refused.Contains(hook))
        {
            return;
        }

        AttemptCounts[hook] = AttemptCounts.GetValueOrDefault(hook) + 1;
        try
        {
            attempt();
        }
        catch (Exception ex) when (IsRefusal(ex))
        {
            Refused.Add(hook);
        }
        catch (Exception)
        {
            // This tick skips this hook only. A refusal is remembered; anything else is asked again.
        }
    }

    private static bool IsRefusal(Exception ex)
        => ex is UnauthorizedAccessException or System.Security.SecurityException
            || (ex is System.Runtime.InteropServices.COMException && ex.HResult == unchecked((int)0x80070005));

    private static void WriteLoginEntry(InstallOptions options, string command)
    {
        using RegistryKey run = Registry.CurrentUser.CreateSubKey(options.RunKeyPath);
        if (!string.Equals(run.GetValue(options.RunValueName) as string, command, StringComparison.Ordinal))
        {
            run.SetValue(options.RunValueName, command, RegistryValueKind.String);
        }
    }

    private static void WriteHiddenList(InstallOptions options, string command)
    {
        using RegistryKey policy = Registry.CurrentUser.CreateSubKey(options.PolicyRunKeyPath);
        if (!string.Equals(policy.GetValue(options.RunValueName) as string, command, StringComparison.Ordinal))
        {
            policy.SetValue(options.RunValueName, command, RegistryValueKind.String);
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

    private static void EnsureTask(InstallOptions options, string exePath, IStartupTask? task)
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
