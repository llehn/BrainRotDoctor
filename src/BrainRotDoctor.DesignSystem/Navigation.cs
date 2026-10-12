using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BrainRotDoctor.DesignSystem;

/// <summary>An entry of the sidebar: icon, label, optional badge; raised when it is the current page.</summary>
public sealed class NavItem : Border
{
    public NavItem(string glyph, string label, bool active, string? badge = null)
    {
        var row = new DockPanel { LastChildFill = true };
        var icon = new Icon(glyph, 18, active ? TextTone.Primary : TextTone.Secondary) { Margin = new Thickness(0, 0, 10, 0) };
        DockPanel.SetDock(icon, Dock.Left);
        row.Children.Add(icon);
        if (badge is not null)
        {
            var chip = new Chip(badge, Tone.Ink);
            DockPanel.SetDock(chip, Dock.Right);
            row.Children.Add(chip);
        }

        var text = new TextLine(label, TextStyle.Strong, active ? TextTone.Primary : TextTone.Secondary);
        if (!active)
        {
            text.FontWeight = FontWeight.Medium;
        }

        row.Children.Add(text);

        Height = 40;
        Padding = new Thickness(12, 0);
        CornerRadius = new CornerRadius(9);
        Cursor = new Cursor(StandardCursorType.Hand);
        Background = Brushes.Transparent;
        Child = row;
        if (active)
        {
            this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
        }

        AddHandler(Gestures.TappedEvent, (_, _) => Click?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? Click;
}

/// <summary>
/// A menu that drops down from a button: an optional heading, items with an
/// optional trailing hint, dividers and notes. Picking an item closes it.
/// </summary>
public sealed class PopupMenu
{
    private readonly Flyout _flyout = new() { Placement = PlacementMode.BottomEdgeAlignedRight };
    private readonly StackPanel _items;

    public PopupMenu(double width = 280)
    {
        _items = new StackPanel { Width = width, Spacing = 2 };
        _flyout.Content = _items;
    }

    public void AddHeading(string text) =>
        _items.Children.Add(new TextLine(text, TextStyle.Overline) { Margin = new Thickness(10, 6) });

    public void AddItem(string label, string? hint, Action pick)
    {
        var row = new DockPanel { LastChildFill = true };
        if (hint is not null)
        {
            var trailing = new TextLine(hint, TextStyle.Caption);
            DockPanel.SetDock(trailing, Dock.Right);
            row.Children.Add(trailing);
        }

        row.Children.Add(new TextLine(label));
        var item = new Border
        {
            Height = 42,
            Padding = new Thickness(10, 0),
            CornerRadius = new CornerRadius(Radius.Small),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = row,
        };
        item.PointerEntered += (_, _) => item[!Border.BackgroundProperty] = Palette.Dyn(Palette.Track);
        item.PointerExited += (_, _) => item.Background = Brushes.Transparent;
        item.AddHandler(Gestures.TappedEvent, (_, _) =>
        {
            _flyout.Hide();
            pick();
        });
        _items.Children.Add(item);
    }

    public void AddDivider() => _items.Children.Add(new Divider { Margin = new Thickness(4, 6) });

    public void AddNote(string glyph, string text)
    {
        StackPanel row = Layout.Row(Space.S2, new Icon(glyph, 14, TextTone.Tertiary), new TextLine(text, TextStyle.Caption) { MaxWidth = 230 });
        row.Margin = new Thickness(10, 2, 10, 6);
        _items.Children.Add(row);
    }

    public void ShowAt(Control anchor) => _flyout.ShowAt(anchor);
}

/// <summary>
/// The top bar of a full-window screen: a way back, the screen's title (or an
/// editable title), and the screen's actions on the right.
/// </summary>
public sealed class TopBar : Border
{
    public TopBar(Control back, Control title, Control actions)
    {
        var rule = new Border { Width = 1, Height = 24, Margin = new Thickness(4, 0, 12, 0), [!BackgroundProperty] = Palette.Dyn(Palette.Border) };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto") };
        grid.Children.Add(back);
        Grid.SetColumn(rule, 1);
        grid.Children.Add(rule);
        title.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(title, 2);
        grid.Children.Add(title);
        actions.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(actions, 3);
        grid.Children.Add(actions);

        Padding = new Thickness(Space.S6, Space.S3);
        BorderThickness = new Thickness(0, 0, 0, 1);
        Child = grid;
        this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
        this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
    }
}

/// <summary>
/// The main layout: a fixed sidebar (navigation on top, status at the bottom)
/// and a scrolling content area.
/// </summary>
public sealed class SidebarLayout : DockPanel
{
    public SidebarLayout(Control sidebarTop, Control sidebarBottom, Control content)
    {
        var side = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(sidebarTop, Dock.Top);
        DockPanel.SetDock(sidebarBottom, Dock.Bottom);
        side.Children.Add(sidebarTop);
        side.Children.Add(sidebarBottom);

        var sidebar = new Border
        {
            Width = 236,
            Padding = new Thickness(14, Space.S5, 14, Space.S4),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = side,
            [!Border.BackgroundProperty] = Palette.Dyn(Palette.Sidebar),
            [!Border.BorderBrushProperty] = Palette.Dyn(Palette.Border),
        };

        var scroll = new ScrollViewer
        {
            Content = new Border { Padding = new Thickness(Space.S10, Space.S8, Space.S10, Space.S10), Child = content },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        LastChildFill = true;
        DockPanel.SetDock(sidebar, Dock.Left);
        Children.Add(sidebar);
        Children.Add(scroll);
    }
}

/// <summary>A full-window screen: a <see cref="TopBar"/> and a scrolling body of limited width.</summary>
public sealed class FullPageLayout : DockPanel
{
    public FullPageLayout(Control bar, Control body)
    {
        var scroll = new ScrollViewer
        {
            Content = new Border
            {
                Padding = new Thickness(Space.S8, 28, Space.S8, 48),
                MaxWidth = 1280,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = body,
            },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        LastChildFill = true;
        DockPanel.SetDock(bar, Dock.Top);
        Children.Add(bar);
        Children.Add(scroll);
    }
}

/// <summary>A page title with an optional subtitle and the page's actions on the right.</summary>
public sealed class PageHeader : Grid
{
    public PageHeader(string title, string? subtitle = null, Control? actions = null)
    {
        StackPanel text = Layout.Column(6, new TextLine(title, TextStyle.Title));
        if (subtitle is not null)
        {
            text.Children.Add(new TextLine(subtitle, TextStyle.Small) { FontSize = 14, MaxWidth = 640, HorizontalAlignment = HorizontalAlignment.Left });
        }

        ColumnDefinitions = new ColumnDefinitions("*,Auto");
        Children.Add(text);
        if (actions is not null)
        {
            actions.VerticalAlignment = VerticalAlignment.Bottom;
            actions.Margin = new Thickness(Space.S4, 0, 0, 0);
            SetColumn(actions, 1);
            Children.Add(actions);
        }
    }
}
