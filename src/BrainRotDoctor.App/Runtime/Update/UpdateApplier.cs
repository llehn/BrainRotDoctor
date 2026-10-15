using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// Runs from the staged (already verified) binary, in <c>--role apply-update</c>,
/// to overwrite the installed exe and relaunch. This is the only place the
/// installed binary is replaced. The sequence:
/// <list type="number">
///   <item>raise the stand-down marker so the running primary + watchdog exit;</item>
///   <item>wait for them to release the installed exe;</item>
///   <item>back up the old exe, copy the new one over it, re-verify its hash;</item>
///   <item>clear the marker and relaunch the (new) installed exe.</item>
/// </list>
/// On any failure it rolls back to the backup and relaunches, so a bad swap never
/// leaves the user without protection. The departing primary also schedules a
/// safety-net relaunch, covering the case where this process itself is killed.
/// </summary>
internal static class UpdateApplier
{
    private static readonly TimeSpan ExclusiveAccessTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>Entry point for the apply-update role. <paramref name="target"/> is the installed exe.</summary>
    public static void Run(string target)
    {
        UpdatePaths paths = UpdatePaths.ForCurrentProcess(target);
        var standdown = new UpdateStanddown(paths);

        ApplyInfo? info = ReadApplyInfo(paths);
        if (info is null)
        {
            return;
        }

        // Signal the running processes to stand down, then wait for the file lock.
        standdown.WriteMarker(info.Version, info.Sha256);
        if (!WaitForExclusiveAccess(target, ExclusiveAccessTimeout))
        {
            // The old processes never released the exe; abort without touching it.
            // Clearing the marker lets them resume protection on their next tick.
            standdown.DeleteMarker();
            return;
        }

        string backup = target + ".bak";
        try
        {
            BackUp(target, backup);
            CopyWithRetry(paths.StagedExePath, target);

            if (!UpdateSignature.VerifyFileHash(target, info.Sha256))
            {
                throw new IOException("Applied binary failed hash verification.");
            }

            AppendHistory(paths, info.Version);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            RollBack(target, backup);
            standdown.DeleteMarker();
            Relaunch(target, info.ShowWindow);
            return;
        }

        standdown.DeleteMarker();
        Relaunch(target, info.ShowWindow);
        TryDelete(backup);
    }

    private static ApplyInfo? ReadApplyInfo(UpdatePaths paths)
    {
        try
        {
            string path = Path.Combine(paths.StagedDir, ApplyInfo.FileName);
            if (!File.Exists(path))
            {
                return null;
            }

            ApplyInfo? info = JsonSerializer.Deserialize<ApplyInfo>(File.ReadAllText(path));
            return string.IsNullOrWhiteSpace(info?.Version) ? null : info;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Waits until this is the only running instance (others hold the exe lock).</summary>
    private static bool WaitForExclusiveAccess(string target, TimeSpan timeout)
    {
        string name = Path.GetFileNameWithoutExtension(target);
        int self = Environment.ProcessId;
        DateTime deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            bool othersRunning = Process.GetProcessesByName(name).Any(p => p.Id != self);
            if (!othersRunning && IsWritable(target))
            {
                return true;
            }

            Thread.Sleep(PollInterval);
        }

        return false;
    }

    private static bool IsWritable(string path)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        try
        {
            using FileStream _ = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void BackUp(string target, string backup)
    {
        if (!File.Exists(target))
        {
            return;
        }

        TryDelete(backup);
        File.Move(target, backup);
    }

    private static void CopyWithRetry(string source, string target)
    {
        IOException? last = null;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                File.Copy(source, target, overwrite: true);
                return;
            }
            catch (IOException ex)
            {
                last = ex;
                Thread.Sleep(PollInterval);
            }
        }

        throw last ?? new IOException("Failed to copy the update into place.");
    }

    private static void RollBack(string target, string backup)
    {
        try
        {
            if (File.Exists(backup))
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                }

                File.Move(backup, target);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void AppendHistory(UpdatePaths paths, string version)
    {
        try
        {
            Directory.CreateDirectory(paths.UpdateDir);
            File.AppendAllText(
                paths.HistoryLogPath,
                $"{DateTimeOffset.UtcNow:O} applied {version}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void Relaunch(string target, bool showWindow)
    {
        var info = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("/d");
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add("start");
        info.ArgumentList.Add("");
        info.ArgumentList.Add(target);
        if (!showWindow)
        {
            info.ArgumentList.Add(Program.TrayFlag);
        }

        try
        {
            Process.Start(info);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
