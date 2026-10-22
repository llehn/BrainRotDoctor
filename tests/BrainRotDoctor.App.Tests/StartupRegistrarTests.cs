using BrainRotDoctor.App.Runtime;
using Microsoft.Win32;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class StartupRegistrarTests : IDisposable
{
    private readonly string _root = $@"Software\BrainRotDoctorTests\{Guid.NewGuid():N}";
    private readonly InstallOptions _options;
    private readonly RecordingStartupTask _task = new();

    public StartupRegistrarTests()
    {
        _options = new InstallOptions
        {
            SourceExe = @"C:\apps\Brain Rot Doctor.exe",
            InstallDir = @"C:\apps",
            RunKeyPath = _root + @"\Run",
            PolicyRunKeyPath = _root + @"\Policies\Explorer\Run",
            StartupApprovedKeyPath = _root + @"\StartupApproved\Run",
            TaskName = null,
        };
    }

    [Fact]
    public void LaunchCommand_quotes_the_path_and_starts_in_the_tray()
    {
        Assert.Equal(
            "\"C:\\apps\\Brain Rot Doctor.exe\" --started-from-startup",
            StartupRegistrar.LaunchCommand(@"C:\apps\Brain Rot Doctor.exe"));
    }

    [Fact]
    public void IsTurnedOff_treats_only_the_on_stamps_as_on()
    {
        Assert.False(StartupRegistrar.IsTurnedOff(null));
        Assert.False(StartupRegistrar.IsTurnedOff([]));
        Assert.False(StartupRegistrar.IsTurnedOff([0x02, 0x00]));
        Assert.False(StartupRegistrar.IsTurnedOff([0x06, 0x00, 0x00, 0x00]));
        Assert.True(StartupRegistrar.IsTurnedOff([0x03, 0x00, 0x00, 0x00, 0x01]));
    }

    [Fact]
    public void Ensure_writes_both_sign_in_entries_and_puts_an_off_mark_back()
    {
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(_options.StartupApprovedKeyPath))
        {
            approved.SetValue(_options.RunValueName, new byte[] { 0x03, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
        }

        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);

        string command = StartupRegistrar.LaunchCommand(_options.SourceExe);
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        using RegistryKey? policy = Registry.CurrentUser.OpenSubKey(_options.PolicyRunKeyPath);
        using RegistryKey? mark = Registry.CurrentUser.OpenSubKey(_options.StartupApprovedKeyPath);
        Assert.Equal(command, run!.GetValue(_options.RunValueName));
        Assert.Equal(command, policy!.GetValue(_options.RunValueName));
        Assert.False(StartupRegistrar.IsTurnedOff((byte[])mark!.GetValue(_options.RunValueName)!));
        Assert.Equal(1, _task.Writes);
    }

    [Fact]
    public void Ensure_leaves_an_on_mark_alone_and_does_not_rewrite_a_healthy_task()
    {
        byte[] on = [0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(_options.StartupApprovedKeyPath))
        {
            approved.SetValue(_options.RunValueName, on, RegistryValueKind.Binary);
        }

        _task.Status = new StartupTaskStatus(true, true, _options.SourceExe, StartupRegistrar.KickFlag, 2);
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);

        using RegistryKey? mark = Registry.CurrentUser.OpenSubKey(_options.StartupApprovedKeyPath);
        Assert.Equal(on, (byte[])mark!.GetValue(_options.RunValueName)!);
        Assert.Equal(0, _task.Writes);
    }

    [Fact]
    public void Ensure_rewrites_a_task_that_was_turned_off_or_points_elsewhere()
    {
        _task.Status = new StartupTaskStatus(true, false, _options.SourceExe, StartupRegistrar.KickFlag, 1);
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);
        Assert.Equal(1, _task.Writes);

        _task.Status = new StartupTaskStatus(true, true, @"C:\other\BrainRotDoctor.exe", StartupRegistrar.KickFlag, 1);
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);
        Assert.Equal(2, _task.Writes);
    }

    [Fact]
    public void Remove_clears_every_sign_in_entry_and_the_task()
    {
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(_options.StartupApprovedKeyPath))
        {
            approved.SetValue(_options.RunValueName, new byte[] { 0x02 }, RegistryValueKind.Binary);
        }

        StartupRegistrar.Remove(_options, _task);

        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        using RegistryKey? policy = Registry.CurrentUser.OpenSubKey(_options.PolicyRunKeyPath);
        using RegistryKey? mark = Registry.CurrentUser.OpenSubKey(_options.StartupApprovedKeyPath);
        Assert.True(run is null || run.GetValue(_options.RunValueName) is null);
        Assert.True(policy is null || policy.GetValue(_options.RunValueName) is null);
        Assert.True(mark is null || mark.GetValue(_options.RunValueName) is null);
        Assert.Equal(1, _task.Deletes);
    }

    [Theory]
    [InlineData(false, true, @"C:\app\BrainRotDoctor.exe", "--startup-kick", 1, true)]
    [InlineData(true, false, @"C:\app\BrainRotDoctor.exe", "--startup-kick", 1, true)]
    [InlineData(true, true, @"C:\app\BrainRotDoctor.exe", "--startup-kick", 0, true)]
    [InlineData(true, true, @"C:\other\BrainRotDoctor.exe", "--startup-kick", 1, true)]
    [InlineData(true, true, @"C:\app\BrainRotDoctor.exe", "--started-from-startup", 1, true)]
    [InlineData(true, true, @"C:\app\BrainRotDoctor.exe", "--startup-kick", 2, false)]
    public void NeedsTask_is_false_only_when_the_task_still_starts_this_install(
        bool exists, bool enabled, string command, string arguments, int triggers, bool needs)
    {
        var status = new StartupTaskStatus(exists, enabled, command, arguments, triggers);
        Assert.Equal(needs, StartupRegistrar.NeedsTask(status, @"C:\app\BrainRotDoctor.exe"));
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_root, throwOnMissingSubKey: false);
        }
        catch (Exception)
        {
        }
    }

    private sealed class RecordingStartupTask : IStartupTask
    {
        public StartupTaskStatus Status { get; set; }
        public int Writes { get; private set; }
        public int Deletes { get; private set; }

        public StartupTaskStatus Read() => Status;

        public void Write(string exePath)
        {
            Writes++;
            Status = new StartupTaskStatus(true, true, exePath, StartupRegistrar.KickFlag, 2);
        }

        public void Delete() => Deletes++;
    }
}
