using Microsoft.Win32;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>Paths and identity used to install / uninstall (injectable for tests).</summary>
internal sealed class InstallOptions
{
    public required string SourceExe { get; init; }
    public required string InstallDir { get; init; }
    public string ExeName { get; init; } = "BrainRotDoctor.exe";
    public string? AppDataDir { get; init; }

    public string RunKeyPath { get; init; } = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public string RunValueName { get; init; } = "BrainRotDoctor";

    /// <summary>
    /// An earlier build tried to write this list. A normal user cannot. Uninstall
    /// still clears a leftover so it cannot start the app after removal.
    /// </summary>
    public const string LeftoverHiddenList =
        @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run";

    /// <summary>The Startup apps off mark for the login entry. Tests point this at a private key.</summary>
    public string StartupApprovedKeyPath { get; init; } =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    /// <summary>Repeating sign-in task. Null skips it, so tests do not register a real task.</summary>
    public string? TaskName { get; init; } = "BrainRotDoctor";

    public string UninstallKeyPath { get; init; } =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\BrainRotDoctor";

    public string DisplayName { get; init; } = "BrainRotDoctor";
    public string Version { get; init; } = AppVersion.Current.ToString();
    public string Publisher { get; init; } = "BrainRotDoctor";

    public static InstallOptions Default(string sourceExe)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new InstallOptions
        {
            SourceExe = sourceExe,
            InstallDir = Path.Combine(local, "Programs", "BrainRotDoctor"),
            AppDataDir = Path.Combine(local, "BrainRotDoctor"),
        };
    }
}

/// <summary>
/// A per-user (no admin) installer: it copies the single-file executable into
/// <c>%LOCALAPPDATA%\Programs</c>, registers current-user sign-in, and creates
/// the "Apps &amp; features" uninstall entry whose UninstallString re-invokes the
/// app with <c>--uninstall</c>. Because uninstall is our own code, it can refuse
/// while strict mode is active (the friction the product is built on).
/// </summary>
internal sealed class Installer
{
    private readonly InstallOptions _o;

    public Installer(InstallOptions options) => _o = options;

    public string InstalledExePath => Path.Combine(_o.InstallDir, _o.ExeName);

    public bool IsInstalled() => File.Exists(InstalledExePath);

    public static bool IsRunningFrom(string installedExePath, string currentExe)
        => string.Equals(
            Path.GetFullPath(installedExePath),
            Path.GetFullPath(currentExe),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Copies the executable and writes the registry entries.</summary>
    public void Install()
    {
        Directory.CreateDirectory(_o.InstallDir);
        File.Copy(_o.SourceExe, InstalledExePath, overwrite: true);
        WriteRegistration();
    }

    /// <summary>
    /// Writes sign-in registration and the "Apps &amp; features" uninstall entry.
    /// This does not need the exe lock, so it is safe to call to refresh the
    /// recorded version while an older build is still running and the binary
    /// itself is being swapped through the update applier.
    /// </summary>
    public void WriteRegistration()
    {
        Directory.CreateDirectory(_o.InstallDir);
        UninstallGuard.Clear(InstalledExePath);
        StartupRegistrar.Ensure(_o, InstalledExePath);

        using RegistryKey uninstall = Registry.CurrentUser.CreateSubKey(_o.UninstallKeyPath);
        uninstall.SetValue("DisplayName", _o.DisplayName);
        uninstall.SetValue("DisplayVersion", _o.Version);
        uninstall.SetValue("Publisher", _o.Publisher);
        uninstall.SetValue("DisplayIcon", InstalledExePath);
        uninstall.SetValue("InstallLocation", _o.InstallDir);
        uninstall.SetValue("UninstallString", $"\"{InstalledExePath}\" --uninstall");
        uninstall.SetValue("NoModify", 1, RegistryValueKind.DWord);
        uninstall.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    /// <summary>Removes sign-in registration and the uninstall entry.</summary>
    public void RemoveRegistration()
    {
        try
        {
            StartupRegistrar.Remove(_o);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
            or System.Security.SecurityException
            or IOException)
        {
        }

        Registry.CurrentUser.DeleteSubKeyTree(_o.UninstallKeyPath, throwOnMissingSubKey: false);
    }

    /// <summary>Deletes the application data directory (config, strict-mode state).</summary>
    public void RemoveAppData()
    {
        if (!string.IsNullOrWhiteSpace(_o.AppDataDir) && Directory.Exists(_o.AppDataDir))
        {
            Directory.Delete(_o.AppDataDir, recursive: true);
        }
    }

    /// <summary>The install directory to remove once the process exits.</summary>
    public string InstallDir => _o.InstallDir;
}
