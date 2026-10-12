using BrainRotDoctor.Core.Activity;
using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Keeps today's browsing statistics (totals per place, see <see cref="ActivityRecord"/>)
/// across restarts and updates. It holds one day only: the next day's first tick
/// replaces it. Lives in per-user app data, so an uninstall removes it.
/// </summary>
internal sealed class ActivityStore
{
    private readonly string _path;

    public ActivityStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
            "activity-today.json"))
    {
    }

    internal ActivityStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public ActivityRecord? Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                return JsonSerializer.Deserialize<ActivityRecord>(File.ReadAllText(_path));
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
        }

        return null;
    }

    public void Save(ActivityRecord record)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(record));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
