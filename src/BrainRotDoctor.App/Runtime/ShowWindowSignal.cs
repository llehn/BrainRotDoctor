namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Lets a second launch ask the running primary to show its window. The app
/// usually sits in the tray, so opening it again from the Start menu must bring
/// the existing window up rather than silently doing nothing.
/// </summary>
internal static class ShowWindowSignal
{
    private const string EventName = "Local\\BrainRotDoctor.ShowWindow";

    /// <summary>Asks the running primary to show its window.</summary>
    public static void Raise()
    {
        try
        {
            using EventWaitHandle signal = EventWaitHandle.OpenExisting(EventName);
            signal.Set();
        }
        catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
        {
            // An older primary doesn't listen; nothing to do.
        }
    }

    /// <summary>Calls <paramref name="onShow"/> (on a pool thread) every time another launch raises the signal.</summary>
    public static IDisposable Listen(Action onShow)
    {
        var signal = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        RegisteredWaitHandle registration = ThreadPool.RegisterWaitForSingleObject(
            signal, (_, _) => onShow(), null, Timeout.Infinite, executeOnlyOnce: false);
        return new Listener(signal, registration);
    }

    private sealed class Listener(EventWaitHandle signal, RegisteredWaitHandle registration) : IDisposable
    {
        public void Dispose()
        {
            registration.Unregister(null);
            signal.Dispose();
        }
    }
}
