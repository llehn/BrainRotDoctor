using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class BudgetEngineTests
{
    // Monday, local noon. Hour windows align to the local clock hour.
    private static readonly DateTimeOffset T0 =
        new(new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Local));

    private const string Video = "video";

    private static TargetSite Site(string url) => TargetSite.FromUrl(url, url, true);

    private static BudgetEngine Engine(TimeSpan? allowance = null, TimeSpan? cap = null)
    {
        var rule = new Rule(
            Video, "Short video",
            new[] { Site("youtube.com/shorts"), Site("instagram.com/reels") },
            allowance ?? TimeSpan.FromSeconds(10),
            allDay: true, default, default, null);

        return new BudgetEngine(
            new BlockerConfiguration(new[] { rule }),
            new BudgetEngineOptions { MaxAccountedGap = cap ?? TimeSpan.FromMinutes(5) });
    }

    private static BrowserWindowState Win(string id, string? url) => BrowserWindowState.FromString(id, url);

    private static IReadOnlyList<BrowserWindowState> Windows(params BrowserWindowState[] w) => w;

    private static TimeSpan Consumed(TickResult r) => r.Rules.Single(x => x.RuleId == Video).Consumed;

    private const string Shorts = "https://youtube.com/shorts/abc";
    private const string Reels = "https://instagram.com/reels/xyz";
    private const string IgDirect = "https://instagram.com/direct/inbox";

    [Fact]
    public void First_tick_establishes_baseline_and_charges_nothing()
    {
        var engine = Engine();
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0);
        Assert.Equal(TimeSpan.Zero, Consumed(r));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Matching_selected_tab_consumes_allowance()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.FromSeconds(4), Consumed(r));
    }

    [Fact]
    public void Non_matching_tab_consumes_nothing()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", IgDirect)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", IgDirect)), T0 + TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.Zero, Consumed(r));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Rule_is_charged_once_across_multiple_matching_windows()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts), Win("w2", Reels)), T0);
        TickResult r = engine.Tick(
            Windows(Win("w1", Shorts), Win("w2", Reels)),
            T0 + TimeSpan.FromSeconds(6));
        Assert.Equal(TimeSpan.FromSeconds(6), Consumed(r));
    }

    [Fact]
    public void Exhaustion_closes_the_matching_tab_on_the_crossing_tick()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10));

        CloseDecision d = Assert.Single(r.CloseDecisions);
        Assert.Equal("w1", d.WindowId);
        Assert.Equal(Video, d.RuleId);
    }

    [Fact]
    public void Blocking_rule_is_reported_for_any_page_of_an_exhausted_rule_without_charging()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        Assert.Null(engine.BlockingRuleFor(new Uri(Shorts), T0 + TimeSpan.FromSeconds(5)));

        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10));

        // Swiping to the next short changes the address; it is still blocked.
        Assert.Equal(Video, engine.BlockingRuleFor(new Uri("https://youtube.com/shorts/next"), T0 + TimeSpan.FromSeconds(13)));
        Assert.Null(engine.BlockingRuleFor(new Uri(IgDirect), T0 + TimeSpan.FromSeconds(13)));
        Assert.Equal(TimeSpan.FromSeconds(10), Consumed(engine.Tick(Windows(), T0 + TimeSpan.FromSeconds(14))));
    }

    [Fact]
    public void Reopened_surface_is_closed_again_before_the_hour_resets()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10));

        TickResult r = engine.Tick(Windows(Win("w9", Shorts)), T0 + TimeSpan.FromSeconds(40));
        Assert.Equal("w9", Assert.Single(r.CloseDecisions).WindowId);
    }

    [Fact]
    public void Allowance_refills_at_the_next_clock_hour()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10)); // exhaust

        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromHours(1));
        Assert.Empty(r.CloseDecisions);
        Assert.Equal(TimeSpan.Zero, Consumed(r));
    }

    [Fact]
    public void Over_long_gap_within_the_hour_is_clamped()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(20), cap: TimeSpan.FromSeconds(5));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(30));
        Assert.Equal(TimeSpan.FromSeconds(5), Consumed(r));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Clock_going_backwards_charges_nothing()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 - TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.Zero, Consumed(r));
    }

    [Fact]
    public void Block_completely_rule_closes_its_sites_while_active()
    {
        var rule = new Rule(
            "bedtime", "Bedtime",
            new[] { Site("instagram.com") },
            allowance: null, allDay: true, default, default, null);
        var engine = new BudgetEngine(new BlockerConfiguration(new[] { rule }));

        TickResult r = engine.Tick(Windows(Win("w1", "https://instagram.com/reels/x")), T0);
        Assert.Equal("w1", Assert.Single(r.CloseDecisions).WindowId);
        Assert.True(r.Rules.Single().IsBlocking);
    }

    [Fact]
    public void Rule_outside_its_window_does_not_block_or_charge()
    {
        // Active 23:00-07:00; at noon it is inactive.
        var rule = new Rule(
            "bedtime", "Bedtime",
            new[] { Site("instagram.com/reels") },
            allowance: null, allDay: false, new TimeOnly(23, 0), new TimeOnly(7, 0), null);
        var engine = new BudgetEngine(new BlockerConfiguration(new[] { rule }));

        TickResult r = engine.Tick(Windows(Win("w1", Reels)), T0);
        Assert.Empty(r.CloseDecisions);
        Assert.False(r.Rules.Single().IsActive);
    }

    [Fact]
    public void Restored_usage_carries_into_a_rebuilt_engine_within_the_same_hour()
    {
        // Spend 4s, then rebuild the engine (as a config save does) and hand it the
        // exported usage: the consumption must survive, not reset to zero.
        var first = Engine(allowance: TimeSpan.FromSeconds(10));
        first.Tick(Windows(Win("w1", Shorts)), T0);
        first.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));

        var rebuilt = Engine(allowance: TimeSpan.FromSeconds(10));
        rebuilt.RestoreUsage(first.ExportUsage());

        // Baseline tick charges nothing; the restored 4s is already present.
        TickResult r = rebuilt.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.FromSeconds(4), Consumed(r));
    }

    [Fact]
    public void Restored_usage_from_a_past_hour_is_cleared_on_the_next_tick()
    {
        var first = Engine(allowance: TimeSpan.FromSeconds(10));
        first.Tick(Windows(Win("w1", Shorts)), T0);
        first.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));

        var rebuilt = Engine(allowance: TimeSpan.FromSeconds(10));
        rebuilt.RestoreUsage(first.ExportUsage());

        // An hour later the saved usage belongs to a past clock hour and is dropped.
        TickResult r = rebuilt.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromHours(1));
        Assert.Equal(TimeSpan.Zero, Consumed(r));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Restore_ignores_usage_for_unknown_rules()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.RestoreUsage(new[] { new RuleUsage("nonexistent", T0, TimeSpan.FromSeconds(5)) });

        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(2));
        Assert.Equal(TimeSpan.FromSeconds(2), Consumed(r));
    }

    [Fact]
    public void Block_completely_rules_export_no_usage()
    {
        var rule = new Rule(
            "bedtime", "Bedtime",
            new[] { Site("instagram.com") },
            allowance: null, allDay: true, default, default, null);
        var engine = new BudgetEngine(new BlockerConfiguration(new[] { rule }));

        Assert.Empty(engine.ExportUsage());
    }

    [Fact]
    public void Snapshot_reports_remaining_for_an_allowance_rule()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));

        RuleSnapshot s = r.Rules.Single();
        Assert.Equal(TimeSpan.FromSeconds(6), s.Remaining);
        Assert.False(s.IsBlocking);
        Assert.True(s.IsActive);
        Assert.True(s.WasActiveThisTick);
    }
}
