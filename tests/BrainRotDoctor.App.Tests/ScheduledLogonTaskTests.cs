using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class ScheduledLogonTaskTests : IDisposable
{
    private readonly string _name = @"BrainRotDoctorTests-" + Guid.NewGuid().ToString("N");
    private readonly ScheduledLogonTask _task;
    private readonly string _exe = @"C:\Program Files\BrainRotDoctor\BrainRotDoctor.exe";

    public ScheduledLogonTaskTests() => _task = new ScheduledLogonTask(_name);

    [Fact]
    public void Xml_hides_the_task_repeats_every_minute_and_escapes_the_path()
    {
        var now = new DateTimeOffset(2026, 10, 22, 15, 0, 0, TimeSpan.FromHours(2));
        string xml = ScheduledLogonTask.Xml(@"C:\apps\Brain & Rot\BrainRotDoctor.exe", now);

        Assert.Contains("<Hidden>true</Hidden>", xml, StringComparison.Ordinal);
        Assert.Contains("<Interval>PT1M</Interval>", xml, StringComparison.Ordinal);
        Assert.Contains(ScheduledLogonTask.Boundary(now.AddMinutes(1)), xml, StringComparison.Ordinal);
        Assert.Contains("<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>", xml, StringComparison.Ordinal);
        Assert.Contains("<StartWhenAvailable>true</StartWhenAvailable>", xml, StringComparison.Ordinal);
        Assert.Contains("Brain &amp; Rot", xml, StringComparison.Ordinal);
        Assert.Contains("<Arguments>--startup-kick</Arguments>", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("2020-01-01", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("UserInitMprLogonScript", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_registers_a_hidden_task_and_puts_a_disabled_one_back()
    {
        Assert.False(_task.Read().Exists);

        _task.Write(_exe);
        StartupTaskStatus created = _task.Read();
        Assert.True(created.Exists);
        Assert.True(created.Enabled);
        Assert.Equal(_exe, created.Command);
        Assert.Equal(StartupRegistrar.KickFlag, created.Arguments);
        Assert.True(created.EnabledTriggers >= 1);
        string registered = RegisteredXml();
        Assert.Contains("PT1M", registered, StringComparison.Ordinal);
        Assert.Contains("<Hidden>true</Hidden>", registered, StringComparison.Ordinal);
        Assert.Contains("<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>", registered, StringComparison.Ordinal);
        Assert.Contains("<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>", registered, StringComparison.Ordinal);
        Assert.Contains("<StartWhenAvailable>true</StartWhenAvailable>", registered, StringComparison.Ordinal);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), registered, StringComparison.Ordinal);
        Assert.False(HasRun());

        Disable();
        Assert.False(_task.Read().Enabled);

        _task.Write(_exe);
        Assert.True(_task.Read().Enabled);
        Assert.Equal(_exe, _task.Read().Command);
    }

    [Fact]
    public void Delete_removes_the_task()
    {
        _task.Write(_exe);
        _task.Delete();
        _task.Delete();

        Assert.False(_task.Read().Exists);
    }

    public void Dispose()
    {
        try
        {
            _task.Delete();
        }
        catch (Exception)
        {
        }
    }

    private void Disable() => WithTask(task => task.Enabled = false);

    private string RegisteredXml()
    {
        string xml = "";
        WithTask(task => xml = (string)task.Xml);
        return xml;
    }

    private bool HasRun()
    {
        DateTime last = DateTime.MinValue;
        WithTask(task => last = (DateTime)task.LastRunTime);
        return last > DateTime.Now.AddMinutes(-2);
    }

    private void WithTask(Action<dynamic> action)
    {
        Type type = Type.GetTypeFromProgID("Schedule.Service")!;
        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        action(service.GetFolder("\\").GetTask(_name));
    }
}
