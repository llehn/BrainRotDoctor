using System.Diagnostics;
using System.IO;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>Launches the staged binary to apply itself over the installed exe.</summary>
internal interface IApplyLauncher
{
    void Launch(UpdatePaths paths);
}

/// <summary>
/// Starts the staged exe (a verified new build) detached, in apply-update mode,
/// pointed at the installed exe to overwrite. Detached via <c>cmd start</c> so it
/// outlives the current process, which is about to stand down for the swap.
/// </summary>
internal sealed class DetachedApplyLauncher : IApplyLauncher
{
    public void Launch(UpdatePaths paths)
    {
        var info = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("/d");
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add("start");
        info.ArgumentList.Add("");
        info.ArgumentList.Add(paths.StagedExePath);
        info.ArgumentList.Add("--role");
        info.ArgumentList.Add("apply-update");
        info.ArgumentList.Add("--target");
        info.ArgumentList.Add(paths.InstalledExePath);
        Process.Start(info);
    }
}

/// <summary>Instructions handed from the checker to the applier, in the staged dir.</summary>
internal sealed class ApplyInfo
{
    public string Version { get; init; } = "";
    public string Sha256 { get; init; } = "";

    /// <summary>Whether the relaunched app shows its window: yes after the user ran the installer, no after a silent update.</summary>
    public bool ShowWindow { get; init; }

    public static string FileName => "apply-info.json";

    public string FilePath(UpdatePaths paths) => Path.Combine(paths.StagedDir, FileName);
}
