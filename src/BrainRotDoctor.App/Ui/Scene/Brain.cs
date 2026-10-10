using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

internal enum BrainFace { Hypnotised, Straining, Happy }

/// <summary>
/// The brain at one moment: height of its base (scene units), squash, face, the
/// scene time (for the hypnotic eyes) and the pop burst's progress (0–1; none outside).
/// </summary>
internal readonly record struct BrainPose(double Y, double ScaleX, double ScaleY, BrainFace Face, double Time, double Burst);

/// <summary>
/// The brain: a rounded box with folds, four holes on top for the worm, and a face.
/// Its origin is the middle of its base, so it squashes on that base.
/// </summary>
internal sealed class Brain
{
    public static readonly Vector3 Half = new(1.3f, 0.85f, 0.45f);
    public const double Radius = 0.2;

    /// <summary>Where the brain stands across the scene.</summary>
    public const double X = -1.2;

    /// <summary>Height of the top face above the base.</summary>
    public const double Top = 1.7;

    private const double FrontZ = 0.45;

    /// <summary>The worm's holes on top, left to right: tail end, loop out, loop in, head.</summary>
    public static readonly double[] Holes = { -0.95, -0.55, -0.15, 0.6 };

    private static readonly (double X, double Y)[] Eyes = { (-0.44, 0.7), (0.44, 0.7) };

    private Look? _look;

    /// <summary>The brain's solid body at a pose, for hiding what passes inside it.</summary>
    public static RoundedBox Body(BrainPose pose) => new(Half, Radius, Placement(pose.ScaleX, pose.ScaleY, pose.Y));

    public static Regions.Outside OutsideOf(BrainPose pose)
    {
        RoundedBox body = Body(pose);
        return p => body.Distance(p);
    }

    public void Draw(DrawingContext dc, BrainPose pose)
    {
        _look ??= new Look(1, 1);

        // The look is built standing at height 0, unsquashed: rising and sinking only
        // move it, and the brief squash on the pop stretches the drawing about the
        // middle of the base (within a fraction of a pixel of squashing the solid).
        Point foot = SceneView.Project(new System.Numerics.Vector3((float)X, 0, 0));
        Matrix squash = Matrix.CreateTranslation(-foot.X, -foot.Y)
            * Matrix.CreateScale(pose.ScaleX, pose.ScaleY)
            * Matrix.CreateTranslation(foot.X, foot.Y - pose.Y * SceneView.Scale);
        using (dc.PushTransform(squash))
        {
            _look.Draw(dc);
            Matrix local = Matrix.CreateTranslation(X, 0);
            BrainPose upright = pose with { Y = 0, ScaleX = 1, ScaleY = 1 };
            DrawHoles(dc, upright);
            DrawFace(dc, upright, local);
            DrawBurst(dc, upright, local);
        }
    }

    private static Matrix4x4 Placement(double scaleX, double scaleY, double y) =>
        Matrix4x4.CreateTranslation(0, (float)Half.Y, 0)
        * Matrix4x4.CreateScale((float)scaleX, (float)scaleY, 1)
        * Matrix4x4.CreateTranslation((float)X, (float)y, 0);

    private static void DrawHoles(DrawingContext dc, BrainPose pose)
    {
        Matrix plane = Matrix.CreateScale(pose.ScaleX, 1) * Matrix.CreateTranslation(X, 0) * SceneView.FloorPlane(pose.ScaleY * (Top + 0.004));
        using (dc.PushTransform(plane))
        {
            foreach (double x in Holes)
            {
                dc.DrawEllipse(Palette.Flat(Palette.Hole), null, new Point(x, 0), 0.14, 0.14);
            }
        }
    }

    private static void DrawFace(DrawingContext dc, BrainPose pose, Matrix local)
    {
        IBrush line = Palette.Flat(Palette.Line), white = Palette.Flat(Palette.White), pupil = Palette.Flat(Palette.Pupil);
        IBrush mouth = Palette.Flat(Palette.Mouth);
        const double z = FrontZ;
        switch (pose.Face)
        {
            case BrainFace.Hypnotised:
                for (int i = 0; i < Eyes.Length; i++)
                {
                    (double x, double y) = Eyes[i];
                    Flat.Circle(dc, line, z + 0.002, local, x, y, 0.225);
                    Flat.Circle(dc, white, z + 0.004, local, x, y, 0.2);
                    Flat.Ring(dc, Palette.Flat(Palette.Ring), z + 0.006, local, x, y, 0.124, 0.152);
                    Flat.Ring(dc, Palette.Flat(Palette.Ring), z + 0.006, local, x, y, 0.06, 0.088);
                    double a = pose.Time * 7 + (i == 1 ? Math.PI : 0);
                    Flat.Circle(dc, pupil, z + 0.008, local, x + 0.05 * Math.Cos(a), y + 0.05 * Math.Sin(a), 0.035);
                }

                Flat.Sector(dc, mouth, z + 0.004, local, 0, 0.37, 0.1, Math.PI, Math.PI);
                break;

            case BrainFace.Straining:
                foreach ((double x, double y) in Eyes)
                {
                    foreach (int s in new[] { 1, -1 })
                    {
                        Flat.Rect(dc, pupil, z + 0.006, local, x, y + s * 0.04, 0.2, 0.04, (x < 0 ? -1 : 1) * s * 0.42);
                    }
                }

                Flat.Rect(dc, line, z + 0.003, local, 0, 0.33, 0.24, 0.1);
                Flat.Rect(dc, white, z + 0.005, local, 0, 0.33, 0.21, 0.07);
                Flat.Rect(dc, line, z + 0.007, local, 0, 0.33, 0.21, 0.012);
                break;

            default:
                foreach ((double x, double y) in Eyes)
                {
                    Flat.Circle(dc, line, z + 0.002, local, x, y, 0.225);
                    Flat.Circle(dc, white, z + 0.004, local, x, y, 0.2);
                    Flat.Circle(dc, pupil, z + 0.006, local, x + 0.03, y + 0.02, 0.11);
                    Flat.Circle(dc, white, z + 0.008, local, x, y + 0.06, 0.035);
                }

                foreach (double x in new[] { -0.78, 0.78 })
                {
                    Flat.Circle(dc, Palette.Flat(Palette.Cheek), z + 0.003, local, x, 0.5, 0.07);
                }

                // The smile is a thin ring tube, lower half.
                Matrix4x4 squash = Matrix4x4.CreateScale((float)pose.ScaleX, (float)pose.ScaleY, 1) * Matrix4x4.CreateTranslation((float)X, 0, 0);
                Patch smile = Solid.Placed(RoundShapes.RingArc(new Vector3(0, 0.42f, (float)(z + 0.005)), 0.11, 0.018, Math.PI, Math.PI), squash);
                if (Regions.Build(smile, shaded: false).Visible is { } area)
                {
                    dc.DrawGeometry(mouth, null, area);
                }

                break;
        }
    }

    private static void DrawBurst(DrawingContext dc, BrainPose pose, Matrix local)
    {
        double b = pose.Burst;
        if (b < 0 || b >= 1)
        {
            return;
        }

        const double bx = 0.6, by = Top + 0.02;

        // The ring lies in the brain's middle plane; the part inside the brain is hidden.
        var hidden = new StreamGeometry();
        using (StreamGeometryContext ctx = hidden.Open())
        {
            ctx.SetFillRule(FillRule.EvenOdd);
            ctx.BeginFigure(new Point(-10, -10), true);
            ctx.LineTo(new Point(10, -10));
            ctx.LineTo(new Point(10, 10));
            ctx.LineTo(new Point(-10, 10));
            ctx.EndFigure(true);
            AddRoundedRect(ctx, -Half.X, 0, Half.X, 2 * Half.Y, Radius);
        }

        double s = 1 + 5 * b;
        var ring = new StreamGeometry();
        using (StreamGeometryContext ctx = ring.Open())
        {
            ctx.SetFillRule(FillRule.EvenOdd);
            Flat.AddCircle(ctx, new Point(bx, by), 0.13 * s);
            Flat.AddCircle(ctx, new Point(bx, by), 0.1 * s);
        }

        using (Flat.On(dc, 0, local))
        using (dc.PushOpacity(1 - b))
        {
            dc.DrawGeometry(Palette.Flat(Palette.Fold), null, new CombinedGeometry(GeometryCombineMode.Intersect, ring, hidden));
        }

        // Three droplets fly off and fall back.
        Regions.Outside[] outside = { OutsideOf(pose with { Y = 0 }) };
        foreach ((double dx, double dy) in new[] { (-0.5, 0.55), (0.1, 0.8), (0.55, 0.45) })
        {
            var centre = new Vector3(
                (float)(X + pose.ScaleX * (bx + dx * b)),
                (float)(pose.ScaleY * (by + dy * b - 0.6 * b * b)),
                0.05f);
            Patch drop = RoundShapes.Sphere(centre, 0.045 * (1 - b * 0.6));
            Regions.Fill(dc, Regions.Build(drop, shaded: true, outside), k => Palette.Toon(Palette.Base, k));
        }
    }

    private static void AddRoundedRect(StreamGeometryContext ctx, double left, double bottom, double right, double top, double r)
    {
        var size = new Size(r, r);
        ctx.BeginFigure(new Point(left + r, bottom), true);
        ctx.LineTo(new Point(right - r, bottom));
        ctx.ArcTo(new Point(right, bottom + r), size, 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(right, top - r));
        ctx.ArcTo(new Point(right - r, top), size, 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(left + r, top));
        ctx.ArcTo(new Point(left, top - r), size, 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(left, bottom + r));
        ctx.ArcTo(new Point(left + r, bottom), size, 0, false, SweepDirection.Clockwise);
        ctx.EndFigure(true);
    }

    /// <summary>The brain's body, outline and folds at one squash, built standing at height 0.</summary>
    private sealed class Look
    {
        private readonly PatchRegions[] _rim;
        private readonly PatchRegions[] _body;
        private readonly Geometry?[] _folds;

        public Look(double scaleX, double scaleY)
        {
            var box = new RoundedBox(Half, Radius, Placement(scaleX, scaleY, 0));
            _rim = box.Grown(Solid.Outline).Build(shaded: false, rim: true);
            _body = box.Build(shaded: true);
            _folds = RoundedBox.Faces.Select(face => FoldsOn(box, face)).ToArray();
            _shadedFolds = ShadeFolds();
        }

        private readonly List<(int Tone, Geometry Folds)> _shadedFolds;

        public void Draw(DrawingContext dc)
        {
            foreach (PatchRegions face in _rim)
            {
                if (face.Visible is { } area)
                {
                    dc.DrawGeometry(Palette.Flat(Palette.Line), null, area);
                }
            }

            // All faces first, then all folds, so faces overlapping at their seams never cover a fold.
            foreach (PatchRegions face in _body)
            {
                Regions.Fill(dc, face, k => Palette.ToonPainted(Palette.Base, k));
            }

            foreach ((int tone, Geometry folds) in _shadedFolds)
            {
                dc.DrawGeometry(Palette.ToonPainted(Palette.Fold, tone), null, folds);
            }
        }

        /// <summary>Each face's folds cut to each of its tone areas, so every fold piece gets its tone.</summary>
        private List<(int Tone, Geometry Folds)> ShadeFolds()
        {
            var pieces = new List<(int, Geometry)>();
            for (int f = 0; f < _body.Length; f++)
            {
                if (_folds[f] is not { } folds)
                {
                    continue;
                }

                for (int k = 0; k < _body[f].Tones.Length; k++)
                {
                    if (_body[f].Tones[k] is { } area)
                    {
                        pieces.Add((k, new CombinedGeometry(GeometryCombineMode.Intersect, folds, area)));
                    }
                }
            }

            return pieces;
        }

        private static Geometry? FoldsOn(RoundedBox box, RoundedBox.Face face)
        {
            (int wc, int hc) = BrainFolds.Size(face);
            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.SetFillRule(FillRule.NonZero);
                bool Faces(Point pixel)
                {
                    box.At(face, pixel.X / wc, pixel.Y / hc, out _, out System.Numerics.Vector3 n);
                    return System.Numerics.Vector3.Dot(n, SceneView.Toward) > 0;
                }

                foreach (Point[] outline in BrainFolds.Outlines(face, Faces))
                {
                    for (int i = 0; i < outline.Length; i++)
                    {
                        box.At(face, outline[i].X / wc, outline[i].Y / hc, out Vector3 p, out _);
                        Point screen = SceneView.Project(p);
                        if (i == 0)
                        {
                            ctx.BeginFigure(screen, true);
                        }
                        else
                        {
                            ctx.LineTo(screen);
                        }
                    }

                    ctx.EndFigure(true);
                }
            }

            return geometry;
        }
    }
}
