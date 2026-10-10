using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>A point on a surface and the direction the surface faces there.</summary>
internal delegate void SurfacePoint(double u, double v, out Vector3 position, out Vector3 normal);

/// <summary>
/// A piece of a rounded shape, described by two parameters over a grid. The grid is
/// denser where the shape curves, so tone edges and outlines come out smooth.
/// </summary>
internal sealed class Patch
{
    public Patch(double[] us, double[] vs, SurfacePoint at)
    {
        Us = us;
        Vs = vs;
        At = at;
    }

    public double[] Us { get; }

    public double[] Vs { get; }

    public SurfacePoint At { get; }
}

/// <summary>
/// The on-screen areas of one patch: <see cref="Tones"/>[k] outlines the part whose
/// tone is k or lighter (index 0 is the whole visible part). Drawing them from 0 up
/// gives the flat tone steps of the design.
/// </summary>
internal sealed class PatchRegions
{
    public PatchRegions(StreamGeometry?[] tones) => Tones = tones;

    public StreamGeometry?[] Tones { get; }

    public StreamGeometry? Visible => Tones[0];
}

/// <summary>Turns patches into flat tone areas.</summary>
internal static class Regions
{
    /// <summary>Keeps only the part of a surface outside another solid (≥ 0 = outside).</summary>
    public delegate double Outside(Vector3 position);

    /// <summary>
    /// Builds the tone areas of a patch. An unshaded patch gets only its visible
    /// outline. Parts inside any of the <paramref name="outside"/> solids are hidden.
    /// A <paramref name="rim"/> keeps the far side instead (an outline shell shows only
    /// where it peeks out behind its part), hidden wherever one of the solids
    /// <paramref name="behind"/> stands between it and the viewer.
    /// </summary>
    public static PatchRegions Build(Patch patch, bool shaded, Outside[]? outside = null, bool rim = false, Outside[]? behind = null)
    {
        int nu = patch.Us.Length, nv = patch.Vs.Length, count = nu * nv;
        var facing = new double[count];
        var lit = new double[count];
        for (int j = 0; j < nv; j++)
        {
            for (int i = 0; i < nu; i++)
            {
                patch.At(patch.Us[i], patch.Vs[j], out Vector3 p, out Vector3 n);
                double keep = Vector3.Dot(n, SceneView.Toward) * (rim ? -1 : 1);
                if (outside is not null)
                {
                    foreach (Outside solid in outside)
                    {
                        keep = Math.Min(keep, solid(p));
                    }
                }

                if (behind is not null && keep >= 0)
                {
                    foreach (Outside solid in behind)
                    {
                        keep = Math.Min(keep, Clearance(p, solid));
                    }
                }

                facing[j * nu + i] = keep;
                lit[j * nu + i] = Vector3.Dot(n, SceneView.Light);
            }
        }

        Point Map(double u, double v)
        {
            patch.At(u, v, out Vector3 p, out _);
            return SceneView.Project(p);
        }

        var tones = new StreamGeometry?[shaded ? 5 : 1];
        var values = new double[count];
        for (int k = 0; k < tones.Length; k++)
        {
            double edge = k == 0 ? double.NegativeInfinity : Palette.ToneEdges[k - 1];
            for (int c = 0; c < count; c++)
            {
                values[c] = k == 0 ? facing[c] : Math.Min(facing[c], lit[c] - edge);
            }

            var geometry = new StreamGeometry();
            bool drew;
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.SetFillRule(FillRule.EvenOdd);
                drew = Contours.Trace(patch.Us, patch.Vs, values, Map, ctx);
            }

            tones[k] = drew ? geometry : null;
        }

        return new PatchRegions(tones);
    }

    /// <summary>
    /// How far the line of sight from <paramref name="p"/> toward the viewer passes
    /// from a solid: negative when the solid blocks the view.
    /// </summary>
    private static double Clearance(Vector3 p, Outside solid)
    {
        double s = 0, closest = double.MaxValue;
        for (int step = 0; step < 48 && s < 3; step++)
        {
            double d = solid(p + SceneView.Toward * (float)s);
            closest = Math.Min(closest, d);
            if (d < 0)
            {
                break;
            }

            s += Math.Max(d, 0.004);
        }

        return closest;
    }

    /// <summary>Paints the tone steps of a patch, darkest first.</summary>
    public static void Fill(DrawingContext dc, PatchRegions regions, Func<int, IBrush> toneBrush)
    {
        for (int k = 0; k < regions.Tones.Length; k++)
        {
            if (regions.Tones[k] is { } area)
            {
                dc.DrawGeometry(toneBrush(k), null, area);
            }
        }
    }

    /// <summary>Grid positions over [from, to], with extra samples near both ends where a rounded edge bends.</summary>
    public static double[] Graded(double from, double to, double bend, int bendSamples = 14, int flatSamples = 4)
    {
        var list = new List<double>();
        double span = to - from;
        if (bend * 2 >= span)
        {
            int n = bendSamples * 2;
            for (int i = 0; i <= n; i++)
            {
                list.Add(from + span * i / n);
            }

            return list.ToArray();
        }

        for (int i = 0; i <= bendSamples; i++)
        {
            list.Add(from + bend * i / bendSamples);
        }

        for (int i = 1; i < flatSamples; i++)
        {
            list.Add(from + bend + (span - 2 * bend) * i / flatSamples);
        }

        for (int i = 0; i <= bendSamples; i++)
        {
            list.Add(to - bend + bend * i / bendSamples);
        }

        return list.ToArray();
    }

    public static double[] Even(double from, double to, int intervals)
    {
        var values = new double[intervals + 1];
        for (int i = 0; i <= intervals; i++)
        {
            values[i] = from + (to - from) * i / intervals;
        }

        return values;
    }
}

/// <summary>
/// A box whose every edge is rounded with one radius, as the design builds the brain
/// and the doctor. Its six faces are parametrised the way the design laid its
/// pictures (the brain's folds) onto them: (u, v) in [0, 1], v running down the picture.
/// </summary>
internal sealed class RoundedBox
{
    public enum Face { Right, Left, Top, Bottom, Front, Back }

    public static readonly Face[] Faces = { Face.Right, Face.Left, Face.Top, Face.Bottom, Face.Front, Face.Back };

    private readonly Vector3 _half;
    private readonly float _radius;
    private readonly Matrix4x4 _place;
    private readonly Matrix4x4 _toLocal;
    private readonly Matrix4x4 _normalPlace;

    /// <summary>A box of half size <paramref name="half"/>, placed by rotation, scale and position (no shear).</summary>
    public RoundedBox(Vector3 half, double radius, Matrix4x4 place)
    {
        _half = half;
        _radius = (float)radius;
        _place = place;
        Matrix4x4.Invert(place, out _toLocal);
        _normalPlace = Matrix4x4.Transpose(_toLocal);
    }

    public Vector3 Half => _half;

    public float Radius => _radius;

    /// <summary>The same box grown outward by <paramref name="grow"/> (its outline shell).</summary>
    public RoundedBox Grown(double grow) =>
        new(_half + new Vector3((float)grow), _radius + grow, _place);

    /// <summary>Where a point on a face lands on the cube before rounding, in units of the half size.</summary>
    public static Vector3 Cube(Face face, double u, double v) => face switch
    {
        Face.Front => new((float)(2 * u - 1), (float)(1 - 2 * v), 1),
        Face.Back => new((float)(1 - 2 * u), (float)(1 - 2 * v), -1),
        Face.Top => new((float)(2 * u - 1), 1, (float)(2 * v - 1)),
        Face.Bottom => new((float)(2 * u - 1), -1, (float)(1 - 2 * v)),
        Face.Right => new(1, (float)(1 - 2 * v), (float)(1 - 2 * u)),
        _ => new(-1, (float)(1 - 2 * v), (float)(2 * u - 1)),
    };

    private static Vector3 FaceNormal(Face face) => face switch
    {
        Face.Front => Vector3.UnitZ,
        Face.Back => -Vector3.UnitZ,
        Face.Top => Vector3.UnitY,
        Face.Bottom => -Vector3.UnitY,
        Face.Right => Vector3.UnitX,
        _ => -Vector3.UnitX,
    };

    /// <summary>The point of the rounded surface over (u, v) on a face, in scene units.</summary>
    public void At(Face face, double u, double v, out Vector3 position, out Vector3 normal)
    {
        Vector3 c = Cube(face, u, v) * _half;
        Vector3 inner = _half - new Vector3(_radius);
        inner = Vector3.Max(inner, Vector3.Zero);
        Vector3 i = Vector3.Clamp(c, -inner, inner);
        Vector3 d = c - i;
        Vector3 n = d.LengthSquared() < 1e-12f ? FaceNormal(face) : Vector3.Normalize(d);
        Vector3 local = i + n * _radius;
        position = Vector3.Transform(local, _place);
        normal = Vector3.Normalize(Vector3.TransformNormal(n, _normalPlace));
    }

    /// <summary>The face as a patch, reaching slightly past its border so neighbouring faces overlap without a seam.</summary>
    public Patch PatchOf(Face face, double overlap = 0.03)
    {
        // u runs along the first in-face axis, v along the second (see Cube).
        (float uHalf, float vHalf) = face switch
        {
            Face.Front or Face.Back => (_half.X, _half.Y),
            Face.Top or Face.Bottom => (_half.X, _half.Z),
            _ => (_half.Z, _half.Y),
        };
        double uBend = Math.Min(0.5, _radius / uHalf / 2) + overlap;
        double vBend = Math.Min(0.5, _radius / vHalf / 2) + overlap;
        return new Patch(
            Regions.Graded(-overlap, 1 + overlap, uBend),
            Regions.Graded(-overlap, 1 + overlap, vBend),
            (double u, double v, out Vector3 p, out Vector3 n) => At(face, u, v, out p, out n));
    }

    /// <summary>Signed distance to the box surface; positive outside.</summary>
    public double Distance(Vector3 world)
    {
        Vector3 p = Vector3.Abs(Vector3.Transform(world, _toLocal));
        Vector3 inner = Vector3.Max(_half - new Vector3(_radius), Vector3.Zero);
        Vector3 q = p - inner;
        Vector3 outside = Vector3.Max(q, Vector3.Zero);
        return outside.Length() + Math.Min(Math.Max(q.X, Math.Max(q.Y, q.Z)), 0) - _radius;
    }

    /// <summary>The tone areas of every face that can be seen.</summary>
    public PatchRegions[] Build(bool shaded = true, Regions.Outside[]? outside = null, bool rim = false, Regions.Outside[]? behind = null) =>
        Faces.Select(face => Regions.Build(PatchOf(face), shaded, outside, rim, behind)).ToArray();
}

/// <summary>Patches for the round shapes of the scene.</summary>
internal static class RoundShapes
{
    /// <summary>
    /// The visible half of a sphere (or with <paramref name="back"/> the far half). Its
    /// poles sit on the outline, so the grid never pinches inside the sampled half.
    /// </summary>
    public static Patch Sphere(Vector3 centre, double radius, bool back = false)
    {
        Vector3 e1 = SceneView.Toward;
        Vector3 axis = Vector3.Normalize(Vector3.Cross(e1, Vector3.UnitX));
        Vector3 e2 = Vector3.Cross(axis, e1);
        float r = (float)radius;
        return new Patch(
            Regions.Even(0, Math.PI, 28),
            Regions.Even(-Math.PI / 2 - 0.08 + (back ? Math.PI : 0), Math.PI / 2 + 0.08 + (back ? Math.PI : 0), 28),
            (double theta, double phi, out Vector3 p, out Vector3 n) =>
            {
                float s = (float)Math.Sin(theta);
                n = e1 * (s * (float)Math.Cos(phi)) + e2 * (s * (float)Math.Sin(phi)) + axis * (float)Math.Cos(theta);
                p = centre + n * r;
            });
    }

    /// <summary>
    /// A tube of the given radius around a centre line. The cross-section angle is
    /// measured from the side facing the viewer, so only that half (or with
    /// <paramref name="back"/> the far half) is sampled.
    /// </summary>
    public static Patch Tube(IReadOnlyList<Vector3> centre, double radius, bool back = false)
    {
        int count = centre.Count;
        var side = new Vector3[count];
        var up = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 t = Vector3.Normalize(centre[Math.Min(count - 1, i + 1)] - centre[Math.Max(0, i - 1)]);
            Vector3 toward = SceneView.Toward - Vector3.Dot(SceneView.Toward, t) * t;
            side[i] = Vector3.Normalize(toward);
            up[i] = Vector3.Cross(t, side[i]);
        }

        var us = new double[count];
        for (int i = 0; i < count; i++)
        {
            us[i] = i;
        }

        float r = (float)radius;
        return new Patch(
            us,
            Regions.Even(-Math.PI / 2 - 0.08 + (back ? Math.PI : 0), Math.PI / 2 + 0.08 + (back ? Math.PI : 0), 20),
            (double s, double phi, out Vector3 p, out Vector3 n) =>
            {
                int i = Math.Clamp((int)Math.Floor(s), 0, count - 2);
                float f = (float)(s - i);
                Vector3 c = Vector3.Lerp(centre[i], centre[i + 1], f);
                Vector3 a = Vector3.Normalize(Vector3.Lerp(side[i], side[i + 1], f));
                Vector3 b = Vector3.Normalize(Vector3.Lerp(up[i], up[i + 1], f));
                n = a * (float)Math.Cos(phi) + b * (float)Math.Sin(phi);
                p = c + n * r;
            });
    }

    /// <summary>
    /// Part of a ring-shaped tube lying in the front plane, from angle <paramref name="from"/>
    /// over <paramref name="sweep"/> (the design's smiles).
    /// </summary>
    public static Patch RingArc(Vector3 centre, double ringRadius, double tubeRadius, double from, double sweep)
    {
        return new Patch(
            Regions.Even(from, from + sweep, 32),
            Regions.Even(-Math.PI, Math.PI, 24),
            (double a, double psi, out Vector3 p, out Vector3 n) =>
            {
                var radial = new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0);
                n = radial * (float)Math.Cos(psi) + Vector3.UnitZ * (float)Math.Sin(psi);
                p = centre + radial * (float)ringRadius + n * (float)tubeRadius;
            });
    }

    /// <summary>The round side of a short cylinder whose axis points at the viewer's plane (z).</summary>
    public static Patch CylinderSide(Vector3 centre, double radius, double halfDepth)
    {
        return new Patch(
            Regions.Even(-Math.PI, Math.PI, 64),
            Regions.Even(-halfDepth, halfDepth, 2),
            (double a, double z, out Vector3 p, out Vector3 n) =>
            {
                n = new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0);
                p = centre + n * (float)radius + new Vector3(0, 0, (float)z);
            });
    }
}
