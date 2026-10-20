namespace BrainRotDoctor.Core.Activity;

/// <summary>Today's totals for one place: how long it was in front, and how often you came to it.</summary>
public sealed class PlaceTotal
{
    public double Seconds { get; set; }

    public int Visits { get; set; }

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

    /// <summary>Time on sites you marked as okay, all of them together: no site is told apart.</summary>
    public double OkaySeconds { get; set; }

    /// <summary>Blocking was paused at some point today.</summary>
    public bool PausedToday { get; set; }

    /// <summary>Strict mode was on at some point today.</summary>
    public bool StrictToday { get; set; }

    public ActivityRecord Copy() => new()
    {
        Day = Day,
        Places = Places.ToDictionary(p => p.Key, p => new PlaceTotal { Seconds = p.Value.Seconds, Visits = p.Value.Visits }, StringComparer.Ordinal),
        OkaySeconds = OkaySeconds,
        PausedToday = PausedToday,
        StrictToday = StrictToday,
    };
}

/// <summary>
/// Counts where today's browser time goes. Every tick, the page in front (the
/// selected tab of the browser window you're using, if any) charges the elapsed
/// time to each of its places (<see cref="PlaceKey"/>); arriving at a place that
/// wasn't in front the tick before counts as a visit. At local midnight the
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
    private DateTimeOffset? _lastTick;

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

    public void Tick(Uri? front, DateTimeOffset now)
    {
        // The gap before a new day's first tick belongs to the day before.
        bool newDay = RollDay(now);
        TimeSpan elapsed = !newDay && _lastTick is { } last && now > last ? now - last : TimeSpan.Zero;
        if (elapsed > MaxGap)
        {
            elapsed = MaxGap;
        }

        _lastTick = now;

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
    }

    public void MarkPaused(DateTimeOffset now)
    {
        RollDay(now);
        _record.PausedToday = true;
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
        return true;
    }
}
