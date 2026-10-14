using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class NativeLibraryReadyTests
{
    [Fact]
    public void Wait_succeeds_immediately_when_there_is_nothing_to_load()
    {
        int sleeps = 0;
        bool ready = NativeLibraryReady.Wait(
            findLibraries: () => [],
            tryLoad: _ => throw new InvalidOperationException("must not load"),
            limit: TimeSpan.FromSeconds(10),
            delay: TimeSpan.FromMilliseconds(500),
            sleep: _ => sleeps++);

        Assert.True(ready);
        Assert.Equal(0, sleeps);
    }

    [Fact]
    public void Wait_retries_until_every_library_loads()
    {
        var attempts = new Dictionary<string, int>(StringComparer.Ordinal);
        int sleeps = 0;
        bool ready = NativeLibraryReady.Wait(
            findLibraries: () => ["a.dll", "b.dll"],
            tryLoad: path =>
            {
                attempts[path] = attempts.GetValueOrDefault(path) + 1;
                return attempts[path] >= (path == "b.dll" ? 2 : 1);
            },
            limit: TimeSpan.FromSeconds(10),
            delay: TimeSpan.FromMilliseconds(500),
            sleep: _ => sleeps++);

        Assert.True(ready);
        Assert.Equal(1, attempts["a.dll"]);
        Assert.Equal(2, attempts["b.dll"]);
        Assert.Equal(1, sleeps);
    }

    [Fact]
    public void Wait_returns_after_the_bound_so_the_window_is_still_attempted()
    {
        bool ready = NativeLibraryReady.Wait(
            findLibraries: () => ["a.dll", "b.dll"],
            tryLoad: _ => false,
            limit: TimeSpan.Zero,
            delay: TimeSpan.FromMilliseconds(500),
            sleep: _ => throw new InvalidOperationException("must not wait past the bound"));

        Assert.False(ready);
    }
}
