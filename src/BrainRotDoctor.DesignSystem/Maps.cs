using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Path = Avalonia.Controls.Shapes.Path;

namespace BrainRotDoctor.DesignSystem;

/// <summary>
/// An amount of time as clock faces: one circle per hour, the last one filling
/// like a pie as the hour goes on. The more time in total, the stronger the
/// fill, so a lot of time stands out on its own. Read without reading.
/// </summary>
public sealed class ClockFill : WrapPanel
{
    /// <summary>More hours than this show as this many full circles.</summary>
    public const int MaxCircles = 12;

    private TimeSpan _time;
    private double _size;

    public ClockFill(TimeSpan time, double size = 24)
    {
        Orientation = Orientation.Horizontal;
        _time = time;
        _size = size;
        Rebuild();
    }

    public void SetTime(TimeSpan time)
    {
        if (time != _time)
        {
            _time = time;
            Rebuild();
        }
    }

    /// <summary>The diameter of each circle.</summary>
    public void SetSize(double size)
    {
        if (Math.Abs(size - _size) > 0.1)
        {
            _size = size;
            Rebuild();
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        ItemSpacing = Math.Round(_size * 0.22);
        LineSpacing = ItemSpacing;
        double hours = _time.TotalHours;
        string fill = Palette.Clocks[hours < 0.5 ? 0 : hours < 1 ? 1 : hours < 2 ? 2 : hours < 3 ? 3 : 4];
        double left = hours;
        for (int i = 0; i < MaxCircles && left > 0.001; i++, left -= 1)
        {
            Children.Add(Circle(Math.Min(1, left), fill));
        }
    }

    private Control Circle(double fraction, string fill)
    {
        var track = new Ellipse { Width = 24, Height = 24, [!Shape.FillProperty] = Palette.Dyn(Palette.ClockTrack) };
        var wedge = new Path { Data = Geometry.Parse(Wedge(fraction)), [!Shape.FillProperty] = Palette.Dyn(fill) };
        return new Viewbox
        {
            Width = _size,
            Height = _size,
            Stretch = Stretch.Uniform,
            Child = new Canvas { Width = 24, Height = 24, Children = { track, wedge } },
        };
    }

    private static string Wedge(double fraction)
    {
        if (fraction >= 0.999)
        {
            return "M12,0 A12,12 0 1 1 11.999,0 Z";
        }

        double angle = fraction * 2 * Math.PI;
        double x = 12 + 12 * Math.Sin(angle);
        double y = 12 - 12 * Math.Cos(angle);
        return string.Create(CultureInfo.InvariantCulture, $"M12,12 L12,0 A12,12 0 {(fraction > 0.5 ? 1 : 0)} 1 {x:0.###},{y:0.###} Z");
    }
}

/// <summary>
/// Tiles filling all of their space, each with an area in proportion to its
/// weight: the proportions of a whole at a glance. Add the tiles heaviest first;
/// the map splits them where the weight halves, across the longer side, again
/// and again.
/// </summary>
public sealed class TileMap : Panel
{
    private readonly Dictionary<Control, double> _weights = new();

    /// <summary>The space between tiles.</summary>
    public double Gap { get; set; } = Space.S2;

    public void Add(Control tile, double weight)
    {
        _weights[tile] = Math.Max(weight, 1e-6);
        Children.Add(tile);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size size = Resolve(availableSize);
        foreach ((Control tile, Rect rect) in Place(size))
        {
            tile.Measure(rect.Size);
        }

        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach ((Control tile, Rect rect) in Place(finalSize))
        {
            tile.Arrange(rect);
        }

        return finalSize;
    }

    private static Size Resolve(Size available)
    {
        double width = double.IsInfinity(available.Width) ? 600 : available.Width;
        double height = double.IsInfinity(available.Height) ? width * 0.6 : available.Height;
        return new Size(width, height);
    }

    private List<(Control Tile, Rect Rect)> Place(Size size)
    {
        var items = Children.Where(c => c.IsVisible).Select(c => (Tile: c, Weight: _weights.GetValueOrDefault(c, 1))).ToList();
        var placed = new List<(Control, Rect)>();
        if (items.Count == 0)
        {
            return placed;
        }

        // Laid out over the size plus one gap, each tile then gives back half a
        // gap on every side: gaps between tiles, none at the edges.
        double g = Gap;
        Split(items, new Rect(-g / 2, -g / 2, size.Width + g, size.Height + g), placed);
        for (int i = 0; i < placed.Count; i++)
        {
            Rect r = placed[i].Item2.Deflate(g / 2);
            placed[i] = (placed[i].Item1, new Rect(r.X, r.Y, Math.Max(0, r.Width), Math.Max(0, r.Height)));
        }

        return placed;
    }

    private static void Split(List<(Control Tile, double Weight)> items, Rect area, List<(Control, Rect)> placed)
    {
        if (items.Count == 1)
        {
            placed.Add((items[0].Tile, area));
            return;
        }

        double total = items.Sum(i => i.Weight);
        double taken = items[0].Weight;
        int cut = 1;
        while (cut < items.Count - 1 && Math.Abs(taken + items[cut].Weight - total / 2) <= Math.Abs(taken - total / 2))
        {
            taken += items[cut].Weight;
            cut++;
        }

        double f = taken / total;
        Rect first, second;
        if (area.Width >= area.Height)
        {
            first = new Rect(area.X, area.Y, area.Width * f, area.Height);
            second = new Rect(area.X + area.Width * f, area.Y, area.Width * (1 - f), area.Height);
        }
        else
        {
            first = new Rect(area.X, area.Y, area.Width, area.Height * f);
            second = new Rect(area.X, area.Y + area.Height * f, area.Width, area.Height * (1 - f));
        }

        Split(items.GetRange(0, cut), first, placed);
        Split(items.GetRange(cut, items.Count - cut), second, placed);
    }
}

/// <summary>
/// A tile of a <see cref="TileMap"/>: a coloured block with a title, an amount
/// of time as a <see cref="ClockFill"/>, and optionally smaller tiles inside.
/// Its type and clocks grow with its size; what doesn't fit is left out. A
/// <c>nested</c> tile sits inside another one: lighter, with a literal title.
/// </summary>
public sealed class MapTile : Border
{
    private readonly TextLine _title;
    private readonly ClockFill _clock;
    private readonly Control? _inside;
    private readonly bool _nested;

    /// <param name="title">The name on the tile.</param>
    /// <param name="colour">Which tile colour, counted from 0; neighbours should differ.</param>
    /// <param name="time">The amount of time the tile stands for, shown as clocks.</param>
    /// <param name="inside">Smaller tiles inside (a <see cref="TileMap"/> of nested tiles), shown when there's room.</param>
    /// <param name="nested">The tile sits inside another one.</param>
    /// <param name="muted">A plain grey tile that stands apart from the coloured ones and can't be opened.</param>
    public MapTile(string title, int colour, TimeSpan time, Control? inside = null, bool nested = false, bool muted = false)
    {
        _nested = nested;
        _inside = inside;
        _title = new TextLine(title, nested ? TextStyle.Code : TextStyle.Heading);
        if (nested)
        {
            _title.FontWeight = FontWeight.SemiBold;
            _title.FontSize = 13;
            _title.SingleLine = true;
        }

        _title.VerticalAlignment = VerticalAlignment.Top;
        _title.SingleLine = true;
        _clock = new ClockFill(time, nested ? 14 : 24);
        StackPanel head = Layout.Column(nested ? Space.S2 : 10, _title, _clock);

        var dock = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(head, Dock.Top);
        dock.Children.Add(head);
        if (inside is not null)
        {
            inside.Margin = new Thickness(0, Space.S4, 0, 0);
            dock.Children.Add(inside);
        }

        Child = dock;
        ClipToBounds = true;
        CornerRadius = new CornerRadius(nested ? Radius.Control : Radius.Feature);
        Padding = new Thickness(nested ? 12 : Space.S5);
        this[!BackgroundProperty] = Palette.Dyn(nested ? Palette.TileInset : muted ? Palette.Track : Palette.Tiles[Math.Abs(colour) % Palette.Tiles.Length]);
        if (muted)
        {
            BorderThickness = new Thickness(1);
            this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
        }

        SizeChanged += (_, e) => Adapt(e.NewSize);

        if (!nested && !muted)
        {
            Cursor = new Cursor(StandardCursorType.Hand);
            PointerEntered += (_, _) => Opacity = 0.88;
            PointerExited += (_, _) => Opacity = 1;
            AddHandler(Gestures.TappedEvent, (_, _) => Click?.Invoke(this, EventArgs.Empty));
        }
    }

    public event EventHandler? Click;

    private void Adapt(Size size)
    {
        if (_nested)
        {
            bool labelled = size.Width > 64 && size.Height > 34;
            _title.IsVisible = labelled;
            _clock.IsVisible = labelled && size.Height > 64;
            return;
        }

        double area = size.Width * size.Height;
        (double font, double pad, double clock) = area > 120_000 ? (30, Space.S6, 30)
            : area > 50_000 ? (22, Space.S5, 24)
            : area > 20_000 ? (17, Space.S4, 18)
            : (14, Space.S3, 15);

        // A long name in a narrow tile gets smaller rather than broken mid-word
        // (and ends in "…" only when even that isn't enough).
        int letters = Math.Max(1, _title.Text?.Length ?? 1);
        double fits = (size.Width - 2 * pad) / (letters * 0.56);
        _title.FontSize = Math.Max(11, Math.Min(font, Math.Floor(fits)));
        Padding = new Thickness(pad);
        _clock.SetSize(clock);
        if (_inside is not null)
        {
            _inside.IsVisible = area > 50_000;
        }
    }
}

/// <summary>
/// A sheet over a dimmed screen: one thing looked at closely, with its actions.
/// A tap beside the sheet or Escape raises <see cref="Dismissed"/>.
/// </summary>
public sealed class Overlay : Grid
{
    public Overlay(Control behind, Control sheet)
    {
        Children.Add(behind);
        Children.Add(new Border { [!Border.BackgroundProperty] = Palette.Dyn(Palette.Scrim) });

        sheet.MaxWidth = 680;
        sheet.HorizontalAlignment = HorizontalAlignment.Center;
        sheet.VerticalAlignment = VerticalAlignment.Top;
        sheet.Margin = new Thickness(Space.S6, 56, Space.S6, Space.S10);
        var scroll = new ScrollViewer
        {
            Content = sheet,
            Background = Brushes.Transparent,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        scroll.AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            if (e.Source is Visual v && (v == sheet || sheet.IsVisualAncestorOf(v)))
            {
                return;
            }

            Dismissed?.Invoke(this, EventArgs.Empty);
        });
        Children.Add(scroll);

        Focusable = true;
        AttachedToVisualTree += (_, _) => Focus();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Dismissed?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    public event EventHandler? Dismissed;
}
