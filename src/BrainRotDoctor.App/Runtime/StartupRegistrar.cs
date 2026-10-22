using Microsoft.Win32;

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
            EnsureRegistry(options, exePath);
            EnsureTask(options, exePath, task);
        }
    }

    public static void Remove(InstallOptions options, IStartupTask? task = null)
    {
        lock (Gate)
        {
            RemoveRegistry(options);
            TaskFor(options, task)?.Delete();
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

    private static void EnsureRegistry(InstallOptions options, string exePath)
    {
        string command = LaunchCommand(exePath);

        using (RegistryKey run = Registry.CurrentUser.CreateSubKey(options.RunKeyPath))
        {
            if (!string.Equals(run.GetValue(options.RunValueName) as string, command, StringComparison.Ordinal))
            {
                run.SetValue(options.RunValueName, command, RegistryValueKind.String);
            }
        }

        using (RegistryKey policy = Registry.CurrentUser.CreateSubKey(options.PolicyRunKeyPath))
        {
            if (!string.Equals(policy.GetValue(options.RunValueName) as string, command, StringComparison.Ordinal))
            {
                policy.SetValue(options.RunValueName, command, RegistryValueKind.String);
            }
        }

        using RegistryKey approved = Registry.CurrentUser.CreateSubKey(options.StartupApprovedKeyPath);
        if (IsTurnedOff(approved.GetValue(options.RunValueName) as byte[]))
        {
            approved.SetValue(options.RunValueName, EnabledMark, RegistryValueKind.Binary);
        }
    }

    private static void RemoveRegistry(InstallOptions options)
    {
        using (RegistryKey? run = Registry.CurrentUser.OpenSubKey(options.RunKeyPath, writable: true))
        {
            run?.DeleteValue(options.RunValueName, throwOnMissingValue: false);
        }

        using (RegistryKey? policy = Registry.CurrentUser.OpenSubKey(options.PolicyRunKeyPath, writable: true))
        {
            policy?.DeleteValue(options.RunValueName, throwOnMissingValue: false);
        }

        using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(options.StartupApprovedKeyPath, writable: true);
        approved?.DeleteValue(options.RunValueName, throwOnMissingValue: false);
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
