using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Persists a pause so it survives a restart or an update swap. A stored pause
/// with no end (from before every pause had one) is not a pause any more.
/// </summary>
internal sealed class PauseStore
{
    private readonly string _path;

    public PauseStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
            "pause.json"))
    {
    }

    internal PauseStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    /// <summary>The current pause, or null when not paused.</summary>
    public PauseState? Load(DateTimeOffset now)
    {
        try
        {
            if (File.Exists(_path)
                && JsonSerializer.Deserialize<PauseState>(File.ReadAllText(_path)) is { } state
                && state.IsActiveAt(now))
            {
                return state;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
        }

        return null;
    }

    public void Save(PauseState? state)
    {
        try
        {
            if (state is null)
            {
                File.Delete(_path);
            }
            else
            {
                File.WriteAllText(_path, JsonSerializer.Serialize(state));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}

/// <param name="UntilUtc">When the pause ends. Every pause ends by itself, at most <see cref="Longest"/> after it starts.</param>
internal sealed record PauseState(DateTimeOffset UntilUtc)
{
    /// <summary>The longest a pause can last.</summary>
    public static readonly TimeSpan Longest = TimeSpan.FromHours(24);

    /// <summary>Paused at <paramref name="now"/>: before the end, and the end no further away than a pause can last.</summary>
    public bool IsActiveAt(DateTimeOffset now) => now < UntilUtc && UntilUtc - now <= Longest;
}
