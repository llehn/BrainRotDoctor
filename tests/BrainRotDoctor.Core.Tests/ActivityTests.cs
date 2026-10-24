using BrainRotDoctor.Core.Activity;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class ActivityTests
{
    private static readonly DateTimeOffset T0 =
        new(new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Local));

    private static Uri U(string url) => new(url);

    /// <summary>Keeps <paramref name="url"/> in front for <paramref name="seconds"/>, one tick per second.</summary>
    private static DateTimeOffset Stay(ActivityTally tally, string? url, DateTimeOffset from, int seconds)
    {
        for (int i = 1; i <= seconds; i++)
        {
            tally.Tick(url is null ? null : U(url), from.AddSeconds(i));
        }

        return from.AddSeconds(seconds);
    }

    // ---------- Places ----------

    [Fact]
    public void A_page_belongs_to_its_site_and_the_first_sections_of_its_path()
    {
        Assert.Equal(
            new[] { "reddit.com", "reddit.com/r", "reddit.com/r/dotnet", "reddit.com/r/dotnet/comments" },
            PlaceKey.Of(U("https://www.reddit.com/r/dotnet/comments/1h2x9/why_is_it?utm=x#top")));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "youtube.com/watch")]
    [InlineData("https://www.youtube.com/shorts/AbCdEfGhIjK", "youtube.com/shorts")]
    [InlineData("https://www.instagram.com/reel/C9xYz12AbC/", "instagram.com/reel")]
    [InlineData("https://m.facebook.com/groups/12345678/", "facebook.com/groups")]
    [InlineData("https://forum.example.org/t/how-do-i-fix-my-bike/4711", "forum.example.org/t")]
    [InlineData("https://news.site/2026/10/12/story", "news.site")]
    public void Ids_codes_and_titles_end_a_place(string url, string deepest)
    {
        Assert.Equal(deepest, PlaceKey.Of(U(url))[^1]);
    }

    [Fact]
    public void Non_web_pages_have_no_place()
    {
        Assert.Empty(PlaceKey.Of(U("about:blank")));
        Assert.Empty(PlaceKey.Of(null));
    }

    // ---------- Tally ----------

    [Fact]
    public void Time_in_front_is_counted_for_every_level_of_the_place()
    {
        var tally = new ActivityTally();
        tally.Tick(U("https://reddit.com/r/dotnet"), T0);
        Stay(tally, "https://reddit.com/r/dotnet", T0, 60);

        ActivityRecord record = tally.Snapshot();
        Assert.Equal(60, record.Places["reddit.com"].Seconds, 3);
        Assert.Equal(60, record.Places["reddit.com/r/dotnet"].Seconds, 3);
    }

    [Fact]
    public void Coming_back_to_a_site_counts_as_a_visit_but_moving_within_it_does_not()
    {
        var tally = new ActivityTally();
        DateTimeOffset t = Stay(tally, "https://reddit.com/r/a", T0, 5);
        t = Stay(tally, "https://reddit.com/r/b", t, 5);
        t = Stay(tally, null, t, 5);
        Stay(tally, "https://reddit.com/r/a", t, 5);

        ActivityRecord record = tally.Snapshot();
        Assert.Equal(2, record.Places["reddit.com"].Visits);
        Assert.Equal(2, record.Places["reddit.com/r/a"].Visits);
        Assert.Equal(1, record.Places["reddit.com/r/b"].Visits);
    }

    [Fact]
    public void A_long_gap_is_counted_as_a_few_seconds_only()
    {
        var tally = new ActivityTally();
        tally.Tick(U("https://x.com/home"), T0);
        tally.Tick(U("https://x.com/home"), T0.AddHours(2));

        Assert.Equal(ActivityTally.MaxGap.TotalSeconds, tally.Snapshot().Places["x.com"].Seconds, 3);
    }

    [Fact]
    public void Totals_start_over_at_midnight_and_stored_totals_of_another_day_are_dropped()
    {
        var tally = new ActivityTally();
        tally.Tick(U("https://x.com/home"), T0);
        Stay(tally, "https://x.com/home", T0, 10);
        ActivityRecord yesterday = tally.Snapshot();

        tally.Tick(U("https://x.com/home"), T0.AddDays(1));
        Assert.Equal(0, tally.Snapshot().Places["x.com"].Seconds, 3);

        var restored = new ActivityTally();
        restored.Restore(yesterday, T0.AddDays(1));
        Assert.Empty(restored.Snapshot().Places);

        restored.Restore(yesterday, T0.AddHours(1));
        Assert.Equal(10, restored.Snapshot().Places["x.com"].Seconds, 3);
    }

    [Fact]
    public void Paused_time_adds_up_over_the_day_and_starts_over_at_midnight()
    {
        var tally = new ActivityTally();
        tally.Tick(null, T0);
        for (int i = 1; i <= 90; i++)
        {
            tally.Tick(null, T0.AddSeconds(i));
            if (i <= 30 || i > 60)
            {
                tally.MarkPaused(T0.AddSeconds(i));
            }
        }

        Assert.Equal(60, tally.Snapshot().PausedSeconds, 3);
        Assert.Equal(60, tally.Snapshot().Copy().PausedSeconds, 3);

        tally.Tick(null, T0.AddDays(1));
        tally.MarkPaused(T0.AddDays(1));
        Assert.Equal(0, tally.Snapshot().PausedSeconds, 3);
    }

    [Fact]
    public void A_pause_counts_in_full_while_the_pc_sleeps_but_browsing_does_not()
    {
        var tally = new ActivityTally();
        var page = new Uri("https://x.com/home");
        tally.Tick(page, T0);
        tally.Tick(page, T0.AddMinutes(20));
        tally.MarkPaused(T0.AddMinutes(20));

        Assert.Equal(20 * 60, tally.Snapshot().PausedSeconds, 3);
        Assert.Equal(ActivityTally.MaxGap.TotalSeconds, tally.Snapshot().Places["x.com"].Seconds, 3);
    }

    // ---------- Report ----------

    private static ActivityRecord Record(params (string Key, double Minutes, int Visits)[] places) => new()
    {
        Day = ActivityTally.DayOf(T0),
        Places = places.ToDictionary(p => p.Key, p => new PlaceTotal { Seconds = p.Minutes * 60, Visits = p.Visits }),
    };

    [Fact]
    public void A_site_used_evenly_stays_one_line()
    {
        ActivityReport report = ActivityReport.Build(Record(
            ("github.com", 40, 3),
            ("github.com/a", 9, 1), ("github.com/b", 9, 1), ("github.com/c", 8, 1), ("github.com/d", 8, 1)));

        ActivityPlace place = Assert.Single(report.Sites[0].Places);
        Assert.Equal("", place.Path);
        Assert.False(place.IsRest);
    }

    [Fact]
    public void Where_most_of_the_time_goes_is_split_out_down_to_the_deepest_place_holding_it()
    {
        ActivityReport report = ActivityReport.Build(Record(
            ("forum.org", 50, 4),
            ("forum.org/r", 45, 4),
            ("forum.org/r/bikes", 40, 3),
            ("forum.org/r/bikes/comments", 35, 3),
            ("forum.org/r/cats", 5, 1)));

        ActivitySite site = report.Sites[0];
        Assert.Equal(new[] { "r/bikes/comments", "" }, site.Places.Select(p => p.Path));
        Assert.Equal(TimeSpan.FromMinutes(35), site.Places[0].Time);
        Assert.True(site.Places[1].IsRest);
        Assert.Equal(TimeSpan.FromMinutes(15), site.Places[1].Time);
    }

    [Fact]
    public void Several_big_sections_each_get_a_line()
    {
        ActivityReport report = ActivityReport.Build(Record(
            ("youtube.com", 60, 5),
            ("youtube.com/shorts", 30, 3),
            ("youtube.com/watch", 29, 2)));

        Assert.Equal(new[] { "shorts", "watch" }, report.Sites[0].Places.Select(p => p.Path));
    }

    [Fact]
    public void Sites_come_longest_first_and_ones_under_a_minute_only_count_toward_the_total()
    {
        ActivityReport report = ActivityReport.Build(Record(("tiktok.com", 40, 3), ("work.com", 90, 2), ("news.com", 5, 2), ("tiny.com", 0.5, 1)));

        Assert.Equal(TimeSpan.FromMinutes(135.5), report.Total);
        Assert.Equal(new[] { "work.com", "tiktok.com", "news.com" }, report.Sites.Select(s => s.Host));
    }

    // ---------- Not tracked ----------

    [Fact]
    public void An_okay_site_counts_only_into_the_shared_total_and_its_own_places_are_dropped()
    {
        var tally = new ActivityTally();
        DateTimeOffset t = Stay(tally, "https://reddit.com/r/a", T0, 10);
        t = Stay(tally, "https://github.com/x", t, 10);

        tally.SetUntracked(new[] { "www.reddit.com" });
        Assert.DoesNotContain(tally.Snapshot().Places.Keys, k => k.StartsWith("reddit.com", StringComparison.Ordinal));
        Assert.Equal(9, tally.Snapshot().OkaySeconds, 3);

        t = Stay(tally, "https://reddit.com/r/a", t, 10);
        Assert.DoesNotContain(tally.Snapshot().Places.Keys, k => k.StartsWith("reddit.com", StringComparison.Ordinal));
        Assert.Equal(19, tally.Snapshot().OkaySeconds, 3);
        Assert.Equal(10, tally.Snapshot().Places["github.com"].Seconds, 3);

        tally.SetUntracked(Array.Empty<string>());
        Stay(tally, "https://reddit.com/r/a", t, 10);
        Assert.Equal(10, tally.Snapshot().Places["reddit.com"].Seconds, 3);
        Assert.Equal(19, tally.Snapshot().OkaySeconds, 3);
    }

    [Fact]
    public void Restored_totals_move_okay_sites_into_the_shared_total()
    {
        var tally = new ActivityTally();
        tally.SetUntracked(new[] { "reddit.com" });
        tally.Restore(Record(("reddit.com", 20, 2), ("github.com", 30, 1)), T0);

        Assert.Equal(new[] { "github.com" }, tally.Snapshot().Places.Keys);
        Assert.Equal(TimeSpan.FromMinutes(20), ActivityReport.Build(tally.Snapshot()).Okay);
    }
}
