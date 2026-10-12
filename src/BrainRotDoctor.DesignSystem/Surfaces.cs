using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BrainRotDoctor.DesignSystem;

public enum CardStyle
{
    /// <summary>White (dark: raised grey) with a hairline border: the default container.</summary>
    Surface,

    /// <summary>Sunk into the page background, no border: a summary or a result inside a card.</summary>
    Inset,

    /// <summary>Like <see cref="Inset"/> but outlined: a nested editing area inside a card.</summary>
    Well,

    /// <summary>Only an outline: an empty state waiting for content.</summary>
    Outline,

    /// <summary>Filled ink: the single most important block of a screen. Content uses the OnInk tones.</summary>
    Ink,
}

public enum CardPadding
{
    None,

    /// <summary>16.</summary>
    Compact,

    /// <summary>20.</summary>
    Regular,

    /// <summary>24.</summary>
    Roomy,

    /// <summary>32.</summary>
    Spacious,
}

/// <summary>
/// The container for a group of related content. Subscribing to <see cref="Click"/>
/// makes the whole card clickable (hand cursor, stronger border on hover).
/// </summary>
public sealed class Card : Border
{
    private readonly CardStyle _style;
    private EventHandler? _click;
    private bool _interactive;

    public Card(Control content, CardStyle style = CardStyle.Surface, CardPadding padding = CardPadding.Regular)
    {
        _style = style;
        Child = content;
        ClipToBounds = true;
        Padding = new Thickness(padding switch
        {
            CardPadding.None => 0,
            CardPadding.Compact => Space.S4,
            CardPadding.Roomy => Space.S6,
            CardPadding.Spacious => Space.S8,
            _ => Space.S5,
        });
        CornerRadius = new CornerRadius(style switch
        {
            CardStyle.Surface => Radius.Card,
            CardStyle.Ink => Radius.Feature,
            _ => Radius.Inset,
        });

        switch (style)
        {
            case CardStyle.Surface:
                BorderThickness = new Thickness(1);
                this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
                this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
                break;
            case CardStyle.Inset:
                this[!BackgroundProperty] = Palette.Dyn(Palette.AppBg);
                break;
            case CardStyle.Well:
                BorderThickness = new Thickness(1);
                this[!BackgroundProperty] = Palette.Dyn(Palette.AppBg);
                this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
                break;
            case CardStyle.Outline:
                BorderThickness = new Thickness(1.5);
                this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
                break;
            case CardStyle.Ink:
                this[!BackgroundProperty] = Palette.Dyn(Palette.Ink);
                break;
        }
    }

    public event EventHandler? Click
    {
        add
        {
            MakeInteractive();
            _click += value;
        }

        remove => _click -= value;
    }

    private void MakeInteractive()
    {
        if (_interactive)
        {
            return;
        }

        _interactive = true;
        Cursor = new Cursor(StandardCursorType.Hand);
        AddHandler(Gestures.TappedEvent, (_, _) => _click?.Invoke(this, EventArgs.Empty));
        if (_style == CardStyle.Surface)
        {
            PointerEntered += (_, _) => this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
            PointerExited += (_, _) => this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
        }
    }
}

/// <summary>
/// A full-width row inside a card (rows are separated by <see cref="Divider"/>s).
/// <see cref="Shaded"/> sets it apart: a summary row, or the row that matches something.
/// </summary>
public sealed class Band : Border
{
    public Band(Control content, Thickness padding, bool shaded = false)
    {
        Child = content;
        Padding = padding;
        Shaded = shaded;
    }

    public bool Shaded
    {
        get => Background is not null && Background != Brushes.Transparent;
        set
        {
            if (value)
            {
                this[!BackgroundProperty] = Palette.Dyn(Palette.SurfaceAlt);
            }
            else
            {
                Background = Brushes.Transparent;
            }
        }
    }
}

/// <summary>A hairline between rows or sections.</summary>
public sealed class Divider : Border
{
    public Divider()
    {
        Height = 1;
        this[!BackgroundProperty] = Palette.Dyn(Palette.Border);
    }
}

/// <summary>
/// A page-level message: an icon, the message, and an optional action at the end.
/// <see cref="Tone.Warn"/> for a temporary state, <see cref="Tone.Danger"/> for errors.
/// </summary>
public sealed class Banner : Border
{
    public Banner(Tone tone, string glyph, Control message, Control? action = null)
    {
        (string surface, _) = Palette.Soft(tone);
        TextTone fg = tone switch
        {
            Tone.Danger => TextTone.Danger,
            Tone.Warn => TextTone.Warn,
            Tone.Accent => TextTone.Accent,
            _ => TextTone.Secondary,
        };

        var icon = new Icon(glyph, 18, fg) { Margin = new Thickness(0, 0, 14, 0) };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        grid.Children.Add(icon);
        Grid.SetColumn(message, 1);
        message.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(message);
        if (action is not null)
        {
            action.Margin = new Thickness(14, 0, 0, 0);
            Grid.SetColumn(action, 2);
            grid.Children.Add(action);
        }

        Child = grid;
        CornerRadius = new CornerRadius(Radius.Inset);
        Padding = new Thickness(Space.S4, Space.S3);
        BorderThickness = new Thickness(1);
        this[!BackgroundProperty] = Palette.Dyn(surface);
        this[!BorderBrushProperty] = Palette.Dyn(tone == Tone.Warn ? Palette.WarnBorder : surface);
    }

    /// <summary>The message text in the banner's colour.</summary>
    public static TextLine Message(Tone tone, params Phrase[] phrases) =>
        TextLine.Rich(TextStyle.Body, tone switch
        {
            Tone.Danger => TextTone.Danger,
            Tone.Warn => TextTone.Warn,
            Tone.Accent => TextTone.Accent,
            _ => TextTone.Primary,
        }, phrases);
}

/// <summary>An outcome inside a card: a coloured dot, a title in the tone's colour, and an explanation.</summary>
public sealed class Callout : Border
{
    public Callout(Tone tone, string title, string detail)
    {
        (string surface, string fg) = Palette.Soft(tone);
        var dot = new Dot(tone, 10) { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 5, 12, 0) };
        var heading = new TextLine(title, TextStyle.Strong);
        heading[!TextBlock.ForegroundProperty] = Palette.Dyn(fg);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(dot);
        Control text = Layout.Column(3, heading, new TextLine(detail));
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        Child = grid;
        CornerRadius = new CornerRadius(Radius.Inset);
        Padding = new Thickness(Space.S4, 14);
        this[!BackgroundProperty] = Palette.Dyn(surface);
    }
}

/// <summary>A dashed tile inviting to create something new, at the end of a grid of tiles.</summary>
public sealed class PlaceholderTile : Grid
{
    public PlaceholderTile(string glyph, string title, string subtitle)
    {
        var dash = new Rectangle
        {
            RadiusX = Radius.Card,
            RadiusY = Radius.Card,
            StrokeThickness = 1.5,
            StrokeDashArray = new AvaloniaList<double> { 4, 3 },
            [!Shape.StrokeProperty] = Palette.Dyn(Palette.BorderStrong),
        };

        var plus = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Icon(glyph, 20) { HorizontalAlignment = HorizontalAlignment.Center },
            [!Border.BackgroundProperty] = Palette.Dyn(Palette.Track),
        };
        Control content = Layout.Column(Space.S3,
            plus,
            new TextLine(title, TextStyle.Label) { Centered = true },
            new TextLine(subtitle, TextStyle.Small) { Centered = true });
        content.VerticalAlignment = VerticalAlignment.Center;

        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);
        Children.Add(dash);
        Children.Add(content);
        AddHandler(Gestures.TappedEvent, (_, _) => Click?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? Click;
}
