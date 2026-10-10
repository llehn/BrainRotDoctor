using Avalonia;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// The brain's folds, generated exactly as the design generated them: on each face a
/// square grid, random non-crossing walks of horizontal and vertical steps, every
/// bend rounded with half a grid cell, from the same seeds. The result is the same
/// fold pattern, as filled outlines in the face's picture coordinates (pixels of a
/// 512-pixel-wide front face).
/// </summary>
internal static class BrainFolds
{
    public const double Grid = 512.0 / 16;
    public const double StrokeWidth = Grid * 0.2;

    private static readonly double PixelsPerUnit = 512 / Brain.Half.X;

    /// <summary>Picture size of a face in pixels.</summary>
    public static (int Width, int Height) Size(RoundedBox.Face face)
    {
        (double w, double h) = face switch
        {
            RoundedBox.Face.Front or RoundedBox.Face.Back => (Brain.Half.X, Brain.Half.Y),
            RoundedBox.Face.Top or RoundedBox.Face.Bottom => (Brain.Half.X, Brain.Half.Z),
            _ => (Brain.Half.Z, Brain.Half.Y),
        };
        return (Math.Max(64, JsRound(w * PixelsPerUnit)), Math.Max(64, JsRound(h * PixelsPerUnit)));
    }

    /// <summary>
    /// The fold outlines of one face, each a closed polygon in picture pixels. Only the
    /// stretches of fold lying where <paramref name="visible"/> holds are kept.
    /// </summary>
    public static List<Point[]> Outlines(RoundedBox.Face face, Func<Point, bool> visible)
    {
        (int wc, int hc) = Size(face);
        string name = face.ToString().ToLowerInvariant();
        int seed = 3 * 31 + name.Length * 7 + name[0];
        var outlines = new List<Point[]>();
        void Add(List<Point> line, double half, bool roundStart, bool roundEnd)
        {
            int i = 0;
            while (i < line.Count)
            {
                while (i < line.Count && !visible(line[i])) i++;
                int start = i;
                while (i < line.Count && visible(line[i])) i++;
                if (i - start >= 2)
                {
                    // A stretch cut off where the face turns away gets a flat end there.
                    outlines.Add(Stroke(line.GetRange(start, i - start), half, roundStart && start == 0, roundEnd && i == line.Count));
                }
            }
        }

        foreach (List<Point> line in Walks(wc, hc, seed, (x, y) => Blocked(face, wc, hc, x, y)))
        {
            Add(line, StrokeWidth / 2, true, true);
        }

        // The centre fissure: down the middle of the top and back, and a short way down the front.
        double fissure = StrokeWidth * 1.4 / 2;
        if (face is RoundedBox.Face.Top or RoundedBox.Face.Back)
        {
            Add(Line(new Point(wc / 2.0, 0), new Point(wc / 2.0, hc)), fissure, false, false);
        }
        else if (face == RoundedBox.Face.Front)
        {
            Add(Line(new Point(wc / 2.0, 0), new Point(wc / 2.0, hc * 0.22)), fissure, false, true);
        }

        return outlines;
    }

    private static bool Blocked(RoundedBox.Face face, int wc, int hc, double x, double y)
    {
        const double g = Grid;
        if (x < g * 0.4 || y < g * 0.4 || x > wc - g * 0.4 || y > hc - g * 0.4)
        {
            return true;
        }

        if (face == RoundedBox.Face.Top)
        {
            if (Math.Abs(x - wc / 2.0) < g * 0.75)
            {
                return true;
            }

            foreach (double hole in Brain.Holes)
            {
                double qx = hole / Brain.Half.X;
                if (Math.Sqrt(Math.Pow(x - (qx + 1) / 2 * wc, 2) + Math.Pow(y - 0.5 * hc, 2)) < g * 1.4)
                {
                    return true;
                }
            }

            return false;
        }

        if (face == RoundedBox.Face.Front)
        {
            if (Math.Abs(x - wc / 2.0) < g * 0.75 && y < hc * 0.3)
            {
                return true;
            }

            return Math.Pow(x - wc / 2.0, 2) / Math.Pow(wc * 0.37, 2) + Math.Pow(y - hc * 0.64, 2) / Math.Pow(hc * 0.3, 2) < 1;
        }

        return false;
    }

    private static IEnumerable<List<Point>> Walks(int wc, int hc, int seed, Func<double, double, bool> block)
    {
        const double g = Grid;
        Func<double> random = Mulberry(seed);
        int cx = (int)Math.Floor(wc / g), cy = (int)Math.Floor(hc / g);
        double ox = (wc - cx * g) / 2, oy = (hc - cy * g) / 2;
        var taken = new HashSet<(int, int)>();
        bool Open(int i, int j) => i >= 0 && j >= 0 && i <= cx && j <= cy && !block(ox + i * g, oy + j * g);

        var nodes = new List<(int I, int J, double Order)>();
        for (int j = 0; j <= cy; j++)
        {
            for (int i = 0; i <= cx; i++)
            {
                if (Open(i, j))
                {
                    nodes.Add((i, j, random()));
                }
            }
        }

        nodes = nodes.OrderBy(n => n.Order).ToList();
        int[][] steps = { new[] { 1, 0 }, new[] { 0, 1 }, new[] { -1, 0 }, new[] { 0, -1 } };
        foreach ((int si, int sj, _) in nodes)
        {
            if (taken.Contains((si, sj)))
            {
                continue;
            }

            var path = new List<(int I, int J)> { (si, sj) };
            taken.Add((si, sj));
            int dir = (int)Math.Floor(random() * 4), run = 1 + (int)Math.Floor(random() * 4), straight = 0;
            while (path.Count < 60)
            {
                (int ci, int cj) = path[^1];
                int[] turns = random() < 0.5 ? new[] { (dir + 3) % 4, (dir + 1) % 4 } : new[] { (dir + 1) % 4, (dir + 3) % 4 };
                int[] order = straight >= run ? turns.Append(dir).ToArray() : new[] { dir }.Concat(turns).ToArray();
                bool moved = false;
                foreach (int d in order)
                {
                    int ni = ci + steps[d][0], nj = cj + steps[d][1];
                    if (!Open(ni, nj) || taken.Contains((ni, nj)))
                    {
                        continue;
                    }

                    if (d != dir)
                    {
                        run = 1 + (int)Math.Floor(random() * 4);
                        straight = 0;
                        dir = d;
                    }
                    else
                    {
                        straight++;
                    }

                    path.Add((ni, nj));
                    taken.Add((ni, nj));
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    break;
                }
            }

            if (path.Count >= 3)
            {
                yield return RoundedLine(path.Select(p => new Point(ox + p.I * g, oy + p.J * g)).ToList(), g / 2);
            }
            else
            {
                foreach ((int I, int J) p in path)
                {
                    taken.Remove(p);
                }
            }
        }
    }

    /// <summary>The design's random numbers (mulberry32), reproduced bit for bit.</summary>
    private static Func<double> Mulberry(int seed) => () =>
    {
        unchecked
        {
            seed += 0x6D2B79F5;
            int t = (seed ^ (int)((uint)seed >> 15)) * (1 | seed);
            t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
            return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
        }
    };

    /// <summary>A grid walk as a dense centre line: straight runs, every corner a quarter circle.</summary>
    private static List<Point> RoundedLine(List<Point> points, double radius)
    {
        var line = new List<Point>();
        Point current = points[0];
        for (int i = 1; i < points.Count - 1; i++)
        {
            Point a = points[i - 1], p = points[i], b = points[i + 1];
            Vector into = Unit(p - a), outOf = Unit(b - p);
            if (into == outOf)
            {
                continue;
            }

            Point start = p - into * radius;
            AddLine(line, current, start);
            Point centre = start + outOf * radius;
            for (int k = 1; k <= 9; k++)
            {
                double angle = Math.PI / 2 * k / 9;
                line.Add(centre - outOf * (radius * Math.Cos(angle)) + into * (radius * Math.Sin(angle)));
            }

            current = p + outOf * radius;
        }

        AddLine(line, current, points[^1]);
        return line;
    }

    private static List<Point> Line(Point from, Point to)
    {
        var line = new List<Point>();
        AddLine(line, from, to);
        return line;
    }

    private static void AddLine(List<Point> line, Point from, Point to)
    {
        if (line.Count == 0)
        {
            line.Add(from);
        }

        double length = Length(to - from);
        int steps = Math.Max(1, (int)Math.Ceiling(length / 3));
        for (int k = 1; k <= steps; k++)
        {
            line.Add(from + (to - from) * ((double)k / steps));
        }
    }

    /// <summary>The outline of a line drawn with the given half width, optionally with round ends.</summary>
    private static Point[] Stroke(List<Point> line, double half, bool roundStart, bool roundEnd)
    {
        int n = line.Count;
        var left = new Point[n];
        var right = new Point[n];
        for (int i = 0; i < n; i++)
        {
            Vector t = Unit(line[Math.Min(n - 1, i + 1)] - line[Math.Max(0, i - 1)]);
            var normal = new Vector(-t.Y, t.X);
            left[i] = line[i] + normal * half;
            right[i] = line[i] - normal * half;
        }

        var outline = new List<Point>(left);
        AddCap(outline, line[n - 1], Unit(line[n - 1] - line[n - 2]), half, roundEnd);
        for (int i = n - 1; i >= 0; i--)
        {
            outline.Add(right[i]);
        }

        AddCap(outline, line[0], Unit(line[0] - line[1]), half, roundStart);
        return outline.ToArray();
    }

    private static void AddCap(List<Point> outline, Point end, Vector outward, double half, bool round)
    {
        if (!round)
        {
            return;
        }

        var normal = new Vector(-outward.Y, outward.X);
        for (int k = 1; k < 10; k++)
        {
            double angle = Math.PI * k / 10;
            outline.Add(end + normal * (half * Math.Cos(angle)) + outward * (half * Math.Sin(angle)));
        }
    }

    private static double Length(Vector v) => Math.Sqrt(v.X * v.X + v.Y * v.Y);

    private static Vector Unit(Vector v)
    {
        double length = Length(v);
        return new Vector(Math.Round(v.X / length, 12), Math.Round(v.Y / length, 12));
    }

    private static int JsRound(double x) => (int)Math.Floor(x + 0.5);
}
