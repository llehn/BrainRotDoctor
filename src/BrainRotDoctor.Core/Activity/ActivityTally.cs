using BrainRotDoctor.Core.Apps;

namespace BrainRotDoctor.Core.Activity;

/// <summary>Today's totals for one place: how long it was in front, and how often you came to it.</summary>
public sealed class PlaceTotal
{
    public double Seconds { get; set; }

    public int Visits { get; set; }

    public TimeSpan Time => TimeSpan.FromSeconds(Seconds);
}

/// <summary>Today's total for one app: how long it was in front, how often you came to it, and what it is.</summary>
public sealed class AppTotal
{
    public double Seconds { get; set; }

    public int Visits { get; set; }

    /// <summary>Which app (its name and how it's recognized), so it can be shown and blocked when it isn't running.</summary>
    public AppIdentity? App { get; set; }

    public TimeSpan Time => TimeSpan.FromSeconds(Seconds);
}

/// <summary>
/// The stored form of a day's statistics: totals per place, plus a few facts
/// about the day. No timestamps, no order, no page addresses — nothing a browsing
/// history could be rebuilt from.
/// </summary>
public sealed class ActivityRecord
{
    /// <summary>The local day the totals belong to, "yyyy-MM-dd".</summary>
    public string Day { get; set; } = "";

    public Dictionary<string, PlaceTotal> Places { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Totals per app, by <see cref="AppIdentity.Key"/>.</summary>
    public Dictionary<string, AppTotal> Apps { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Time on sites and apps you marked as okay, all of them together: none is told apart.</summary>
    public double OkaySeconds { get; set; }

    /// <summary>Blocking was paused at some point today.</summary>
    public bool PausedToday { get; set; }

    /// <summary>How long blocking was paused today, all pauses together.</summary>
    public double PausedSeconds { get; set; }

    public TimeSpan PausedTime => TimeSpan.FromSeconds(PausedSeconds);

    /// <summary>Strict mode was on at some point today.</summary>
    public bool StrictToday { get; set; }

    public ActivityRecord Copy() => new()
    {
        Day = Day,
        Places = Places.ToDictionary(p => p.Key, p => new PlaceTotal { Seconds = p.Value.Seconds, Visits = p.Value.Visits }, StringComparer.Ordinal),
        Apps = Apps.ToDictionary(a => a.Key, a => new AppTotal { Seconds = a.Value.Seconds, Visits = a.Value.Visits, App = a.Value.App }, StringComparer.Ordinal),
        OkaySeconds = OkaySeconds,
        PausedToday = PausedToday,
        PausedSeconds = PausedSeconds,
        StrictToday = StrictToday,
    };
}

/// <summary>
/// Counts where today's time goes. Every tick, what is in front — the page of the
/// browser window you're using (charged to each of its places, <see cref="PlaceKey"/>)
/// or the app you're using — is charged the elapsed time; arriving at a place or app
/// that wasn't in front the tick before counts as a visit. At local midnight the
/// totals start over. Like the budget engine it owns no clock: the caller passes
/// the time, so it is fully testable.
/// </summary>
public sealed class ActivityTally
{
    /// <summary>A longer gap between ticks (sleep, a stall) is counted as this much.</summary>
    public static readonly TimeSpan MaxGap = TimeSpan.FromSeconds(5);

    /// <summary>Beyond this many places, only whole sites are added, so the record stays small.</summary>
    public const int MaxPlaces = 2000;

    private ActivityRecord _record = new();
    private HashSet<string> _inFront = new(StringComparer.Ordinal);
    private HashSet<string> _untracked = new(StringComparer.Ordinal);
    private HashSet<string> _untrackedApps = new(StringComparer.Ordinal);
    private string? _appInFront;
    private DateTimeOffset? _lastTick;

    // The whole time since the tick before, sleep included: a pause runs on while the PC sleeps.
    private TimeSpan _lastGap;

    /// <summary>
    /// Sites (by host) you marked as okay: their time is counted only into one
    /// shared total, never per site. What they already had today joins that total
    /// and their own places are dropped.
    /// </summary>
    public void SetUntracked(IEnumerable<string> hosts)
    {
        _untracked = hosts.Select(PlaceKey.Host).ToHashSet(StringComparer.Ordinal);
        foreach (string key in _record.Places.Keys.Where(k => _untracked.Contains(PlaceKey.HostOf(k))).ToList())
        {
            if (!key.Contains('/'))
            {
                _record.OkaySeconds += _record.Places[key].Seconds;
            }

            _record.Places.Remove(key);
        }
    }

    /// <summary>
    /// Apps (by <see cref="AppIdentity.Key"/>) you marked as okay: like okay sites, their
    /// time joins the one shared total, what they had today included.
    /// </summary>
    public void SetUntrackedApps(IEnumerable<string> keys)
    {
        _untrackedApps = keys.ToHashSet(StringComparer.Ordinal);
        foreach (string key in _record.Apps.Keys.Where(_untrackedApps.Contains).ToList())
        {
            _record.OkaySeconds += _record.Apps[key].Seconds;
            _record.Apps.Remove(key);
        }
    }

    public void Tick(Uri? front, DateTimeOffset now) => Tick(front, null, now);

    /// <summary>Charges the page in front, or else the app in front (at most one of them is given).</summary>
    public void Tick(Uri? front, AppIdentity? frontApp, DateTimeOffset now)
    {
        // The gap before a new day's first tick belongs to the day before.
        bool newDay = RollDay(now);
        TimeSpan gap = !newDay && _lastTick is { } last && now > last ? now - last : TimeSpan.Zero;
        TimeSpan elapsed = gap > MaxGap ? MaxGap : gap;
        _lastTick = now;
        _lastGap = gap;

        IReadOnlyList<string> keys = PlaceKey.Of(front);
        if (keys.Count > 0 && _untracked.Contains(keys[0]))
        {
            _record.OkaySeconds += elapsed.TotalSeconds;
            keys = Array.Empty<string>();
        }

        var inFront = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            if (!_record.Places.TryGetValue(key, out PlaceTotal? total))
            {
                if (_record.Places.Count >= MaxPlaces && key.Contains('/'))
                {
                    break;
                }

                total = new PlaceTotal();
                _record.Places[key] = total;
            }

            total.Seconds += elapsed.TotalSeconds;
            if (!_inFront.Contains(key))
            {
                total.Visits++;
            }

            inFront.Add(key);
        }

        _inFront = inFront;

        string? appKey = keys.Count == 0 ? frontApp?.Key : null;
        if (appKey is not null && _untrackedApps.Contains(appKey))
        {
            _record.OkaySeconds += elapsed.TotalSeconds;
            appKey = null;
        }
        else if (appKey is not null)
        {
            if (!_record.Apps.TryGetValue(appKey, out AppTotal? app))
            {
                app = new AppTotal();
                _record.Apps[appKey] = app;
            }

            app.Seconds += elapsed.TotalSeconds;
            app.App = frontApp;
            if (_appInFront != appKey)
            {
                app.Visits++;
            }
        }

        _appInFront = appKey;
    }

    /// <summary>
    /// Blocking is paused at <paramref name="now"/>: the whole time since the tick
    /// before counts as paused, also when the PC slept in between.
    /// </summary>
    public void MarkPaused(DateTimeOffset now)
    {
        if (RollDay(now))
        {
            _lastGap = TimeSpan.Zero;
        }

        _record.PausedToday = true;
        _record.PausedSeconds += _lastGap.TotalSeconds;
        _lastGap = TimeSpan.Zero;
    }

    public void MarkStrict(DateTimeOffset now)
    {
        RollDay(now);
        _record.StrictToday = true;
    }

    /// <summary>A copy of today's totals, for showing and storing.</summary>
    public ActivityRecord Snapshot() => _record.Copy();

    /// <summary>Continues from stored totals if they are today's; older ones are dropped.</summary>
    public void Restore(ActivityRecord? record, DateTimeOffset now)
    {
        if (record is not null && record.Day == DayOf(now))
        {
            _record = record.Copy();
            SetUntracked(_untracked);
            SetUntrackedApps(_untrackedApps);
        }
        else
        {
            RollDay(now);
        }
    }

    public static string DayOf(DateTimeOffset now) => now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private bool RollDay(DateTimeOffset now)
    {
        string day = DayOf(now);
        if (_record.Day == day)
        {
            return false;
        }

        _record = new ActivityRecord { Day = day };
        _inFront.Clear();
        _appInFront = null;
        return true;
    }
}
