using Avalonia;
using BrainRotDoctor.App.Runtime;
using SkiaSharp;
using System.IO;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// The heads-up before an open app is closed: a dark panel in the bottom-right corner of
/// the app's screen, above the taskbar, counting down ("Valheim closes in 0:58 / Night ·
/// Save your progress now."). Like the worm scene it never takes focus and clicks pass
/// through it, so a game keeps running under it. A sound marks its start and the last ten
/// seconds, for a fullscreen game that hides it. Updated once a second from the status.
/// </summary>
internal sealed class HeadsUpOverlay
{
    private const float Width = 400;
    private const float Height = 84;
    private const float Margin = 24;
    private static readonly TimeSpan LastCall = TimeSpan.FromSeconds(10);

    private LayeredSurface? _surface;
    private float _scaling = 1;
    private bool _lastCallSounded;

    /// <summary>Shows the coming closes, or hides the panel when there are none.</summary>
    public void Update(IReadOnlyList<HeadsUp> coming, DateTimeOffset now)
    {
        if (coming.Count == 0)
        {
            Hide();
            return;
        }

        HeadsUp first = coming.MinBy(h => h.At)!;
        TimeSpan left = first.At - now;
        if (left < TimeSpan.Zero)
        {
            left = TimeSpan.Zero;
        }

        if (_surface is null)
        {
            Open(first.Window);
            Sound();
            _lastCallSounded = left <= LastCall;
        }
        else if (!_lastCallSounded && left <= LastCall)
        {
            _lastCallSounded = true;
            Sound();
        }

        string clock = $"{(int)left.TotalMinutes}:{left.Seconds:00}";
        Func<string, string> title = coming.Count == 1
            ? name => Loc.T("closes_in", name, clock)
            : _ => Loc.T("apps_close_in", clock);
        string detail = first.Rule.Length == 0 ? Loc.T("save_now") : $"{first.Rule} · {Loc.T("save_now")}";
        Paint(title, first.App, detail);
    }

    /// <summary>Windows' own warning sound, so it follows the system volume and sound scheme.</summary>
    private static void Sound() =>
        NativeMethods.PlaySoundAlias("SystemExclamation", IntPtr.Zero, NativeMethods.SND_ALIAS | NativeMethods.SND_ASYNC | NativeMethods.SND_NODEFAULT);

    public void Hide()
    {
        _surface?.Dispose();
        _surface = null;
    }

    /// <summary>Places the panel in the corner of the work area of the screen showing <paramref name="near"/>.</summary>
    private void Open(IntPtr near)
    {
        var point = default(NativeMethods.POINT);
        if (near != IntPtr.Zero && !NativeMethods.IsIconic(near) && NativeMethods.GetWindowRect(near, out NativeMethods.RECT r))
        {
            point = new NativeMethods.POINT { X = (r.Left + r.Right) / 2, Y = (r.Top + r.Bottom) / 2 };
        }

        IntPtr monitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new NativeMethods.MONITORINFO { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        NativeMethods.GetMonitorInfo(monitor, ref info);
        _scaling = NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpi, out _) == 0 && dpi > 0 ? dpi / 96f : 1;

        int width = (int)Math.Ceiling(Width * _scaling);
        int height = (int)Math.Ceiling(Height * _scaling);
        int margin = (int)(Margin * _scaling);
        _surface = new LayeredSurface(info.Work.Right - width - margin, info.Work.Bottom - height - margin, width, height);
    }

    /// <param name="title">The title around an app's name, given the name as it fits.</param>
    /// <param name="name">The app's name; only it is shortened, so the countdown always shows.</param>
    /// <param name="detail">The second line.</param>
    private void Paint(Func<string, string> title, string name, string detail)
    {
        LayeredSurface surface = _surface!;
        surface.Pixels.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(surface.Pixels))
        {
            canvas.Scale(_scaling);
            using var card = new SKPaint { Color = new SKColor(0x16, 0x17, 0x1B, 0xF2), IsAntialias = true };
            canvas.DrawRoundRect(new SKRect(0, 0, Width, Height), 14, 14, card);

            using var strong = new SKPaint { Color = SKColors.White, IsAntialias = true, TextSize = 19, Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) };
            using var regular = new SKPaint { Color = new SKColor(0xC9, 0xCB, 0xD3), IsAntialias = true, TextSize = 14, Typeface = SKTypeface.FromFamilyName("Segoe UI") };
            canvas.DrawText(FitName(title, name, strong, Width - 40), 20, 36, strong);
            canvas.DrawText(Fit(detail, regular, Width - 40), 20, 62, regular);
        }

        surface.Show();
    }

#if DEBUG
    /// <summary>Dev only: counts down 15 seconds for <paramref name="app"/> in the corner of the main screen, then quits.</summary>
    public static void Preview(string app, string[] args) =>
        Avalonia.AppBuilder.Configure(() => new PreviewApp(app)).UsePlatformDetect().StartWithClassicDesktopLifetime(args);

    private sealed class PreviewApp : Avalonia.Application
    {
        private readonly string _app;

        public PreviewApp(string app) => _app = app;

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                Loc.Initialize("auto");
                var overlay = new HeadsUpOverlay();
                DateTimeOffset at = DateTimeOffset.Now.AddSeconds(15);
                var coming = new[] { new HeadsUp(_app, at, "Night", IntPtr.Zero) };
                var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (_, _) =>
                {
                    if (DateTimeOffset.Now >= at)
                    {
                        overlay.Hide();
                        desktop.Shutdown();
                        return;
                    }

                    overlay.Update(coming, DateTimeOffset.Now);
                };
                overlay.Update(coming, DateTimeOffset.Now);
                if (Environment.GetEnvironmentVariable("BRD_HEADS_UP_PNG") is { Length: > 0 } png)
                {
                    using SKData data = SKImage.FromBitmap(overlay._surface!.Pixels).Encode(SKEncodedImageFormat.Png, 100);
                    File.WriteAllBytes(png, data.ToArray());
                }

                timer.Start();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
#endif

    /// <summary>The title with <paramref name="name"/> cut with "…" as far as needed to fit <paramref name="width"/>.</summary>
    private static string FitName(Func<string, string> title, string name, SKPaint font, float width)
    {
        for (int length = name.Length; length > 0; length--)
        {
            string text = title(length == name.Length ? name : name[..length].TrimEnd() + "…");
            if (font.MeasureText(text) <= width)
            {
                return text;
            }
        }

        return Fit(title("…"), font, width);
    }

    /// <summary>The text, cut with "…" when it is wider than <paramref name="width"/>.</summary>
    private static string Fit(string text, SKPaint font, float width)
    {
        if (font.MeasureText(text) <= width)
        {
            return text;
        }

        for (int length = text.Length - 1; length > 0; length--)
        {
            string cut = text[..length].TrimEnd() + "…";
            if (font.MeasureText(cut) <= width)
            {
                return cut;
            }
        }

        return "…";
    }
}
