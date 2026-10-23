using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class StartupKeeperTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(59, false)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public void The_repeating_task_is_due_at_start_and_once_a_minute(int tick, bool due)
    {
        Assert.Equal(due, StartupKeeper.RepeatingTaskDue(tick));
    }
}
