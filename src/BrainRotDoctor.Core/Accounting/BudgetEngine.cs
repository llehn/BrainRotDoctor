using BrainRotDoctor.Core.Configuration;

namespace BrainRotDoctor.Core.Accounting;

/// <summary>
/// A snapshot of one allowance rule's usage, used to carry the current hour's
/// consumption across a config save, an app restart, or an update swap. The
/// <see cref="HourStart"/> tags which clock hour the consumption belongs to so a
/// stale record from an earlier hour is discarded rather than wrongly applied.
/// </summary>
public readonly record struct RuleUsage(string RuleId, DateTimeOffset HourStart, TimeSpan Consumed);

/// <summary>
/// The deterministic core of the product: given the selected tab of every open
/// browser window and the current time, it decides which selected tabs must be
/// closed because a rule blocks the site they show.
///
/// The engine owns no clock and touches no OS resources. The caller supplies the
/// browser snapshot and the current instant on every <see cref="Tick"/>, which
/// makes the whole model reproducible and unit-testable.
///
/// Per rule:
/// <list type="bullet">
///   <item>A rule only counts and blocks while it is active (its day-of-week and
///   time window).</item>
///   <item>A <em>block-completely</em> rule blocks its sites whenever it is
///   active.</item>
///   <item>An <em>allowance</em> rule charges elapsed time (once per tick, never
///   multiplied by matching windows) against a budget that refills each clock
///   hour, and blocks once the hour's allowance is spent.</item>
///   <item>An over-long gap between ticks is clamped (see
///   <see cref="BudgetEngineOptions.MaxAccountedGap"/>) so sleep/lock pauses
///   accounting.</item>
/// </list>
/// </summary>
public sealed class BudgetEngine
{
    private readonly BlockerConfiguration _configuration;
    private readonly BudgetEngineOptions _options;
    private readonly Dictionary<string, RuleRuntime> _states;
    private DateTimeOffset? _lastTick;

    public BudgetEngine(BlockerConfiguration configuration, BudgetEngineOptions? options = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? new BudgetEngineOptions();
        _states = new Dictionary<string, RuleRuntime>(StringComparer.Ordinal);
        foreach (Rule rule in _configuration.Rules)
        {
            _states[rule.Id] = new RuleRuntime(rule);
        }
    }

    public TickResult Tick(IReadOnlyList<BrowserWindowState> windows, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(windows);

        TimeSpan elapsed = ComputeElapsed(now);
        _lastTick = now;

        foreach (RuleRuntime state in _states.Values)
        {
            state.Roll(now);
        }

        // Which allowance rules are being consumed this tick: matched by a
        // selected tab and currently active. A rule is charged once even when
        // several windows show a matching page.
        var affected = new HashSet<string>(StringComparer.Ordinal);
        foreach (BrowserWindowState window in windows)
        {
            if (window.Url is null)
            {
                continue;
            }

            foreach (Rule rule in _configuration.MatchingRules(window.Url))
            {
                if (!rule.BlocksCompletely && rule.IsActiveAt(now))
                {
                    affected.Add(rule.Id);
                }
            }
        }

        if (elapsed > TimeSpan.Zero)
        {
            foreach (string ruleId in affected)
            {
                _states[ruleId].Charge(elapsed, now);
            }
        }

        var decisions = new List<CloseDecision>();
        foreach (BrowserWindowState window in windows)
        {
            if (window.Url is null)
            {
                continue;
            }

            foreach (Rule rule in _configuration.MatchingRules(window.Url))
            {
                if (_states[rule.Id].IsBlocking(now))
                {
                    decisions.Add(new CloseDecision(window.WindowId, window.Url, rule.Id));
                    break;
                }
            }
        }

        return new TickResult(decisions, BuildSnapshots(now, affected));
    }

    /// <summary>
    /// The rule that blocks <paramref name="url"/> right now, without advancing time or
    /// charging anything; null when the page may stay open.
    /// </summary>
    public string? BlockingRuleFor(Uri url, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(url);
        foreach (Rule rule in _configuration.MatchingRules(url))
        {
            if (_states[rule.Id].IsBlocking(now))
            {
                return rule.Id;
            }
        }

        return null;
    }

    /// <summary>Current state of every rule without advancing time.</summary>
    public IReadOnlyList<RuleSnapshot> GetRuleSnapshots(DateTimeOffset now)
        => BuildSnapshots(now, activeRules: null);

    /// <summary>
    /// The current hour's consumption of every allowance rule, for persistence.
    /// Block-completely rules carry no usage and are omitted.
    /// </summary>
    public IReadOnlyList<RuleUsage> ExportUsage()
    {
        var usage = new List<RuleUsage>();
        foreach (Rule rule in _configuration.Rules)
        {
            if (!rule.BlocksCompletely)
            {
                usage.Add(_states[rule.Id].ToUsage(rule.Id));
            }
        }

        return usage;
    }

    /// <summary>
    /// Seeds rules with previously recorded usage (by rule id). Records for rules
    /// this engine doesn't have are ignored. A record from a past clock hour is
    /// harmless: the next <see cref="Tick"/> rolls the rule and clears it, so a new
    /// hour always starts fresh.
    /// </summary>
    public void RestoreUsage(IEnumerable<RuleUsage> usage)
    {
        ArgumentNullException.ThrowIfNull(usage);
        foreach (RuleUsage record in usage)
        {
            if (_states.TryGetValue(record.RuleId, out RuleRuntime? state))
            {
                state.Restore(record.HourStart, record.Consumed);
            }
        }
    }

    private TimeSpan ComputeElapsed(DateTimeOffset now)
    {
        if (_lastTick is not { } last)
        {
            return TimeSpan.Zero;
        }

        TimeSpan raw = now - last;
        if (raw <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return raw > _options.MaxAccountedGap ? _options.MaxAccountedGap : raw;
    }

    private IReadOnlyList<RuleSnapshot> BuildSnapshots(DateTimeOffset now, ISet<string>? activeRules)
    {
        var snapshots = new List<RuleSnapshot>(_states.Count);
        foreach (Rule rule in _configuration.Rules)
        {
            snapshots.Add(_states[rule.Id].ToSnapshot(
                now,
                activeRules?.Contains(rule.Id) ?? false));
        }

        return snapshots;
    }

    /// <summary>Mutable per-rule runtime: the current hour's allowance usage.</summary>
    private sealed class RuleRuntime
    {
        private readonly Rule _rule;
        private DateTimeOffset _hourStart;
        private TimeSpan _consumed;

        public RuleRuntime(Rule rule)
        {
            _rule = rule;
            _hourStart = rule.HourWindowStart(DateTimeOffset.UnixEpoch);
        }

        public bool IsBlocking(DateTimeOffset now)
        {
            if (!_rule.IsActiveAt(now))
            {
                return false;
            }

            return _rule.BlocksCompletely || _consumed >= _rule.Allowance!.Value;
        }

        public void Roll(DateTimeOffset now)
        {
            if (_rule.BlocksCompletely)
            {
                return;
            }

            DateTimeOffset hourStart = _rule.HourWindowStart(now);
            if (hourStart != _hourStart)
            {
                _hourStart = hourStart;
                _consumed = TimeSpan.Zero;
            }
        }

        public void Charge(TimeSpan elapsed, DateTimeOffset now)
        {
            if (_rule.BlocksCompletely)
            {
                return;
            }

            // Only the part of the gap inside the current hour may be charged.
            TimeSpan withinHour = now - _hourStart;
            if (elapsed > withinHour)
            {
                elapsed = withinHour;
            }

            if (elapsed <= TimeSpan.Zero)
            {
                return;
            }

            _consumed += elapsed;
            if (_consumed > _rule.Allowance!.Value)
            {
                _consumed = _rule.Allowance.Value;
            }
        }

        public RuleUsage ToUsage(string ruleId) => new(ruleId, _hourStart, _consumed);

        public void Restore(DateTimeOffset hourStart, TimeSpan consumed)
        {
            if (_rule.BlocksCompletely)
            {
                return;
            }

            _hourStart = hourStart;
            _consumed = consumed > _rule.Allowance!.Value ? _rule.Allowance.Value
                : consumed < TimeSpan.Zero ? TimeSpan.Zero
                : consumed;
        }

        public RuleSnapshot ToSnapshot(DateTimeOffset now, bool wasActiveThisTick)
        {
            bool active = _rule.IsActiveAt(now);
            return new RuleSnapshot(
                _rule.Id,
                _rule.Name,
                active,
                _rule.BlocksCompletely,
                IsBlocking(now),
                _rule.Allowance ?? TimeSpan.Zero,
                _rule.BlocksCompletely ? TimeSpan.Zero : _consumed,
                _rule.BlocksCompletely ? null : _hourStart + Rule.AllowancePeriod,
                _rule.ActiveWindowEndsAt(now),
                wasActiveThisTick);
        }
    }
}
