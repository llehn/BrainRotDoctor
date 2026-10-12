using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace BrainRotDoctor.DesignSystem;

/// <summary>A short status in a rounded pill, coloured by its tone. Can change state in place.</summary>
public sealed class Chip : Border
{
    private readonly TextLine _label;

    public Chip(string text, Tone tone = Tone.Neutral)
    {
        _label = new TextLine("", TextStyle.CaptionStrong) { FontSize = 12, TextWrapping = TextWrapping.NoWrap };
        CornerRadius = new CornerRadius(20);
        Padding = new Thickness(9, 3, 9, 4);
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Left;
        Child = _label;
        SetState(text, tone);
    }

    public void SetState(string text, Tone tone)
    {
        (string surface, string fg) = Palette.Soft(tone);
        _label.SetText(text);
        _label[!TextBlock.ForegroundProperty] = Palette.Dyn(fg);
        this[!BackgroundProperty] = Palette.Dyn(surface);
    }
}

/// <summary>A small solid circle marking a state. <see cref="Tone.Ink"/> is for use on ink surfaces.</summary>
public sealed class Dot : Border
{
    public Dot(Tone tone, double size = 8)
    {
        Width = size;
        Height = size;
        CornerRadius = new CornerRadius(size / 2);
        VerticalAlignment = VerticalAlignment.Center;
        SetTone(tone);
    }

    public void SetTone(Tone tone) => this[!BackgroundProperty] = Palette.Dyn(Palette.Solid(tone));
}

/// <summary>
/// How much of something is used: a thin bar filled to a fraction in its tone.
/// <c>onInk</c> draws it for an ink surface.
/// </summary>
public sealed class Meter : Border
{
    private readonly ColumnDefinition _fill = new(0, GridUnitType.Star);
    private readonly ColumnDefinition _rest = new(1, GridUnitType.Star);
    private readonly Border _bar;

    public Meter(double fraction, Tone tone, bool onInk = false)
    {
        double height = onInk ? 8 : 6;
        _bar = new Border { CornerRadius = new CornerRadius(height / 2) };
        var grid = new Grid { Height = height, ColumnDefinitions = new ColumnDefinitions { _fill, _rest } };
        grid.Children.Add(_bar);

        Height = height;
        CornerRadius = new CornerRadius(height / 2);
        ClipToBounds = true;
        Child = grid;
        this[!BackgroundProperty] = Palette.Dyn(onInk ? Palette.InkTrack : Palette.Track);
        if (onInk)
        {
            _bar[!BackgroundProperty] = Palette.Dyn(Palette.InkText);
        }
        else
        {
            SetTone(tone);
        }

        SetFraction(fraction);
    }

    public void SetFraction(double fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        _fill.Width = new GridLength(fraction, GridUnitType.Star);
        _rest.Width = new GridLength(1 - fraction, GridUnitType.Star);
    }

    public void SetTone(Tone tone) => _bar[!BackgroundProperty] = Palette.Dyn(Palette.Solid(tone));
}

/// <summary>Brand colours for a <see cref="Monogram"/>, as "#RRGGBB" hex.</summary>
public readonly record struct Swatch(string Background, string Foreground)
{
    /// <summary>The neutral swatch for things without a brand colour.</summary>
    public static Swatch Neutral => new("#E6E4DE", "#3D3F45");
}

/// <summary>A rounded square with one or two letters, standing in for a logo.</summary>
public sealed class Monogram : Border
{
    public Monogram(string initials, Swatch swatch, double size = 36)
    {
        Width = size;
        Height = size;
        CornerRadius = new CornerRadius(size * 0.28);
        Background = new SolidColorBrush(Color.Parse(swatch.Background));
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Left;
        Child = new TextBlock
        {
            Text = initials,
            FontSize = size * 0.36,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse(swatch.Foreground)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    /// <summary>Up to two letters from a name: "Reddit" → "Re", "x.com" → "Xc".</summary>
    public static string InitialsOf(string name)
    {
        string letters = new(name.Where(char.IsLetterOrDigit).Take(2).ToArray());
        return letters.Length == 0 ? "?" : char.ToUpperInvariant(letters[0]) + letters[1..].ToLowerInvariant();
    }
}

/// <summary>A compact label with a leading mark (e.g. a monogram) and an optional muted detail.</summary>
public sealed class InfoChip : Border
{
    public InfoChip(Control leading, string label, string? detail = null)
    {
        var row = Layout.Row(6, leading, new TextLine(label, TextStyle.Caption, TextTone.Primary) { FontWeight = FontWeight.Medium, TextWrapping = TextWrapping.NoWrap });
        if (detail is not null)
        {
            row.Children.Add(new TextLine(detail, TextStyle.Caption) { TextWrapping = TextWrapping.NoWrap });
        }

        CornerRadius = new CornerRadius(7);
        Padding = new Thickness(3, 3, 9, 3);
        Child = row;
        this[!BackgroundProperty] = Palette.Dyn(Palette.AppBg);
    }
}

/// <summary>A clickable literal value offered as an example to try.</summary>
public sealed class SuggestionChip : Border
{
    public SuggestionChip(string text)
    {
        Height = 32;
        Padding = new Thickness(10, 0);
        CornerRadius = new CornerRadius(Radius.Small);
        BorderThickness = new Thickness(1);
        Cursor = new Cursor(StandardCursorType.Hand);
        Child = new TextLine(text, TextStyle.Code, TextTone.Secondary);
        this[!BackgroundProperty] = Palette.Dyn(Palette.AppBg);
        this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            Click?.Invoke(this, EventArgs.Empty);
        });
    }

    public event EventHandler? Click;
}

/// <summary>A literal value in a list that can be removed with its ×.</summary>
public sealed class RemovableChip : Border
{
    public RemovableChip(string text, string removeTip)
    {
        var remove = new IconButton(Glyph.Close, removeTip, IconButtonStyle.Bare);
        remove.Click += (_, _) => Removed?.Invoke();
        CornerRadius = new CornerRadius(Radius.Small);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(12, 2, 2, 2);
        Child = Layout.Row(Space.S1, new TextLine(text, TextStyle.Code), remove);
        this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
        this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
    }

    public event Action? Removed;
}

/// <summary>
/// The overall state of the app at a glance: a dot, a title and a line of detail.
/// Inverted (ink) for the most serious state.
/// </summary>
public sealed class StatusPanel : Border
{
    private readonly Dot _dot = new(Tone.Success);
    private readonly TextLine _title = new("", TextStyle.Strong);
    private readonly TextLine _detail = new("", TextStyle.Small);

    public StatusPanel()
    {
        CornerRadius = new CornerRadius(Radius.Inset);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(14);
        Child = Layout.Column(Space.S1, Layout.Row(Space.S2, _dot, _title), _detail);
        SetState(Tone.Success, "", "", inverted: false);
    }

    public void SetState(Tone dot, string title, string detail, bool inverted)
    {
        _dot.SetTone(inverted ? Tone.Ink : dot);
        _title.SetText(title);
        _detail.SetText(detail);
        _title.SetTone(inverted ? TextTone.OnInk : TextTone.Primary);
        _detail.SetTone(inverted ? TextTone.OnInkMuted : TextTone.Secondary);
        this[!BackgroundProperty] = Palette.Dyn(inverted ? Palette.Ink : Palette.Surface);
        this[!BorderBrushProperty] = Palette.Dyn(inverted ? Palette.Ink : Palette.Border);
    }
}
