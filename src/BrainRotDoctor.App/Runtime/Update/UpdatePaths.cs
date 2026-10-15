using System.IO;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// Filesystem locations the updater uses. All live under the per-user app-data
/// directory so the updater needs no admin rights (matching the installer).
/// </summary>
internal sealed class UpdatePaths
{
    public UpdatePaths(string appDataDir, string installedExePath)
    {
        AppDataDir = appDataDir;
        InstalledExePath = installedExePath;
        UpdateDir = Path.Combine(appDataDir, "update");
        StagedDir = Path.Combine(UpdateDir, "staged");
        StagedExePath = Path.Combine(StagedDir, Path.GetFileName(installedExePath));
        MarkerPath = Path.Combine(UpdateDir, "pending.json");
        HistoryLogPath = Path.Combine(UpdateDir, "history.log");
        FrequentChecksFlagPath = Path.Combine(UpdateDir, "check-frequently");
    }

    /// <summary>%LOCALAPPDATA%\BrainRotDoctor</summary>
    public string AppDataDir { get; }

    /// <summary>The exe the watchdog/autostart launch; the swap target.</summary>
    public string InstalledExePath { get; }

    public string UpdateDir { get; }
    public string StagedDir { get; }

    /// <summary>The verified new binary, copied over <see cref="InstalledExePath"/> on apply.</summary>
    public string StagedExePath { get; }

    /// <summary>Authenticates an in-progress swap so running processes stand down.</summary>
    public string MarkerPath { get; }

    /// <summary>Append-only record of applied updates (for a future "what's new" view).</summary>
    public string HistoryLogPath { get; }

    /// <summary>
    /// Opt-in for developers and testers: while a file exists here (any content),
    /// the updater checks every minute instead of twice a day, so a fresh release
    /// arrives almost immediately. It changes only how often the app asks, never
    /// what it accepts — every build is still signature- and hash-verified.
    /// </summary>
    public string FrequentChecksFlagPath { get; }

    public static UpdatePaths ForCurrentProcess(string installedExePath)
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor");
        return new UpdatePaths(appData, installedExePath);
    }
}
