using Avalonia;
using System.Diagnostics;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Shows the Avalonia UI, and if native libraries are not ready yet ("Dll was
/// not found"), waits and starts this exe again. A failed Skia type initializer
/// cannot be retried in the same process, so the retry is a new process — still
/// this product, not a packed starter.
/// </summary>
internal static class UiStartRetry
{
    public const string RetryCountVariable = "BRD_UI_RETRY";

    public static void Run(Func<AppBuilder> build, string[] args)
    {
        try
        {
            build().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (IsNativeLibraryLoadFailure(ex))
        {
            if (!TryRelaunch(args))
            {
                throw;
            }
        }
    }

    internal static bool IsNativeLibraryLoadFailure(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is DllNotFoundException)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryRelaunch(string[] args)
    {
        int n = 0;
        string? raw = Environment.GetEnvironmentVariable(RetryCountVariable);
        if (!string.IsNullOrEmpty(raw))
        {
            _ = int.TryParse(raw, out n);
        }

        if (n >= ProcessLaunchRetry.MaxAttempts)
        {
            return false;
        }

        Thread.Sleep(ProcessLaunchRetry.DelayMs);
        string exe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        var info = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory,
        };
        foreach (string arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        info.Environment[RetryCountVariable] = (n + 1).ToString();
        Process.Start(info);
        return true;
    }
}
