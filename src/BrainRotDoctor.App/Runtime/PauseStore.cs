using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Persists a pause so it survives a restart or an update swap. A pause with no
/// end lasts until the user resumes.
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

/// <param name="UntilUtc">When the pause ends, or null to pause until resumed.</param>
internal sealed record PauseState(DateTimeOffset? UntilUtc)
{
    public bool IsActiveAt(DateTimeOffset now) => UntilUtc is not { } until || now < until;
}
