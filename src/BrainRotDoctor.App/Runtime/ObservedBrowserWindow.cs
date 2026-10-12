namespace BrainRotDoctor.App.Runtime;

internal sealed class ObservedBrowserWindow
{
    public ObservedBrowserWindow(string windowId, IntPtr windowHandle, string browserName, Uri? url, bool isForeground = false)
    {
        WindowId = windowId;
        WindowHandle = windowHandle;
        BrowserName = browserName;
        Url = url;
        IsForeground = isForeground;
    }

    public string WindowId { get; }
    public IntPtr WindowHandle { get; }
    public string BrowserName { get; }
    public Uri? Url { get; }

    /// <summary>The window you're using right now (the foreground window).</summary>
    public bool IsForeground { get; }
}
