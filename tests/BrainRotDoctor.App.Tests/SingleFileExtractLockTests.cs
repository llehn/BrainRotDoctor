using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BrainRotDoctor.Starter;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Launches the published single-file exe the way a downloaded copy starts
/// (not from Programs → Install window). The control launch has no
/// interference. The lock test holds extracted natives only until the first
/// start dies, then the starter retries — the real AV pattern.
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

        using SingleFileLaunch locked = SingleFileLaunch.RunViaStarter(
            _published.StarterExePath,
            _published.ExePath,
            extract.Path,
            lockNatives: true,
            releaseAfterFirstDeath: true);

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
/// Starts the published exe (or the starter in front of it) with a private
/// extract directory. Locks can be held for the whole launch or dropped after
/// the first payload process dies so retry can succeed.
/// </summary>
internal sealed class SingleFileLaunch : IDisposable
{
    private readonly Process _process;
    private readonly string _payloadPath;
    private readonly List<FileStream> _locks;
    private readonly StringBuilder _stderr;

    public bool SawInstallWindow { get; }
    public string? WindowTitle { get; }

    private SingleFileLaunch(Process process, string payloadPath, List<FileStream> locks, StringBuilder stderr, bool saw, string? title)
    {
        _process = process;
        _payloadPath = payloadPath;
        _locks = locks;
        _stderr = stderr;
        SawInstallWindow = saw;
        WindowTitle = title;
    }

    public static SingleFileLaunch Run(string exe, string extractBase) =>
        Start(exe, exe, extractBase, lockNatives: false, releaseAfterFirstDeath: false, payloadEnv: false);

    public static SingleFileLaunch RunViaStarter(
        string starterExe,
        string payloadExe,
        string extractBase,
        bool lockNatives,
        bool releaseAfterFirstDeath) =>
        Start(starterExe, payloadExe, extractBase, lockNatives, releaseAfterFirstDeath, payloadEnv: true);

    private static SingleFileLaunch Start(
        string exe,
        string payloadPath,
        string extractBase,
        bool lockNatives,
        bool releaseAfterFirstDeath,
        bool payloadEnv)
    {
        Directory.CreateDirectory(extractBase);
        string payloadFull = System.IO.Path.GetFullPath(payloadPath);
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
        if (payloadEnv)
        {
            psi.Environment[SetupPayload.PayloadEnvVar] = payloadFull;
        }

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

        var seenPayload = new HashSet<int>();
        bool saw = false;
        string? title = null;
        DateTime deadline = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < deadline)
        {
            if (TryGetAppWindow(payloadFull, out title))
            {
                saw = true;
                break;
            }

            HashSet<int> living = PayloadPids(payloadFull);
            foreach (int id in living)
            {
                seenPayload.Add(id);
            }

            if (releaseAfterFirstDeath && locks.Count > 0 && seenPayload.Count > 0 && living.Count == 0)
            {
                Release(locks);
            }

            if (process.HasExited && living.Count == 0)
            {
                break;
            }

            Thread.Sleep(50);
        }

        if (!saw)
        {
            process.WaitForExit(2000);
        }

        return new SingleFileLaunch(process, payloadFull, locks, stderr, saw, title);
    }

    public string Describe()
    {
        string code = _process.HasExited ? _process.ExitCode.ToString() : "running";
        string stderr;
        lock (_stderr)
        {
            stderr = _stderr.ToString();
        }

        return "exited=" + _process.HasExited
            + " code=" + code
            + " title=" + (WindowTitle ?? "(none)")
            + Environment.NewLine
            + stderr;
    }

    public void Dispose()
    {
        Release(_locks);
        KillPayload(_payloadPath);
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }

        try
        {
            _process.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }

        _process.Dispose();
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
