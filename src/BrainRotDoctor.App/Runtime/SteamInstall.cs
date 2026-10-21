using BrainRotDoctor.Core.Apps;
using Microsoft.Win32;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// What Steam has installed on this PC, for names and for picking games. Whether a
/// running program is a Steam game never depends on this list: any program inside a
/// "steamapps\common" folder is one (<see cref="SteamGameFolder"/>), on whatever drive.
/// </summary>
internal static class SteamInstall
{
    private const string GamesFolder = @"\steamapps\common\";
    private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(5);

    /// <summary>Steam's own tools that live with the games but are none.</summary>
    private static readonly HashSet<string> Tools = new(StringComparer.OrdinalIgnoreCase)
    {
        "Steamworks Shared", "SteamVR", "Steam Controller Configs", "Steam.dll", "SteamLinuxRuntime",
        "SteamLinuxRuntime_soldier", "SteamLinuxRuntime_sniper", "Steamworks Common Redistributables",
    };

    private static readonly object Sync = new();
    private static (DateTimeOffset At, IReadOnlyList<SteamGame> Games, bool Installed)? _cached;
    private static bool _refreshing;

    /// <summary>True when Steam is installed for this user.</summary>
    public static bool IsInstalled => Read().Installed;

    /// <summary>The installed games, by name; Steam's tools left out.</summary>
    public static IReadOnlyList<SteamGame> Games => Read().Games;

    /// <summary>The game folder a program file lives in, or null when it isn't a Steam game (or is one of Steam's tools).</summary>
    public static string? SteamGameFolder(string programPath)
    {
        int at = programPath.IndexOf(GamesFolder, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
        {
            return null;
        }

        string rest = programPath[(at + GamesFolder.Length)..];
        int slash = rest.IndexOf('\\');
        string folder = slash < 0 ? "" : rest[..slash];
        return folder.Length == 0 || Tools.Contains(folder) ? null : folder;
    }

    /// <summary>The name Steam gives the game in <paramref name="folder"/>; the folder name when Steam keeps no record of it.</summary>
    public static string NameOf(string folder) =>
        Games.FirstOrDefault(g => string.Equals(g.Folder, folder, StringComparison.OrdinalIgnoreCase))?.Name ?? folder;

    /// <summary>
    /// What Steam has installed. The first look reads it; after that a stale list is
    /// still served while a fresh one is read in the background, so neither the
    /// once-a-second check nor the picker waits on a disk.
    /// </summary>
    private static (DateTimeOffset At, IReadOnlyList<SteamGame> Games, bool Installed) Read()
    {
        lock (Sync)
        {
            if (_cached is { } cached)
            {
                if (DateTimeOffset.UtcNow - cached.At >= Fresh && !_refreshing)
                {
                    _refreshing = true;
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var fresh = Scan();
                        lock (Sync)
                        {
                            _cached = fresh;
                            _refreshing = false;
                        }
                    });
                }

                return cached;
            }
        }

        var first = Scan();
        lock (Sync)
        {
            _cached ??= first;
            return _cached.Value;
        }
    }

    private static (DateTimeOffset At, IReadOnlyList<SteamGame> Games, bool Installed) Scan()
    {
        string? steam = SteamPath();
        var games = new Dictionary<string, SteamGame>(StringComparer.OrdinalIgnoreCase);
        if (steam is not null)
        {
            foreach (string library in Libraries(steam))
            {
                AddGames(Path.Combine(library, "steamapps"), games);
            }
        }

        return (DateTimeOffset.UtcNow, games.Values.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase).ToList(), steam is not null);
    }

    private static string? SteamPath()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            string? path = key?.GetValue("SteamPath") as string;
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? Path.GetFullPath(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
    }

    private static IEnumerable<string> Libraries(string steam)
    {
        var libraries = new List<string> { steam };
        try
        {
            string list = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(list))
            {
                libraries.AddRange(SteamLibrary.ParseLibraryFolders(File.ReadAllText(list)));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        return libraries.Where(Directory.Exists).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A program in the folder or a few levels down (a folder left behind by an uninstall has none).</summary>
    private static bool HasProgram(string dir, int depth = 3) =>
        Directory.EnumerateFiles(dir, "*.exe").Any()
        || (depth > 0 && Directory.EnumerateDirectories(dir).Any(sub => HasProgram(sub, depth - 1)));

    /// <summary>Every game folder of a library: named by Steam's install record, else by the folder.</summary>
    private static void AddGames(string steamapps, Dictionary<string, SteamGame> games)
    {
        try
        {
            var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string manifest in Directory.EnumerateFiles(steamapps, "appmanifest_*.acf"))
            {
                if (SteamLibrary.ParseAppManifest(File.ReadAllText(manifest)) is { } game)
                {
                    named[game.Folder] = game.Name;
                }
            }

            string common = Path.Combine(steamapps, "common");
            if (!Directory.Exists(common))
            {
                return;
            }

            foreach (string dir in Directory.EnumerateDirectories(common))
            {
                string folder = Path.GetFileName(dir);
                if (!Tools.Contains(folder) && HasProgram(dir))
                {
                    games.TryAdd(folder, new SteamGame(folder, named.TryGetValue(folder, out string? name) ? name : folder));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
