using BrainRotDoctor.App.Runtime;
using Microsoft.Win32;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class InstallerTests : IDisposable
{
    private readonly string _root;
    private readonly string _sourceExe;
    private readonly string _testRegRoot;
    private readonly InstallOptions _options;

    public InstallerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "brd-install-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        _sourceExe = Path.Combine(_root, "BrainRotDoctor.exe");
        File.WriteAllBytes(_sourceExe, new byte[] { 1, 2, 3, 4 }); // stand-in payload

        _testRegRoot = $@"Software\BrainRotDoctorTests\{Guid.NewGuid():N}";
        _options = new InstallOptions
        {
            SourceExe = _sourceExe,
            InstallDir = Path.Combine(_root, "install"),
            AppDataDir = Path.Combine(_root, "appdata"),
            RunKeyPath = _testRegRoot + @"\Run",
            StartupApprovedKeyPath = _testRegRoot + @"\StartupApproved\Run",
            TaskName = null,
            UninstallKeyPath = _testRegRoot + @"\Uninstall\BrainRotDoctor",
        };
    }

    [Fact]
    public void Install_copies_exe_and_writes_registry()
    {
        var installer = new Installer(_options);
        installer.Install();

        Assert.True(File.Exists(installer.InstalledExePath));

        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        Assert.NotNull(run);
        Assert.Equal(
            $"\"{installer.InstalledExePath}\" {StartupRegistrar.StartupFlag}",
            run!.GetValue(_options.RunValueName));

        using RegistryKey? un = Registry.CurrentUser.OpenSubKey(_options.UninstallKeyPath);
        Assert.NotNull(un);
        Assert.Equal("BrainRotDoctor", un!.GetValue("DisplayName"));
        Assert.Equal($"\"{installer.InstalledExePath}\" --uninstall", un.GetValue("UninstallString"));
    }

    [Fact]
    public void RemoveRegistration_clears_run_and_uninstall_entries()
    {
        var installer = new Installer(_options);
        installer.Install();
        installer.RemoveRegistration();

        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        Assert.True(run is null || run.GetValue(_options.RunValueName) is null);
        Assert.Null(Registry.CurrentUser.OpenSubKey(_options.UninstallKeyPath));
    }

    [Fact]
    public void RemoveAppData_deletes_the_data_directory()
    {
        Directory.CreateDirectory(_options.AppDataDir!);
        File.WriteAllText(Path.Combine(_options.AppDataDir!, "config.json"), "{}");

        new Installer(_options).RemoveAppData();

        Assert.False(Directory.Exists(_options.AppDataDir));
    }

    [Fact]
    public void IsRunningFrom_matches_install_path_case_insensitively()
    {
        var installer = new Installer(_options);
        Assert.True(Installer.IsRunningFrom(installer.InstalledExePath, installer.InstalledExePath.ToUpperInvariant()));
        Assert.False(Installer.IsRunningFrom(installer.InstalledExePath, _sourceExe));
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_testRegRoot, throwOnMissingSubKey: false);
        }
        catch (Exception)
        {
        }

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (Exception)
        {
        }
    }
}
