using System.Numerics;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>Centre lines for tubes, sampled densely and evenly along their length.</summary>
internal static class Curves
{
    /// <summary>Spacing of the samples along a centre line, in scene units.</summary>
    public const double Step = 0.012;

    /// <summary>
    /// A line through the points made of straight runs joined by bends of one radius;
    /// a bend only gets smaller where a run is too short for it (the design's worm).
    /// </summary>
    public static List<Vector3> Rounded(IReadOnlyList<Vector3> points, double radius)
    {
        var q = new List<Vector3> { points[0] };
        for (int i = 1; i < points.Count; i++)
        {
            if (Vector3.Distance(points[i], q[^1]) > 1e-4f)
            {
                q.Add(points[i]);
            }
        }

        var dense = new List<Vector3> { q[0] };
        Vector3 current = q[0];
        for (int i = 1; i < q.Count - 1; i++)
        {
            Vector3 a = q[i - 1], b = q[i], c = q[i + 1];
            float la = Vector3.Distance(b, a), lb = Vector3.Distance(c, b);
            Vector3 da = (b - a) / la, db = (c - b) / lb;
            float r = (float)Math.Min(radius, Math.Min(la / 2, lb / 2));
            Vector3 s1 = b - da * r, e1 = b + db * r;
            if (Vector3.Distance(current, s1) > 1e-5f)
            {
                AddSegment(dense, current, s1);
            }

            for (int k = 1; k <= 24; k++)
            {
                float t = k / 24f;
                dense.Add((1 - t) * (1 - t) * s1 + 2 * (1 - t) * t * b + t * t * e1);
            }

            current = e1;
        }

        if (Vector3.Distance(current, q[^1]) > 1e-5f)
        {
            AddSegment(dense, current, q[^1]);
        }

        return Even(dense);
    }

    /// <summary>A smooth curve through the points (centripetal Catmull–Rom, as the design used).</summary>
    public static List<Vector3> CatmullRom(IReadOnlyList<Vector3> points)
    {
        var dense = new List<Vector3>();
        const int perSpan = 24;
        int spans = points.Count - 1;
        for (int k = 0; k <= spans * perSpan; k++)
        {
            dense.Add(CatmullRomPoint(points, (double)k / (spans * perSpan)));
        }

        return Even(dense);
    }

    public static List<Vector3> QuadraticBezier(Vector3 a, Vector3 control, Vector3 b)
    {
        var dense = new List<Vector3>();
        for (int k = 0; k <= 64; k++)
        {
            float t = k / 64f;
            dense.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b);
        }

        return Even(dense);
    }

    public static Vector3 EndTangent(IReadOnlyList<Vector3> line) => Vector3.Normalize(line[^1] - line[^2]);

    private static void AddSegment(List<Vector3> dense, Vector3 from, Vector3 to)
    {
        int steps = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(from, to) / (Step / 2)));
        for (int k = 1; k <= steps; k++)
        {
            dense.Add(Vector3.Lerp(from, to, (float)k / steps));
        }
    }

    /// <summary>Resamples a dense line at even spacing along its length.</summary>
    private static List<Vector3> Even(List<Vector3> dense)
    {
        var lengths = new double[dense.Count];
        for (int i = 1; i < dense.Count; i++)
        {
            lengths[i] = lengths[i - 1] + Vector3.Distance(dense[i], dense[i - 1]);
        }

        double total = lengths[^1];
        int count = Math.Max(2, (int)Math.Ceiling(total / Step) + 1);
        var even = new List<Vector3>(count);
        int seg = 1;
        for (int k = 0; k < count; k++)
        {
            double at = total * k / (count - 1);
            while (seg < dense.Count - 1 && lengths[seg] < at)
            {
                seg++;
            }

            double span = lengths[seg] - lengths[seg - 1];
            float f = span <= 0 ? 0 : (float)((at - lengths[seg - 1]) / span);
            even.Add(Vector3.Lerp(dense[seg - 1], dense[seg], f));
        }

        return even;
    }

    private static Vector3 CatmullRomPoint(IReadOnlyList<Vector3> points, double t)
    {
        int l = points.Count;
        double p = (l - 1) * t;
        int index = (int)Math.Floor(p);
        double weight = p - index;
        if (weight == 0 && index == l - 1)
        {
            index = l - 2;
            weight = 1;
        }

        Vector3 p0 = index > 0 ? points[index - 1] : points[0] - points[1] + points[0];
        Vector3 p1 = points[index], p2 = points[index + 1];
        Vector3 p3 = index + 2 < l ? points[index + 2] : points[l - 1] - points[l - 2] + points[l - 1];

        double dt0 = Math.Pow(Vector3.DistanceSquared(p0, p1), 0.25);
        double dt1 = Math.Pow(Vector3.DistanceSquared(p1, p2), 0.25);
        double dt2 = Math.Pow(Vector3.DistanceSquared(p2, p3), 0.25);
        if (dt1 < 1e-4) dt1 = 1;
        if (dt0 < 1e-4) dt0 = dt1;
        if (dt2 < 1e-4) dt2 = dt1;

        double Axis(float x0, float x1, float x2, float x3)
        {
            double t1 = ((x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1) * dt1;
            double t2 = ((x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2) * dt1;
            double c2 = -3 * x1 + 3 * x2 - 2 * t1 - t2;
            double c3 = 2 * x1 - 2 * x2 + t1 + t2;
            return x1 + t1 * weight + c2 * weight * weight + c3 * weight * weight * weight;
        }

        return new Vector3(
            (float)Axis(p0.X, p1.X, p2.X, p3.X),
            (float)Axis(p0.Y, p1.Y, p2.Y, p3.Y),
            (float)Axis(p0.Z, p1.Z, p2.Z, p3.Z));
    }
}
