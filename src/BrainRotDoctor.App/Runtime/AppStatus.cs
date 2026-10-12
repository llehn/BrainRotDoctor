using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Activity;

namespace BrainRotDoctor.App.Runtime;

internal sealed class AppStatus
{
    public AppStatus(
        DateTimeOffset updatedAt,
        bool isRunning,
        string configSource,
        IReadOnlyList<ObservedBrowserWindow> windows,
        IReadOnlyList<RuleSnapshot> rules,
        IReadOnlyList<CloseEvent> recentClosures,
        StrictModeSnapshot strictMode,
        PauseState? pause,
        string? lastError,
        ActivityRecord? activity = null)
    {
        Activity = activity ?? new ActivityRecord();
        UpdatedAt = updatedAt;
        IsRunning = isRunning;
        ConfigSource = configSource;
        Windows = windows;
        Rules = rules;
        RecentClosures = recentClosures;
        StrictMode = strictMode;
        Pause = pause is not null && pause.IsActiveAt(updatedAt) ? pause : null;
        LastError = lastError;
    }

    /// <summary>The active pause, or null while blocking runs normally.</summary>
    public PauseState? Pause { get; }

    public bool IsPaused => Pause is not null;

    public DateTimeOffset UpdatedAt { get; }
    public bool IsRunning { get; }
    public string ConfigSource { get; }
    public IReadOnlyList<ObservedBrowserWindow> Windows { get; }
    public IReadOnlyList<RuleSnapshot> Rules { get; }
    public IReadOnlyList<CloseEvent> RecentClosures { get; }
    public StrictModeSnapshot StrictMode { get; }
    public string? LastError { get; }

    /// <summary>Today's browsing statistics: totals per place, never a history.</summary>
    public ActivityRecord Activity { get; }
}

internal sealed class CloseEvent
{
    public CloseEvent(DateTimeOffset closedAt, string browser, Uri url, string ruleId)
    {
        ClosedAt = closedAt;
        Browser = browser;
        Url = url;
        RuleId = ruleId;
    }

    public DateTimeOffset ClosedAt { get; }
    public string Browser { get; }
    public Uri Url { get; }
    public string RuleId { get; }
}
