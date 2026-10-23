namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Puts sign-in registration back while the app is running. The off switch is
/// checked every second. The repeating task is checked at start and once a minute.
/// </summary>
internal sealed class StartupKeeper : IDisposable
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    public const int RepeatingTaskEvery = 60;

    private readonly System.Threading.Timer _timer;
    private int _running;
    private int _ticks;

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

    /// <summary>True on the first pass and every minute after. The switch is not on this clock.</summary>
    public static bool RepeatingTaskDue(int tick) => tick % RepeatingTaskEvery == 0;

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

            StartupRegistrar.EnsureCurrentUserStartup(includeRepeatingTask: RepeatingTaskDue(_ticks));
            _ticks++;
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
