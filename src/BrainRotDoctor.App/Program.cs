using Avalonia;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.App.Runtime.Update;
using BrainRotDoctor.App.Ui;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;
using System.IO;

namespace BrainRotDoctor.App;

internal static class Program
{
    /// <summary>Starts the app in the tray, with no window.</summary>
    internal const string TrayFlag = "--tray";

    [STAThread]
    private static void Main(string[] args)
    {
        if (HasOption(args, "--role", "watchdog"))
        {
            ProcessProtector.RunWatchdog();
            return;
        }

        // The repeating sign-in task. Starts the app if it is down, then exits,
        // so the task never owns the running process.
        if (HasFlag(args, StartupRegistrar.KickFlag))
        {
            StartupKick.Run();
            return;
        }

        // A verified, staged build re-invokes itself in this role to overwrite the
        // installed exe and relaunch. It runs before any normal startup so it never
        // acquires the primary mutex or starts its own watchdog.
        if (HasOption(args, "--role", "apply-update")
            && TryGetOption(args, "--target", "--target") is { } applyTarget)
        {
            UpdateApplier.Run(applyTarget);
            return;
        }

        if (TryGetOption(args, "--dump-icon", "--dump-icon") is { } iconPath)
        {
            DumpIcon(iconPath);
            return;
        }

#if DEBUG
        if (TryGetOption(args, "--ui-snapshots", "--ui-snapshots") is { } snapshotDir)
        {
            UiSnapshots.Run(snapshotDir, args);
            return;
        }

        if (TryGetOption(args, "--scene-frames", "--scene-frames") is { } framesPath)
        {
            Ui.Scene.SceneFrames.Run(framesPath, TryGetOption(args, "--times", "--times"));
            return;
        }

        if (TryGetOption(args, "--scene-clip", "--scene-clip") is { } clipDir)
        {
            Ui.Scene.SceneFrames.Clip(clipDir);
            return;
        }

        if (HasFlag(args, "--list-apps"))
        {
            foreach (ObservedApp app in new WindowsAppObserver().GetApps())
            {
                Console.WriteLine($"{app.Identity.Key} | {app.Identity.Name} | {app.Identity.Description} | windows {app.Windows.Count} | on screen {app.OnScreen} | front {app.IsForeground} | counts {app.Counts}");
            }

            return;
        }

        if (TryGetOption(args, "--close-app", "--close-app") is { } closeKey)
        {
            ObservedApp? found = new WindowsAppObserver().GetApps().FirstOrDefault(a => a.Identity.Key == closeKey);
            Console.WriteLine(found is null ? $"{closeKey} not found" : $"{closeKey}: {new AppCloser().Close(found)}");
            return;
        }

        if (TryGetOption(args, "--heads-up-preview", "--heads-up-preview") is { } headsUpApp)
        {
            Ui.Scene.HeadsUpOverlay.Preview(headsUpApp, args);
            return;
        }

        if (HasFlag(args, "--scene-preview"))
        {
            Ui.Scene.SceneFrames.Preview(int.TryParse(TryGetOption(args, "--scene-preview", "--scene-preview"), out int times) ? times : 1, args);
            return;
        }
#endif

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
            // An upgrade over a running build is handed to the applier (it relaunches);
            // a fresh copy is launched here unless suppressed.
            bool launchNow = SelfInstall.Run(installOptions, new DetachedApplyLauncher());
            if (launchNow && !HasFlag(args, "--no-launch"))
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
            NativeLibraryReady.Wait();
            BuildInstallerApp(installOptions).StartWithClassicDesktopLifetime(args);
            return;
        }

        // A copy started during uninstall must not put registration back.
        // An update swap is not checked here: a start during that window is how
        // a failed swap recovers, and the minute check refuses to be that start.
        if (UninstallGuard.IsBlocked(currentExe))
        {
            return;
        }

        // Launches nobody asked to see (sign-in, a watchdog revival, an update
        // relaunch) start in the tray; one the user opened shows the window.
        bool startInTray = HasFlag(args, StartupRegistrar.StartupFlag)
            || HasFlag(args, TrayFlag)
            || HasOption(args, "--role", "primary");

        using var mutex = ProcessProtector.AcquirePrimary(out bool ownsMutex);
        if (!ownsMutex)
        {
            // Another primary instance already owns the UI; bring its window up.
            if (!startInTray)
            {
                ShowWindowSignal.Raise();
            }

            return;
        }

        // A fresh primary marks the end of any update: drop the stand-down marker
        // and the staged binary / backup left behind by a swap.
        UpdateRecovery.CleanupOnStartup(currentExe);

        // While installed, sign-in registration is put back if it is turned off.
        // --no-startup is for temporary runs that must not leave any of it behind.
        using StartupKeeper? keeper = HasFlag(args, "--no-startup") ? null : StartupKeeper.Start();

        using ProcessProtector? protector = HasFlag(args, "--no-watchdog")
            ? null
            : ProcessProtector.StartForPrimary(args);

        using UpdateService? updateService = HasFlag(args, "--no-update")
            ? null
            : UpdateService.CreateDefault(currentExe);
        updateService?.Start();

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
            TryGetOption(args, "--log", "-l"),
            appObserver: new WindowsAppObserver(),
            appCloser: new AppCloser());
        controller.SetUntrackedSites(uiSettings.LoadUntrackedSites());
        controller.SetUntrackedApps(uiSettings.LoadUntrackedApps().Keys);

        NativeLibraryReady.Wait();
        controller.Start();
        try
        {
            BuildAvaloniaApp(controller, uiSettings, startInTray).StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            controller.Stop();
        }
    }

    private static AppBuilder BuildAvaloniaApp(EnforcementController controller, UiSettingsStore uiSettings, bool startInTray) =>
        AppBuilder.Configure(() => new BrainRotDoctor.App.Ui.App(controller, uiSettings, startInTray))
            .UsePlatformDetect()
            .UseDesignSystem()
            .LogToTrace();

    private static AppBuilder BuildInstallerApp(InstallOptions options) =>
        AppBuilder.Configure(() => new BrainRotDoctor.App.Ui.InstallerApp(options))
            .UsePlatformDetect()
            .UseDesignSystem()
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
                    "BrainRotDoctor",
                    NativeMethods.MB_ICONERROR);
            }

            Environment.ExitCode = 1;
            return;
        }

        // Mark uninstall first, so a copy the watchdog or the sign-in task still
        // manages to start exits without putting the registration back. Then
        // remove, stop, and wait out a repair that was already in flight.
        UninstallGuard.Block(installer.InstalledExePath);
        for (int pass = 0; pass < 2; pass++)
        {
            TryRemoveRegistration(installer);
            StopRunningInstances(installer.InstalledExePath);
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        StopRunningInstances(installer.InstalledExePath);
        TryRemoveRegistration(installer);
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
                "BrainRotDoctor",
                NativeMethods.MB_ICONINFORMATION);
        }
    }

    /// <summary>Drops sign-in registration. A failure here must not skip the rest of uninstall.</summary>
    private static void TryRemoveRegistration(Installer installer)
    {
        try
        {
            installer.RemoveRegistration();
        }
        catch (Exception)
        {
            // Keep going. A registration that will not clear must not skip file removal.
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
        string batch = Path.Combine(Path.GetTempPath(), $"brd-uninstall-{Guid.NewGuid():N}.bat");
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

    /// <summary>Dev helper: render the product icon to a PNG, or to the exe's .ico file.</summary>
    private static void DumpIcon(string path)
    {
        AppBuilder.Configure<Avalonia.Application>().UsePlatformDetect().SetupWithoutStarting();
        BrainRotDoctor.App.Ui.ProductIcon.Save(path);
    }

    private static LoadedConfiguration LoadConfiguration(string[] args, StrictModeStore strictModeStore)
    {
        LoadedConfiguration loaded = LoadUserConfiguration(args);

        // While strict mode is on, a hand edit of the file can only make things
        // stricter: any rule it changed, switched off or removed comes back as
        // the app last accepted it.
        var baseline = new RuleBaselineStore();
        string json = loaded.Json;
        if (strictModeStore.GetSnapshot().IsActive && baseline.Load() is { } accepted)
        {
            try
            {
                json = RuleGuard.Restore(accepted, loaded.Json);
                ConfigurationLoader.Load(json);
            }
            catch (ConfigurationException)
            {
                json = loaded.Json;
            }
        }

        baseline.Save(json);
        return json == loaded.Json
            ? loaded
            : new LoadedConfiguration(ConfigurationLoader.Load(json), loaded.Source, json, loaded.FilePath);
    }

    private static LoadedConfiguration LoadUserConfiguration(string[] args)
    {
        // The user's working config lives at the per-user path; the shipped
        // config/default-config.json is only a read-only seed used on first run.
        // Editing rules in the app therefore never rewrites the shipped default.
        string userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
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
        return DefaultConfiguration.CreateDocument(shortVideo, feeds).ToJson();
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
