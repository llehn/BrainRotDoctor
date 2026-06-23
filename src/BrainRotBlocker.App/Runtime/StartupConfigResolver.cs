using BrainRotBlocker.Core.Configuration;
using System.IO;

namespace BrainRotBlocker.App.Runtime;

/// <summary>The raw config text to load plus the file that edits are saved to.</summary>
internal sealed record ConfigResolution(string Json, string Source, string FilePath);

/// <summary>
/// Decides which configuration the app loads at startup and — crucially — which
/// file the user's edits are saved back to.
///
/// The user's working configuration always lives at the per-user path
/// (<c>%LOCALAPPDATA%\BrainRotBlocker\config.json</c>). The shipped
/// <c>config/default-config.json</c> in the repository is only a read-only
/// <em>seed</em> used to populate that file on first run, so editing rules in the
/// app never rewrites the shipped default (which is also guarded by a test).
/// </summary>
internal static class StartupConfigResolver
{
    public static ConfigResolution Resolve(
        string? explicitConfigPath,
        string userConfigPath,
        Func<string?> seedLocator,
        Func<string> buildBuiltInDefaults)
    {
        // 1. An explicit --config path is a deliberate override (dev/power user):
        //    load it and save back to it. If it does not exist yet, seed it with
        //    defaults rather than failing, so the chosen file is still honored.
        if (!string.IsNullOrWhiteSpace(explicitConfigPath))
        {
            if (File.Exists(explicitConfigPath))
            {
                return new ConfigResolution(
                    File.ReadAllText(explicitConfigPath), explicitConfigPath, explicitConfigPath);
            }

            string seeded = buildBuiltInDefaults();
            WriteFile(explicitConfigPath, seeded);
            return new ConfigResolution(seeded, explicitConfigPath, explicitConfigPath);
        }

        // 2. Once the user has their own config, it is authoritative.
        if (File.Exists(userConfigPath))
        {
            return new ConfigResolution(File.ReadAllText(userConfigPath), userConfigPath, userConfigPath);
        }

        // 3. First run: seed the user config from the shipped default if it can be
        //    found (running from the repo), otherwise from the built-in localized
        //    defaults. The seed locator is consulted only here, so an explicit or
        //    existing-user config never pays for its directory walk.
        string? locatedSeed = seedLocator();
        string seed = !string.IsNullOrWhiteSpace(locatedSeed) && File.Exists(locatedSeed)
            ? File.ReadAllText(locatedSeed)
            : buildBuiltInDefaults();

        // Never persist a seed we cannot load: fall back to the known-good
        // built-in defaults so the file written on disk is always valid.
        if (!IsLoadable(seed))
        {
            seed = buildBuiltInDefaults();
        }

        WriteFile(userConfigPath, seed);
        return new ConfigResolution(seed, userConfigPath, userConfigPath);
    }

    private static void WriteFile(string path, string content)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, content);
    }

    private static bool IsLoadable(string json)
    {
        try
        {
            ConfigurationLoader.Load(json);
            return true;
        }
        catch (ConfigurationException)
        {
            return false;
        }
    }
}
