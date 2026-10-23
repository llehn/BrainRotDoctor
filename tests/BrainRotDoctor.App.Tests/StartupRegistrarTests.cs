using BrainRotDoctor.App.Runtime;
using Microsoft.Win32;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class StartupRegistrarTests : IDisposable
{
    private readonly string _root = $@"Software\BrainRotDoctorTests\{Guid.NewGuid():N}";
    private readonly InstallOptions _options;
    private readonly RecordingStartupTask _task = new();
    private readonly List<string> _locked = new();

    /// <summary>The off stamp Startup apps wrote on the machine where the switch stayed off.</summary>
    private static readonly byte[] LiveOffMark = [0x01, 0x00, 0x00, 0x00, 0x68, 0xD1, 0xC0, 0xA7, 0xC3, 0x62, 0xDD, 0x01];

    public StartupRegistrarTests()
    {
        _options = new InstallOptions
        {
            SourceExe = @"C:\apps\Brain Rot Doctor.exe",
            InstallDir = @"C:\apps",
            RunKeyPath = _root + @"\Run",
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
    public void Ensure_writes_the_login_entry_puts_the_off_switch_back_and_writes_the_task()
    {
        using (RegistryKey approved = Registry.CurrentUser.CreateSubKey(_options.StartupApprovedKeyPath))
        {
            approved.SetValue(_options.RunValueName, new byte[] { 0x03, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
        }

        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);

        string command = StartupRegistrar.LaunchCommand(_options.SourceExe);
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        using RegistryKey? mark = Registry.CurrentUser.OpenSubKey(_options.StartupApprovedKeyPath);
        Assert.Equal(command, run!.GetValue(_options.RunValueName));
        Assert.False(StartupRegistrar.IsTurnedOff((byte[])mark!.GetValue(_options.RunValueName)!));
        Assert.Equal(1, _task.Writes);
    }

    [Fact]
    public void The_same_failure_three_times_stops_that_hook_for_this_run()
    {
        WriteOffMark(LiveOffMark);
        DenyWrites(_options.StartupApprovedKeyPath);

        for (int i = 0; i < StartupRegistrar.SameFailureLimit; i++)
        {
            TryEnsure(_options);
        }

        int asked = StartupRegistrar.Attempts(_options.StartupApprovedKeyPath);
        Assert.Equal(StartupRegistrar.SameFailureLimit, asked);

        TryEnsure(_options);
        TryEnsure(_options);

        Assert.Equal(asked, StartupRegistrar.Attempts(_options.StartupApprovedKeyPath));
        Assert.Equal(1, _task.Writes);
    }

    [Fact]
    public void A_different_failure_starts_the_count_again()
    {
        var failing = new FailingTask { Error = new UnauthorizedAccessException() };
        string hook = "task:" + _options.RunKeyPath;
        StartupRegistrar.EnsureRepeatingTask(_options, _options.SourceExe, failing);
        StartupRegistrar.EnsureRepeatingTask(_options, _options.SourceExe, failing);
        failing.Error = new IOException("different");
        StartupRegistrar.EnsureRepeatingTask(_options, _options.SourceExe, failing);

        int asked = StartupRegistrar.Attempts(hook);
        Assert.Equal(3, asked);

        StartupRegistrar.EnsureRepeatingTask(_options, _options.SourceExe, failing);
        StartupRegistrar.EnsureRepeatingTask(_options, _options.SourceExe, failing);
        Assert.Equal(5, StartupRegistrar.Attempts(hook));

        StartupRegistrar.EnsureRepeatingTask(_options, _options.SourceExe, failing);
        Assert.Equal(5, StartupRegistrar.Attempts(hook));
    }

    [Fact]
    public void A_working_off_switch_is_still_checked_every_pass()
    {
        WriteOffMark(LiveOffMark);
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);
        int asked = StartupRegistrar.Attempts(_options.StartupApprovedKeyPath);

        WriteOffMark(LiveOffMark);
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);

        Assert.True(StartupRegistrar.Attempts(_options.StartupApprovedKeyPath) > asked);
        using RegistryKey? mark = Registry.CurrentUser.OpenSubKey(_options.StartupApprovedKeyPath);
        Assert.False(StartupRegistrar.IsTurnedOff((byte[])mark!.GetValue(_options.RunValueName)!));
    }

    [Fact]
    public void A_locked_off_switch_does_not_stop_the_repeating_task_being_put_back()
    {
        WriteOffMark(LiveOffMark);
        DenyWrites(_options.StartupApprovedKeyPath);

        TryEnsure(_options);

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
    public void A_locked_off_switch_does_not_stop_uninstall_removing_the_rest()
    {
        StartupRegistrar.Ensure(_options, _options.SourceExe, _task);
        DenyWrites(_options.StartupApprovedKeyPath);

        StartupRegistrar.Remove(_options, _task);

        Assert.Equal(1, _task.Deletes);
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        Assert.True(run is null || run.GetValue(_options.RunValueName) is null);
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
        using RegistryKey? mark = Registry.CurrentUser.OpenSubKey(_options.StartupApprovedKeyPath);
        Assert.True(run is null || run.GetValue(_options.RunValueName) is null);
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
        foreach (string path in _locked)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    path,
                    RegistryRights.ChangePermissions | RegistryRights.ReadKey)!;
                RegistrySecurity security = key.GetAccessControl();
                security.RemoveAccessRuleAll(new RegistryAccessRule(
                    WindowsIdentity.GetCurrent().User!,
                    RegistryRights.CreateSubKey | RegistryRights.SetValue,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Deny));
                key.SetAccessControl(security);
            }
            catch (Exception)
            {
            }
        }

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_root, throwOnMissingSubKey: false);
        }
        catch (Exception)
        {
        }
    }

    private void WriteOffMark(byte[] mark)
    {
        using RegistryKey approved = Registry.CurrentUser.CreateSubKey(_options.StartupApprovedKeyPath);
        approved.SetValue(_options.RunValueName, mark, RegistryValueKind.Binary);
    }

    /// <summary>The live refusal: the key can be read, and creating or writing under it is denied.</summary>
    private void DenyWrites(string subKeyPath)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(subKeyPath);
        RegistrySecurity security = key.GetAccessControl();
        security.AddAccessRule(new RegistryAccessRule(
            WindowsIdentity.GetCurrent().User!,
            RegistryRights.CreateSubKey | RegistryRights.SetValue,
            InheritanceFlags.None,
            PropagationFlags.None,
            AccessControlType.Deny));
        key.SetAccessControl(security);
        _locked.Add(subKeyPath);
    }

    private void TryEnsure(InstallOptions options)
    {
        try
        {
            StartupRegistrar.Ensure(options, options.SourceExe, _task);
        }
        catch (UnauthorizedAccessException)
        {
            // The repair gives up here today. The asserts are what should have happened anyway.
        }
    }

    private sealed class FailingTask : IStartupTask
    {
        public Exception Error { get; set; } = new InvalidOperationException();

        public StartupTaskStatus Read() => throw Error;

        public void Write(string exePath) => throw Error;

        public void Delete()
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
