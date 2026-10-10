using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using SkiaSharp;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// The worm scene on screen: a see-through overlay in the bottom-right corner of a
/// screen, over the taskbar. It never takes focus (the user stays in their
/// browser), clicks pass through it, and it shows in no taskbar or Alt-Tab list.
/// It plays the scene once, reports the pop and the end, and goes away.
/// </summary>
/// <remarks>
/// Each frame is painted on the processor at exactly the screen's pixels (sharp at
/// any zoom level) and handed to Windows as a layered window. This is Windows' own
/// overlay mechanism, so the scene plays smoothly regardless of the graphics setup;
/// the app's regular windows are not affected.
/// </remarks>
internal sealed class SceneOverlay
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromSeconds(1 / 60.0);

    private readonly WormScene _scene;
    private readonly double _popAt;
    private readonly PixelPoint? _near;
    private readonly Stopwatch _clock = new();
    private readonly Drawing _drawing;
    private IntPtr _window, _screenDc, _memoryDc, _dib, _previousBitmap, _bits;
    private SKBitmap? _pixels;
    private NativeMethods.POINT _position;
    private NativeMethods.SIZE _size;
    private double _scaling = 1;
    private bool _popped;
    private bool _finished;

    /// <summary>
    /// Plays <paramref name="scene"/>, reporting the pop at <paramref name="popAt"/>
    /// seconds, on the screen holding <paramref name="near"/> (physical pixels; null for
    /// the main screen).
    /// </summary>
    public SceneOverlay(WormScene scene, double popAt, PixelPoint? near)
    {
        _scene = scene;
        _popAt = popAt;
        _near = near;
        _drawing = new Drawing(scene);
    }

    /// <summary>The worm has just popped out: the moment the tab closes.</summary>
    public event EventHandler? Popped;

    /// <summary>The scene is over and the overlay has gone.</summary>
    public event EventHandler? Finished;

#if DEBUG
    /// <summary>Frames shown so far (dev preview only).</summary>
    public int Frames { get; private set; }

    /// <summary>The longest wait between two frames, in milliseconds (dev preview only).</summary>
    public double LongestGap { get; private set; }

    private double _lastFrame;

    public IntPtr Handle => _window;

    public NativeMethods.POINT Position => _position;
#endif

    /// <summary>Shows the overlay and starts the scene. Call on the UI thread.</summary>
    public void Start()
    {
        PlaceInCorner();
        _window = NativeMethods.CreateWindowEx(
            (int)(NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOPMOST),
            "Static", null, NativeMethods.WS_POPUP,
            _position.X, _position.Y, _size.Width, _size.Height,
            IntPtr.Zero, IntPtr.Zero, NativeMethods.GetModuleHandle(IntPtr.Zero), IntPtr.Zero);
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException("The scene overlay could not be created.");
        }

        CreateSurface();
        NativeMethods.timeBeginPeriod(1);
        _clock.Start();
        ShowFrame(0);
        NativeMethods.ShowWindow(_window, NativeMethods.SW_SHOWNOACTIVATE);

        // A steady 60-frames-a-second beat from a background thread; each beat asks the
        // UI thread for one frame, and is skipped while the previous one is still drawing.
        var beat = new Thread(() =>
        {
            double next = 0;
            while (!_finished)
            {
                next += FrameInterval.TotalSeconds;
                double wait = next - _clock.Elapsed.TotalSeconds;
                if (wait > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(wait));
                }
                else
                {
                    next = _clock.Elapsed.TotalSeconds;
                }

                if (Interlocked.Exchange(ref _framePending, 1) == 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        OnFrame();
                        Volatile.Write(ref _framePending, 0);
                    }, DispatcherPriority.Render);
                }
            }
        })
        { IsBackground = true, Name = "Worm scene frames" };
        beat.Start();
    }

    private int _framePending;

    private void OnFrame()
    {
        if (_finished)
        {
            return;
        }

        double t = _clock.Elapsed.TotalSeconds;
        if (!_popped && t >= _popAt)
        {
            _popped = true;
            Popped?.Invoke(this, EventArgs.Empty);
        }

        if (t >= _scene.Script.Length)
        {
            Finish();
            return;
        }

        try
        {
            ShowFrame(t);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Finish();
        }
#if DEBUG
        Frames++;
        LongestGap = Math.Max(LongestGap, (t - _lastFrame) * 1000);
        _lastFrame = t;
#endif
    }

    private void Finish()
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        NativeMethods.timeEndPeriod(1);
        ReleaseSurface();

        // Never leave a tab open because the scene ended early.
        if (!_popped)
        {
            _popped = true;
            Popped?.Invoke(this, EventArgs.Empty);
        }

        Finished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Paints the scene at time <paramref name="t"/> and hands the pixels to Windows.</summary>
    private void ShowFrame(double t)
    {
        _pixels!.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(_pixels))
        {
            _drawing.Time = t;
            var dpi = new Vector(96 * _scaling, 96 * _scaling);
            Avalonia.Skia.Helpers.DrawingContextHelper.RenderAsync(canvas, _drawing, new Rect(0, 0, _size.Width, _size.Height), dpi).Wait();
        }

        NativeMethods.GdiFlush();
        var source = default(NativeMethods.POINT);
        var blend = new NativeMethods.BLENDFUNCTION
        {
            BlendOp = NativeMethods.AC_SRC_OVER,
            SourceConstantAlpha = 255,
            AlphaFormat = NativeMethods.AC_SRC_ALPHA,
        };
        NativeMethods.UpdateLayeredWindow(_window, _screenDc, ref _position, ref _size, _memoryDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
    }

    /// <summary>
    /// The bottom-right corner of the chosen screen itself (over the taskbar), in physical
    /// pixels, so the brain rises from the screen's bottom edge and the doctor enters from its right edge.
    /// </summary>
    private void PlaceInCorner()
    {
        var point = _near is { } p ? new NativeMethods.POINT { X = p.X, Y = p.Y } : default;
        IntPtr monitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new NativeMethods.MONITORINFO { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        NativeMethods.GetMonitorInfo(monitor, ref info);
        if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpi, out _) == 0 && dpi > 0)
        {
            _scaling = dpi / 96.0;
        }

        _drawing.Scaling = _scaling;
        _size = new NativeMethods.SIZE
        {
            Width = (int)Math.Ceiling(SceneView.Width * _scaling),
            Height = (int)Math.Ceiling(SceneView.Height * _scaling),
        };
        _position = new NativeMethods.POINT { X = info.Monitor.Right - _size.Width, Y = info.Monitor.Bottom - _size.Height };
    }

    /// <summary>A pixel buffer Windows can take directly (top-down, premultiplied BGRA), which the painter draws into.</summary>
    private void CreateSurface()
    {
        _screenDc = NativeMethods.GetDC(IntPtr.Zero);
        _memoryDc = NativeMethods.CreateCompatibleDC(_screenDc);
        var header = new NativeMethods.BITMAPINFOHEADER
        {
            Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
            Width = _size.Width,
            Height = -_size.Height,
            Planes = 1,
            BitCount = 32,
        };
        _dib = NativeMethods.CreateDIBSection(_memoryDc, ref header, 0, out _bits, IntPtr.Zero, 0);
        if (_dib == IntPtr.Zero)
        {
            throw new InvalidOperationException("The scene overlay's pixels could not be allocated.");
        }

        _previousBitmap = NativeMethods.SelectObject(_memoryDc, _dib);
        _pixels = new SKBitmap();
        _pixels.InstallPixels(new SKImageInfo(_size.Width, _size.Height, SKColorType.Bgra8888, SKAlphaType.Premul), _bits, _size.Width * 4);
    }

    private void ReleaseSurface()
    {
        if (_window != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_window);
            _window = IntPtr.Zero;
        }

        _pixels?.Dispose();
        _pixels = null;
        if (_memoryDc != IntPtr.Zero)
        {
            NativeMethods.SelectObject(_memoryDc, _previousBitmap);
            NativeMethods.DeleteDC(_memoryDc);
            _memoryDc = IntPtr.Zero;
        }

        if (_dib != IntPtr.Zero)
        {
            NativeMethods.DeleteObject(_dib);
            _dib = IntPtr.Zero;
        }

        if (_screenDc != IntPtr.Zero)
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, _screenDc);
            _screenDc = IntPtr.Zero;
        }
    }

    /// <summary>The scene as a visual, so Avalonia's painter can draw it onto the overlay's pixels.</summary>
    private sealed class Drawing : Control
    {
        private readonly WormScene _scene;

        public Drawing(WormScene scene)
        {
            _scene = scene;
            Measure(new Size(SceneView.Width, SceneView.Height));
            Arrange(new Rect(0, 0, SceneView.Width, SceneView.Height));
        }

        public double Time { get; set; }

        /// <summary>The screen's zoom level; the painter draws in physical pixels, so the scene is scaled up here.</summary>
        public double Scaling { get; set; } = 1;

        public override void Render(DrawingContext context)
        {
            using (context.PushTransform(Matrix.CreateScale(Scaling, Scaling)))
            {
                _scene.Draw(context, Time);
            }
        }
    }
}
