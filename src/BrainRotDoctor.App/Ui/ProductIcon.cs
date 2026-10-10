using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BrainRotDoctor.App.Ui.Scene;
using System.IO;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// The product mark: the hypnotised brain from the worm scene, drawn at runtime by
/// the scene itself so it always matches the animation and we ship no image. The
/// exe's own icon file is generated from here (<c>--dump-icon file.ico</c>).
/// </summary>
internal static class ProductIcon
{
    /// <summary>The scene moment the mark shows: brain up, spiral eyes, worm peeking out.</summary>
    private const double Moment = 0.5;

    /// <summary>The square of the scene (in its DIPs) that frames the brain and worm.</summary>
    private static readonly Rect Frame = new(4, 23, 196, 196);

    /// <summary>The sizes inside the exe's icon file, from the tray up to the Start menu.</summary>
    private static readonly int[] IconFileSizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };

    /// <summary>At this size and below the brain's folds are left out: they only blur into noise.</summary>
    private const int PlainUpTo = 24;

    private static readonly Dictionary<int, RenderTargetBitmap> Cache = new();

    public static WindowIcon Create() => new(At(64));

    /// <summary>The mark at <paramref name="size"/> pixels square, drawn once and kept.</summary>
    public static RenderTargetBitmap At(int size)
    {
        if (!Cache.TryGetValue(size, out RenderTargetBitmap? bitmap))
        {
            bitmap = Render(size);
            Cache[size] = bitmap;
        }

        return bitmap;
    }

    /// <summary>Writes the mark as a PNG, or as a multi-size icon file when the path ends in .ico.</summary>
    public static void Save(string path)
    {
        if (!path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
        {
            Render(512).Save(path);
            return;
        }

        var images = IconFileSizes.Select(size =>
        {
            using RenderTargetBitmap bitmap = Render(size);
            using var png = new MemoryStream();
            bitmap.Save(png);
            return (size, png.ToArray());
        }).ToList();

        // ICONDIR, one ICONDIRENTRY per size, then the PNG images (Windows Vista+).
        using var file = new BinaryWriter(File.Create(path));
        file.Write((ushort)0);
        file.Write((ushort)1);
        file.Write((ushort)images.Count);
        int offset = 6 + 16 * images.Count;
        foreach ((int size, byte[] data) in images)
        {
            file.Write((byte)(size >= 256 ? 0 : size));
            file.Write((byte)(size >= 256 ? 0 : size));
            file.Write((byte)0);
            file.Write((byte)0);
            file.Write((ushort)1);
            file.Write((ushort)32);
            file.Write(data.Length);
            file.Write(offset);
            offset += data.Length;
        }

        foreach ((_, byte[] data) in images)
        {
            file.Write(data);
        }
    }

    private static RenderTargetBitmap Render(int size)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(96, 96));
        using DrawingContext dc = bitmap.CreateDrawingContext();
        double k = size / Frame.Width;
        using (dc.PushTransform(Matrix.CreateTranslation(-Frame.X, -Frame.Y) * Matrix.CreateScale(k, k)))
        {
            new WormScene(new ExtractionScript(), folds: size > PlainUpTo).Draw(dc, Moment);
        }

        return bitmap;
    }
}
