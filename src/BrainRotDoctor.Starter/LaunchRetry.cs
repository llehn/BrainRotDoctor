namespace BrainRotDoctor.Starter;

/// <summary>A process the retry loop can wait on without owning its lifetime.</summary>
internal interface IChildProcess : IDisposable
{
    /// <summary>True if the process exited within <paramref name="milliseconds"/>.</summary>
    bool WaitForExit(int milliseconds);
}

/// <summary>Starts one attempt at the real app.</summary>
internal interface IChildStarter
{
    IChildProcess Start();
}

/// <summary>
/// Starts the app and retries while it dies inside the host-failure window
/// (unpack then immediate load). A process that is still alive after that
/// window is treated as started; the caller must not kill it.
/// </summary>
internal static class LaunchRetry
{
    public const int FailureWindowMs = 2000;
    public const int MaxAttempts = 20;
    public const int DelayMs = 500;

    public static IChildProcess RunUntilAlive(
        IChildStarter starter,
        int maxAttempts = MaxAttempts,
        int failureWindowMs = FailureWindowMs,
        int delayMs = DelayMs,
        Action<int>? sleep = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        sleep ??= static ms => Thread.Sleep(ms);

        IChildProcess? last = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            last?.Dispose();
            last = starter.Start();
            if (!last.WaitForExit(failureWindowMs))
            {
                return last;
            }

            if (attempt + 1 < maxAttempts)
            {
                sleep(delayMs);
            }
        }

        last?.Dispose();
        throw new InvalidOperationException("BrainRotDoctor could not start. Try again.");
    }
}
