namespace BrainRotDoctor.App.Runtime;

internal sealed class ObservedBrowserWindow
{
    public ObservedBrowserWindow(
        string windowId,
        IntPtr windowHandle,
        string browserName,
        Uri? url,
        bool isForeground = false,
        bool isPrivate = false)
    {
        WindowId = windowId;
        WindowHandle = windowHandle;
        BrowserName = browserName;
        Url = url;
        IsForeground = isForeground;
        IsPrivate = isPrivate;
    }

    public string WindowId { get; }
    public IntPtr WindowHandle { get; }
    public string BrowserName { get; }
    public Uri? Url { get; }

    /// <summary>The window you're using right now (the foreground window).</summary>
    public bool IsForeground { get; }

    /// <summary>
    /// A private session (Incognito, InPrivate, Private Browsing). Still blocked
    /// when a rule says so; never counted on Status.
    /// </summary>
    public bool IsPrivate { get; }
}
