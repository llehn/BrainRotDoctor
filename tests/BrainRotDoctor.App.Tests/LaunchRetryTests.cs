using BrainRotDoctor.Starter;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class LaunchRetryTests
{
    [Fact]
    public void RunUntilAlive_retries_while_the_child_dies_inside_the_failure_window()
    {
        int starts = 0;
        var starter = new ScriptStarter(() =>
        {
            starts++;
            return new FakeChild(exitsImmediately: starts < 3);
        });

        using IChildProcess alive = LaunchRetry.RunUntilAlive(
            starter, maxAttempts: 5, failureWindowMs: 20, delayMs: 0, sleep: _ => { });

        Assert.Equal(3, starts);
        Assert.False(alive.WaitForExit(10));
    }

    [Fact]
    public void RunUntilAlive_gives_up_after_the_attempt_budget()
    {
        var starter = new ScriptStarter(() => new FakeChild(exitsImmediately: true));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            LaunchRetry.RunUntilAlive(starter, maxAttempts: 3, failureWindowMs: 10, delayMs: 0, sleep: _ => { }));

        Assert.Contains("could not start", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ScriptStarter : IChildStarter
    {
        private readonly Func<IChildProcess> _start;

        public ScriptStarter(Func<IChildProcess> start) => _start = start;

        public IChildProcess Start() => _start();
    }

    private sealed class FakeChild : IChildProcess
    {
        private readonly bool _exitsImmediately;

        public FakeChild(bool exitsImmediately) => _exitsImmediately = exitsImmediately;

        public bool WaitForExit(int milliseconds) => _exitsImmediately;

        public void Dispose()
        {
        }
    }
}
