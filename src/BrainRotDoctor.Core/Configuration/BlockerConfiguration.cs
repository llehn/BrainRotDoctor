using BrainRotDoctor.Core.Apps;

namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// The validated rule set: the rules the user has configured. This is the
/// browser-independent product model; it knows nothing about browsers, the OS,
/// or timers.
/// </summary>
public sealed class BlockerConfiguration
{
    public BlockerConfiguration(IReadOnlyList<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Rule rule in rules)
        {
            if (!seen.Add(rule.Id))
            {
                throw new ConfigurationException($"Duplicate rule id '{rule.Id}'.");
            }
        }

        Rules = rules.ToArray();
    }

    public IReadOnlyList<Rule> Rules { get; }

    /// <summary>Returns every rule that blocks the given URL.</summary>
    public IEnumerable<Rule> MatchingRules(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        foreach (Rule rule in Rules)
        {
            if (rule.MatchesUrl(uri))
            {
                yield return rule;
            }
        }
    }

    /// <summary>Returns every rule that blocks the given app.</summary>
    public IEnumerable<Rule> MatchingRules(AppIdentity app)
    {
        ArgumentNullException.ThrowIfNull(app);
        foreach (Rule rule in Rules)
        {
            if (rule.MatchesApp(app))
            {
                yield return rule;
            }
        }
    }
}
