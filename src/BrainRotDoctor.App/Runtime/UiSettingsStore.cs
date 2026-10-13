using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

internal enum ThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>Persists small preferences: theme, language, and the sites you don't want tracked.</summary>
internal sealed class UiSettingsStore
{
    private readonly string _path;

    public UiSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
            "ui-settings.json"))
    {
    }

    internal UiSettingsStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public ThemePreference LoadTheme()
    {
        State state = Read();
        return Enum.TryParse(state.Theme, out ThemePreference theme) ? theme : ThemePreference.System;
    }

    public void SaveTheme(ThemePreference theme)
    {
        State state = Read();
        state.Theme = theme.ToString();
        Write(state);
    }

    /// <summary>Language preference: "auto" or a two-letter code (e.g. "de").</summary>
    public string LoadLanguage()
    {
        string lang = Read().Language;
        return string.IsNullOrWhiteSpace(lang) ? "auto" : lang;
    }

    public void SaveLanguage(string language)
    {
        State state = Read();
        state.Language = language;
        Write(state);
    }

    /// <summary>Sites you said are fine, by host: their time is not recorded at all.</summary>
    public HashSet<string> LoadUntrackedSites() => new(Read().UntrackedSites ?? new(), StringComparer.Ordinal);

    public void SaveUntrackedSites(IEnumerable<string> hosts)
    {
        State state = Read();
        state.UntrackedSites = hosts.OrderBy(h => h, StringComparer.Ordinal).ToList();
        Write(state);
    }

    private State Read()
    {
        try
        {
            if (File.Exists(_path) && JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) is { } state)
            {
                return state;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
        }

        return new State();
    }

    private void Write(State state)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(state));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed class State
    {
        public string Theme { get; set; } = nameof(ThemePreference.System);
        public string Language { get; set; } = "auto";
        public List<string>? UntrackedSites { get; set; }
    }
}
