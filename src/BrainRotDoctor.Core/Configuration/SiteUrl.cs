namespace BrainRotDoctor.Core.Configuration;

/// <summary>A typed website address split into its parts.</summary>
public sealed record SiteAddress(string Host, string Path, IReadOnlyList<KeyValuePair<string, string>> Query);

/// <summary>
/// Parses the website addresses a user types ("instagram.com/?variant=following")
/// so nobody has to think about schemes, "www." or URL encoding.
/// </summary>
public static class SiteUrl
{
    /// <summary>Extracts the normalized host and path from a typed URL.</summary>
    public static (string Host, string Path) Parse(string url)
    {
        SiteAddress address = ParseAddress(url);
        return (address.Host, address.Path);
    }

    /// <summary>Extracts the normalized host, path and query conditions from a typed URL.</summary>
    public static SiteAddress ParseAddress(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ConfigurationException("A site URL must not be empty.");
        }

        string text = url.Trim();
        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ConfigurationException($"'{url}' is not a valid website address.");
        }

        string host = NormalizeHost(uri.Host);
        if (host.Length == 0)
        {
            throw new ConfigurationException($"'{url}' is missing a website host.");
        }

        return new SiteAddress(host, uri.AbsolutePath, ParseQuery(uri.Query));
    }

    /// <summary>Lower-cases a host and drops a leading "www." and stray dots.</summary>
    public static string NormalizeHost(string? host)
    {
        string text = (host ?? "").Trim().Trim('.').ToLowerInvariant();
        if (text.Contains("://", StringComparison.Ordinal))
        {
            text = text[(text.IndexOf("://", StringComparison.Ordinal) + 3)..];
        }

        int slash = text.IndexOf('/');
        if (slash >= 0)
        {
            text = text[..slash];
        }

        return text.StartsWith("www.", StringComparison.Ordinal) ? text[4..] : text;
    }

    /// <summary>Splits "?a=1&amp;b" into decoded name/value pairs.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> ParseQuery(string? query)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        string text = (query ?? "").TrimStart('?');
        foreach (string part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = part.IndexOf('=');
            string key = eq >= 0 ? part[..eq] : part;
            string value = eq >= 0 ? part[(eq + 1)..] : "";
            pairs.Add(new(Decode(key), Decode(value)));
        }

        return pairs;
    }

    private static string Decode(string text) => Uri.UnescapeDataString(text.Replace('+', ' '));
}
