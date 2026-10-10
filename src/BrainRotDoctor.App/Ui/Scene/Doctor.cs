using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

internal enum DoctorFace { Focused, Happy }

/// <summary>
/// The doctor at one moment: where the head centre is (scene units), how far the
/// forceps' moving blade is open (radians, 0 = closed) and the face.
/// </summary>
internal readonly record struct DoctorPose(double X, double Y, double Jaw, DoctorFace Face);

/// <summary>
/// The doctor: head and shoulders only, cut off by the bottom screen edge, built from
/// the same rounded boxes as the brain. Cap, head mirror, mask, white coat with a
/// pocket and pen, one arm holding forceps. Stands in front of the brain.
/// </summary>
internal sealed class Doctor
{
    /// <summary>How far in front of the brain's middle plane the doctor stands.</summary>
    public const double Depth = 0.85;

    private static readonly Vector3 Hand = new(-0.98f, -0.42f, 0.25f);
    private const double ForcepsTurn = 2.225;

    /// <summary>The forceps tip relative to the doctor's head centre.</summary>
    public static readonly Vector3 Tip = Hand + new Vector3((float)(1.34 * Math.Cos(ForcepsTurn)), (float)(1.34 * Math.Sin(ForcepsTurn)), 0.04f);

    private readonly Lazy<Look> _look = new(() => new Look());
    private (double Jaw, Solid Blade)? _jaw;

    /// <summary>Where the doctor must stand so the forceps tip appears exactly over <paramref name="tip"/> (on the worm's plane).</summary>
    public static (double X, double Y) StandingFor(Vector3 tip)
    {
        double z = Depth + Tip.Z;
        return (tip.X + SceneView.KC * z - Tip.X, tip.Y + SceneView.KS * z - Tip.Y);
    }

    public void Draw(DrawingContext dc, DoctorPose pose)
    {
        if (_jaw is not { } jaw || jaw.Jaw != pose.Jaw)
        {
            jaw = (pose.Jaw, Look.Blade(0.03, pose.Jaw));
            _jaw = jaw;
        }

        using (dc.PushTransform(Matrix.CreateTranslation(pose.X * SceneView.Scale, -pose.Y * SceneView.Scale)))
        {
            _look.Value.Draw(dc, pose.Face, jaw.Blade);
        }
    }

    /// <summary>Everything that never changes shape, built once with the doctor at the origin.</summary>
    private sealed class Look
    {
        private readonly Solid _torso, _pen, _pocket, _sleeve, _head, _ear, _cap, _mirror, _mask, _blade, _glove;

        public Look()
        {
            Func<int, IBrush> Tone(uint color) => k => Palette.Toon(color, k);
            Matrix4x4 At(double x, double y, double z) => Solid.Place(x, y, z + Depth);

            // Each part is cut where it passes into another, and its outline hidden where
            // a part drawn before it stands in front.
            _torso = Solid.Box(new Vector3(0.8f, 0.9f, 0.32f), 0.2, At(0.15, -1.43, 0), Tone(Palette.Coat));
            Regions.Outside[] inTorso = { _torso.Distance };
            _pen = Solid.Box(new Vector3(0.035f, 0.1f, 0.02f), 0.02, At(0.47, -0.93, 0.31), Tone(Palette.Cap), outside: inTorso);
            _pocket = Solid.Box(new Vector3(0.17f, 0.13f, 0.02f), 0.02, At(0.55, -1.08, 0.33), Tone(Palette.Coat), outside: inTorso, behind: new[] { _pen.Distance });

            List<Vector3> arm = Curves.QuadraticBezier(
                new Vector3(-0.42f, -0.92f, 0.22f + (float)Depth),
                new Vector3(-0.85f, -0.85f, 0.25f + (float)Depth),
                Hand + new Vector3(0, 0, (float)Depth));
            _sleeve = Solid.Round((grow, back) => RoundShapes.Tube(arm, 0.14 + grow, back), Tone(Palette.Coat), Solid.Outline, Solid.TubeDistance(arm, 0.14), inTorso);

            _head = Solid.Box(new Vector3(0.5f, 0.48f, 0.36f), 0.2, At(0, 0, 0), Tone(Palette.Skin), behind: new[] { _torso.Distance });
            Regions.Outside[] inHead = { _head.Distance };
            _ear = Solid.Box(new Vector3(0.06f, 0.1f, 0.07f), 0.05, At(0.53, -0.03, 0), Tone(Palette.Skin), outside: inHead, behind: inHead);
            _cap = Solid.Box(new Vector3(0.53f, 0.16f, 0.39f), 0.14, At(0, 0.44, 0), Tone(Palette.Cap), behind: new[] { _head.Distance, _ear.Distance });

            var mirror = new Vector3(-0.32f, 0.3f, 0.42f + (float)Depth);
            const double mirrorRim = Solid.Outline * 0.8;
            Regions.Outside[] mirrorBehind = { _head.Distance, _cap.Distance };
            _mirror = new Solid(
                new[] { Regions.Build(RoundShapes.CylinderSide(mirror, 0.12 + mirrorRim, 0.02 + mirrorRim), shaded: false, rim: true, behind: mirrorBehind) },
                new[] { Regions.Build(RoundShapes.CylinderSide(mirror, 0.12, 0.02), shaded: true) },
                Tone(Palette.Steel),
                Solid.SphereDistance(mirror, 0.12));
            MirrorFront = mirror.Z + 0.02f;
            MirrorRimBack = mirror.Z - 0.02f - (float)mirrorRim;

            _mask = Solid.Box(new Vector3(0.3f, 0.13f, 0.05f), 0.05, At(-0.04, -0.27, 0.35), Tone(Palette.Mask), outside: inHead, behind: inHead);
            _blade = Blade(-0.03, 0);
            Regions.Outside[] inSleeve = { _sleeve.Distance };
            _glove = Solid.Round(
                (grow, back) => RoundShapes.Sphere(Hand + new Vector3(0, 0, 0.03f + (float)Depth), 0.14 + grow, back),
                Tone(Palette.Glove),
                Solid.Outline,
                Solid.SphereDistance(Hand + new Vector3(0, 0, 0.03f + (float)Depth), 0.14),
                inSleeve,
                new[] { _sleeve.Distance, _blade.Distance });
            _happyEyes = new[] { -0.2, 0.12 }
                .Select(x => Regions.Build(RoundShapes.RingArc(new Vector3((float)x, -0.03f, 0.365f + (float)Depth), 0.055, 0.016, 0, Math.PI), shaded: false).Visible)
                .ToArray();
        }

        private readonly StreamGeometry?[] _happyEyes;

        private float MirrorFront { get; }

        private float MirrorRimBack { get; }

        /// <summary>One forceps blade, offset across the forceps by <paramref name="side"/> and opened by <paramref name="open"/>.</summary>
        public static Solid Blade(double side, double open)
        {
            Matrix4x4 place = Matrix4x4.CreateTranslation(0.67f, (float)side, 0)
                * Matrix4x4.CreateRotationZ((float)open)
                * Matrix4x4.CreateRotationZ((float)ForcepsTurn)
                * Matrix4x4.CreateTranslation(Hand + new Vector3(0, 0, (float)Depth));
            return Solid.Box(new Vector3(0.67f, 0.022f, 0.022f), 0.02, place, k => Palette.Toon(Palette.Steel, k));
        }

        public void Draw(DrawingContext dc, DoctorFace face, Solid jaw)
        {
            Matrix none = Matrix.Identity;
            const double d = Depth;

            _torso.Draw(dc);
            Flat.Polygon(dc, Palette.Flat(Palette.Line), d + 0.322, none, new(-0.23, -0.56), new(0.23, -0.56), new(0, -0.97));
            Flat.Polygon(dc, Palette.Flat(Palette.Cap), d + 0.324, none, new(-0.18, -0.585), new(0.18, -0.585), new(0, -0.9));
            _pen.Draw(dc);
            _pocket.Draw(dc);
            _sleeve.Draw(dc);
            _head.Draw(dc);
            _ear.Draw(dc);
            _cap.Draw(dc);

            Flat.Circle(dc, Palette.Flat(Palette.Line), MirrorRimBack, none, -0.32, 0.3, 0.12 + Solid.Outline * 0.8);
            _mirror.DrawRim(dc);
            _mirror.DrawBody(dc);
            Flat.Circle(dc, Palette.Toon(Palette.Steel, FrontTone), MirrorFront, none, -0.32, 0.3, 0.12);
            Flat.Circle(dc, Palette.Flat(Palette.MirrorCentre), d + 0.445, none, -0.32, 0.3, 0.04);
            Flat.Circle(dc, Palette.Flat(Palette.White), d + 0.446, none, -0.37, 0.35, 0.022);

            _mask.Draw(dc);
            foreach (double y in new[] { -0.235, -0.305 })
            {
                Flat.Rect(dc, Palette.Flat(Palette.MaskLine), d + 0.402, none, -0.04, y, 0.42, 0.014);
            }

            IBrush pupil = Palette.Flat(Palette.Pupil), brow = Palette.Flat(Palette.Brow);
            for (int i = 0; i < 2; i++)
            {
                double x = i == 0 ? -0.2 : 0.12;
                if (face == DoctorFace.Focused)
                {
                    Flat.Circle(dc, pupil, d + 0.365, none, x, -0.01, 0.055, scaleY: 1.3);
                    Flat.Circle(dc, Palette.Flat(Palette.White), d + 0.367, none, x - 0.015, 0.025, 0.018);
                    Flat.Rect(dc, brow, d + 0.365, none, x, 0.13, 0.15, 0.035, x < 0 ? -0.2 : 0.2);
                }
                else
                {
                    if (_happyEyes[i] is { } eye)
                    {
                        dc.DrawGeometry(pupil, null, eye);
                    }

                    Flat.Rect(dc, brow, d + 0.365, none, x, 0.16, 0.15, 0.035, x < 0 ? 0.12 : -0.12);
                }
            }

            _blade.Draw(dc);
            jaw.Draw(dc);
            _glove.Draw(dc);
        }

        /// <summary>The tone of a surface facing the viewer straight on.</summary>
        private static int FrontTone
        {
            get
            {
                double lit = SceneView.Light.Z;
                int tone = 0;
                while (tone < 4 && lit >= Palette.ToneEdges[tone])
                {
                    tone++;
                }

                return tone;
            }
        }
    }
}
