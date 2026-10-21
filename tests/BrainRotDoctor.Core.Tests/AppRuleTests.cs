using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Apps;
using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

/// <summary>Apps and games as rule targets: matching, loading, accounting, the coming block, and today's statistics.</summary>
public class AppRuleTests
{
    // Monday, local noon.
    private static readonly DateTimeOffset T0 = new(new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Local));

    private static readonly AppIdentity Valheim = new(AppKind.SteamGame, "Valheim", "valheim.exe", "Valheim", "Valheim");
    private static readonly AppIdentity Skyrim = new(AppKind.SteamGame, "Skyrim Special Edition", "skyrimse.exe", null, "Skyrim Special Edition");
    private static readonly AppIdentity Steam = new(AppKind.Steam, "Steam", "steamwebhelper.exe");
    private static readonly AppIdentity Discord = new(AppKind.Program, "Discord", "discord.exe", "Discord");
    private static readonly AppIdentity RenamedDiscord = new(AppKind.Program, "Discord", "notdiscord.exe", "Discord");
    private static readonly AppIdentity Notepad = new(AppKind.Program, "Notepad", "notepad.exe", "Notepad");

    private static Rule AppRule(string id, TimeSpan? allowance, bool allDay, TimeOnly from, TimeOnly to, params TargetApp[] apps) =>
        new(id, id, Array.Empty<TargetSite>(), allowance, allDay, from, to, null, apps);

    private static BudgetEngine Engine(params Rule[] rules) =>
        new(new BlockerConfiguration(rules), new BudgetEngineOptions { MaxAccountedGap = TimeSpan.FromMinutes(5) });

    private static readonly TargetApp AllGames = new(TargetAppKind.AllSteamGames, "All Steam games");

    [Fact]
    public void All_Steam_games_covers_every_game_but_not_Steam_or_programs()
    {
        Assert.True(AllGames.Matches(Valheim));
        Assert.True(AllGames.Matches(Skyrim));
        Assert.False(AllGames.Matches(Steam));
        Assert.False(AllGames.Matches(Discord));
    }

    [Fact]
    public void One_game_is_known_by_its_folder()
    {
        var game = new TargetApp(TargetAppKind.SteamGame, "Valheim", steamFolder: "valheim");
        Assert.True(game.Matches(Valheim));
        Assert.False(game.Matches(Skyrim));
    }

    [Fact]
    public void A_program_is_known_by_its_file_or_its_description_so_renaming_it_does_not_help()
    {
        var discord = new TargetApp(TargetAppKind.Program, "Discord", exe: "Discord.exe", description: "Discord");
        Assert.True(discord.Matches(Discord));
        Assert.True(discord.Matches(RenamedDiscord));
        Assert.False(discord.Matches(Notepad));
        Assert.False(discord.Matches(Steam));
    }

    [Fact]
    public void Programs_sharing_an_engines_description_stay_apart()
    {
        var gameA = new AppIdentity(AppKind.Program, "Game A", "game-a.exe", "Godot Engine");
        var gameB = new AppIdentity(AppKind.Program, "Game B", "game-b.exe", "Godot Engine");
        var blockA = new TargetApp(TargetAppKind.Program, "Game A", exe: "game-a.exe", description: "Godot Engine");
        Assert.True(blockA.Matches(gameA));
        Assert.False(blockA.Matches(gameB));
        Assert.NotEqual(gameA.Key, gameB.Key);
    }

    [Fact]
    public void Two_programs_called_game_exe_stay_apart_by_their_description()
    {
        var first = new AppIdentity(AppKind.Program, "First", "game.exe", "First Game");
        var second = new AppIdentity(AppKind.Program, "Second", "game.exe", "Second Game");
        var blockFirst = new TargetApp(TargetAppKind.Program, "First", exe: "game.exe", description: "First Game");
        Assert.True(blockFirst.Matches(first));
        Assert.False(blockFirst.Matches(second));
        Assert.NotEqual(first.Key, second.Key);
    }

    [Fact]
    public void Steam_is_only_the_client()
    {
        var steam = new TargetApp(TargetAppKind.Steam, "Steam");
        Assert.True(steam.Matches(Steam));
        Assert.False(steam.Matches(Valheim));
    }

    [Fact]
    public void A_rule_may_hold_only_apps_but_not_nothing()
    {
        Rule rule = ConfigurationLoader.Load("""
            { "rules": [ { "id": "night", "apps": [ { "kind": "steam-games" }, { "kind": "program", "label": "Discord", "exe": "discord.exe" } ] } ] }
            """).Rules.Single();
        Assert.Equal(2, rule.Apps.Count);
        Assert.Empty(rule.Sites);
        Assert.True(rule.MatchesApp(Valheim));

        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load("""{ "rules": [ { "id": "empty" } ] }"""));
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load("""{ "rules": [ { "id": "x", "apps": [ { "kind": "steam-game" } ] } ] }"""));
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load("""{ "rules": [ { "id": "x", "apps": [ { "kind": "arcade" } ] } ] }"""));
    }

    [Fact]
    public void A_counting_app_uses_its_rules_allowance_and_is_blocked_when_spent()
    {
        BudgetEngine engine = Engine(AppRule("games", TimeSpan.FromSeconds(10), true, default, default, AllGames));
        engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim }, T0);
        TickResult spent = engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim }, T0 + TimeSpan.FromSeconds(10));

        AppCloseDecision decision = Assert.Single(spent.AppCloseDecisions);
        Assert.Equal(Valheim, decision.App);
        Assert.Equal("games", decision.RuleId);
        Assert.Equal("games", engine.BlockingRuleFor(Skyrim, T0 + TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void Two_games_of_one_rule_are_charged_once()
    {
        BudgetEngine engine = Engine(AppRule("games", TimeSpan.FromMinutes(5), true, default, default, AllGames));
        engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim, Skyrim }, T0);
        TickResult r = engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim, Skyrim }, T0 + TimeSpan.FromSeconds(3));
        Assert.Equal(TimeSpan.FromSeconds(3), r.Rules.Single().Consumed);
    }

    [Fact]
    public void A_block_completely_rule_starting_within_the_minute_is_foreseen_to_the_second()
    {
        var at = new DateTimeOffset(new DateTime(2026, 6, 22, 22, 59, 20, DateTimeKind.Local));
        BudgetEngine engine = Engine(AppRule("night", null, false, new TimeOnly(23, 0), new TimeOnly(7, 0), AllGames));

        BlockAhead? ahead = engine.BlockAheadFor(Valheim, at, TimeSpan.FromMinutes(1), counting: true);
        Assert.Equal(new BlockAhead(at + TimeSpan.FromSeconds(40), "night"), ahead);
        Assert.Null(engine.BlockAheadFor(Valheim, at - TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), counting: true));
        Assert.Equal(at + TimeSpan.FromMinutes(1), engine.BlockAheadFor(Valheim, at + TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), counting: true)!.At);
    }

    [Fact]
    public void An_allowance_running_out_while_the_app_counts_is_foreseen()
    {
        BudgetEngine engine = Engine(AppRule("games", TimeSpan.FromMinutes(2), true, default, default, AllGames));
        engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim }, T0);
        engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim }, T0 + TimeSpan.FromSeconds(80));

        DateTimeOffset now = T0 + TimeSpan.FromSeconds(80);
        Assert.Equal(new BlockAhead(T0 + TimeSpan.FromMinutes(2), "games"), engine.BlockAheadFor(Valheim, now, TimeSpan.FromMinutes(1), counting: true));
        Assert.Null(engine.BlockAheadFor(Valheim, now, TimeSpan.FromMinutes(1), counting: false));
        Assert.Null(engine.BlockAheadFor(Valheim, now, TimeSpan.FromSeconds(30), counting: true));
    }

    [Fact]
    public void An_allowance_refilled_on_the_hour_before_it_runs_out_is_no_block()
    {
        BudgetEngine engine = Engine(AppRule("games", TimeSpan.FromMinutes(2), true, default, default, AllGames));
        DateTimeOffset start = T0 + TimeSpan.FromMinutes(58);
        engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim }, start);
        engine.Tick(Array.Empty<BrowserWindowState>(), new[] { Valheim }, start + TimeSpan.FromSeconds(90));

        // 30 seconds left, but the hour turns in 30 seconds too: a fresh allowance comes first.
        Assert.Null(engine.BlockAheadFor(Valheim, start + TimeSpan.FromSeconds(90), TimeSpan.FromMinutes(1), counting: true));
    }

    [Fact]
    public void A_blocked_app_is_blocked_now()
    {
        BudgetEngine engine = Engine(AppRule("always", null, true, default, default, new TargetApp(TargetAppKind.Steam, "Steam")));
        Assert.Equal(new BlockAhead(T0, "always"), engine.BlockAheadFor(Steam, T0, TimeSpan.FromMinutes(1), counting: false));
        Assert.Null(engine.BlockAheadFor(Discord, T0, TimeSpan.FromMinutes(1), counting: true));
    }

    [Fact]
    public void The_app_in_front_is_counted_when_no_page_is()
    {
        var tally = new ActivityTally();
        tally.Tick(null, Discord, T0);
        tally.Tick(null, Discord, T0 + TimeSpan.FromSeconds(3));
        tally.Tick(new Uri("https://reddit.com/r/x"), Discord, T0 + TimeSpan.FromSeconds(5));

        ActivityRecord record = tally.Snapshot();
        AppTotal discord = record.Apps[Discord.Key];
        Assert.Equal(3, discord.Seconds);
        Assert.Equal(1, discord.Visits);
        Assert.Equal(Discord, discord.App);
        Assert.Equal(2, record.Places["reddit.com"].Seconds);
    }

    [Fact]
    public void An_okay_app_joins_the_shared_okay_time_with_what_it_had_today()
    {
        var tally = new ActivityTally();
        tally.Tick(null, Discord, T0);
        tally.Tick(null, Discord, T0 + TimeSpan.FromSeconds(4));
        tally.SetUntrackedApps(new[] { Discord.Key });
        tally.Tick(null, Discord, T0 + TimeSpan.FromSeconds(5));

        ActivityRecord record = tally.Snapshot();
        Assert.Empty(record.Apps);
        Assert.Equal(5, record.OkaySeconds);
    }

    [Fact]
    public void The_report_lists_apps_with_a_minute_or_more_and_counts_them_in_the_total()
    {
        var record = new ActivityRecord
        {
            Day = ActivityTally.DayOf(T0),
            Apps =
            {
                [Valheim.Key] = new AppTotal { Seconds = 600, App = Valheim },
                [Discord.Key] = new AppTotal { Seconds = 30, App = Discord },
            },
        };

        ActivityReport report = ActivityReport.Build(record);
        Assert.Equal(Valheim, Assert.Single(report.Apps).App);
        Assert.Equal(TimeSpan.FromSeconds(630), report.Total);
    }
}
