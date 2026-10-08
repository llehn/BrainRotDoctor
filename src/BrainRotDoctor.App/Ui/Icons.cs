using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Line icons (Lucide shapes on a 24-unit grid) drawn as stroked paths, so they
/// stay crisp at any size and take their colour from the theme.
/// </summary>
internal static class Icons
{
    public const string Rules = "M3 6h.01 M3 12h.01 M3 18h.01 M8 6h13 M8 12h13 M8 18h13";
    public const string Lock = "M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2z M7 11V7a5 5 0 0 1 10 0v4";
    public const string Settings =
        "M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z " +
        "M9 12a3 3 0 1 0 6 0a3 3 0 1 0-6 0";
    public const string Shield =
        "M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M9 12l2 2 4-4";
    public const string Pause = "M7 4h2a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1z M15 4h2a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1z";
    public const string Play = "M6 3l14 9-14 9z";
    public const string Plus = "M5 12h14 M12 5v14";
    public const string ArrowLeft = "M12 19l-7-7 7-7 M19 12H5";
    public const string ChevronDown = "M6 9l6 6 6-6";
    public const string ChevronUp = "M6 15l6-6 6 6";
    public const string Pencil =
        "M21.17 6.81a1 1 0 0 0-3.99-3.99L3.84 16.17a2 2 0 0 0-.5.83l-1.32 4.35a.5.5 0 0 0 .62.62l4.35-1.32a2 2 0 0 0 .83-.5z";
    public const string Trash = "M3 6h18 M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6 M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2";
    public const string Close = "M18 6L6 18 M6 6l12 12";
    public const string Clock = "M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0 M12 6v6l4 2";
    public const string Globe = "M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0 M12 2a14.5 14.5 0 0 0 0 20a14.5 14.5 0 0 0 0-20 M2 12h20";
    public const string Check = "M20 6L9 17l-5-5";
    public const string Ban = "M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0 M4.9 4.9l14.2 14.2";
    public const string Search = "M3 11a8 8 0 1 0 16 0a8 8 0 1 0-16 0 M21 21l-4.3-4.3";

    /// <summary>An icon of <paramref name="size"/> px whose stroke follows a theme colour key.</summary>
    public static Control Make(string data, double size = 18, string colorKey = UiTheme.TextPrimary, double stroke = 2)
    {
        Path path = BuildPath(data, stroke);
        path[!Shape.StrokeProperty] = UiTheme.Dyn(colorKey);
        return Wrap(path, size);
    }

    /// <summary>An icon with a fixed stroke brush (used on coloured surfaces).</summary>
    public static Control Make(string data, double size, IBrush brush, double stroke = 2)
    {
        Path path = BuildPath(data, stroke);
        path.Stroke = brush;
        return Wrap(path, size);
    }

    private static Path BuildPath(string data, double stroke) =>
        new()
        {
            Data = Geometry.Parse(data),
            StrokeThickness = stroke,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Fill = null,
        };

    private static Control Wrap(Path path, double size) => new Viewbox
    {
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        Child = new Canvas { Width = 24, Height = 24, Children = { path } },
    };
}
