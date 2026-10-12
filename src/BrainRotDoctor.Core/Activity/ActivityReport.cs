namespace BrainRotDoctor.Core.Activity;

/// <summary>How a site's day looks, judged from how you use it — not from a list of sites.</summary>
public enum Verdict
{
    Fine,
    WatchOut,
    BrainRot,
}

/// <summary>
/// A place worth its own line: a section of a site that holds a good part of
/// the site's time, the whole site, or the rest of a site whose sections were
/// split out.
/// </summary>
public sealed record ActivityPlace(string Host, string Path, TimeSpan Time, bool IsRest)
{
    /// <summary>An address of this place: the section itself, or the site's home for a whole site or the rest.</summary>
    public string Url => "https://" + Host + "/" + Path;

    /// <summary>"reddit.com/r/dotnet", or the host alone for a whole site.</summary>
    public string Label => Path.Length == 0 ? Host : Host + "/" + Path;
}

/// <summary>One site's day: its time, visits, verdict, and the places its time went to.</summary>
public sealed record ActivitySite(
    string Host,
    TimeSpan Time,
    int Visits,
    Verdict Verdict,
    bool MarkedFine,
    IReadOnlyList<ActivityPlace> Places);

/// <summary>
/// Today's statistics turned into what the status view shows: sites sorted by
/// time, each judged, and each split into the places that hold its time.
///
/// Splitting follows the time: a section gets its own line once it holds at
/// least <see cref="SplitShare"/> of the place above it and at least
/// <see cref="SplitMinimum"/>; it is split further the same way. So a site you
/// spend most of an hour in one forum's comments on shows exactly that, while a
/// site used evenly stays one line. Whatever isn't split out stays with the site
/// as "everything else".
///
/// The verdict comes from behaviour alone: long time on a site, or coming back
/// to it again and again. A site you mark as fine is never counted as brain rot.
/// </summary>
public sealed class ActivityReport
{
    public static readonly TimeSpan SiteMinimum = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan SplitMinimum = TimeSpan.FromMinutes(2);
    public const double SplitShare = 0.25;

    /// <summary>A site this long today is brain rot.</summary>
    public static readonly TimeSpan RotTime = TimeSpan.FromMinutes(30);

    /// <summary>A site opened this often today, for at least <see cref="RotCheckingTime"/>, is brain rot too.</summary>
    public const int RotVisits = 10;

    public static readonly TimeSpan RotCheckingTime = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan WatchTime = TimeSpan.FromMinutes(15);
    public const int WatchVisits = 6;
    public static readonly TimeSpan WatchCheckingTime = TimeSpan.FromMinutes(5);

    private ActivityReport(IReadOnlyList<ActivitySite> sites, TimeSpan total, int shortSites)
    {
        Sites = sites;
        Total = total;
        ShortSites = shortSites;
        BrainRot = TimeSpan.FromTicks(sites.Where(s => s.Verdict == Verdict.BrainRot).Sum(s => s.Time.Ticks));
    }

    /// <summary>Sites with at least <see cref="SiteMinimum"/> today, longest first.</summary>
    public IReadOnlyList<ActivitySite> Sites { get; }

    /// <summary>All time with a web page in front today.</summary>
    public TimeSpan Total { get; }

    /// <summary>Time on sites judged brain rot.</summary>
    public TimeSpan BrainRot { get; }

    /// <summary>Sites visited today for less than <see cref="SiteMinimum"/> each, not listed.</summary>
    public int ShortSites { get; }

    public static ActivityReport Build(ActivityRecord record, IReadOnlySet<string>? markedFine = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        var children = new Dictionary<string, List<(string Key, TimeSpan Time)>>(StringComparer.Ordinal);
        foreach ((string key, PlaceTotal total) in record.Places)
        {
            if (PlaceKey.ParentOf(key) is { } parent)
            {
                if (!children.TryGetValue(parent, out List<(string, TimeSpan)>? list))
                {
                    list = new();
                    children[parent] = list;
                }

                list.Add((key, total.Time));
            }
        }

        var sites = new List<ActivitySite>();
        TimeSpan all = TimeSpan.Zero;
        int shortSites = 0;
        foreach ((string host, PlaceTotal total) in record.Places.Where(p => !p.Key.Contains('/')))
        {
            all += total.Time;
            if (total.Time < SiteMinimum)
            {
                shortSites++;
                continue;
            }

            bool fine = markedFine?.Contains(host) == true;
            Verdict verdict = fine ? Verdict.Fine : Judge(total.Time, total.Visits);
            sites.Add(new ActivitySite(host, total.Time, total.Visits, verdict, fine, Split(host, total.Time, children)));
        }

        return new ActivityReport(sites.OrderByDescending(s => s.Time).ToList(), all, shortSites);
    }

    public static Verdict Judge(TimeSpan time, int visits)
    {
        if (time >= RotTime || (visits >= RotVisits && time >= RotCheckingTime))
        {
            return Verdict.BrainRot;
        }

        if (time >= WatchTime || (visits >= WatchVisits && time >= WatchCheckingTime))
        {
            return Verdict.WatchOut;
        }

        return Verdict.Fine;
    }

    private static IReadOnlyList<ActivityPlace> Split(string host, TimeSpan siteTime, Dictionary<string, List<(string Key, TimeSpan Time)>> children)
    {
        var places = new List<ActivityPlace>();

        void Expand(string key, TimeSpan time)
        {
            var significant = children.TryGetValue(key, out List<(string Key, TimeSpan Time)>? list)
                ? list.Where(c => c.Time >= SplitMinimum && c.Time.Ticks >= time.Ticks * SplitShare).ToList()
                : new List<(string Key, TimeSpan Time)>();

            if (significant.Count == 0)
            {
                if (key != host)
                {
                    places.Add(new ActivityPlace(host, PlaceKey.PathOf(key), time, IsRest: false));
                }

                return;
            }

            foreach ((string child, TimeSpan childTime) in significant)
            {
                Expand(child, childTime);
            }
        }

        Expand(host, siteTime);
        if (places.Count == 0)
        {
            return new[] { new ActivityPlace(host, "", siteTime, IsRest: false) };
        }

        places.Sort((a, b) => b.Time.CompareTo(a.Time));
        TimeSpan rest = siteTime - TimeSpan.FromTicks(places.Sum(p => p.Time.Ticks));
        if (rest >= SplitMinimum)
        {
            places.Add(new ActivityPlace(host, "", rest, IsRest: true));
        }

        return places;
    }
}
