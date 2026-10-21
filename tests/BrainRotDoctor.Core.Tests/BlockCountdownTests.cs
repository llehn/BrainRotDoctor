using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Apps;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

/// <summary>When an open app is closed: on time when the block was foreseen, a minute later when it wasn't, at once when it shows up blocked.</summary>
public class BlockCountdownTests
{
    private static readonly DateTimeOffset T0 = new(new DateTime(2026, 6, 22, 22, 0, 0, DateTimeKind.Local));
    private static readonly AppIdentity Valheim = new(AppKind.SteamGame, "Valheim", "valheim.exe", null, "Valheim");
    private static readonly AppIdentity Steam = new(AppKind.Steam, "Steam", "steamwebhelper.exe");

    private static DateTimeOffset At(double seconds) => T0 + TimeSpan.FromSeconds(seconds);

    private static CountdownResult Tick(BlockCountdown countdown, double seconds, BlockAhead? ahead, AppIdentity? app = null, bool held = false) =>
        countdown.Tick(new[] { new CountingApp(app ?? Valheim, ahead, held) }, At(seconds));

    private static CountdownResult Nothing(BlockCountdown countdown, double seconds) => countdown.Tick(Array.Empty<CountingApp>(), At(seconds));

    [Fact]
    public void A_foreseen_block_is_announced_and_closes_on_time()
    {
        var countdown = new BlockCountdown();
        CountdownResult warned = Tick(countdown, 0, new BlockAhead(At(40), "night"));
        Assert.Equal(new AppWarning(Valheim, At(40), "night"), Assert.Single(warned.Warn));
        Assert.Empty(warned.Close);

        Assert.Single(Tick(countdown, 39, new BlockAhead(At(40), "night")).Warn);
        CountdownResult closed = Tick(countdown, 40, new BlockAhead(At(40), "night"));
        Assert.Equal(new AppCloseDecision(Valheim, "night"), Assert.Single(closed.Close));
        Assert.Empty(closed.Warn);
    }

    [Fact]
    public void A_block_nobody_could_foresee_gives_a_running_app_a_minute()
    {
        var countdown = new BlockCountdown();
        Tick(countdown, 0, null);
        CountdownResult warned = Tick(countdown, 1, new BlockAhead(At(1), "new rule"));
        Assert.Equal(At(61), Assert.Single(warned.Warn).At);

        Assert.Empty(Tick(countdown, 60, new BlockAhead(At(60), "new rule")).Close);
        Assert.Single(Tick(countdown, 61, new BlockAhead(At(61), "new rule")).Close);
    }

    [Fact]
    public void An_app_that_shows_up_blocked_closes_at_once()
    {
        var countdown = new BlockCountdown();
        CountdownResult first = Tick(countdown, 0, new BlockAhead(At(0), "night"));
        Assert.Single(first.Close);
        Assert.Empty(first.Warn);
    }

    [Fact]
    public void Starting_it_again_buys_no_time()
    {
        var countdown = new BlockCountdown();
        Tick(countdown, 0, null);
        Tick(countdown, 1, new BlockAhead(At(1), "night"));
        Assert.Single(Tick(countdown, 61, new BlockAhead(At(61), "night")).Close);

        // Closed, and started again a few seconds later, or long after: closed at once either way.
        Nothing(countdown, 62);
        Assert.Single(Tick(countdown, 66, new BlockAhead(At(66), "night")).Close);
        Nothing(countdown, 67);
        Nothing(countdown, 90);
        Assert.Single(Tick(countdown, 91, new BlockAhead(At(91), "night")).Close);
    }

    [Fact]
    public void A_launcher_handing_over_to_its_game_still_counts_as_running()
    {
        var countdown = new BlockCountdown();
        Tick(countdown, 0, null);
        Nothing(countdown, 3);
        Assert.Equal(At(6) + BlockCountdown.Lead, Assert.Single(Tick(countdown, 6, new BlockAhead(At(6), "new rule")).Warn).At);
    }

    [Fact]
    public void Steam_is_never_warned()
    {
        var countdown = new BlockCountdown();
        Tick(countdown, 0, null, Steam);
        CountdownResult blocked = Tick(countdown, 1, new BlockAhead(At(1), "night"), Steam);
        Assert.Empty(blocked.Warn);
        Assert.Single(blocked.Close);

        var foreseen = new BlockCountdown();
        Assert.Empty(Tick(foreseen, 0, new BlockAhead(At(30), "night"), Steam).Warn);
    }

    [Fact]
    public void A_held_app_is_left_alone()
    {
        var countdown = new BlockCountdown();
        CountdownResult result = Tick(countdown, 0, new BlockAhead(At(0), "night"), held: true);
        Assert.Empty(result.Close);
        Assert.Empty(result.Warn);
    }

    [Fact]
    public void A_foreseen_block_that_does_not_come_is_dropped()
    {
        var countdown = new BlockCountdown();
        Tick(countdown, 0, new BlockAhead(At(30), "games"));

        // At its time the block is later after all (an allowance refilled): no close, and counting starts over.
        CountdownResult result = Tick(countdown, 30, new BlockAhead(At(50), "games"));
        Assert.Empty(result.Close);
        Assert.Equal(At(50), Assert.Single(Tick(countdown, 31, new BlockAhead(At(50), "games")).Warn).At);
    }

    [Fact]
    public void Lifting_the_block_ends_the_countdown()
    {
        var countdown = new BlockCountdown();
        Tick(countdown, 0, null);
        Tick(countdown, 1, new BlockAhead(At(1), "new rule"));
        CountdownResult lifted = Tick(countdown, 2, null);
        Assert.Empty(lifted.Warn);
        Assert.Empty(lifted.Close);
    }
}
