namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Puts sign-in registration back while the app is running. A switch that is
/// only repaired at our own start stays off across a sign-out.
/// </summary>
internal sealed class StartupKeeper : IDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly System.Threading.Timer _timer;
    private int _running;

    private StartupKeeper()
    {
        _timer = new System.Threading.Timer(_ => Tick(), null, Interval, Interval);
    }

    public static StartupKeeper Start()
    {
        var keeper = new StartupKeeper();
        keeper.Tick();
        return keeper;
    }

    public void Dispose() => _timer.Dispose();

    private void Tick()
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }

        try
        {
            string? exe = Environment.ProcessPath
                ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe is not null && UninstallGuard.IsBlocked(exe))
            {
                Environment.Exit(0);
            }

            StartupRegistrar.EnsureCurrentUserStartup();
        }
        catch (Exception)
        {
            // The next tick tries again. A failed repair must not take the app down.
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
