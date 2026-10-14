using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Launches the published single-file exe the way a downloaded copy starts
/// (not from Programs → Install window). The control launch has no
/// interference. The lock test holds extracted natives briefly, then releases
/// them so the in-exe retry can show Install — the real AV pattern.
/// </summary>
[Collection("SingleFileLaunch")]
public sealed class SingleFileExtractLockTests
{
    private readonly PublishedSingleFileFixture _published;

    public SingleFileExtractLockTests(PublishedSingleFileFixture published) => _published = published;

    [Fact(Timeout = 120000)]
    public Task Without_locking_extracted_natives_the_install_window_appears()
    {
        using var extract = new ExtractDir();
        using SingleFileLaunch launch = SingleFileLaunch.Run(_published.ExePath, extract.Path);

        Assert.True(launch.SawInstallWindow, launch.Describe());
        return Task.CompletedTask;
    }

    [Fact(Timeout = 180000)]
    public Task With_extracted_natives_locked_the_install_window_still_appears()
    {
        using var extract = new ExtractDir();
        using (SingleFileLaunch primed = SingleFileLaunch.Run(_published.ExePath, extract.Path))
        {
            Assert.True(
                primed.SawInstallWindow,
                "Control launch failed; the lock harness cannot be trusted. " + primed.Describe());
        }

        using SingleFileLaunch locked = SingleFileLaunch.Run(
            _published.ExePath,
            extract.Path,
            lockNatives: true,
            releaseAfter: TimeSpan.FromMilliseconds(200));

        Assert.True(locked.SawInstallWindow, locked.Describe());
        return Task.CompletedTask;
    }
}

/// <summary>Temp folder for <c>DOTNET_BUNDLE_EXTRACT_BASE_DIR</c>.</summary>
internal sealed class ExtractDir : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "brd-extract-lock-tests", Guid.NewGuid().ToString("N"));

    public ExtractDir() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}

/// <summary>
/// Starts the published exe with a private extract directory. Optional exclusive
/// locks on extracted natives can be dropped after a delay so a retry can succeed.
/// </summary>
internal sealed class SingleFileLaunch : IDisposable
{
    private readonly string _payloadPath;
    private readonly StringBuilder _stderr;

    public bool SawInstallWindow { get; }
    public string? WindowTitle { get; }

    private SingleFileLaunch(string payloadPath, StringBuilder stderr, bool saw, string? title)
    {
        _payloadPath = payloadPath;
        _stderr = stderr;
        SawInstallWindow = saw;
        WindowTitle = title;
    }

    public static SingleFileLaunch Run(
        string exe,
        string extractBase,
        bool lockNatives = false,
        TimeSpan? releaseAfter = null)
    {
        Directory.CreateDirectory(extractBase);
        string payloadFull = System.IO.Path.GetFullPath(exe);
        var locks = new List<FileStream>();
        if (lockNatives)
        {
            string[] dlls = Directory.GetFiles(extractBase, "*.dll", SearchOption.AllDirectories);
            if (dlls.Length == 0)
            {
                throw new InvalidOperationException(
                    "No extracted native DLLs under " + extractBase + " to lock.");
            }

            foreach (string dll in dlls)
            {
                locks.Add(new FileStream(dll, FileMode.Open, FileAccess.Read, FileShare.None));
            }
        }

        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = System.IO.Path.GetDirectoryName(exe)!,
        };
        psi.Environment["DOTNET_BUNDLE_EXTRACT_BASE_DIR"] = extractBase;

        Process process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start " + exe);

        var stderr = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stderr)
                {
                    stderr.AppendLine(e.Data);
                }
            }
        };
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        DateTime started = DateTime.UtcNow;
        DateTime deadline = started.AddSeconds(40);
        bool saw = false;
        string? title = null;
        while (DateTime.UtcNow < deadline)
        {
            if (releaseAfter is { } delay && locks.Count > 0 && DateTime.UtcNow - started >= delay)
            {
                Release(locks);
            }

            if (TryGetAppWindow(payloadFull, out title))
            {
                saw = true;
                break;
            }

            Thread.Sleep(50);
        }

        return new SingleFileLaunch(payloadFull, stderr, saw, title);
    }

    public string Describe()
    {
        string stderr;
        lock (_stderr)
        {
            stderr = _stderr.ToString();
        }

        return "title=" + (WindowTitle ?? "(none)") + Environment.NewLine + stderr;
    }

    public void Dispose()
    {
        KillPayload(_payloadPath);
    }

    private static void Release(List<FileStream> locks)
    {
        foreach (FileStream stream in locks)
        {
            stream.Dispose();
        }

        locks.Clear();
    }

    private static void KillPayload(string payloadPath)
    {
        foreach (Process process in Process.GetProcessesByName("BrainRotDoctor"))
        {
            try
            {
                if (IsPayload(process, payloadPath) && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static HashSet<int> PayloadPids(string payloadPath)
    {
        var ids = new HashSet<int>();
        foreach (Process process in Process.GetProcessesByName("BrainRotDoctor"))
        {
            try
            {
                if (IsPayload(process, payloadPath) && !process.HasExited)
                {
                    ids.Add(process.Id);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return ids;
    }

    private static bool IsPayload(Process process, string payloadPath)
    {
        try
        {
            string? file = process.MainModule?.FileName;
            return file is not null
                && string.Equals(file, payloadPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool TryGetAppWindow(string payloadPath, out string? title)
    {
        HashSet<int> ours = PayloadPids(payloadPath);
        string? found = null;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint windowPid);
            if (!ours.Contains((int)windowPid) || !IsWindowVisible(h))
            {
                return true;
            }

            var buffer = new StringBuilder(512);
            GetWindowText(h, buffer, buffer.Capacity);
            string text = buffer.ToString();
            if (text.Contains("BrainRotDoctor", StringComparison.OrdinalIgnoreCase))
            {
                found = text;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        title = found;
        return found is not null;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
}
