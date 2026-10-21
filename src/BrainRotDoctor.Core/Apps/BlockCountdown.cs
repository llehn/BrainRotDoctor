using BrainRotDoctor.Core.Accounting;

namespace BrainRotDoctor.Core.Apps;

/// <summary>One app that counts right now, with the block coming for it (null when none is).</summary>
/// <param name="App">The app.</param>
/// <param name="Ahead">The block within <see cref="BlockCountdown.Lead"/>; its time is now when the app is blocked already.</param>
/// <param name="Held">Leave it alone this tick (it is being closed, or Windows refused to close it a moment ago).</param>
public readonly record struct CountingApp(AppIdentity App, BlockAhead? Ahead, bool Held = false);

/// <summary>An app closing soon: when, and because of which rule.</summary>
public sealed record AppWarning(AppIdentity App, DateTimeOffset At, string RuleId);

/// <summary>What to do this tick: the apps to close now, and the ones to warn about.</summary>
public sealed record CountdownResult(IReadOnlyList<AppCloseDecision> Close, IReadOnlyList<AppWarning> Warn);

/// <summary>
/// When an open app is closed. A block that can be foreseen (a rule starting, an
/// allowance running out while the app counts, a pause ending) is announced
/// <see cref="Lead"/> ahead and closes on time. A block that can't (a rule just added or
/// switched on, Resume) gives an app that was running unblocked the same minute. An app
/// that shows up while already blocked closes at once, so starting it again buys no
/// time. Steam is never warned: closing it only sends it to its tray. Like the engine
/// it owns no clock: the caller passes the time.
/// </summary>
public sealed class BlockCountdown
{
    /// <summary>How long before a block an open app is told, so a game can be saved.</summary>
    public static readonly TimeSpan Lead = TimeSpan.FromMinutes(1);

    /// <summary>
    /// An app gone for longer than this is no longer "running": brought back while
    /// blocked, it closes at once. Shorter covers a launcher handing over to its game.
    /// </summary>
    public static readonly TimeSpan StillRunning = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, BlockAhead> _due = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _unblockedAt = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _seenAt = new(StringComparer.Ordinal);

    public CountdownResult Tick(IReadOnlyList<CountingApp> apps, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(apps);
        var close = new List<AppCloseDecision>();
        var warn = new List<AppWarning>();
        foreach (CountingApp counting in apps.DistinctBy(a => a.App.Key))
        {
            AppIdentity app = counting.App;
            string key = app.Key;
            bool steam = app.Kind == AppKind.Steam;
            _seenAt[key] = now;
            if (counting.Ahead is not { } ahead)
            {
                _due.Remove(key);
                _unblockedAt[key] = now;
                continue;
            }

            if (counting.Held)
            {
                continue;
            }

            if (!_due.TryGetValue(key, out BlockAhead? when))
            {
                bool wasRunning = _unblockedAt.TryGetValue(key, out DateTimeOffset unblocked) && now - unblocked <= StillRunning;
                when = ahead.At <= now && wasRunning && !steam ? new BlockAhead(now + Lead, ahead.RuleId) : ahead;
                _due[key] = when;
            }

            if (when.At > now)
            {
                if (!steam)
                {
                    warn.Add(new AppWarning(app, when.At, when.RuleId));
                }
            }
            else if (ahead.At <= now)
            {
                close.Add(new AppCloseDecision(app, ahead.RuleId));
            }
            else
            {
                // Its time came, but the block didn't (the rule's time ended first): count again.
                _due.Remove(key);
            }
        }

        foreach (string gone in _seenAt.Where(s => now - s.Value > StillRunning).Select(s => s.Key).ToList())
        {
            _seenAt.Remove(gone);
            _due.Remove(gone);
            _unblockedAt.Remove(gone);
        }

        return new CountdownResult(close, warn);
    }
}
