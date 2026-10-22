using System.Security;
using System.Security.Principal;

namespace BrainRotDoctor.App.Runtime;

/// <summary>What the repeating sign-in task looks like right now.</summary>
internal readonly record struct StartupTaskStatus(
    bool Exists,
    bool Enabled,
    string? Command,
    string? Arguments,
    int EnabledTriggers);

/// <summary>The repeating sign-in task. Tests substitute their own.</summary>
internal interface IStartupTask
{
    StartupTaskStatus Read();

    void Write(string exePath);

    void Delete();
}

/// <summary>
/// A per-user task that starts the app at sign-in and again every minute.
/// It is not on the Startup apps page. The action is a short kick that exits
/// immediately, so ending or disabling the task does not kill the app, and
/// the minute repeat still fires while the app is running.
/// </summary>
internal sealed class ScheduledLogonTask : IStartupTask
{
    private const int CreateOrUpdate = 6;
    private const int InteractiveToken = 3;

    private readonly string _name;

    public ScheduledLogonTask(string name) => _name = name;

    public StartupTaskStatus Read()
    {
        dynamic? task = Find();
        if (task is null)
        {
            return default;
        }

        dynamic definition = task.Definition;
        dynamic actions = definition.Actions;
        string? command = null;
        string? arguments = null;
        if ((int)actions.Count >= 1)
        {
            dynamic action = actions.Item(1);
            command = (string?)action.Path;
            arguments = (string?)action.Arguments;
        }

        int enabledTriggers = 0;
        dynamic triggers = definition.Triggers;
        int count = (int)triggers.Count;
        for (int i = 1; i <= count; i++)
        {
            if ((bool)triggers.Item(i).Enabled)
            {
                enabledTriggers++;
            }
        }

        return new StartupTaskStatus(true, (bool)task.Enabled, command, arguments, enabledTriggers);
    }

    public void Write(string exePath)
    {
        dynamic folder = Folder();
        folder.RegisterTask(
            _name,
            Xml(exePath),
            CreateOrUpdate,
            Type.Missing,
            Type.Missing,
            InteractiveToken,
            Type.Missing);
    }

    public void Delete()
    {
        try
        {
            Folder().DeleteTask(_name, 0);
        }
        catch (Exception ex) when (IsMissing(ex))
        {
        }
    }

    /// <summary>The task definition. Internal so a test can lock the shape without registering.</summary>
    internal static string Xml(string exePath, DateTimeOffset? now = null)
    {
        DateTimeOffset clock = now ?? DateTimeOffset.Now;
        string soon = Boundary(clock.AddMinutes(1));
        string midnight = Boundary(clock.ToLocalTime().Date.AddDays(1));
        string user = XmlText(WindowsIdentity.GetCurrent().Name);
        string command = XmlText(exePath);
        string kick = XmlText(StartupRegistrar.KickFlag);
        // Both repeats start in the future, so registering the task does not launch
        // the exe that is on disk right now (an update may still be the old build).
        // The minute repeat covers the rest of today. The daily one covers every
        // day after that, and catches a midnight the machine slept through.
        return $"""
            <?xml version="1.0"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Description>Starts BrainRotDoctor at sign-in while it is installed.</Description>
              </RegistrationInfo>
              <Triggers>
                <LogonTrigger>
                  <Enabled>true</Enabled>
                  <UserId>{user}</UserId>
                </LogonTrigger>
                <TimeTrigger>
                  <StartBoundary>{soon}</StartBoundary>
                  <Enabled>true</Enabled>
                  <Repetition>
                    <Interval>PT1M</Interval>
                    <Duration>PT23H59M</Duration>
                    <StopAtDurationEnd>false</StopAtDurationEnd>
                  </Repetition>
                </TimeTrigger>
                <CalendarTrigger>
                  <StartBoundary>{midnight}</StartBoundary>
                  <Enabled>true</Enabled>
                  <ScheduleByDay>
                    <DaysInterval>1</DaysInterval>
                  </ScheduleByDay>
                  <Repetition>
                    <Interval>PT1M</Interval>
                    <Duration>PT23H59M</Duration>
                    <StopAtDurationEnd>false</StopAtDurationEnd>
                  </Repetition>
                </CalendarTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>LeastPrivilege</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <AllowHardTerminate>true</AllowHardTerminate>
                <StartWhenAvailable>true</StartWhenAvailable>
                <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                <AllowStartOnDemand>true</AllowStartOnDemand>
                <Enabled>true</Enabled>
                <Hidden>true</Hidden>
                <RunOnlyIfIdle>false</RunOnlyIfIdle>
                <WakeToRun>false</WakeToRun>
                <ExecutionTimeLimit>PT1M</ExecutionTimeLimit>
                <Priority>7</Priority>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{command}</Command>
                  <Arguments>{kick}</Arguments>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    internal static string Boundary(DateTimeOffset instant)
        => instant.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

    private dynamic? Find()
    {
        try
        {
            return Folder().GetTask(_name);
        }
        catch (Exception ex) when (IsMissing(ex))
        {
            return null;
        }
    }

    private static dynamic Folder()
    {
        Type? type = Type.GetTypeFromProgID("Schedule.Service");
        if (type is null)
        {
            throw new InvalidOperationException("Task Scheduler is not available.");
        }

        dynamic service = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Task Scheduler is not available.");
        service.Connect();
        return service.GetFolder("\\");
    }

    // The binder surfaces a missing task as FileNotFoundException, not COMException.
    private static bool IsMissing(Exception ex)
        => ex.HResult is unchecked((int)0x80070002) or unchecked((int)0x80070003) or unchecked((int)0x8004130F);

    private static string XmlText(string value) => SecurityElement.Escape(value) ?? "";
}
