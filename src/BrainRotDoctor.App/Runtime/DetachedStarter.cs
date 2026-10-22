namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Starts the app from the sign-in task without the task owning it. The task
/// runs inside a job that kills every child when the task exits, and breaking
/// out of that job is not allowed, so a normal launch dies with the check.
/// WMI creates the process outside the job.
/// </summary>
internal static class DetachedStarter
{
    public static void Start(string exePath, string arguments)
    {
        Type? type = Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
        if (type is null)
        {
            throw new InvalidOperationException("WMI is not available.");
        }

        dynamic locator = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("WMI is not available.");
        dynamic services = locator.ConnectServer(".", @"root\cimv2");
        dynamic processClass = services.Get("Win32_Process");
        dynamic input = processClass.Methods_.Item("Create").InParameters.SpawnInstance_();
        input.CommandLine = $"\"{exePath}\" {arguments}";
        dynamic output = processClass.ExecMethod_("Create", input);
        if ((int)output.ReturnValue != 0)
        {
            throw new InvalidOperationException("Could not start the app.");
        }
    }
}
