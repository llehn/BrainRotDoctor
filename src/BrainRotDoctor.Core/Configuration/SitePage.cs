namespace BrainRotDoctor.Core.Configuration;

/// <summary>How a page's path is compared with the path of an address.</summary>
public enum PageMatch
{
    /// <summary>Only this exact path (a trailing slash is ignored).</summary>
    Exact,

    /// <summary>This path and every path beneath it: "/explore" covers "/explore/tags/x".</summary>
    Under,

    /// <summary>Every path that starts with this text: "/reel" covers "/reel/x" and "/reels".</summary>
    Prefix,
}

/// <summary>
/// One named part of a website ("Home feed", "Reels", "Messages") together with
/// whether a rule blocks or allows it.
///
/// A page is identified by a path, a <see cref="PageMatch"/> mode and optional
/// query conditions ("variant=following"). When several pages of a site fit the
/// same address the most specific one decides (see <see cref="Score"/>), which is
/// what lets a rule block Instagram's home feed while keeping the Following feed —
/// the same start page plus <c>?variant=following</c> — open.
/// </summary>
public sealed class SitePage
{
    private const int QueryWeight = 1000;
    private const int ExactWeight = 500;

    public SitePage(
        string name,
        string path,
        PageMatch match,
        IReadOnlyList<KeyValuePair<string, string>>? query,
        bool blocks)
    {
        Path = NormalizePath(path);
        Name = string.IsNullOrWhiteSpace(name) ? Path : name.Trim();
        Match = match;
        Blocks = blocks;

        var conditions = new List<KeyValuePair<string, string>>();
        foreach ((string key, string value) in query ?? Array.Empty<KeyValuePair<string, string>>())
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ConfigurationException($"Page '{Name}' has a condition without a name.");
            }

            conditions.Add(new(key.Trim(), (value ?? "").Trim()));
        }

        Query = conditions;
    }

    public string Name { get; }

    /// <summary>The normalized path: starts with "/", no trailing slash (except the root).</summary>
    public string Path { get; }

    public PageMatch Match { get; }

    /// <summary>
    /// Conditions on the address's query string, all of which must hold. A
    /// condition with an empty value only requires the parameter to be present.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Query { get; }

    /// <summary>True when the rule blocks this page; false when it keeps it open.</summary>
    public bool Blocks { get; }

    /// <summary>
    /// How specifically this page fits <paramref name="uri"/>, or -1 when it does
    /// not fit at all. Query conditions outrank an exact path, which outranks a
    /// path range; within the same kind a longer path is more specific.
    /// </summary>
    public int Score(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!PathMatches(uri.AbsolutePath) || !QueryMatches(uri.Query))
        {
            return -1;
        }

        return Query.Count * QueryWeight + (Match == PageMatch.Exact ? ExactWeight : 0) + Path.Length;
    }

    /// <summary>Path normalization shared with the UI: leading slash, no trailing slash.</summary>
    public static string NormalizePath(string? path)
    {
        string text = (path ?? "").Trim();
        int cut = text.IndexOfAny(new[] { '?', '#' });
        if (cut >= 0)
        {
            text = text[..cut];
        }

        text = text.TrimEnd('/');
        return text.StartsWith('/') ? text : "/" + text;
    }

    private bool PathMatches(string rawPath)
    {
        string path = NormalizePath(rawPath);
        return Match switch
        {
            PageMatch.Exact => string.Equals(path, Path, StringComparison.OrdinalIgnoreCase),
            PageMatch.Under => Path == "/"
                || string.Equals(path, Path, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(Path + "/", StringComparison.OrdinalIgnoreCase),
            _ => (rawPath.Length == 0 ? "/" : rawPath).StartsWith(Path, StringComparison.OrdinalIgnoreCase),
        };
    }

    private bool QueryMatches(string rawQuery)
    {
        if (Query.Count == 0)
        {
            return true;
        }

        IReadOnlyList<KeyValuePair<string, string>> actual = SiteUrl.ParseQuery(rawQuery);
        foreach ((string key, string value) in Query)
        {
            bool found = actual.Any(pair =>
                string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)
                && (value.Length == 0 || string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase)));
            if (!found)
            {
                return false;
            }
        }

        return true;
    }
}
