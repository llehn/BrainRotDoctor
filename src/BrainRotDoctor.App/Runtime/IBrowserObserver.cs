namespace BrainRotDoctor.App.Runtime;

internal interface IBrowserObserver
{
    IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs();

    /// <summary>The page in the selected tab of one browser window right now; null when unknown or the window is gone.</summary>
    Uri? ReadSelectedUrl(IntPtr windowHandle);
}
