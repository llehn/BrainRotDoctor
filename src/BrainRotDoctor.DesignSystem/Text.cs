using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace BrainRotDoctor.DesignSystem;

/// <summary>A piece of a <see cref="TextLine"/> with mixed emphasis.</summary>
public readonly record struct Phrase(string Text, bool Strong = false);

/// <summary>
/// All text in the app. The <see cref="TextStyle"/> fixes size and weight; the
/// <see cref="TextTone"/> (defaulting per style) sets the colour. Wraps by default.
/// </summary>
public sealed class TextLine : TextBlock
{
    private readonly TextStyle _style;

    public TextLine(string text, TextStyle style = TextStyle.Body, TextTone? tone = null)
    {
        _style = style;
        (double size, FontWeight weight) = Metrics(style);
        FontSize = size;
        FontWeight = weight;
        TextWrapping = TextWrapping.Wrap;
        VerticalAlignment = VerticalAlignment.Center;
        if (style == TextStyle.Code)
        {
            FontFamily = new FontFamily(Fonts.Mono);
        }

        if (style == TextStyle.Overline)
        {
            LetterSpacing = 0.7;
        }

        SetText(text);
        SetTone(tone ?? DefaultTone(style));
    }

    /// <summary>Text made of phrases, some of them strong.</summary>
    public static TextLine Rich(TextStyle style, TextTone? tone, params Phrase[] phrases)
    {
        var line = new TextLine("", style, tone);
        line.SetPhrases(phrases);
        return line;
    }

    /// <summary>Keeps the text on one line, ending in "…" when it doesn't fit.</summary>
    public bool SingleLine
    {
        get => TextWrapping == TextWrapping.NoWrap;
        set
        {
            TextWrapping = value ? TextWrapping.NoWrap : TextWrapping.Wrap;
            TextTrimming = value ? TextTrimming.CharacterEllipsis : TextTrimming.None;
        }
    }

    /// <summary>Centres the text, both the lines and the block.</summary>
    public bool Centered
    {
        get => TextAlignment == TextAlignment.Center;
        set
        {
            TextAlignment = value ? TextAlignment.Center : TextAlignment.Left;
            HorizontalAlignment = value ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        }
    }

    public void SetText(string text) => Text = _style == TextStyle.Overline ? text.ToUpper(DesignTheme.TextCulture) : text;

    public void SetTone(TextTone tone) => this[!ForegroundProperty] = Palette.Dyn(Palette.Of(tone));

    public void SetPhrases(params Phrase[] phrases)
    {
        var inlines = new InlineCollection();
        foreach (Phrase phrase in phrases)
        {
            var run = new Run(phrase.Text);
            if (phrase.Strong)
            {
                run.FontWeight = FontWeight.SemiBold;
            }

            inlines.Add(run);
        }

        Inlines = inlines;
    }

    internal static (double Size, FontWeight Weight) Metrics(TextStyle style) => style switch
    {
        TextStyle.Display => (60, FontWeight.SemiBold),
        TextStyle.Title => (26, FontWeight.SemiBold),
        TextStyle.Heading => (20, FontWeight.SemiBold),
        TextStyle.Subheading => (16, FontWeight.SemiBold),
        TextStyle.Label => (15, FontWeight.SemiBold),
        TextStyle.Strong => (14, FontWeight.SemiBold),
        TextStyle.Small => (13, FontWeight.Normal),
        TextStyle.Caption => (12.5, FontWeight.Normal),
        TextStyle.CaptionStrong => (12.5, FontWeight.SemiBold),
        TextStyle.Overline => (11.5, FontWeight.SemiBold),
        TextStyle.Code => (12.5, FontWeight.Normal),
        _ => (14, FontWeight.Normal),
    };

    private static TextTone DefaultTone(TextStyle style) => style switch
    {
        TextStyle.Small or TextStyle.CaptionStrong => TextTone.Secondary,
        TextStyle.Caption or TextStyle.Overline => TextTone.Tertiary,
        _ => TextTone.Primary,
    };
}

/// <summary>
/// Line icons (Lucide shapes on a 24-unit grid), as path data for <see cref="Icon"/>.
/// Named for what they show, not for where they're used.
/// </summary>
public static class Glyph
{
    public const string List = "M3 6h.01 M3 12h.01 M3 18h.01 M8 6h13 M8 12h13 M8 18h13";
    public const string Lock = "M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2z M7 11V7a5 5 0 0 1 10 0v4";
    public const string Gear =
        "M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z " +
        "M9 12a3 3 0 1 0 6 0a3 3 0 1 0-6 0";
    public const string Pause = "M7 4h2a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1z M15 4h2a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1z";
    public const string Play = "M6 3l14 9-14 9z";
    public const string Plus = "M5 12h14 M12 5v14";
    public const string Minus = "M5 12h14";
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
    public const string Pulse = "M22 12h-4l-3 9L9 3l-3 9H2";
    public const string Award = "M5 8a7 7 0 1 0 14 0a7 7 0 1 0-14 0 M8.21 13.89L7 23l5-3 5 3-1.21-9.12";

    /// <summary>Every glyph by name, for the gallery.</summary>
    internal static readonly (string Name, string Data)[] All =
    {
        (nameof(List), List), (nameof(Lock), Lock), (nameof(Gear), Gear), (nameof(Pause), Pause),
        (nameof(Play), Play), (nameof(Plus), Plus), (nameof(Minus), Minus), (nameof(ArrowLeft), ArrowLeft),
        (nameof(ChevronDown), ChevronDown), (nameof(ChevronUp), ChevronUp), (nameof(Pencil), Pencil),
        (nameof(Trash), Trash), (nameof(Close), Close), (nameof(Clock), Clock), (nameof(Globe), Globe),
        (nameof(Check), Check), (nameof(Ban), Ban), (nameof(Search), Search), (nameof(Pulse), Pulse),
        (nameof(Award), Award),
    };
}

/// <summary>A stroked line icon that takes its colour from a <see cref="TextTone"/>.</summary>
public sealed class Icon : Viewbox
{
    private readonly Path _path;

    public Icon(string glyph, double size = 18, TextTone tone = TextTone.Primary, double stroke = 2)
    {
        _path = new Path
        {
            Data = Geometry.Parse(glyph),
            StrokeThickness = stroke,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Fill = null,
        };
        Width = size;
        Height = size;
        Stretch = Stretch.Uniform;
        VerticalAlignment = VerticalAlignment.Center;
        Child = new Canvas { Width = 24, Height = 24, Children = { _path } };
        SetTone(tone);
    }

    public void SetTone(TextTone tone) => SetColor(Palette.Of(tone));

    /// <summary>A palette colour no text tone names (an icon on a light control of an ink card).</summary>
    internal void SetColor(string paletteKey) => _path[!Shape.StrokeProperty] = Palette.Dyn(paletteKey);
}
