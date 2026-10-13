using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Persists strict mode, which is nothing but a flag that stays on until a set
/// time. It knows nothing about rules: the rules side asks it whether it is on
/// (see <see cref="RuleGuard"/>), as do pausing and uninstalling.
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

    /// <summary>Turns strict mode on for <paramref name="duration"/>; while it is on, this changes nothing.</summary>
    public StrictModeSnapshot Activate(TimeSpan duration)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        StrictModeSnapshot current = BuildSnapshot(ReadState(), now);
        if (current.IsActive)
        {
            return current;
        }

        var state = new StrictModeState { ActivatedAtUtc = now, StrictUntilUtc = now + duration };
        File.WriteAllText(_path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        return BuildSnapshot(state, now);
    }

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
        return remaining <= TimeSpan.Zero
            ? StrictModeSnapshot.Inactive
            : new StrictModeSnapshot(true, until.ToLocalTime(), remaining, state.ActivatedAtUtc.ToLocalTime());
    }

    private sealed class StrictModeState
    {
        public DateTimeOffset ActivatedAtUtc { get; set; }
        public DateTimeOffset StrictUntilUtc { get; set; }
    }
}

internal sealed class StrictModeSnapshot
{
    public static readonly StrictModeSnapshot Inactive = new(false, null, TimeSpan.Zero, null);

    public StrictModeSnapshot(bool isActive, DateTimeOffset? activeUntilLocal, TimeSpan remaining, DateTimeOffset? activatedAtLocal)
    {
        IsActive = isActive;
        ActiveUntilLocal = activeUntilLocal;
        Remaining = remaining;
        ActivatedAtLocal = activatedAtLocal;
    }

    public bool IsActive { get; }
    public DateTimeOffset? ActiveUntilLocal { get; }
    public TimeSpan Remaining { get; }
    public DateTimeOffset? ActivatedAtLocal { get; }

    /// <summary>Share of the strict period already passed, 0–1.</summary>
    public double Progress =>
        IsActive && ActivatedAtLocal is { } start && ActiveUntilLocal is { } end && end > start
            ? Math.Clamp(1 - Remaining.TotalSeconds / (end - start).TotalSeconds, 0, 1)
            : 0;
}
