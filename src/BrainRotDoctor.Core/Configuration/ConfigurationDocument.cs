using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// The on-disk JSON shape of the configuration, shared by the loader (which
/// validates it into a <see cref="BlockerConfiguration"/>) and the editor (which
/// round-trips it). Example:
/// <code>
/// {
///   "rules": [
///     {
///       "id": "feeds", "name": "Feeds",
///       "enabled": false,               // omit when the rule is on
///       "allowanceMinutes": 5,          // omit to block completely
///       "allDay": true,                 // or false with "from"/"to"
///       "days": ["Monday"],             // omit for every day
///       "sites": [
///         {
///           "label": "Instagram", "host": "instagram.com", "catalogId": "instagram",
///           "everythingElse": "allow",
///           "pages": [
///             { "name": "Home feed", "path": "/", "match": "exact", "action": "block" },
///             { "name": "Following feed", "path": "/", "match": "exact",
///               "query": { "variant": "following" }, "action": "allow" }
///           ]
///         }
///       ],
///       "apps": [                       // optional; a rule needs a site or an app
///         { "kind": "steam-games" },
///         { "kind": "steam-game", "label": "Valheim", "folder": "Valheim" },
///         { "kind": "program", "label": "Discord", "exe": "discord.exe", "description": "Discord" }
///       ]
///     }
///   ]
/// }
/// </code>
/// Older files listed sites as <c>{ "catalogId": "ig-reels" }</c> or
/// <c>{ "label", "url", "includeSubpaths" }</c>; <see cref="SiteMigration"/>
/// converts those on load.
/// </summary>
public sealed class ConfigurationDocument
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [JsonPropertyName("rules")]
    public List<RuleDocument>? Rules { get; set; }

    public static ConfigurationDocument Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ConfigurationException("Configuration JSON is empty.");
        }

        try
        {
            return JsonSerializer.Deserialize<ConfigurationDocument>(json, ReadOptions)
                ?? throw new ConfigurationException("Configuration JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Configuration JSON is malformed: {ex.Message}", ex);
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, WriteOptions);
}

public sealed class RuleDocument
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    /// <summary>False switches the rule off; omitted means on.</summary>
    public bool? Enabled { get; set; }

    public int? AllowanceMinutes { get; set; }
    public bool? AllDay { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public List<string>? Days { get; set; }
    public List<SiteDocument>? Sites { get; set; }
    public List<AppDocument>? Apps { get; set; }
}

public sealed class AppDocument
{
    public const string Program = "program";
    public const string SteamGame = "steam-game";
    public const string AllSteamGames = "steam-games";
    public const string Steam = "steam";

    /// <summary>"program", "steam-game", "steam-games" or "steam".</summary>
    public string? Kind { get; set; }

    public string? Label { get; set; }

    /// <summary>A program's file name ("discord.exe").</summary>
    public string? Exe { get; set; }

    /// <summary>A program's file description, which a renamed file keeps.</summary>
    public string? Description { get; set; }

    /// <summary>A Steam game's folder under "steamapps\common".</summary>
    public string? Folder { get; set; }

    public static string KindText(TargetAppKind kind) => kind switch
    {
        TargetAppKind.SteamGame => SteamGame,
        TargetAppKind.AllSteamGames => AllSteamGames,
        TargetAppKind.Steam => Steam,
        _ => Program,
    };

    public static TargetAppKind ParseKind(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        SteamGame => TargetAppKind.SteamGame,
        AllSteamGames => TargetAppKind.AllSteamGames,
        Steam => TargetAppKind.Steam,
        null or "" or Program => TargetAppKind.Program,
        _ => throw new ConfigurationException($"Unknown app kind '{text}'. Use program, steam-game, steam-games or steam."),
    };
}

public sealed class SiteDocument
{
    public const string Block = "block";
    public const string Allow = "allow";

    public string? CatalogId { get; set; }
    public string? Label { get; set; }
    public string? Host { get; set; }

    /// <summary>"block" or "allow": what happens on the site outside the listed pages.</summary>
    public string? EverythingElse { get; set; }

    public List<PageDocument>? Pages { get; set; }

    /// <summary>Old format: one typed address per site.</summary>
    public string? Url { get; set; }

    /// <summary>Old format: whether pages beneath <see cref="Url"/> count too.</summary>
    public bool? IncludeSubpaths { get; set; }

    [JsonIgnore]
    public bool IsLegacy => string.IsNullOrWhiteSpace(Host) && Pages is null;
}

public sealed class PageDocument
{
    public string? Name { get; set; }
    public string? Path { get; set; }

    /// <summary>"exact", "under" or "prefix" (see <see cref="PageMatch"/>).</summary>
    public string? Match { get; set; }

    public Dictionary<string, string>? Query { get; set; }

    /// <summary>"block" or "allow".</summary>
    public string? Action { get; set; }

    public static string MatchText(PageMatch match) => match switch
    {
        PageMatch.Exact => "exact",
        PageMatch.Prefix => "prefix",
        _ => "under",
    };

    public static PageMatch ParseMatch(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "exact" => PageMatch.Exact,
        "prefix" => PageMatch.Prefix,
        null or "" or "under" => PageMatch.Under,
        _ => throw new ConfigurationException($"Unknown page match '{text}'. Use exact, under or prefix."),
    };
}
