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
    public int? AllowanceMinutes { get; set; }
    public bool? AllDay { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public List<string>? Days { get; set; }
    public List<SiteDocument>? Sites { get; set; }
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
