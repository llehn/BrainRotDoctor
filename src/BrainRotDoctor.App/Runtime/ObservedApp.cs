using BrainRotDoctor.Core.Apps;

namespace BrainRotDoctor.App.Runtime;

/// <summary>One process of an app: its id and when it started, so a reused id is never mistaken for it.</summary>
internal readonly record struct AppProcess(uint Id, long Started);

/// <summary>An app with at least one window shown (perhaps minimized), as seen this tick.</summary>
internal sealed class ObservedApp
{
    public ObservedApp(AppIdentity identity, IReadOnlyList<IntPtr> windows, IReadOnlyList<AppProcess> processes, bool onScreen, bool isForeground)
    {
        Identity = identity;
        Windows = windows;
        Processes = processes;
        OnScreen = onScreen;
        IsForeground = isForeground;
    }

    public AppIdentity Identity { get; }

    /// <summary>Its shown top-level windows, minimized ones included.</summary>
    public IReadOnlyList<IntPtr> Windows { get; }

    /// <summary>The processes owning those windows.</summary>
    public IReadOnlyList<AppProcess> Processes { get; }

    /// <summary>A window of it is on the current screen: shown, not minimized, on this virtual desktop.</summary>
    public bool OnScreen { get; }

    /// <summary>It owns the window you're using.</summary>
    public bool IsForeground { get; }

    /// <summary>
    /// It uses time and can be blocked: a Steam game while it runs with a window at all
    /// (a VR game's window stays minimized; a fullscreen game may minimize), any other
    /// app only while it's on screen, as with browser windows (ADR-010).
    /// </summary>
    public bool Counts => Identity.Kind == AppKind.SteamGame || OnScreen;
}

internal interface IAppObserver
{
    /// <summary>Every app with a shown window right now; browsers, the Windows shell and BrainRotDoctor are never among them.</summary>
    IReadOnlyList<ObservedApp> GetApps();
}

internal enum AppCloseResult
{
    /// <summary>It left the screen when asked (closed, or went to the tray).</summary>
    Closed,

    /// <summary>It didn't go when asked, so its processes were ended.</summary>
    Ended,

    /// <summary>Windows refused: it runs with administrator rights.</summary>
    Denied,

    /// <summary>It stayed, in a window that belongs to no process of its own (a Store app's frame), so nothing could be ended.</summary>
    NotClosed,
}

internal interface IAppCloser
{
    /// <summary>Asks the app to close and, if it stays, ends it (Steam is only asked). Blocks for a few seconds at most.</summary>
    AppCloseResult Close(ObservedApp app);
}
