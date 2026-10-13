using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;

namespace BrainRotDoctor.DesignSystem;

/// <summary>
/// The semantic colour of a component: what it means, not what it looks like.
/// Each component maps a tone to its own surface and foreground colours.
/// </summary>
public enum Tone
{
    /// <summary>No particular meaning: greys.</summary>
    Neutral,

    /// <summary>Positive, allowed, on: teal.</summary>
    Accent,

    /// <summary>Destructive, blocked, stop: orange-red.</summary>
    Danger,

    /// <summary>Needs attention, temporary state: amber.</summary>
    Warn,

    /// <summary>All good, healthy: green-teal.</summary>
    Success,

    /// <summary>The strongest emphasis: near-black in light mode, near-white in dark.</summary>
    Ink,
}

/// <summary>The colour of text and icons.</summary>
public enum TextTone
{
    Primary,
    Secondary,
    Tertiary,
    Accent,
    Danger,
    Warn,
    Success,

    /// <summary>Text on an ink surface (ink cards, filled tiles).</summary>
    OnInk,

    /// <summary>Supporting text on an ink surface.</summary>
    OnInkMuted,
}

/// <summary>The type scale. Each style fixes size, weight and default colour.</summary>
public enum TextStyle
{
    /// <summary>60, semibold. One huge number per screen (a countdown).</summary>
    Display,

    /// <summary>26, semibold. The page title.</summary>
    Title,

    /// <summary>20, semibold. The title of a full-window screen, emphatic statements.</summary>
    Heading,

    /// <summary>16, semibold. Card and section titles.</summary>
    Subheading,

    /// <summary>15, semibold. The name on a tile or in a short list; the app name.</summary>
    Label,

    /// <summary>14, regular. Running text.</summary>
    Body,

    /// <summary>14, semibold. Names and labels that must stand out in a list.</summary>
    Strong,

    /// <summary>13, regular, secondary colour. Help text under a title.</summary>
    Small,

    /// <summary>12.5, regular, tertiary colour. Fine print and metadata.</summary>
    Caption,

    /// <summary>12.5, semibold, secondary colour. A short status next to content.</summary>
    CaptionStrong,

    /// <summary>11.5, semibold, upper case, tracked, tertiary colour. Section labels.</summary>
    Overline,

    /// <summary>12.5, monospace. Addresses, paths and other literal input.</summary>
    Code,
}

/// <summary>
/// The spacing scale, on a 4-pixel grid. Use these for margins, paddings and
/// gaps between elements; never invent an in-between value.
/// </summary>
public static class Space
{
    public const double S0 = 2;
    public const double S1 = 4;
    public const double S2 = 8;
    public const double S3 = 12;
    public const double S4 = 16;
    public const double S5 = 20;
    public const double S6 = 24;
    public const double S8 = 32;
    public const double S10 = 40;
}

/// <summary>Font families.</summary>
public static class Fonts
{
    public const string Sans = "Inter, $Default";
    public const string Mono = "Cascadia Mono, Consolas, Courier New, monospace";
}

/// <summary>
/// The colour palette: theme-aware resources, referenced by role. Only the
/// design system reads these; components expose <see cref="Tone"/> and
/// <see cref="TextTone"/> instead.
/// </summary>
internal static class Palette
{
    public const string AppBg = "Ds.AppBg";
    public const string Sidebar = "Ds.Sidebar";
    public const string Surface = "Ds.Surface";
    public const string SurfaceAlt = "Ds.SurfaceAlt";
    public const string Track = "Ds.Track";
    public const string Selected = "Ds.Selected";
    public const string Border = "Ds.Border";
    public const string BorderStrong = "Ds.BorderStrong";
    public const string TextPrimary = "Ds.TextPrimary";
    public const string TextSecondary = "Ds.TextSecondary";
    public const string TextTertiary = "Ds.TextTertiary";
    public const string Ink = "Ds.Ink";
    public const string InkText = "Ds.InkText";
    public const string InkTextMuted = "Ds.InkTextMuted";
    public const string InkTrack = "Ds.InkTrack";
    public const string Accent = "Ds.Accent";
    public const string AccentText = "Ds.AccentText";
    public const string AccentSoft = "Ds.AccentSoft";
    public const string Danger = "Ds.Danger";
    public const string DangerSoft = "Ds.DangerSoft";
    public const string Success = "Ds.Success";
    public const string Warn = "Ds.Warn";
    public const string WarnSoft = "Ds.WarnSoft";
    public const string WarnBorder = "Ds.WarnBorder";
    public const string WarnText = "Ds.WarnText";
    public const string Knob = "Ds.Knob";
    public const string Scrim = "Ds.Scrim";
    public const string TileInset = "Ds.TileInset";
    public const string ClockTrack = "Ds.ClockTrack";

    /// <summary>Tile colours: they tell neighbouring tiles apart and mean nothing else.</summary>
    public static readonly string[] Tiles = { "Ds.Tile1", "Ds.Tile2", "Ds.Tile3", "Ds.Tile4", "Ds.Tile5", "Ds.Tile6", "Ds.Tile7", "Ds.Tile8" };

    /// <summary>Clock fills, from little time to a lot: the more time, the stronger.</summary>
    public static readonly string[] Clocks = { "Ds.Clock1", "Ds.Clock2", "Ds.Clock3", "Ds.Clock4", "Ds.Clock5" };

    /// <summary>The light and dark values of every role. Mirrored in docs/foundations.md.</summary>
    internal static readonly (string Key, string Light, string Dark)[] Values =
    {
        (AppBg, "#F4F3F0", "#17181B"),
        (Sidebar, "#ECEBE7", "#1C1D21"),
        (Surface, "#FFFFFF", "#232428"),
        (SurfaceAlt, "#F8F7F4", "#1F2024"),
        (Track, "#ECEBE7", "#2C2D33"),
        (Selected, "#FFFFFF", "#3A3B42"),
        (Border, "#E2E0DA", "#313238"),
        (BorderStrong, "#D6D3CC", "#44464D"),
        (TextPrimary, "#16171B", "#ECECEE"),
        (TextSecondary, "#4F525A", "#B4B6BD"),
        (TextTertiary, "#6B6E76", "#8E9098"),
        (Ink, "#16171B", "#ECECEE"),
        (InkText, "#FFFFFF", "#16171B"),
        (InkTextMuted, "#C5C5C6", "#4C4D50"),
        (InkTrack, "#333436", "#CFCFD1"),
        (Accent, "#0B6B63", "#2A9D8F"),
        (AccentText, "#FFFFFF", "#FFFFFF"),
        (AccentSoft, "#E1F0EE", "#173A36"),
        (Danger, "#B93D0E", "#D2602F"),
        (DangerSoft, "#FBE9E1", "#45261A"),
        (Success, "#0B6B63", "#3DBBA9"),
        (Warn, "#B07400", "#E2A93B"),
        (WarnSoft, "#FBF0D9", "#3A2E14"),
        (WarnBorder, "#EBD3A0", "#5C4920"),
        (WarnText, "#5C3D00", "#F2C76B"),
        (Knob, "#FFFFFF", "#FFFFFF"),
        (Scrim, "#7316171B", "#99000000"),
        ("Ds.Tile1", "#CFE3F3", "#22384A"),
        ("Ds.Tile2", "#F6D6C8", "#4A3026"),
        ("Ds.Tile3", "#D9EBCF", "#2B3F29"),
        ("Ds.Tile4", "#EBD9F2", "#3D2D45"),
        ("Ds.Tile5", "#F7E7B9", "#463C1F"),
        ("Ds.Tile6", "#CDEBE6", "#1F403B"),
        ("Ds.Tile7", "#F3D3DE", "#462A35"),
        ("Ds.Tile8", "#DCDDF5", "#2E3050"),
        (TileInset, "#80FFFFFF", "#1FFFFFFF"),
        (ClockTrack, "#1A16171B", "#2EFFFFFF"),
        ("Ds.Clock1", "#8CCBC1", "#2F6F66"),
        ("Ds.Clock2", "#4FAA9D", "#3E9E91"),
        ("Ds.Clock3", "#1F8A7E", "#56BCAE"),
        ("Ds.Clock4", "#0B6B63", "#86D8CB"),
        ("Ds.Clock5", "#063F3A", "#C2F2EA"),
    };

    public static ResourceDictionary Build()
    {
        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();
        foreach ((string key, string l, string d) in Values)
        {
            light[key] = new SolidColorBrush(Color.Parse(l));
            dark[key] = new SolidColorBrush(Color.Parse(d));
        }

        var res = new ResourceDictionary();
        res.ThemeDictionaries[ThemeVariant.Light] = light;
        res.ThemeDictionaries[ThemeVariant.Dark] = dark;
        return res;
    }

    public static DynamicResourceExtension Dyn(string key) => new(key);

    public static string Of(TextTone tone) => tone switch
    {
        TextTone.Secondary => TextSecondary,
        TextTone.Tertiary => TextTertiary,
        TextTone.Accent => Accent,
        TextTone.Danger => Danger,
        TextTone.Warn => WarnText,
        TextTone.Success => Success,
        TextTone.OnInk => InkText,
        TextTone.OnInkMuted => InkTextMuted,
        _ => TextPrimary,
    };

    /// <summary>The soft surface and the foreground of a tone (chips, banners, callouts).</summary>
    public static (string Surface, string Foreground) Soft(Tone tone) => tone switch
    {
        Tone.Accent => (AccentSoft, Accent),
        Tone.Danger => (DangerSoft, Danger),
        Tone.Warn => (WarnSoft, WarnText),
        Tone.Success => (AccentSoft, Success),
        Tone.Ink => (Ink, InkText),
        _ => (Track, TextSecondary),
    };

    /// <summary>The solid colour of a tone (dots, meters, filled segments).</summary>
    public static string Solid(Tone tone) => tone switch
    {
        Tone.Accent => Accent,
        Tone.Danger => Danger,
        Tone.Warn => Warn,
        Tone.Success => Success,
        Tone.Ink => InkText,
        _ => TextTertiary,
    };
}

/// <summary>Corner radii.</summary>
internal static class Radius
{
    public const double Small = 8;
    public const double Control = 10;
    public const double Inset = 12;
    public const double Card = 14;
    public const double Feature = 16;
}

/// <summary>Shadows.</summary>
internal static class Elevation
{
    public static BoxShadows Raised => new(new BoxShadow { OffsetY = 1, Blur = 3, Color = Color.FromArgb(40, 0, 0, 0) });

    public static BoxShadows Knob => new(new BoxShadow { OffsetY = 1, Blur = 2, Color = Color.FromArgb(50, 0, 0, 0) });
}
