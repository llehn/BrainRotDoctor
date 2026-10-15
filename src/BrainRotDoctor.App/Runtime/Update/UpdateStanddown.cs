using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>The marker written while a verified swap is in progress.</summary>
internal sealed class PendingUpdate
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = "";

    [JsonPropertyName("createdUtc")]
    public DateTimeOffset CreatedUtc { get; init; }
}

/// <summary>
/// Coordinates the brief window in which the running primary + watchdog must
/// release the installed exe so it can be overwritten.
///
/// The watchdog normally respawns the primary within ~650 ms, so a swap can't
/// just stop the processes. Instead the updater drops a <i>fresh</i> marker file;
/// the primary and watchdog poll for it and stand down (exit) when they see it,
/// then the applier overwrites the exe and relaunches. Requiring a recent marker
/// (not merely a signal anyone could raise) keeps a stray stand-down from being
/// an easier kill switch than the process-kill the watchdog already survives.
///
/// To bound every failure mode (a crashed applier, or a spurious marker), the
/// departing primary first schedules a detached "safety net": after a short
/// delay it relaunches the installed exe if nothing is running, so protection
/// always returns on its own.
/// </summary>
internal sealed class UpdateStanddown
{
    /// <summary>A marker older than this is ignored (a leftover from a crash).</summary>
    public static readonly TimeSpan MarkerFreshness = TimeSpan.FromSeconds(120);

    private static readonly TimeSpan SafetyNetDelay = TimeSpan.FromSeconds(90);

    private readonly UpdatePaths _paths;

    public UpdateStanddown(UpdatePaths paths) => _paths = paths;

    public static UpdateStanddown ForCurrentProcess()
    {
        string exe = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        return new UpdateStanddown(UpdatePaths.ForCurrentProcess(exe));
    }

    /// <summary>True when a verified swap is in progress and processes should stand down.</summary>
    public bool IsActive() => ReadFreshMarker() is not null;

    public PendingUpdate? ReadFreshMarker()
    {
        try
        {
            if (!File.Exists(_paths.MarkerPath))
            {
                return null;
            }

            PendingUpdate? marker = JsonSerializer.Deserialize<PendingUpdate>(File.ReadAllText(_paths.MarkerPath));
            if (marker is null)
            {
                return null;
            }

            return DateTimeOffset.UtcNow - marker.CreatedUtc <= MarkerFreshness ? marker : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void WriteMarker(string version, string sha256)
    {
        Directory.CreateDirectory(_paths.UpdateDir);
        var marker = new PendingUpdate
        {
            Version = version,
            Sha256 = sha256,
            CreatedUtc = DateTimeOffset.UtcNow,
        };
        File.WriteAllText(_paths.MarkerPath, JsonSerializer.Serialize(marker));
    }

    public void DeleteMarker()
    {
        try
        {
            if (File.Exists(_paths.MarkerPath))
            {
                File.Delete(_paths.MarkerPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Schedules a detached process that, after a delay, relaunches the installed
    /// exe if no instance is running — guaranteeing recovery even if the applier
    /// dies mid-swap or the stand-down was spurious. Mirrors the uninstall
    /// directory-deletion helper: a self-deleting batch run detached from cmd.
    /// </summary>
    public void ScheduleSafetyNetRelaunch()
    {
        string exe = _paths.InstalledExePath;
        string processName = Path.GetFileNameWithoutExtension(exe);
        int delaySeconds = (int)SafetyNetDelay.TotalSeconds;
        string batch = Path.Combine(Path.GetTempPath(), $"brd-update-net-{Guid.NewGuid():N}.bat");

        // Wait the delay, then relaunch only if no instance is running.
        string script =
            "@echo off\r\n" +
            $"ping 127.0.0.1 -n {delaySeconds + 1} >nul\r\n" +
            $"tasklist /fi \"imagename eq {processName}.exe\" | find /i \"{processName}.exe\" >nul\r\n" +
            "if %errorlevel%==0 goto done\r\n" +
            $"if exist \"{exe}\" start \"\" \"{exe}\" {Program.TrayFlag}\r\n" +
            ":done\r\n" +
            "del \"%~f0\" >nul 2>nul\r\n";

        try
        {
            File.WriteAllText(batch, script);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var info = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add(batch);
        try
        {
            Process.Start(info);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }
}
