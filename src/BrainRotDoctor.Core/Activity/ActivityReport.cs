namespace BrainRotDoctor.Core.Activity;

/// <summary>
/// A place worth its own card: a section of a site that holds a good part of
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

/// <summary>One site's day: its time and the places its time went to.</summary>
public sealed record ActivitySite(string Host, TimeSpan Time, IReadOnlyList<ActivityPlace> Places)
{
    /// <summary>The site was split into sections (and maybe the rest), not shown whole.</summary>
    public bool IsSplit => Places.Count > 1 || (Places.Count == 1 && Places[0].Path.Length > 0);
}

/// <summary>
/// Today's statistics turned into what the status view shows: sites sorted by
/// time, each split into the places that hold its time. Nothing is judged:
/// only you decide what is a waste of time.
///
/// Splitting follows the time: a section gets its own card once it holds at
/// least <see cref="SplitShare"/> of the place above it and at least
/// <see cref="SplitMinimum"/>; it is split further the same way. So a site you
/// spend most of an hour in one forum's comments on shows exactly that, while a
/// site used evenly stays whole. Whatever isn't split out stays with the site
/// as "the rest".
/// </summary>
public sealed class ActivityReport
{
    public static readonly TimeSpan SiteMinimum = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan SplitMinimum = TimeSpan.FromMinutes(2);
    public const double SplitShare = 0.25;

    private ActivityReport(IReadOnlyList<ActivitySite> sites, TimeSpan total)
    {
        Sites = sites;
        Total = total;
    }

    /// <summary>Sites with at least <see cref="SiteMinimum"/> today, longest first.</summary>
    public IReadOnlyList<ActivitySite> Sites { get; }

    /// <summary>All time with a web page in front today.</summary>
    public TimeSpan Total { get; }

    public static ActivityReport Build(ActivityRecord record)
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
        foreach ((string host, PlaceTotal total) in record.Places.Where(p => !p.Key.Contains('/')))
        {
            all += total.Time;
            if (total.Time >= SiteMinimum)
            {
                sites.Add(new ActivitySite(host, total.Time, Split(host, total.Time, children)));
            }
        }

        return new ActivityReport(sites.OrderByDescending(s => s.Time).ToList(), all);
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
