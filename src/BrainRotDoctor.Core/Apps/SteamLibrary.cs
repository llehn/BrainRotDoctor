using System.Text.RegularExpressions;

namespace BrainRotDoctor.Core.Apps;

/// <summary>One game Steam has installed: its folder name under "steamapps\common" and its name.</summary>
public sealed record SteamGame(string Folder, string Name);

/// <summary>
/// Reads Steam's own records of what it installed: the list of library folders
/// (<c>steamapps\libraryfolders.vdf</c>) and one install record per game
/// (<c>steamapps\appmanifest_&lt;id&gt;.acf</c>). Both are Steam's simple
/// <c>"key" "value"</c> text format; only the few values needed here are read.
/// </summary>
public static partial class SteamLibrary
{
    /// <summary>The library folders listed in <c>libraryfolders.vdf</c> (each holds a "steamapps" folder).</summary>
    public static IReadOnlyList<string> ParseLibraryFolders(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Pairs(text)
            .Where(p => p.Key.Equals("path", StringComparison.OrdinalIgnoreCase) && p.Value.Length > 0)
            .Select(p => p.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The game an <c>appmanifest_*.acf</c> describes, or null when it names no install folder.</summary>
    public static SteamGame? ParseAppManifest(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string? folder = null;
        string? name = null;
        foreach ((string key, string value) in Pairs(text))
        {
            if (folder is null && key.Equals("installdir", StringComparison.OrdinalIgnoreCase))
            {
                folder = value;
            }
            else if (name is null && key.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                name = value;
            }
        }

        return string.IsNullOrWhiteSpace(folder) ? null : new SteamGame(folder, string.IsNullOrWhiteSpace(name) ? folder : name);
    }

    /// <summary>Every <c>"key" "value"</c> pair on a line, unescaped; nested blocks are walked through.</summary>
    private static IEnumerable<KeyValuePair<string, string>> Pairs(string text)
    {
        foreach (Match match in PairPattern().Matches(text))
        {
            yield return new(Unescape(match.Groups[1].Value), Unescape(match.Groups[2].Value));
        }
    }

    private static string Unescape(string value) => value.Replace("\\\\", "\\", StringComparison.Ordinal).Replace("\\\"", "\"", StringComparison.Ordinal);

    [GeneratedRegex("^\\s*\"((?:[^\"\\\\]|\\\\.)*)\"\\s+\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Multiline)]
    private static partial Regex PairPattern();
}
