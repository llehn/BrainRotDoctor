using BrainRotDoctor.App.Runtime;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Opens a real private window and a real ordinary window in Chrome, Firefox and
/// Edge and checks what the observer reports. Skips a browser that is not
/// installed. Not parallel: each case owns the browsers it starts.
/// </summary>
[Collection("LiveBrowsers")]
public sealed class PrivateWindowLiveTests
{
    [Theory]
    [InlineData("chrome", true)]
    [InlineData("chrome", false)]
    [InlineData("msedge", true)]
    [InlineData("msedge", false)]
    [InlineData("firefox", true)]
    [InlineData("firefox", false)]
    public void Observer_marks_only_private_windows(string processName, bool isPrivate)
    {
        string? exe = BrowserExe(processName);
        if (exe is null)
        {
            // xunit 2 has no dynamic skip; a missing browser must not fail CI.
            return;
        }

        string profile = Path.Combine(Path.GetTempPath(), "brd-live-private", processName + (isPrivate ? "-priv" : "-norm") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(profile);
        PrepareFirefoxProfile(processName, profile);
        DateTime startedAt = DateTime.UtcNow.AddSeconds(-2);
        Process? launched = null;
        ObservedBrowserWindow? window = null;
        try
        {
            launched = Process.Start(StartInfo(exe, processName, profile, isPrivate));
            Assert.NotNull(launched);

            var observer = new UiAutomationBrowserObserver();
            window = WaitForWindow(observer, processName, startedAt, isPrivate, TimeSpan.FromSeconds(30));
            Assert.True(window is not null, processName + (isPrivate ? " private" : " ordinary") + " window was not seen");
            Assert.Equal(isPrivate, window!.IsPrivate);
        }
        finally
        {
            CloseOurs(window, launched);
            launched?.Dispose();
            try
            {
                Directory.Delete(profile, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static ObservedBrowserWindow? WaitForWindow(
        UiAutomationBrowserObserver observer,
        string processName,
        DateTime startedAt,
        bool wantPrivate,
        TimeSpan limit)
    {
        var watch = Stopwatch.StartNew();
        ObservedBrowserWindow? last = null;
        while (watch.Elapsed < limit)
        {
            foreach (ObservedBrowserWindow window in observer.GetSelectedTabs())
            {
                if (!window.BrowserName.Equals(BrowserLabel(processName), StringComparison.OrdinalIgnoreCase)
                    || !StartedWithUs(window.WindowHandle, processName, startedAt))
                {
                    continue;
                }

                last = window;
                if (wantPrivate)
                {
                    if (window.IsPrivate)
                    {
                        return window;
                    }
                }
                else if (watch.Elapsed >= TimeSpan.FromSeconds(3))
                {
                    return window;
                }
            }

            Thread.Sleep(250);
        }

        return last;
    }

    private static bool StartedWithUs(IntPtr handle, string processName, DateTime startedAt)
    {
        NativeMethods.GetWindowThreadProcessId(handle, out uint pid);
        if (pid == 0)
        {
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById((int)pid);
            return process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)
                && process.StartTime.ToUniversalTime() >= startedAt.ToUniversalTime();
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static ProcessStartInfo StartInfo(string exe, string processName, string profile, bool isPrivate)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
        if (processName == "firefox")
        {
            psi.ArgumentList.Add("-no-remote");
            psi.ArgumentList.Add("-profile");
            psi.ArgumentList.Add(profile);
            if (isPrivate)
            {
                psi.ArgumentList.Add("-private-window");
            }

            psi.ArgumentList.Add("https://example.com/");
            return psi;
        }

        psi.ArgumentList.Add("--user-data-dir=" + profile);
        psi.ArgumentList.Add("--no-first-run");
        psi.ArgumentList.Add("--no-default-browser-check");
        psi.ArgumentList.Add("--disable-extensions");
        psi.ArgumentList.Add("--disable-sync");
        psi.ArgumentList.Add("--new-window");
        if (isPrivate)
        {
            psi.ArgumentList.Add(processName == "msedge" ? "--inprivate" : "--incognito");
        }

        psi.ArgumentList.Add("https://example.com/");
        return psi;
    }

    private static void PrepareFirefoxProfile(string processName, string profile)
    {
        if (processName != "firefox")
        {
            return;
        }

        File.WriteAllText(
            Path.Combine(profile, "user.js"),
            """
            user_pref("browser.shell.checkDefaultBrowser", false);
            user_pref("datareporting.policy.dataSubmissionEnabled", false);
            user_pref("toolkit.telemetry.enabled", false);
            user_pref("browser.startup.homepage_override.mstone", "ignore");
            user_pref("browser.aboutwelcome.enabled", false);
            user_pref("startup.homepage_welcome_url", "");
            user_pref("startup.homepage_welcome_url.additional", "");
            user_pref("toolkit.legacyUserProfileCustomizations.stylesheets", false);
            """);
    }

    private static void CloseOurs(ObservedBrowserWindow? window, Process? launched)
    {
        if (window is not null)
        {
            NativeMethods.GetWindowThreadProcessId(window.WindowHandle, out uint pid);
            if (pid != 0)
            {
                try
                {
                    using Process process = Process.GetProcessById((int)pid);
                    process.Kill(entireProcessTree: true);
                }
                catch (ArgumentException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        try
        {
            launched?.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string BrowserLabel(string processName) => processName switch
    {
        "chrome" => "Chrome",
        "msedge" => "Edge",
        "firefox" => "Firefox",
        _ => processName,
    };

    private static string? BrowserExe(string processName)
    {
        string[] paths = processName switch
        {
            "chrome" =>
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            ],
            "msedge" =>
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            ],
            "firefox" =>
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox", "firefox.exe"),
            ],
            _ => [],
        };

        return paths.FirstOrDefault(File.Exists);
    }
}

[CollectionDefinition("LiveBrowsers", DisableParallelization = true)]
public sealed class LiveBrowsersCollection;
