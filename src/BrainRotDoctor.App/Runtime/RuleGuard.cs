using BrainRotDoctor.Core.Configuration;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// What the rules allow while strict mode is on: only changes that make
/// blocking stricter. Adding a rule and switching a rule on are fine; changing,
/// switching off or deleting an existing rule is not. Strict mode itself knows
/// nothing of this: it is a flag, and the rules side checks it.
/// </summary>
internal static class RuleGuard
{
    /// <summary>The names of the rules of <paramref name="before"/> that <paramref name="after"/> changes, switches off or removes; empty when it only tightens.</summary>
    public static IReadOnlyList<string> Loosened(EditableConfiguration before, EditableConfiguration after)
    {
        var loosened = new List<string>();
        foreach (EditableConfiguration.EditableRule rule in before.Rules)
        {
            EditableConfiguration.EditableRule? now = after.Rules.FirstOrDefault(r => r.Id == rule.Id);
            if (now is null || !OnlyTightened(rule, now))
            {
                loosened.Add(rule.Name);
            }
        }

        return loosened;
    }

    /// <summary>
    /// <paramref name="found"/> with every rule of <paramref name="accepted"/> it
    /// loosened put back as it was. New rules, and rules only switched on, stay:
    /// so editing the config file by hand can't undo what strict mode protects.
    /// </summary>
    public static string Restore(string accepted, string found)
    {
        EditableConfiguration before = EditableConfiguration.FromJson(accepted);
        EditableConfiguration config = EditableConfiguration.FromJson(found);
        foreach (EditableConfiguration.EditableRule rule in before.Rules)
        {
            int index = config.Rules.FindIndex(r => r.Id == rule.Id);
            if (index < 0)
            {
                config.Rules.Add(rule);
            }
            else if (!OnlyTightened(rule, config.Rules[index]))
            {
                config.Rules[index] = rule;
            }
        }

        return config.ToJson();
    }

    /// <summary>The same rule, or the same rule switched on.</summary>
    private static bool OnlyTightened(EditableConfiguration.EditableRule before, EditableConfiguration.EditableRule after)
    {
        if (before.Enabled && !after.Enabled)
        {
            return false;
        }

        EditableConfiguration.EditableRule a = before.Clone();
        EditableConfiguration.EditableRule b = after.Clone();
        a.Enabled = b.Enabled = true;
        return Serialize(a) == Serialize(b);
    }

    private static string Serialize(EditableConfiguration.EditableRule rule) =>
        new ConfigurationDocument { Rules = new() { rule.ToDocument() } }.ToJson();
}

/// <summary>
/// The rules as the app last accepted them, kept apart from the config file so
/// a hand edit made while strict mode is on can be undone (<see cref="RuleGuard.Restore"/>).
/// </summary>
internal sealed class RuleBaselineStore
{
    private readonly string _path;

    public RuleBaselineStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
            "rules-accepted.json"))
    {
    }

    internal RuleBaselineStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public string? Load()
    {
        try
        {
            return File.Exists(_path) ? File.ReadAllText(_path) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Save(string json)
    {
        try
        {
            File.WriteAllText(_path, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
