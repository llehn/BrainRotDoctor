namespace BrainRotDoctor.Core.Apps;

/// <summary>What kind of program an app is.</summary>
public enum AppKind
{
    /// <summary>Any program, known by its program file.</summary>
    Program,

    /// <summary>A game installed with Steam: a program inside a Steam library's games folder.</summary>
    SteamGame,

    /// <summary>The Steam client itself: store, library and community.</summary>
    Steam,
}

/// <summary>
/// One app as the PC runs it: what it is, decided once per process by the Windows
/// layer. Programs are known by their file name and their file description (which
/// survives renaming the file); Steam games by their folder under "steamapps\common".
/// This is the only app-derived input the model takes; how it is found is outside it.
/// </summary>
/// <param name="Kind">Program, Steam game or Steam.</param>
/// <param name="Name">The name to show ("Discord", "Cyberpunk 2077").</param>
/// <param name="Exe">The program file name ("discord.exe"); lower case.</param>
/// <param name="Description">The program file's description, when it has one.</param>
/// <param name="SteamFolder">For a Steam game, its folder name under "steamapps\common".</param>
public sealed record AppIdentity(AppKind Kind, string Name, string Exe, string? Description = null, string? SteamFolder = null)
{
    /// <summary>
    /// Descriptions many different programs carry: a game engine's, a runtime's, a
    /// generic launcher's. A program isn't told apart, or recognized after a rename, by these.
    /// </summary>
    private static readonly HashSet<string> SharedDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Godot Engine", "Electron", "Unity", "Unity Player", "Unreal Engine", "UnrealGame", "Launcher", "Game",
        "Game Launcher", "Setup", "Installer", "Application", "Java(TM) Platform SE binary", "Python", "Node.js",
    };

    /// <summary>
    /// Stable key of the app across processes and days: "steam", "steam-game:valheim",
    /// or "program:discord.exe|discord" (two programs both called game.exe stay apart).
    /// </summary>
    public string Key => Kind switch
    {
        AppKind.Steam => "steam",
        AppKind.SteamGame => "steam-game:" + (SteamFolder ?? Exe).ToLowerInvariant(),
        _ => "program:" + Exe.ToLowerInvariant() + (Description is { } d && !IsSharedDescription(d) ? "|" + d.ToLowerInvariant() : ""),
    };

    /// <summary>True for a description many different programs carry.</summary>
    public static bool IsSharedDescription(string description) => SharedDescriptions.Contains(description.Trim());
}
