using Avalonia;
using BrainRotBlocker.App.Runtime;
using BrainRotBlocker.App.Ui;
using BrainRotBlocker.Core.Configuration;
using System.IO;

namespace BrainRotBlocker.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (HasOption(args, "--role", "watchdog"))
        {
            ProcessProtector.RunWatchdog();
            return;
        }

        if (TryGetOption(args, "--dump-icon", "--dump-icon") is { } iconPath)
        {
            DumpIcon(iconPath);
            return;
        }

        string currentExe = Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        InstallOptions installOptions = InstallOptions.Default(currentExe);
        var installer = new Installer(installOptions);

        if (HasFlag(args, "--uninstall"))
        {
            RunUninstall(installer, silent: HasFlag(args, "--silent"));
            return;
        }

        if (HasFlag(args, "--install"))
        {
            installer.Install();
            if (!HasFlag(args, "--no-launch"))
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(installer.InstalledExePath) { UseShellExecute = true });
            }

            return;
        }

        // A downloaded exe (not running from the install location) offers to install.
        if (!Installer.IsRunningFrom(installer.InstalledExePath, currentExe)
            && !HasFlag(args, "--no-install-prompt"))
        {
            BuildInstallerApp(installOptions).StartWithClassicDesktopLifetime(args);
            return;
        }

        using var mutex = ProcessProtector.AcquirePrimary(out bool ownsMutex);
        if (!ownsMutex)
        {
            // Another primary instance already owns the UI; nothing to do.
            return;
        }

        if (!HasFlag(args, "--no-startup"))
        {
            StartupRegistrar.EnsureCurrentUserStartup();
        }

        using ProcessProtector? protector = HasFlag(args, "--no-watchdog")
            ? null
            : ProcessProtector.StartForPrimary(args);

        var uiSettings = new UiSettingsStore();
        // Resolve the language before first-run config so default rule names are localized.
        Ui.Loc.Initialize(uiSettings.LoadLanguage());

        var strictModeStore = new StrictModeStore();
        LoadedConfiguration loaded = LoadConfiguration(args, strictModeStore);

        var controller = new EnforcementController(
            loaded.Configuration,
            new UiAutomationBrowserObserver(),
            new BrowserTabCloser(),
            loaded.Source,
            loaded.Json,
            loaded.FilePath,
            strictModeStore,
            TryGetOption(args, "--log", "-l"));

        controller.Start();
        try
        {
            BuildAvaloniaApp(controller, uiSettings).StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            controller.Stop();
        }
    }

    private static AppBuilder BuildAvaloniaApp(EnforcementController controller, UiSettingsStore uiSettings) =>
        AppBuilder.Configure(() => new BrainRotBlocker.App.Ui.App(controller, uiSettings))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static AppBuilder BuildInstallerApp(InstallOptions options) =>
        AppBuilder.Configure(() => new BrainRotBlocker.App.Ui.InstallerApp(options))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void RunUninstall(Installer installer, bool silent)
    {
        Ui.Loc.Initialize(new UiSettingsStore().LoadLanguage());
        var strict = new StrictModeStore();
        StrictModeSnapshot snapshot = strict.GetSnapshot();
        if (snapshot.IsActive)
        {
            if (!silent)
            {
                string until = snapshot.ActiveUntilLocal?.ToString("dddd, HH:mm", Ui.Loc.Culture) ?? "";
                NativeMethods.MessageBoxW(
                    IntPtr.Zero,
                    Ui.Loc.T("uninstall_blocked", until),
                    "BrainRotBlocker",
                    NativeMethods.MB_ICONERROR);
            }

            Environment.ExitCode = 1;
            return;
        }

        StopRunningInstances(installer.InstalledExePath);
        installer.RemoveRegistration();
        try
        {
            installer.RemoveAppData();
        }
        catch (IOException)
        {
            // App data may be briefly locked by a process still exiting; ignore.
        }

        ScheduleDirectoryDeletion(installer.InstallDir);

        if (!silent)
        {
            NativeMethods.MessageBoxW(
                IntPtr.Zero,
                Ui.Loc.T("removed"),
                "BrainRotBlocker",
                NativeMethods.MB_ICONINFORMATION);
        }
    }

    /// <summary>Kills the running app/watchdog instances (not this uninstaller).</summary>
    private static void StopRunningInstances(string installedExePath)
    {
        string name = Path.GetFileNameWithoutExtension(installedExePath);
        int self = Environment.ProcessId;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            System.Diagnostics.Process[] running = System.Diagnostics.Process.GetProcessesByName(name);
            bool any = false;
            foreach (System.Diagnostics.Process process in running)
            {
                if (process.Id == self)
                {
                    continue;
                }

                any = true;
                try
                {
                    process.Kill();
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }
            }

            if (!any)
            {
                break;
            }

            System.Threading.Thread.Sleep(250);
        }
    }

    /// <summary>
    /// Deletes the install directory after this process exits. A single-file exe
    /// stays locked for a few seconds after exit, so a small batch file retries
    /// until the folder is gone, then deletes itself. (Running the batch by path
    /// avoids the cmd argument-quoting pitfalls of a long inline command.)
    /// </summary>
    private static void ScheduleDirectoryDeletion(string directory)
    {
        string batch = Path.Combine(Path.GetTempPath(), $"brb-uninstall-{Guid.NewGuid():N}.bat");
        string script =
            "@echo off\r\n" +
            "for /l %%i in (1,1,60) do (\r\n" +
            $"  rmdir /s /q \"{directory}\" >nul 2>nul\r\n" +
            $"  if not exist \"{directory}\" goto done\r\n" +
            "  ping 127.0.0.1 -n 2 >nul\r\n" +
            ")\r\n" +
            ":done\r\n" +
            "del \"%~f0\" >nul 2>nul\r\n";
        File.WriteAllText(batch, script);

        var info = new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add(batch);
        System.Diagnostics.Process.Start(info);
    }

    /// <summary>Dev helper: render the product icon to a PNG for inspection.</summary>
    private static void DumpIcon(string path)
    {
        AppBuilder.Configure<Avalonia.Application>().UsePlatformDetect().SetupWithoutStarting();
        using var bmp = BrainRotBlocker.App.Ui.ProductIcon.RenderBitmap(256);
        bmp.Save(path);
    }

    private static LoadedConfiguration LoadConfiguration(string[] args, StrictModeStore strictModeStore)
    {
        if (strictModeStore.TryLoadActiveConfiguration(out LoadedConfiguration? strictLoaded)
            && strictLoaded is not null)
        {
            return strictLoaded;
        }

        // The user's working config lives at the per-user path; the shipped
        // config/default-config.json is only a read-only seed used on first run.
        // Editing rules in the app therefore never rewrites the shipped default.
        string userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotBlocker",
            "config.json");

        ConfigResolution resolution;
        try
        {
            resolution = StartupConfigResolver.Resolve(
                TryGetConfigPath(args),
                userPath,
                ConfigurationPathResolver.FindDefaultConfig,
                BuildDefaultConfigJson);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A missing/unreadable --config path or other filesystem error must
            // not stop the guard. Fall back to in-memory defaults targeting the
            // user config so the app still runs and can re-save.
            string fallback = BuildDefaultConfigJson();
            return new LoadedConfiguration(ConfigurationLoader.Load(fallback), userPath, fallback, userPath);
        }

        try
        {
            return new LoadedConfiguration(
                ConfigurationLoader.Load(resolution.Json), resolution.Source, resolution.Json, resolution.FilePath);
        }
        catch (ConfigurationException)
        {
            // A malformed file should not stop the guard. Fall back to localized
            // defaults; the save target stays the same so the user can fix and
            // re-save from the UI.
            string defaults = BuildDefaultConfigJson();
            return new LoadedConfiguration(
                ConfigurationLoader.Load(defaults), resolution.Source, defaults, resolution.FilePath);
        }
    }

    /// <summary>The first-run rule set, with rule names in the active language.</summary>
    private static string BuildDefaultConfigJson()
    {
        (string shortVideo, string feeds) = Ui.Loc.DefaultRuleNames();
        return $$"""
        {
          "rules": [
            {
              "id": "short-video", "name": "{{shortVideo}}", "allowanceMinutes": 5, "allDay": true,
              "sites": [
                { "catalogId": "yt-shorts" },
                { "catalogId": "ig-reels" },
                { "catalogId": "fb-reels" },
                { "catalogId": "tiktok" }
              ]
            },
            {
              "id": "feeds", "name": "{{feeds}}", "allowanceMinutes": 5, "allDay": true,
              "sites": [
                { "catalogId": "ig-feed" }
              ]
            }
          ]
        }
        """;
    }

    private static string? TryGetConfigPath(string[] args) => TryGetOption(args, "--config", "-c");

    private static bool HasOption(string[] args, string longName, string value)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == longName && args[i + 1] == value)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFlag(string[] args, string flag) =>
        args.Any(arg => arg.Equals(flag, StringComparison.OrdinalIgnoreCase));

    private static string? TryGetOption(string[] args, string longName, string shortName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == longName || args[i] == shortName) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
