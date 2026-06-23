using BrainRotBlocker.App.Runtime;
using System.IO;
using Xunit;

namespace BrainRotBlocker.App.Tests;

public sealed class StartupConfigResolverTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "brainrotblocker-tests", Guid.NewGuid().ToString("N"));

    private const string BuiltIn = """{ "rules": [ { "id": "builtin", "name": "Built-in", "allDay": true, "sites": [ { "label": "Example", "url": "example.com" } ] } ] }""";
    private const string Seed = """{ "rules": [ { "id": "seed", "name": "Seed", "allDay": true, "sites": [ { "label": "Seed", "url": "seed.example" } ] } ] }""";

    // Valid JSON that the loader rejects (an allowance of an hour or more), used
    // to exercise the "do not persist an unloadable seed" path.
    private const string UnloadableSeed = """{ "rules": [ { "id": "bad", "allowanceMinutes": 60, "sites": [ { "url": "a.com" } ] } ] }""";

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private string Path2(string name) => Path.Combine(_dir, name);

    [Fact]
    public void First_run_seeds_user_config_from_shipped_default_and_saves_there()
    {
        string userPath = Path2("config.json");
        string seedPath = Path2("default-config.json");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(seedPath, Seed);

        ConfigResolution result = StartupConfigResolver.Resolve(null, userPath, () => seedPath, () => BuiltIn);

        // Seeded from the shipped default, but the save target is the user path.
        Assert.Equal(Seed, result.Json);
        Assert.Equal(userPath, result.FilePath);
        Assert.True(File.Exists(userPath));
        Assert.Equal(Seed, File.ReadAllText(userPath));
        // The shipped default is left untouched.
        Assert.Equal(Seed, File.ReadAllText(seedPath));
    }

    [Fact]
    public void First_run_without_a_seed_writes_built_in_defaults_to_user_path()
    {
        string userPath = Path2("config.json");

        ConfigResolution result = StartupConfigResolver.Resolve(null, userPath, () => null, () => BuiltIn);

        Assert.Equal(BuiltIn, result.Json);
        Assert.Equal(userPath, result.FilePath);
        Assert.Equal(BuiltIn, File.ReadAllText(userPath));
    }

    [Fact]
    public void First_run_with_an_unloadable_seed_persists_built_in_defaults_instead()
    {
        string userPath = Path2("config.json");
        string seedPath = Path2("default-config.json");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(seedPath, UnloadableSeed);

        ConfigResolution result = StartupConfigResolver.Resolve(null, userPath, () => seedPath, () => BuiltIn);

        // The unloadable seed must never reach disk; the user config is valid.
        Assert.Equal(BuiltIn, result.Json);
        Assert.Equal(BuiltIn, File.ReadAllText(userPath));
    }

    [Fact]
    public void Existing_user_config_is_authoritative_and_seed_is_not_consulted()
    {
        string userPath = Path2("config.json");
        Directory.CreateDirectory(_dir);
        const string userJson = """{ "rules": [ { "id": "mine", "name": "Mine", "allDay": true, "sites": [ { "url": "mine.example" } ] } ] }""";
        File.WriteAllText(userPath, userJson);
        bool seedConsulted = false;

        ConfigResolution result = StartupConfigResolver.Resolve(
            null, userPath, () => { seedConsulted = true; return null; }, () => BuiltIn);

        Assert.Equal(userJson, result.Json);
        Assert.Equal(userPath, result.FilePath);
        // The seed locator (a directory walk) must not run when a user config exists.
        Assert.False(seedConsulted);
    }

    [Fact]
    public void Explicit_existing_config_path_loads_and_saves_to_that_file()
    {
        string explicitPath = Path2("custom.json");
        string userPath = Path2("config.json");
        Directory.CreateDirectory(_dir);
        const string custom = """{ "rules": [ { "id": "custom", "name": "Custom", "allDay": true, "sites": [ { "url": "custom.example" } ] } ] }""";
        File.WriteAllText(explicitPath, custom);
        bool seedConsulted = false;

        ConfigResolution result = StartupConfigResolver.Resolve(
            explicitPath, userPath, () => { seedConsulted = true; return null; }, () => BuiltIn);

        Assert.Equal(custom, result.Json);
        Assert.Equal(explicitPath, result.FilePath);
        Assert.False(seedConsulted);
        // An explicit path must not silently create a user config.
        Assert.False(File.Exists(userPath));
    }

    [Fact]
    public void Explicit_missing_config_path_is_seeded_with_defaults_and_used_as_target()
    {
        string explicitPath = Path2("new-config.json");
        string userPath = Path2("config.json");

        ConfigResolution result = StartupConfigResolver.Resolve(explicitPath, userPath, () => null, () => BuiltIn);

        // The chosen file is created and honored as the save target; no crash.
        Assert.Equal(BuiltIn, result.Json);
        Assert.Equal(explicitPath, result.FilePath);
        Assert.True(File.Exists(explicitPath));
        Assert.Equal(BuiltIn, File.ReadAllText(explicitPath));
        Assert.False(File.Exists(userPath));
    }
}
