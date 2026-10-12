using BrainRotDoctor.Core.Configuration;

namespace BrainRotDoctor.Core.Activity;

/// <summary>
/// Turns a page address into the places it belongs to: the site, then at most the
/// first few sections of its path, stopping at the first section that names one
/// single item (an id, a code, a post's title). "www.reddit.com/r/dotnet/comments/1h2x9/why"
/// belongs to reddit.com, reddit.com/r, reddit.com/r/dotnet and
/// reddit.com/r/dotnet/comments. Query, fragment and everything that would
/// identify a single page are dropped, so time is only ever counted per place,
/// never per page.
/// </summary>
public static class PlaceKey
{
    /// <summary>How many path sections a place goes down at most.</summary>
    public const int MaxDepth = 3;

    private static readonly string[] MobilePrefixes = { "m.", "mobile." };

    /// <summary>
    /// The places of <paramref name="url"/>, widest first: the site's host, then each
    /// deeper section ("host/a", "host/a/b"). Empty for anything that isn't a web page.
    /// </summary>
    public static IReadOnlyList<string> Of(Uri? url)
    {
        if (url is null || !url.IsAbsoluteUri || url.Scheme is not ("http" or "https"))
        {
            return Array.Empty<string>();
        }

        string host = Host(url.Host);
        if (host.Length == 0)
        {
            return Array.Empty<string>();
        }

        var keys = new List<string> { host };
        string key = host;
        foreach (string raw in url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Take(MaxDepth))
        {
            string section = Uri.UnescapeDataString(raw).Trim().ToLowerInvariant();
            if (section.Length == 0 || NamesOneItem(Uri.UnescapeDataString(raw)))
            {
                break;
            }

            key += "/" + section;
            keys.Add(key);
        }

        return keys;
    }

    /// <summary>The host part of a place key.</summary>
    public static string HostOf(string key)
    {
        int slash = key.IndexOf('/');
        return slash < 0 ? key : key[..slash];
    }

    /// <summary>The path part of a place key, without leading slash; "" for a whole site.</summary>
    public static string PathOf(string key)
    {
        int slash = key.IndexOf('/');
        return slash < 0 ? "" : key[(slash + 1)..];
    }

    /// <summary>The place one level up, or null for a whole site.</summary>
    public static string? ParentOf(string key)
    {
        int slash = key.LastIndexOf('/');
        return slash < 0 ? null : key[..slash];
    }

    /// <summary>Lower case, without "www.", "m." or "mobile." in front.</summary>
    public static string Host(string host)
    {
        string normalized = SiteUrl.NormalizeHost(host);
        foreach (string prefix in MobilePrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal) && normalized.Length > prefix.Length)
            {
                return normalized[prefix.Length..];
            }
        }

        return normalized;
    }

    /// <summary>
    /// Whether a path section names one item rather than a place: numbers and
    /// codes (two or more digits), random-looking ids (mixed case, no separators),
    /// file names, long strings, and post titles turned into slugs.
    /// </summary>
    internal static bool NamesOneItem(string section)
    {
        if (section.Length > 30 || section.Contains('.'))
        {
            return true;
        }

        if (section.Count(char.IsDigit) >= 2)
        {
            return true;
        }

        bool separated = section.Contains('-') || section.Contains('_');
        if (!separated && section.Length >= 8 && section.Skip(1).Any(char.IsUpper) && section.Any(char.IsLower))
        {
            return true;
        }

        return section.Count(c => c is '-' or '_') >= 3;
    }
}
