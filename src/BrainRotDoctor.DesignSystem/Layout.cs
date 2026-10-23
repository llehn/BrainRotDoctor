using Avalonia.Controls;
using Avalonia.Layout;

namespace BrainRotDoctor.DesignSystem;

/// <summary>
/// Arrangement helpers. Screens may also use Avalonia's plain layout panels
/// (Grid, StackPanel, DockPanel, WrapPanel, ScrollViewer) directly.
/// </summary>
public static class Layout
{
    /// <summary>Children side by side, vertically centred.</summary>
    public static StackPanel Row(double spacing, params Control[] children)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (Control c in children)
        {
            c.VerticalAlignment = VerticalAlignment.Center;
            s.Children.Add(c);
        }

        return s;
    }

    /// <summary>Children stacked top to bottom.</summary>
    public static StackPanel Column(double spacing, params Control[] children)
    {
        var s = new StackPanel { Spacing = spacing };
        foreach (Control c in children)
        {
            s.Children.Add(c);
        }

        return s;
    }

    /// <summary>
    /// Two columns side by side, always: there is no stacked layout; the window's
    /// minimum size keeps both readable. <paramref name="fixedWidth"/> sizes the
    /// left column, or the right one when <paramref name="fixedRight"/> is set;
    /// the other takes the rest.
    /// </summary>
    public static Grid Columns(Control left, Control right, double fixedWidth, double gap = Space.S6, bool fixedRight = false)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(fixedRight ? $"*,{gap},{fixedWidth}" : $"{fixedWidth},{gap},*") };
        left.VerticalAlignment = VerticalAlignment.Top;
        right.VerticalAlignment = VerticalAlignment.Top;
        grid.Children.Add(left);
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        return grid;
    }

    /// <summary>Equal-width tiles whose count per row follows the available width.</summary>
    public static WrapPanel Tiles(IReadOnlyList<Control> tiles, double minTileWidth, double gap)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = gap, LineSpacing = gap };
        foreach (Control tile in tiles)
        {
            wrap.Children.Add(tile);
        }

        wrap.SizeChanged += (_, e) =>
        {
            double width = e.NewSize.Width;
            int columns = Math.Max(1, (int)((width + gap) / (minTileWidth + gap)));
            double tileWidth = Math.Floor((width - gap * (columns - 1)) / columns) - 0.5;
            foreach (Control tile in wrap.Children)
            {
                tile.Width = Math.Max(minTileWidth * 0.6, tileWidth);
            }
        };
        return wrap;
    }

    /// <summary>Children in one row of equal columns that fill the width, all as tall as the tallest.</summary>
    public static Grid EqualColumns(double gap, params Control[] children)
    {
        var grid = new Grid();
        for (int i = 0; i < children.Length; i++)
        {
            if (i > 0)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(gap, GridUnitType.Pixel));
            }

            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            Grid.SetColumn(children[i], i * 2);
            grid.Children.Add(children[i]);
        }

        return grid;
    }

    /// <summary>Fades content that is present but currently not in effect.</summary>
    public static T Inactive<T>(T control) where T : Control
    {
        control.Opacity = 0.5;
        return control;
    }
}
