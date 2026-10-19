namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Answers "is a live process holding this mutex?" An abandoned mutex is not a
/// live holder: its process is already gone.
/// </summary>
internal static class RoleMutex
{
    public static bool IsHeld(string name)
    {
        using var mutex = new Mutex(initiallyOwned: false, name, out bool createdNew);
        if (createdNew)
        {
            return false;
        }

        try
        {
            if (!mutex.WaitOne(0))
            {
                return true;
            }
        }
        catch (AbandonedMutexException)
        {
            // The owner died without releasing. We now own it, and nobody is running.
        }

        mutex.ReleaseMutex();
        return false;
    }

    /// <summary>Takes the mutex if no live owner has it, including when the previous owner died. Null if a live owner holds it.</summary>
    public static Mutex? TryAcquire(string name)
    {
        var mutex = new Mutex(initiallyOwned: false, name);
        try
        {
            try
            {
                if (!mutex.WaitOne(0))
                {
                    mutex.Dispose();
                    return null;
                }
            }
            catch (AbandonedMutexException)
            {
                // Previous owner died. Ownership is ours.
            }

            return mutex;
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
    }
}

/// <summary>
/// One check for one partner. Wakes it if frozen. If it has exited, starts a
/// replacement and holds <i>a different mutex</i> until that replacement has
/// taken the role mutex, so a second check does not start another copy. The
/// role mutex still means only "this role is running," which is why the new
/// process is allowed to take it.
/// </summary>
internal sealed class PartnerWatch : IDisposable
{
    private readonly string _roleMutexName;
    private readonly string _startMutexName;
    private readonly TimeSpan _startTimeout;
    private readonly Action _start;
    private readonly Action _thaw;

    private Mutex? _startGuard;
    private DateTimeOffset _startedAt;

    public PartnerWatch(string roleMutexName, string startMutexName, TimeSpan startTimeout, Action start, Action thaw)
    {
        _roleMutexName = roleMutexName;
        _startMutexName = startMutexName;
        _startTimeout = startTimeout;
        _start = start;
        _thaw = thaw;
    }

    public void Tick(DateTimeOffset now)
    {
        _thaw();

        if (_startGuard is not null)
        {
            // Drop the guard once the new process has the role, or if it never
            // arrived. The next check is what tries again; this pass does not
            // start a second copy.
            if (RoleMutex.IsHeld(_roleMutexName) || now - _startedAt >= _startTimeout)
            {
                ReleaseStart();
            }

            return;
        }

        if (RoleMutex.IsHeld(_roleMutexName))
        {
            return;
        }

        _startGuard = RoleMutex.TryAcquire(_startMutexName);
        if (_startGuard is null)
        {
            return;
        }

        _startedAt = now;
        try
        {
            _start();
        }
        catch
        {
            ReleaseStart();
            throw;
        }
    }

    public void ReleaseStart()
    {
        if (_startGuard is null)
        {
            return;
        }

        _startGuard.ReleaseMutex();
        _startGuard.Dispose();
        _startGuard = null;
    }

    public void Dispose() => ReleaseStart();
}
