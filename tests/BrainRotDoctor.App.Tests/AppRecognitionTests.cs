using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Apps;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// What a program file is as an app: a Steam game by its folder, Steam itself, or a
/// program; and never the Windows shell, a shared runtime, a browser or BrainRotDoctor.
/// </summary>
public sealed class AppRecognitionTests
{
    private readonly WindowsAppObserver _observer = new();

    [Theory]
    [InlineData(@"C:\Windows\explorer.exe")]
    [InlineData(@"C:\Windows\System32\ApplicationFrameHost.exe")]
    [InlineData(@"C:\Program Files\Java\bin\javaw.exe")]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe")]
    [InlineData(@"C:\Program Files\Mozilla Firefox\firefox.exe")]
    [InlineData(@"C:\Steam\steamapps\common\Valheim\GameOverlayUI.exe")]
    [InlineData(@"C:\Users\x\AppData\Local\Programs\BrainRotDoctor\BrainRotDoctor.exe")]
    public void Never_an_app(string path) => Assert.Null(_observer.Identify(path));

    [Theory]
    [InlineData(@"C:\Steam\steam.exe")]
    [InlineData(@"C:\Steam\bin\cef\cef.win64\steamwebhelper.exe")]
    public void The_Steam_client_is_Steam(string path) => Assert.Equal(AppKind.Steam, _observer.Identify(path)!.Kind);

    [Fact]
    public void A_program_in_any_Steam_games_folder_is_that_game()
    {
        AppIdentity game = _observer.Identify(@"D:\Games\SteamLibrary\steamapps\common\Some Game\bin\x64\game.exe")!;
        Assert.Equal(AppKind.SteamGame, game.Kind);
        Assert.Equal("Some Game", game.SteamFolder);
        Assert.Equal("steam-game:some game", game.Key);
    }

    [Fact]
    public void Steams_own_tools_are_programs_not_games()
    {
        Assert.Null(SteamInstall.SteamGameFolder(@"C:\Steam\steamapps\common\Steamworks Shared\_CommonRedist\vcredist.exe"));
        Assert.Null(SteamInstall.SteamGameFolder(@"C:\Steam\steamapps\common\SteamVR\bin\win64\vrmonitor.exe"));
        Assert.Null(SteamInstall.SteamGameFolder(@"C:\Program Files\Discord\Discord.exe"));
        Assert.Equal("Valheim", SteamInstall.SteamGameFolder(@"C:\Steam\steamapps\common\Valheim\valheim.exe"));
        Assert.Equal(AppKind.Program, _observer.Identify(@"C:\Steam\steamapps\common\SteamVR\bin\win64\vrmonitor.exe")!.Kind);
    }

    [Fact]
    public void Any_other_program_is_known_by_its_file()
    {
        AppIdentity app = _observer.Identify(@"C:\Nowhere\SomeChat.exe")!;
        Assert.Equal(AppKind.Program, app.Kind);
        Assert.Equal("somechat.exe", app.Exe);
        Assert.Equal("SomeChat", app.Name);
    }

    [Fact]
    public void Todays_app_time_survives_a_restart()
    {
        string path = Path.Combine(Path.GetTempPath(), "brainrotdoctor-tests", Guid.NewGuid().ToString("N"), "activity.json");
        var store = new ActivityStore(path);
        var discord = new AppIdentity(AppKind.Program, "Discord", "discord.exe", "Discord");
        var record = new ActivityRecord { Day = "2026-10-21" };
        record.Apps[discord.Key] = new AppTotal { Seconds = 90, Visits = 2, App = discord };
        store.Save(record);

        AppTotal back = store.Load()!.Apps[discord.Key];
        Assert.Equal(90, back.Seconds);
        Assert.Equal(discord, back.App);
    }

    [Fact]
    public void A_hand_edit_that_takes_apps_out_of_a_rule_is_put_back_in_strict_mode()
    {
        const string accepted = """
        { "rules": [ { "id": "night", "name": "Night", "apps": [ { "kind": "steam-games" } ] } ] }
        """;
        const string edited = """
        { "rules": [ { "id": "night", "name": "Night", "apps": [ { "kind": "program", "label": "Notepad", "exe": "notepad.exe" } ] } ] }
        """;

        EditableConfiguration restored = EditableConfiguration.FromJson(RuleGuard.Restore(accepted, edited));
        Assert.Equal(Core.Configuration.TargetAppKind.AllSteamGames, Assert.Single(Assert.Single(restored.Rules).Apps).Kind);
    }
}
