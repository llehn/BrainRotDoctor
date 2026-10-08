using BrainRotDoctor.Core.Configuration;
using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Persists strict mode: until when it runs and a snapshot of the rules it locks.
/// Locked rules cannot be changed or removed until it ends; anything else in the
/// configuration (including new rules) stays editable. The snapshot is re-applied
/// on every start so editing the config file by hand can't undo a lock.
/// </summary>
internal sealed class StrictModeStore
{
    private readonly string _path;

    public StrictModeStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
            "strict-mode.json"))
    {
    }

    internal StrictModeStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public StrictModeSnapshot GetSnapshot() => BuildSnapshot(ReadState(), DateTimeOffset.UtcNow);

    /// <param name="duration">How long the lock lasts.</param>
    /// <param name="configJson">The configuration at the moment of locking.</param>
    /// <param name="lockedRuleIds">The rules to lock, or null for every rule.</param>
    public StrictModeSnapshot Activate(TimeSpan duration, string configJson, IReadOnlyCollection<string>? lockedRuleIds = null)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        StrictModeSnapshot current = BuildSnapshot(ReadState(), now);
        if (current.IsActive)
        {
            return current;
        }

        var state = new StrictModeState
        {
            ActivatedAtUtc = now,
            StrictUntilUtc = now + duration,
            LockedConfigJson = configJson,
            LockedRuleIds = lockedRuleIds?.ToList(),
        };
        File.WriteAllText(_path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        return BuildSnapshot(state, now);
    }

    /// <summary>The locked rules as they were when strict mode started (empty when inactive).</summary>
    public IReadOnlyList<EditableConfiguration.EditableRule> GetLockedRules()
    {
        StrictModeState? state = ReadState();
        StrictModeSnapshot snapshot = BuildSnapshot(state, DateTimeOffset.UtcNow);
        if (!snapshot.IsActive || state is null || string.IsNullOrWhiteSpace(state.LockedConfigJson))
        {
            return Array.Empty<EditableConfiguration.EditableRule>();
        }

        return EditableConfiguration.FromJson(state.LockedConfigJson).Rules
            .Where(rule => snapshot.IsLocked(rule.Id))
            .ToList();
    }

    /// <summary>
    /// Puts every locked rule back into <paramref name="configJson"/> exactly as it
    /// was locked (re-adding any that went missing), leaving other rules alone.
    /// Returns the input unchanged when strict mode is not active.
    /// </summary>
    public string ApplyLock(string configJson)
    {
        IReadOnlyList<EditableConfiguration.EditableRule> locked = GetLockedRules();
        if (locked.Count == 0)
        {
            return configJson;
        }

        EditableConfiguration config = EditableConfiguration.FromJson(configJson);
        foreach (EditableConfiguration.EditableRule rule in locked)
        {
            int index = config.Rules.FindIndex(r => r.Id == rule.Id);
            if (index >= 0)
            {
                config.Rules[index] = rule;
            }
            else
            {
                config.Rules.Add(rule);
            }
        }

        return config.ToJson();
    }

    /// <summary>
    /// Null when <paramref name="candidate"/> keeps every locked rule unchanged;
    /// otherwise the names of the locked rules it changes or removes.
    /// </summary>
    public IReadOnlyList<string>? FindLockViolations(EditableConfiguration candidate)
    {
        var violations = new List<string>();
        foreach (EditableConfiguration.EditableRule locked in GetLockedRules())
        {
            EditableConfiguration.EditableRule? now = candidate.Rules.FirstOrDefault(r => r.Id == locked.Id);
            if (now is null || Serialize(now) != Serialize(locked))
            {
                violations.Add(locked.Name);
            }
        }

        return violations.Count == 0 ? null : violations;
    }

    private static string Serialize(EditableConfiguration.EditableRule rule) =>
        new ConfigurationDocument { Rules = new() { rule.ToDocument() } }.ToJson();

    private StrictModeState? ReadState()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StrictModeState>(File.ReadAllText(_path));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static StrictModeSnapshot BuildSnapshot(StrictModeState? state, DateTimeOffset now)
    {
        if (state?.StrictUntilUtc is not { } until)
        {
            return StrictModeSnapshot.Inactive;
        }

        TimeSpan remaining = until - now;
        if (remaining <= TimeSpan.Zero)
        {
            return StrictModeSnapshot.Inactive;
        }

        // Older files (and "lock everything") list no ids: every rule of the
        // snapshot is locked, but rules added later are not.
        IEnumerable<string> ids = state.LockedRuleIds
            ?? EditableConfiguration.FromJson(state.LockedConfigJson).Rules.Select(r => r.Id);
        return new StrictModeSnapshot(
            true,
            until.ToLocalTime(),
            remaining,
            state.ActivatedAtUtc.ToLocalTime(),
            ids.ToHashSet(StringComparer.Ordinal));
    }

    private sealed class StrictModeState
    {
        public DateTimeOffset ActivatedAtUtc { get; set; }
        public DateTimeOffset StrictUntilUtc { get; set; }
        public string LockedConfigJson { get; set; } = "";

        /// <summary>Null (older files, or "all") locks every rule in the snapshot.</summary>
        public List<string>? LockedRuleIds { get; set; }
    }
}

internal sealed class StrictModeSnapshot
{
    public static readonly StrictModeSnapshot Inactive = new(false, null, TimeSpan.Zero, null, new HashSet<string>());

    public StrictModeSnapshot(
        bool isActive,
        DateTimeOffset? activeUntilLocal,
        TimeSpan remaining,
        DateTimeOffset? activatedAtLocal,
        IReadOnlySet<string> lockedRuleIds)
    {
        IsActive = isActive;
        ActiveUntilLocal = activeUntilLocal;
        Remaining = remaining;
        ActivatedAtLocal = activatedAtLocal;
        LockedRuleIds = lockedRuleIds;
    }

    public bool IsActive { get; }
    public DateTimeOffset? ActiveUntilLocal { get; }
    public TimeSpan Remaining { get; }
    public DateTimeOffset? ActivatedAtLocal { get; }

    /// <summary>The ids of the rules strict mode locks.</summary>
    public IReadOnlySet<string> LockedRuleIds { get; }

    public bool IsLocked(string ruleId) => IsActive && LockedRuleIds.Contains(ruleId);

    /// <summary>Share of the strict period already passed, 0–1.</summary>
    public double Progress =>
        IsActive && ActivatedAtLocal is { } start && ActiveUntilLocal is { } end && end > start
            ? Math.Clamp(1 - Remaining.TotalSeconds / (end - start).TotalSeconds, 0, 1)
            : 0;
}
