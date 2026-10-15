using BrainRotDoctor.App.Runtime;
using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// Installs the build the user just downloaded, choosing the right way to put the
/// binary in place:
/// <list type="bullet">
///   <item>a fresh machine (nothing installed) → a plain copy;</item>
///   <item>an older build already installed (and, thanks to the watchdog, almost
///   certainly running and holding its own exe locked) → the very same
///   stand-down/apply handshake the silent auto-updater uses. The running build
///   and its watchdog see the update marker, step aside, the applier swaps the
///   binary, and it relaunches itself.</item>
/// </list>
/// This is why "download a new installer and run it over an old version" works
/// without asking the user to quit a program that is built to resist being quit.
/// </summary>
internal static class SelfInstall
{
    /// <summary>
    /// Performs the install. Returns <c>true</c> when the caller should launch the
    /// app itself (a fresh copy is ready); <c>false</c> when the swap was handed to
    /// the applier, which relaunches the app once the running build releases its exe.
    /// </summary>
    public static bool Run(InstallOptions options, IApplyLauncher launcher)
    {
        var installer = new Installer(options);
        if (!installer.IsInstalled())
        {
            installer.Install();
            return true;
        }

        // Refresh registration now (registry writes don't need the exe lock), then
        // stage this build and let the applier perform the locked-file swap.
        installer.WriteRegistration();

        UpdatePaths paths = UpdatePaths.ForCurrentProcess(installer.InstalledExePath);
        Stage(paths, options.SourceExe, options.Version);
        launcher.Launch(paths);
        return false;
    }

    /// <summary>
    /// Places the downloaded exe in the staging area and records the apply
    /// instructions, exactly as the silent updater does before invoking the applier.
    /// </summary>
    private static void Stage(UpdatePaths paths, string sourceExe, string version)
    {
        if (Directory.Exists(paths.StagedDir))
        {
            Directory.Delete(paths.StagedDir, recursive: true);
        }

        Directory.CreateDirectory(paths.StagedDir);
        File.Copy(sourceExe, paths.StagedExePath, overwrite: true);

        var info = new ApplyInfo
        {
            Version = version,
            Sha256 = UpdateSignature.ComputeFileSha256(paths.StagedExePath),
            ShowWindow = true,
        };
        File.WriteAllText(info.FilePath(paths), JsonSerializer.Serialize(info));
    }
}
