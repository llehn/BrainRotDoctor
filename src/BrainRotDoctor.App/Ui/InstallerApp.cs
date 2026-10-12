using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.App.Runtime.Update;
using BrainRotDoctor.DesignSystem;
using System.Diagnostics;

namespace BrainRotDoctor.App.Ui;

/// <summary>The one-click installer window shown when the downloaded exe is run.</summary>
internal sealed class InstallerApp : Application
{
    private readonly InstallOptions _options;

    public InstallerApp(InstallOptions options) => _options = options;

    public InstallerApp() => throw new InvalidOperationException("InstallerApp needs options.");

    public override void Initialize()
    {
        Loc.Initialize(Loc.Auto); // pre-install: follow the Windows language
        DesignTheme.Install(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            desktop.MainWindow = new InstallerWindow(_options) { Icon = ProductIcon.Create() };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class InstallerWindow : AppWindow
{
    private readonly InstallOptions _options;

    public InstallerWindow(InstallOptions options)
    {
        _options = options;

        Title = Loc.T("install_title");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var icon = new Image
        {
            Source = ProductIcon.At(96),
            Width = 72,
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var install = new PillButton(Loc.T("install"), ButtonKind.Primary);
        install.Click += (_, _) => DoInstall();
        var cancel = new PillButton(Loc.T("not_now"), ButtonKind.Ghost);
        cancel.Click += (_, _) => Close();

        StackPanel actions = Layout.Row(10, cancel, install);
        actions.HorizontalAlignment = HorizontalAlignment.Right;

        Content = new StackPanel
        {
            Margin = new Thickness(28, 26, 28, 22),
            Spacing = Space.S4,
            Children =
            {
                icon,
                new TextLine(Loc.T("install_title"), TextStyle.Title) { Centered = true },
                new TextLine(Loc.T("install_body"), TextStyle.Small) { Centered = true, MaxWidth = 380 },
                actions,
            },
        };
    }

    private void DoInstall()
    {
        try
        {
            // A fresh install copies and launches here; an upgrade over a running
            // build hands the swap to the applier, which relaunches the app itself.
            if (SelfInstall.Run(_options, new DetachedApplyLauncher()))
            {
                var installer = new Installer(_options);
                Process.Start(new ProcessStartInfo(installer.InstalledExePath) { UseShellExecute = true });
            }

            Close();
        }
        catch (Exception ex)
        {
            NativeMethods.MessageBoxW(IntPtr.Zero, ex.Message, Loc.T("install_failed"), NativeMethods.MB_ICONERROR);
        }
    }
}
