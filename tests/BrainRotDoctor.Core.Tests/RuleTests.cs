using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class RuleTests
{
    private static DateTimeOffset Local(int y, int m, int d, int h, int min)
        => new(new DateTime(y, m, d, h, min, 0, DateTimeKind.Local));

    private static TargetSite Site(string url, bool sub = true)
        => TargetSite.FromUrl(url, url, sub);

    private static Rule Rule(
        TimeSpan? allowance = null,
        bool allDay = true,
        TimeOnly? from = null,
        TimeOnly? to = null,
        IReadOnlyCollection<DayOfWeek>? days = null)
        => new(
            "r", "r",
            new[] { Site("instagram.com/reels") },
            allowance,
            allDay,
            from ?? new TimeOnly(0, 0),
            to ?? new TimeOnly(0, 0),
            days);

    [Fact]
    public void Matches_its_sites()
    {
        Rule rule = Rule();
        Assert.True(rule.MatchesUrl(new Uri("https://instagram.com/reels/x")));
        Assert.False(rule.MatchesUrl(new Uri("https://instagram.com/direct")));
    }

    [Fact]
    public void All_day_rule_is_active_on_selected_days_only()
    {
        // 2026-06-22 is Monday.
        Rule rule = Rule(allDay: true, days: new[] { DayOfWeek.Monday });
        Assert.True(rule.IsActiveAt(Local(2026, 6, 22, 12, 0)));
        Assert.False(rule.IsActiveAt(Local(2026, 6, 23, 12, 0)));
    }

    [Fact]
    public void Windowed_rule_is_active_only_inside_its_window()
    {
        Rule rule = Rule(allDay: false, from: new TimeOnly(9, 0), to: new TimeOnly(17, 0));
        Assert.False(rule.IsActiveAt(Local(2026, 6, 22, 8, 0)));
        Assert.True(rule.IsActiveAt(Local(2026, 6, 22, 12, 0)));
        Assert.False(rule.IsActiveAt(Local(2026, 6, 22, 17, 0)));
    }

    [Fact]
    public void Window_wrapping_midnight_covers_evening_and_next_morning()
    {
        Rule rule = Rule(allDay: false, from: new TimeOnly(23, 0), to: new TimeOnly(7, 0));
        Assert.True(rule.IsActiveAt(Local(2026, 6, 22, 23, 30)));
        Assert.True(rule.IsActiveAt(Local(2026, 6, 23, 6, 0)));
        Assert.False(rule.IsActiveAt(Local(2026, 6, 23, 8, 0)));
    }

    [Fact]
    public void Block_completely_rule_has_no_allowance()
    {
        Rule rule = Rule(allowance: null);
        Assert.True(rule.BlocksCompletely);
    }

    [Fact]
    public void Allowance_must_be_under_one_hour()
    {
        Assert.Throws<ConfigurationException>(() => Rule(allowance: TimeSpan.FromHours(1)));
        Assert.Throws<ConfigurationException>(() => Rule(allowance: TimeSpan.Zero));
    }

    [Fact]
    public void Empty_window_that_is_not_all_day_is_rejected()
    {
        Assert.Throws<ConfigurationException>(
            () => Rule(allDay: false, from: new TimeOnly(9, 0), to: new TimeOnly(9, 0)));
    }

    [Fact]
    public void Rule_with_no_sites_is_rejected()
    {
        Assert.Throws<ConfigurationException>(() => new Rule(
            "r", "r", Array.Empty<TargetSite>(), null, true, default, default, null));
    }
}
