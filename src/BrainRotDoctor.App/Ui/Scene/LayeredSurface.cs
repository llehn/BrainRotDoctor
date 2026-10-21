using BrainRotDoctor.App.Runtime;
using SkiaSharp;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// A see-through window over everything that never takes focus, lets clicks pass
/// through and shows in no taskbar or Alt-Tab list, with a pixel buffer the painter draws
/// into: Windows' own layered window. The worm scene and the heads-up use it.
/// </summary>
internal sealed class LayeredSurface : IDisposable
{
    private IntPtr _window, _screenDc, _memoryDc, _dib, _previousBitmap;
    private NativeMethods.POINT _position;
    private NativeMethods.SIZE _size;
    private bool _shown;

    /// <summary>Creates the window at <paramref name="x"/>, <paramref name="y"/> (physical pixels), hidden until the first <see cref="Show"/>.</summary>
    public LayeredSurface(int x, int y, int width, int height)
    {
        _position = new NativeMethods.POINT { X = x, Y = y };
        _size = new NativeMethods.SIZE { Width = width, Height = height };
        _window = NativeMethods.CreateWindowEx(
            (int)(NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOPMOST),
            "Static", null, NativeMethods.WS_POPUP,
            x, y, width, height,
            IntPtr.Zero, IntPtr.Zero, NativeMethods.GetModuleHandle(IntPtr.Zero), IntPtr.Zero);
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException("The overlay could not be created.");
        }

        _screenDc = NativeMethods.GetDC(IntPtr.Zero);
        _memoryDc = NativeMethods.CreateCompatibleDC(_screenDc);
        var header = new NativeMethods.BITMAPINFOHEADER
        {
            Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
            Width = width,
            Height = -height,
            Planes = 1,
            BitCount = 32,
        };
        _dib = NativeMethods.CreateDIBSection(_memoryDc, ref header, 0, out IntPtr bits, IntPtr.Zero, 0);
        if (_dib == IntPtr.Zero)
        {
            Dispose();
            throw new InvalidOperationException("The overlay's pixels could not be allocated.");
        }

        _previousBitmap = NativeMethods.SelectObject(_memoryDc, _dib);
        Pixels = new SKBitmap();
        Pixels.InstallPixels(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul), bits, width * 4);
    }

    /// <summary>The buffer to paint (top-down, premultiplied BGRA); <see cref="Show"/> hands it to Windows.</summary>
    public SKBitmap Pixels { get; private set; }

    public IntPtr Handle => _window;

    public NativeMethods.POINT Position => _position;

    /// <summary>Shows what was painted into <see cref="Pixels"/>; the first call also shows the window.</summary>
    public void Show()
    {
        NativeMethods.GdiFlush();
        var source = default(NativeMethods.POINT);
        var blend = new NativeMethods.BLENDFUNCTION
        {
            BlendOp = NativeMethods.AC_SRC_OVER,
            SourceConstantAlpha = 255,
            AlphaFormat = NativeMethods.AC_SRC_ALPHA,
        };
        NativeMethods.UpdateLayeredWindow(_window, _screenDc, ref _position, ref _size, _memoryDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
        if (!_shown)
        {
            _shown = true;
            NativeMethods.ShowWindow(_window, NativeMethods.SW_SHOWNOACTIVATE);
        }
    }

    public void Dispose()
    {
        if (_window != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_window);
            _window = IntPtr.Zero;
        }

        Pixels?.Dispose();
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
}
