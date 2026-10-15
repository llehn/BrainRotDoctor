using Microsoft.Win32;

namespace BrainRotDoctor.App.Runtime;

internal static class StartupRegistrar
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BrainRotDoctor";

    /// <summary>Marks the sign-in launch, which starts in the tray.</summary>
    public const string StartupFlag = "--started-from-startup";

    public static void EnsureCurrentUserStartup()
    {
        string exe = Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(ValueName, $"\"{exe}\" {StartupFlag}", RegistryValueKind.String);
    }
}
