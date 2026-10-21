using BrainRotDoctor.Core.Apps;

namespace BrainRotDoctor.Core.Accounting;

/// <summary>
/// The decision to close one browser window's selected tab because the site it
/// shows is blocked by a rule.
/// </summary>
public sealed class CloseDecision
{
    public CloseDecision(string windowId, Uri url, string ruleId)
    {
        WindowId = windowId;
        Url = url;
        RuleId = ruleId;
    }

    public string WindowId { get; }

    public Uri Url { get; }

    /// <summary>The rule that triggered the close.</summary>
    public string RuleId { get; }
}

/// <summary>The decision to close an app because a rule blocks it.</summary>
public sealed record AppCloseDecision(AppIdentity App, string RuleId);

/// <summary>A block coming for an app: when, and by which rule.</summary>
public sealed record BlockAhead(DateTimeOffset At, string RuleId);

/// <summary>Inspectable state of one rule at a point in time, for display.</summary>
public sealed class RuleSnapshot
{
    public RuleSnapshot(
        string ruleId,
        string name,
        bool isActive,
        bool blocksCompletely,
        bool isBlocking,
        TimeSpan allowance,
        TimeSpan consumed,
        DateTimeOffset? hourResetsAt,
        DateTimeOffset? activeWindowEndsAt,
        bool wasActiveThisTick)
    {
        RuleId = ruleId;
        Name = name;
        IsActive = isActive;
        BlocksCompletely = blocksCompletely;
        IsBlocking = isBlocking;
        Allowance = allowance;
        Consumed = consumed;
        HourResetsAt = hourResetsAt;
        ActiveWindowEndsAt = activeWindowEndsAt;
        WasActiveThisTick = wasActiveThisTick;
    }

    public string RuleId { get; }

    public string Name { get; }

    /// <summary>True if the rule is in effect right now (window + days).</summary>
    public bool IsActive { get; }

    public bool BlocksCompletely { get; }

    /// <summary>True if the rule's sites are blocked right now.</summary>
    public bool IsBlocking { get; }

    public TimeSpan Allowance { get; }

    public TimeSpan Consumed { get; }

    public TimeSpan Remaining => Consumed >= Allowance ? TimeSpan.Zero : Allowance - Consumed;

    /// <summary>When the hourly allowance next refills (allowance rules only).</summary>
    public DateTimeOffset? HourResetsAt { get; }

    /// <summary>When the current active window ends (time-bounded rules only).</summary>
    public DateTimeOffset? ActiveWindowEndsAt { get; }

    public bool WasActiveThisTick { get; }
}

/// <summary>The outcome of advancing the engine by one tick.</summary>
public sealed class TickResult
{
    public TickResult(
        IReadOnlyList<CloseDecision> closeDecisions,
        IReadOnlyList<RuleSnapshot> rules,
        IReadOnlyList<AppCloseDecision>? appCloseDecisions = null)
    {
        CloseDecisions = closeDecisions;
        Rules = rules;
        AppCloseDecisions = appCloseDecisions ?? Array.Empty<AppCloseDecision>();
    }

    /// <summary>The selected tabs to close this tick. At most one per window.</summary>
    public IReadOnlyList<CloseDecision> CloseDecisions { get; }

    public IEnumerable<string> WindowIdsToClose => CloseDecisions.Select(d => d.WindowId);

    /// <summary>The apps to close this tick, one decision per app.</summary>
    public IReadOnlyList<AppCloseDecision> AppCloseDecisions { get; }

    /// <summary>State of every rule after this tick, for display and tests.</summary>
    public IReadOnlyList<RuleSnapshot> Rules { get; }
}
