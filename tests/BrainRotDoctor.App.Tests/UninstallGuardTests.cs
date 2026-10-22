using BrainRotDoctor.App.Runtime;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class UninstallGuardTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "brd-uninstall-guard", Guid.NewGuid().ToString("N"));

    public UninstallGuardTests() => Directory.CreateDirectory(_dir);

    [Fact]
    public void Block_stops_a_start_until_install_clears_it()
    {
        string exe = Path.Combine(_dir, "BrainRotDoctor.exe");
        File.WriteAllText(exe, "x");

        Assert.False(UninstallGuard.IsBlocked(exe));
        UninstallGuard.Block(exe);
        Assert.True(File.Exists(UninstallGuard.MarkerPath(exe)));
        Assert.True(UninstallGuard.IsBlocked(exe));

        UninstallGuard.Clear(exe);
        UninstallGuard.Clear(exe);
        Assert.False(UninstallGuard.IsBlocked(exe));
    }

    [Fact]
    public void Detached_start_leaves_a_process_running()
    {
        var before = Process.GetProcessesByName("ping").Select(process => process.Id).ToHashSet();
        string ping = Path.Combine(Environment.SystemDirectory, "ping.exe");

        DetachedStarter.Start(ping, "-n 30 127.0.0.1");

        Process? started = null;
        for (int i = 0; i < 20 && started is null; i++)
        {
            Thread.Sleep(100);
            started = Process.GetProcessesByName("ping").FirstOrDefault(process => !before.Contains(process.Id));
        }

        Assert.NotNull(started);
        try
        {
            started.Kill();
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (Exception)
        {
        }
    }
}
