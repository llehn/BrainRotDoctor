using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// One shaded part of a character with its outline: the outline is the part grown
/// by a hair and painted in the line colour first, so it shows as a thin rim.
/// </summary>
internal sealed class Solid
{
    private readonly PatchRegions[] _rim;
    private readonly PatchRegions[] _body;
    private readonly Func<int, IBrush> _tone;

    public Solid(PatchRegions[] rim, PatchRegions[] body, Func<int, IBrush> tone, Regions.Outside distance)
    {
        _rim = rim;
        _body = body;
        _tone = tone;
        Distance = distance;
    }

    /// <summary>The design's outline thickness, in scene units.</summary>
    public const double Outline = 0.022;

    /// <summary>Signed distance to the part's surface (positive outside), for hiding what is inside or behind it.</summary>
    public Regions.Outside Distance { get; }

    /// <summary>
    /// A rounded box part. <paramref name="outside"/>: parts it passes into (what is
    /// inside them is hidden); <paramref name="behind"/>: parts already drawn that may
    /// stand in front of it or its outline.
    /// </summary>
    public static Solid Box(Vector3 half, double radius, Matrix4x4 place, Func<int, IBrush> tone, double rim = Outline, Regions.Outside[]? outside = null, Regions.Outside[]? behind = null)
    {
        var box = new RoundedBox(half, radius, place);
        return new Solid(
            rim > 0 ? box.Grown(rim).Build(shaded: false, outside, rim: true, behind) : Array.Empty<PatchRegions>(),
            box.Build(shaded: true, outside, behind: behind),
            tone,
            box.Distance);
    }

    /// <summary>
    /// A round part. <paramref name="shape"/> gives its surface grown by an amount (the
    /// flag asks for the far half, used for the outline).
    /// </summary>
    public static Solid Round(Func<double, bool, Patch> shape, Func<int, IBrush> tone, double rim, Regions.Outside distance, Regions.Outside[]? outside = null, Regions.Outside[]? behind = null) =>
        new(
            rim > 0 ? new[] { Regions.Build(shape(rim, true), shaded: false, outside, rim: true, behind) } : Array.Empty<PatchRegions>(),
            new[] { Regions.Build(shape(0, false), shaded: true, outside, behind: behind) },
            tone,
            distance);

    public static Regions.Outside SphereDistance(Vector3 centre, double radius) => p => Vector3.Distance(p, centre) - radius;

    public static Regions.Outside TubeDistance(IReadOnlyList<Vector3> line, double radius) => p =>
    {
        double best = double.MaxValue;
        for (int i = 1; i < line.Count; i++)
        {
            Vector3 a = line[i - 1], ab = line[i] - a;
            float t = Math.Clamp(Vector3.Dot(p - a, ab) / Math.Max(ab.LengthSquared(), 1e-12f), 0, 1);
            best = Math.Min(best, Vector3.DistanceSquared(p, a + ab * t));
        }

        return Math.Sqrt(best) - radius;
    };

    public void DrawRim(DrawingContext dc)
    {
        foreach (PatchRegions part in _rim)
        {
            if (part.Visible is { } area)
            {
                dc.DrawGeometry(Palette.Flat(Palette.Line), null, area);
            }
        }
    }

    public void DrawBody(DrawingContext dc)
    {
        foreach (PatchRegions part in _body)
        {
            Regions.Fill(dc, part, _tone);
        }
    }

    public void Draw(DrawingContext dc)
    {
        DrawRim(dc);
        DrawBody(dc);
    }

    /// <summary>A placement: scale, then turn about z, then move.</summary>
    public static Matrix4x4 Place(double x, double y, double z, double turn = 0, double scaleX = 1, double scaleY = 1) =>
        Matrix4x4.CreateScale((float)scaleX, (float)scaleY, 1)
        * Matrix4x4.CreateRotationZ((float)turn)
        * Matrix4x4.CreateTranslation((float)x, (float)y, (float)z);

    /// <summary>A patch moved (and possibly squashed) as a whole.</summary>
    public static Patch Placed(Patch patch, Matrix4x4 place)
    {
        Matrix4x4.Invert(place, out Matrix4x4 inverse);
        Matrix4x4 normals = Matrix4x4.Transpose(inverse);
        return new Patch(patch.Us, patch.Vs, (double u, double v, out Vector3 p, out Vector3 n) =>
        {
            patch.At(u, v, out p, out n);
            p = Vector3.Transform(p, place);
            n = Vector3.Normalize(Vector3.TransformNormal(n, normals));
        });
    }
}

/// <summary>Unshaded flat shapes lying on a plane facing the viewer (faces, eyes, marks).</summary>
internal static class Flat
{
    /// <summary>Moves into a front-facing plane: local units are scene units, y up.</summary>
    public static DrawingContext.PushedState On(DrawingContext dc, double z, Matrix local) =>
        dc.PushTransform(local * SceneView.FrontPlane(z));

    public static void Circle(DrawingContext dc, IBrush brush, double z, Matrix local, double x, double y, double r, double scaleY = 1)
    {
        using (On(dc, z, local))
        {
            dc.DrawEllipse(brush, null, new Point(x, y), r, r * scaleY);
        }
    }

    public static void Ring(DrawingContext dc, IBrush brush, double z, Matrix local, double x, double y, double inner, double outer)
    {
        var g = new StreamGeometry();
        using (StreamGeometryContext ctx = g.Open())
        {
            ctx.SetFillRule(FillRule.EvenOdd);
            AddCircle(ctx, new Point(x, y), outer);
            AddCircle(ctx, new Point(x, y), inner);
        }

        using (On(dc, z, local))
        {
            dc.DrawGeometry(brush, null, g);
        }
    }

    /// <summary>A w × h rectangle centred on (x, y), turned by <paramref name="turn"/> (radians, counter-clockwise).</summary>
    public static void Rect(DrawingContext dc, IBrush brush, double z, Matrix local, double x, double y, double w, double h, double turn = 0)
    {
        Matrix place = Matrix.CreateRotation(turn) * Matrix.CreateTranslation(x, y);
        using (dc.PushTransform(place * local * SceneView.FrontPlane(z)))
        {
            dc.DrawRectangle(brush, null, new Rect(-w / 2, -h / 2, w, h));
        }
    }

    public static void Polygon(DrawingContext dc, IBrush brush, double z, Matrix local, params Point[] points)
    {
        var g = new StreamGeometry();
        using (StreamGeometryContext ctx = g.Open())
        {
            ctx.BeginFigure(points[0], true);
            for (int i = 1; i < points.Length; i++)
            {
                ctx.LineTo(points[i]);
            }

            ctx.EndFigure(true);
        }

        using (On(dc, z, local))
        {
            dc.DrawGeometry(brush, null, g);
        }
    }

    /// <summary>A filled circle segment from angle <paramref name="from"/> over <paramref name="sweep"/>.</summary>
    public static void Sector(DrawingContext dc, IBrush brush, double z, Matrix local, double x, double y, double r, double from, double sweep)
    {
        var points = new List<Point> { new(x, y) };
        for (int k = 0; k <= 32; k++)
        {
            double a = from + sweep * k / 32;
            points.Add(new Point(x + r * Math.Cos(a), y + r * Math.Sin(a)));
        }

        Polygon(dc, brush, z, local, points.ToArray());
    }

    public static void AddCircle(StreamGeometryContext ctx, Point centre, double r)
    {
        ctx.BeginFigure(new Point(centre.X + r, centre.Y), true);
        ctx.ArcTo(new Point(centre.X - r, centre.Y), new Size(r, r), 0, false, SweepDirection.Clockwise);
        ctx.ArcTo(new Point(centre.X + r, centre.Y), new Size(r, r), 0, false, SweepDirection.Clockwise);
        ctx.EndFigure(true);
    }
}
