using BrainRotDoctor.Core.Apps;

namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// The product's central entity (the AppBlock "Zeitplan"): <em>what</em> to block
/// and <em>when</em>. A rule groups the sites it limits with a single combined
/// condition:
/// <list type="bullet">
///   <item><b>Allowance</b> — an optional time budget of
///   <see cref="Allowance"/> per hour. When it is null the sites are blocked
///   completely while the rule is active.</item>
///   <item><b>Active window</b> — either all day (<see cref="AllDay"/>) or the
///   local time range <see cref="From"/>–<see cref="To"/> (which may wrap past
///   midnight).</item>
///   <item><b>Days</b> — the days of the week the rule applies on.</item>
/// </list>
/// Several rules may target the same site; the site is blocked if any active rule
/// blocks it.
/// </summary>
public sealed class Rule
{
    /// <summary>The allowance always refills once per clock hour.</summary>
    public static readonly TimeSpan AllowancePeriod = TimeSpan.FromHours(1);

    private static readonly DayOfWeek[] AllDaysOfWeek = Enum.GetValues<DayOfWeek>();

    private readonly HashSet<DayOfWeek> _days;

    /// <param name="id">Stable identifier for the rule.</param>
    /// <param name="name">Human-readable name for display.</param>
    /// <param name="sites">The sites this rule blocks.</param>
    /// <param name="allowance">Allowed time per hour, or null to block completely.</param>
    /// <param name="allDay">True if the rule is active all day; otherwise the window <paramref name="from"/>–<paramref name="to"/> applies.</param>
    /// <param name="from">Start of the active window (local), used when not all day.</param>
    /// <param name="to">End of the active window (local), used when not all day; earlier than <paramref name="from"/> wraps past midnight.</param>
    /// <param name="days">Days the rule applies on. Null or empty means every day.</param>
    /// <param name="apps">The apps this rule blocks. A rule needs at least one site or app.</param>
    public Rule(
        string id,
        string name,
        IReadOnlyList<TargetSite> sites,
        TimeSpan? allowance,
        bool allDay,
        TimeOnly from,
        TimeOnly to,
        IReadOnlyCollection<DayOfWeek>? days,
        IReadOnlyList<TargetApp>? apps = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ConfigurationException("Rule id must not be empty.");
        }

        ArgumentNullException.ThrowIfNull(sites);
        if (sites.Count == 0 && apps is not { Count: > 0 })
        {
            throw new ConfigurationException($"Rule '{id}' must block at least one site or app.");
        }

        if (allowance is { } a && (a <= TimeSpan.Zero || a >= AllowancePeriod))
        {
            throw new ConfigurationException(
                $"Rule '{id}' allowance must be between 1 minute and 59 minutes per hour.");
        }

        if (!allDay && from == to)
        {
            throw new ConfigurationException(
                $"Rule '{id}' must have a start time different from its end time, or be set to all day.");
        }

        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
        Sites = sites.ToArray();
        Apps = apps?.ToArray() ?? Array.Empty<TargetApp>();
        Allowance = allowance;
        AllDay = allDay;
        From = from;
        To = to;
        _days = days is { Count: > 0 }
            ? new HashSet<DayOfWeek>(days)
            : new HashSet<DayOfWeek>(AllDaysOfWeek);
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<TargetSite> Sites { get; }

    public IReadOnlyList<TargetApp> Apps { get; }

    /// <summary>Allowed time per hour, or null when the rule blocks completely.</summary>
    public TimeSpan? Allowance { get; }

    public bool BlocksCompletely => Allowance is null;

    public bool AllDay { get; }

    public TimeOnly From { get; }

    public TimeOnly To { get; }

    public IReadOnlyCollection<DayOfWeek> Days => _days;

    public bool WrapsMidnight => !AllDay && To <= From;

    /// <summary>True if one of the rule's sites blocks <paramref name="uri"/>.</summary>
    public bool MatchesUrl(Uri uri) => Sites.Any(s => s.Blocks(uri));

    /// <summary>True if one of the rule's apps is <paramref name="app"/>.</summary>
    public bool MatchesApp(AppIdentity app) => Apps.Any(a => a.Matches(app));

    /// <summary>
    /// The first moment after <paramref name="now"/>, at most <paramref name="within"/>
    /// later and to the second, when the rule is in effect; null when it isn't in that
    /// time or is in effect already.
    /// </summary>
    public DateTimeOffset? StartsWithin(DateTimeOffset now, TimeSpan within)
    {
        if (IsActiveAt(now))
        {
            return null;
        }

        for (TimeSpan step = TimeSpan.FromSeconds(1); step <= within; step += TimeSpan.FromSeconds(1))
        {
            if (IsActiveAt(now + step))
            {
                return now + step;
            }
        }

        return null;
    }

    /// <summary>True if the rule is in effect at <paramref name="now"/>.</summary>
    public bool IsActiveAt(DateTimeOffset now)
    {
        DateTime local = now.ToLocalTime().DateTime;
        if (AllDay)
        {
            return _days.Contains(local.DayOfWeek);
        }

        if (IsWithinWindowStartingOn(local, local.Date))
        {
            return true;
        }

        return WrapsMidnight && IsWithinWindowStartingOn(local, local.Date.AddDays(-1));
    }

    /// <summary>
    /// When the rule is active and time-bounded, the instant the current active
    /// window ends; otherwise null (all-day rules have no end).
    /// </summary>
    public DateTimeOffset? ActiveWindowEndsAt(DateTimeOffset now)
    {
        if (AllDay)
        {
            return null;
        }

        DateTimeOffset local = now.ToLocalTime();
        foreach (DateTime startDay in new[] { local.Date, local.Date.AddDays(-1) })
        {
            if (TryGetWindow(startDay, out DateTime start, out DateTime end)
                && local.DateTime >= start && local.DateTime < end)
            {
                return new DateTimeOffset(end, local.Offset);
            }
        }

        return null;
    }

    /// <summary>Start of the clock hour containing <paramref name="now"/> (local).</summary>
    public DateTimeOffset HourWindowStart(DateTimeOffset now)
    {
        DateTimeOffset local = now.ToLocalTime();
        return new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, 0, 0, local.Offset);
    }

    private bool IsWithinWindowStartingOn(DateTime local, DateTime startDay)
        => TryGetWindow(startDay, out DateTime start, out DateTime end)
           && local >= start && local < end;

    private bool TryGetWindow(DateTime startDay, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        if (!_days.Contains(startDay.DayOfWeek))
        {
            return false;
        }

        start = startDay + From.ToTimeSpan();
        end = WrapsMidnight ? startDay.AddDays(1) + To.ToTimeSpan() : startDay + To.ToTimeSpan();
        return true;
    }
}
