using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// The fixed view the worm scene is drawn in, taken from the approved design: a
/// front-on oblique drawing (depth runs up and to the right at 30°, drawn at half
/// length, no perspective), one fixed light, and flat tone steps. Because neither
/// the view nor the light ever moves, every shape is drawn directly as flat 2D
/// outlines; scene units are converted to device-independent pixels here.
/// </summary>
internal static class SceneView
{
    public const double Width = 360;
    public const double Height = 220;

    /// <summary>Half the visible height in scene units.</summary>
    public const double HalfHeight = 1.69;

    public static readonly double HalfWidth = HalfHeight * Width / Height;

    /// <summary>DIPs per scene unit.</summary>
    public static readonly double Scale = Width / (2 * HalfWidth);

    // A point at depth z appears shifted by (-KC·z, -KS·z).
    public static readonly double KC = 0.5 * Math.Cos(Math.PI / 6);
    public static readonly double KS = 0.5 * Math.Sin(Math.PI / 6);

    /// <summary>Unit direction from the scene toward the viewer; a surface is seen where its normal points this way.</summary>
    public static readonly Vector3 Toward = Vector3.Normalize(new Vector3((float)KC, (float)KS, 1f));

    public static readonly Vector3 Light = Vector3.Normalize(new Vector3(0.45f, 1f, 0.75f));

    /// <summary>Where a scene point lands on screen, in DIPs from the top-left corner.</summary>
    public static Point Project(Vector3 p) =>
        new((p.X - KC * p.Z + HalfWidth) * Scale, (HalfHeight - (p.Y - KS * p.Z)) * Scale);

    /// <summary>The on-screen shift of a scene offset, in DIPs (y down).</summary>
    public static Avalonia.Vector ProjectOffset(double x, double y, double z) =>
        new((x - KC * z) * Scale, -(y - KS * z) * Scale);

    /// <summary>A transform that draws shapes given in scene units on the plane at depth z.</summary>
    public static Matrix FrontPlane(double z) =>
        new(Scale, 0, 0, -Scale, (HalfWidth - KC * z) * Scale, (HalfHeight + KS * z) * Scale);

    /// <summary>A transform that draws shapes given as (x, z) on the horizontal plane at height y.</summary>
    public static Matrix FloorPlane(double y) =>
        new(Scale, 0, -KC * Scale, KS * Scale, HalfWidth * Scale, (HalfHeight - y) * Scale);
}

/// <summary>
/// The colours of the design. Shaded surfaces use five flat tone steps picked by how
/// directly they face the light; outlines, holes and faces are unshaded. The values
/// are worked out exactly as the design computed them, so they match pixel for pixel.
/// </summary>
internal static class Palette
{
    public const uint Base = 0xF59AC0;
    public const uint Fold = 0xB83F7A;
    public const uint Line = 0xB83F7A;
    public const uint Worm = 0x8FD96B;
    public const uint WormRing = 0x73C24F;
    public const uint Ring = 0x7C5CFF;
    public const uint Mouth = 0x6E1A47;
    public const uint Pupil = 0x1E1424;
    public const uint Coat = 0xFFFFFF;
    public const uint Skin = 0xF6CDB0;
    public const uint Cap = 0x7C5CFF;
    public const uint Mask = 0x9EE0D4;
    public const uint MaskLine = 0x5FB3A4;
    public const uint Glove = 0x8FC9F0;
    public const uint Steel = 0xC9D3DD;
    public const uint Hole = 0x6E1A47;
    public const uint Brow = 0x6B4A3A;
    public const uint White = 0xFFFFFF;
    public const uint Cheek = 0xFF6FA3;
    public const uint MirrorCentre = 0x7D8A97;

    /// <summary>How directly a surface must face the light to reach tone 1, 2, 3 and 4.</summary>
    public static readonly double[] ToneEdges = { -0.6, -0.2, 0.2, 0.6 };

    private static readonly double[] Ramp = { 120, 165, 205, 240, 255 };
    private const double Ambient = 0.32;
    private const double Key = 0.78;

    private static readonly Dictionary<(uint, int, int), IImmutableSolidColorBrush> Cache = new();

    /// <summary>A shaded colour at tone 0–4.</summary>
    public static IImmutableSolidColorBrush Toon(uint hex, int tone) => Get(hex, tone, 0);

    /// <summary>A shaded colour that the design painted from an image (brain folds, worm skin).</summary>
    public static IImmutableSolidColorBrush ToonPainted(uint hex, int tone) => Get(hex, tone, 1);

    /// <summary>An unshaded colour.</summary>
    public static IImmutableSolidColorBrush Flat(uint hex) => Get(hex, -1, 0);

    public static Color FlatColor(uint hex) => Flat(hex).Color;

    // The worm skin: rings a quarter as wide as their spacing, so fine that the design
    // shows them softened. Skin and rings are drawn at half contrast around the same
    // average colour (three parts skin to one part ring).
    public static uint WormSkin { get; } = Blend(Worm, WormRing, 0.125);

    public static uint WormRings { get; } = Blend(Worm, WormRing, 0.625);

    private static IImmutableSolidColorBrush Get(uint hex, int tone, int painted)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue((hex, tone, painted), out IImmutableSolidColorBrush? brush))
            {
                double light = tone < 0 ? 1 : Ambient + Key * Ramp[tone] / 255;
                byte Channel(int shift)
                {
                    double c = ((hex >> shift) & 0xFF) / 255.0;
                    if (painted == 1)
                    {
                        c = Decode(c);
                    }

                    return (byte)Math.Round(Math.Clamp(Encode(Math.Min(1, c * light)), 0, 1) * 255);
                }

                brush = new ImmutableSolidColorBrush(Color.FromRgb(Channel(16), Channel(8), Channel(0)));
                Cache[(hex, tone, painted)] = brush;
            }

            return brush;
        }
    }

    private static uint Blend(uint a, uint b, double t)
    {
        uint Mix(int shift) => (uint)Math.Round(((a >> shift) & 0xFF) * (1 - t) + ((b >> shift) & 0xFF) * t);
        return (Mix(16) << 16) | (Mix(8) << 8) | Mix(0);
    }

    private static double Encode(double c) => c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

    private static double Decode(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
}
