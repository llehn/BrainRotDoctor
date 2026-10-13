using System.Diagnostics;

namespace BrainRotDoctor.Starter;

/// <summary>Starts the real app exe, leaving it running if it stays alive.</summary>
internal sealed class ExeStarter : IChildStarter
{
    private readonly string _exe;
    private readonly string[] _args;

    public ExeStarter(string exe, string[] args)
    {
        _exe = exe;
        _args = args;
    }

    public IChildProcess Start()
    {
        var info = new ProcessStartInfo(_exe)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = Path.GetDirectoryName(_exe) ?? Environment.CurrentDirectory,
        };
        foreach (string arg in _args)
        {
            info.ArgumentList.Add(arg);
        }

        Process process = Process.Start(info)
            ?? throw new InvalidOperationException("Could not start " + _exe);
        return new RealChildProcess(process);
    }
}

/// <summary>
/// Does not kill or dispose the underlying process: after a successful start
/// the child must outlive this starter.
/// </summary>
internal sealed class RealChildProcess : IChildProcess
{
    private readonly Process _process;

    public RealChildProcess(Process process) => _process = process;

    public bool WaitForExit(int milliseconds) => _process.WaitForExit(milliseconds);

    public void Dispose()
    {
        // Intentionally not disposing _process: Dispose can race a still-running child.
    }
}
