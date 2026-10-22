using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// A file beside the exe that means "uninstall has started." A copy started by
/// the watchdog or the sign-in task after that sees it and exits without putting
/// the registration back. Install removes the file, so a reinstall can run.
/// </summary>
internal static class UninstallGuard
{
    public const string MarkerName = "uninstalling";

    public static void Block(string exePath)
    {
        string path = MarkerPath(exePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "1");
    }

    public static void Clear(string exePath)
    {
        try
        {
            string path = MarkerPath(exePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    public static bool IsBlocked(string exePath)
    {
        try
        {
            return File.Exists(MarkerPath(exePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // If we cannot tell, do not start. A missed start is retried; a start
            // during uninstall puts the registration back.
            return true;
        }
    }

    public static string MarkerPath(string exePath)
        => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(exePath)) ?? "", MarkerName);
}
