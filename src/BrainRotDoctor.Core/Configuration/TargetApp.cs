using BrainRotDoctor.Core.Apps;

namespace BrainRotDoctor.Core.Configuration;

/// <summary>What an app entry of a rule covers.</summary>
public enum TargetAppKind
{
    /// <summary>One program, by file name or file description.</summary>
    Program,

    /// <summary>One Steam game, by its folder.</summary>
    SteamGame,

    /// <summary>Every game installed with Steam, also ones installed later.</summary>
    AllSteamGames,

    /// <summary>The Steam client: store, library and community.</summary>
    Steam,
}

/// <summary>
/// One app in a rule. Apps are limited whole: there are no pages. The same app may
/// appear in several rules; it is blocked if any active rule blocks it.
/// </summary>
public sealed class TargetApp
{
    public TargetApp(TargetAppKind kind, string label, string? exe = null, string? description = null, string? steamFolder = null)
    {
        Kind = kind;
        Exe = string.IsNullOrWhiteSpace(exe) ? null : exe.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SteamFolder = string.IsNullOrWhiteSpace(steamFolder) ? null : steamFolder.Trim();
        if (kind == TargetAppKind.Program && Exe is null && Description is null)
        {
            throw new ConfigurationException($"App '{label}' is missing its program.");
        }

        if (kind == TargetAppKind.SteamGame && SteamFolder is null)
        {
            throw new ConfigurationException($"Steam game '{label}' is missing its folder.");
        }

        Label = string.IsNullOrWhiteSpace(label) ? SteamFolder ?? Description ?? Exe ?? kind.ToString() : label.Trim();
    }

    public TargetAppKind Kind { get; }

    public string Label { get; }

    /// <summary>A program's file name, lower case.</summary>
    public string? Exe { get; }

    /// <summary>A program's file description; a renamed file still has it.</summary>
    public string? Description { get; }

    /// <summary>A Steam game's folder under "steamapps\common".</summary>
    public string? SteamFolder { get; }

    public bool Matches(AppIdentity app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return Kind switch
        {
            TargetAppKind.Steam => app.Kind == AppKind.Steam,
            TargetAppKind.AllSteamGames => app.Kind == AppKind.SteamGame,
            TargetAppKind.SteamGame => app.Kind == AppKind.SteamGame
                && string.Equals(app.SteamFolder, SteamFolder, StringComparison.OrdinalIgnoreCase),
            _ => app.Kind != AppKind.Steam && MatchesProgram(app),
        };
    }

    /// <summary>
    /// The same program file (with the same description, when both have one), or a
    /// renamed copy of it: another file name with the same description, unless that
    /// description is one many programs share (a game engine's, a launcher's).
    /// </summary>
    private bool MatchesProgram(AppIdentity app)
    {
        bool sameDescription = Description is not null && string.Equals(app.Description, Description, StringComparison.OrdinalIgnoreCase);
        if (Exe is not null && string.Equals(app.Exe, Exe, StringComparison.OrdinalIgnoreCase))
        {
            return Description is null || app.Description is null || sameDescription;
        }

        return sameDescription && !AppIdentity.IsSharedDescription(Description!);
    }
}
