using System.Numerics;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

/// <param name="Line">The worm's centre line from tail to head; null when it is not in the scene.</param>
/// <param name="Squint">Eyes pinched shut (while being tugged).</param>
internal readonly record struct WormPose(IReadOnlyList<Vector3>? Line, bool Squint);

/// <summary>
/// The worm: one green tube with a round tail and a bigger round head with eyes.
/// Where it runs inside the brain it is hidden by the brain.
/// </summary>
internal sealed class Worm
{
    public const double Radius = 0.075;
    private const double Rim = Solid.Outline * 0.8;

    // How deep the worm runs under the brain's top face, and the radius of its bends.
    private const double Deep = 0.3;
    private const double Bend = 0.2;

    /// <summary>
    /// Threaded through the brain: tail stub, loop and head, joined underground.
    /// <paramref name="tailOut"/> and <paramref name="loopOut"/> are how far tail and
    /// loop stick out of the top (negative: drawn in); <paramref name="head"/> is the
    /// head's own line from its hole to its tip.
    /// </summary>
    public static List<Vector3> Threaded(double tailOut, double loopOut, IReadOnlyList<Vector3> head, double brainY, double scaleX, double scaleY)
    {
        double top = Brain.Top;
        double[] h = Brain.Holes;
        (double X, double Y)[] body =
        {
            (h[0], top + tailOut), (h[0], top - Deep), (h[1], top - Deep), (h[1], top + loopOut),
            (h[2], top + loopOut), (h[2], top - Deep), (h[3], top - Deep),
        };
        var points = body.Select(p => At(p.X, p.Y, brainY, scaleX, scaleY)).Concat(head).ToList();
        return Curves.Rounded(points, Bend);
    }

    /// <summary>A point on the brain's middle plane, given in brain coordinates.</summary>
    public static Vector3 At(double x, double y, double brainY, double scaleX, double scaleY) =>
        new((float)(Brain.X + x * scaleX), (float)(brainY + y * scaleY), 0);

    /// <summary>Hanging from the forceps tip: tail at <paramref name="tail"/>, gently swinging.</summary>
    public static List<Vector3> Hanging(Vector3 tail, Vector3 tip, double wave, double time)
    {
        Vector3 dir = Vector3.Normalize(tip - tail);
        var normal = new Vector3(-dir.Y, dir.X, 0);
        var points = new List<Vector3>();
        for (int i = 0; i <= 12; i++)
        {
            double u = i / 12.0;
            double off = wave * Math.Sin(Math.PI * 2 * 1.3 * u - time * 10) * Math.Sin(Math.PI * u);
            points.Add(Vector3.Lerp(tail, tip, (float)u) + normal * (float)off);
        }

        return Curves.CatmullRom(points);
    }

    public void Draw(DrawingContext dc, WormPose pose, Regions.Outside brain)
    {
        if (pose.Line is not { Count: >= 2 } line)
        {
            return;
        }

        Regions.Outside[] outside = { brain };
        Vector3 tail = line[0];
        Vector3 head = line[^1] + Curves.EndTangent(line) * (float)(Radius * 0.4);
        Solid[] parts =
        {
            Solid.Round((grow, back) => RoundShapes.Tube(line, Radius + grow, back), k => Palette.ToonPainted(Palette.WormSkin, k), Rim, Solid.TubeDistance(line, Radius), outside),
            Solid.Round((grow, back) => RoundShapes.Sphere(tail, Radius + grow, back), k => Palette.Toon(Palette.Worm, k), Rim, Solid.SphereDistance(tail, Radius), outside),
            Solid.Round((grow, back) => RoundShapes.Sphere(head, Radius * 1.3 + grow, back), k => Palette.Toon(Palette.Worm, k), Rim, Solid.SphereDistance(head, Radius * 1.3), outside),
        };

        // All outlines first: where the head meets the body there is no line.
        foreach (Solid part in parts)
        {
            part.DrawRim(dc);
        }

        parts[0].DrawBody(dc);
        DrawRings(dc, line, outside[0]);
        parts[1].DrawBody(dc);
        parts[2].DrawBody(dc);
        DrawEyes(dc, head, pose.Squint);
    }

    // The skin's fine rings: one every RingPitch along the body, a quarter of that wide.
    private const double RingPitch = 0.24 / 16;
    private const double RingWidth = RingPitch / 4;

    /// <summary>The darker rings around the body, shaded with the same tone steps as the body.</summary>
    private static void DrawRings(DrawingContext dc, IReadOnlyList<Vector3> line, Regions.Outside brain)
    {
        double length = 0;
        for (int i = 1; i < line.Count; i++)
        {
            length += Vector3.Distance(line[i], line[i - 1]);
        }

        double spacing = length / (line.Count - 1);
        Patch tube = RoundShapes.Tube(line, Radius);
        double[] phis = Regions.Even(-Math.PI / 2 - 0.08, Math.PI / 2 + 0.08, 20);
        var tones = new StreamGeometry[5];
        var contexts = new StreamGeometryContext[5];
        for (int k = 0; k < 5; k++)
        {
            tones[k] = new StreamGeometry();
            contexts[k] = tones[k].Open();
        }

        var keep = new double[phis.Length];
        var lit = new double[phis.Length];
        for (double s = 0; s < length; s += RingPitch)
        {
            double u0 = s / spacing, u1 = Math.Min(length, s + RingWidth) / spacing;
            for (int j = 0; j < phis.Length; j++)
            {
                tube.At((u0 + u1) / 2, phis[j], out Vector3 p, out Vector3 n);
                keep[j] = Math.Min(Vector3.Dot(n, SceneView.Toward), brain(p));
                lit[j] = Vector3.Dot(n, SceneView.Light);
            }

            for (int k = 0; k < 5; k++)
            {
                double edge = k == 0 ? double.NegativeInfinity : Palette.ToneEdges[k - 1];
                double F(int j) => k == 0 ? keep[j] : Math.Min(keep[j], lit[j] - edge);
                int j0 = 0;
                while (j0 < phis.Length)
                {
                    while (j0 < phis.Length && F(j0) < 0) j0++;
                    if (j0 >= phis.Length) break;
                    int j1 = j0;
                    while (j1 + 1 < phis.Length && F(j1 + 1) >= 0) j1++;

                    // The band of this tone across the ring, with its ends found between samples.
                    double from = j0 == 0 ? phis[0] : Cross(phis[j0 - 1], phis[j0], F(j0 - 1), F(j0));
                    double to = j1 == phis.Length - 1 ? phis[^1] : Cross(phis[j1], phis[j1 + 1], F(j1), F(j1 + 1));
                    var across = new List<double> { from };
                    for (int j = j0; j <= j1; j++) across.Add(phis[j]);
                    across.Add(to);

                    StreamGeometryContext ctx = contexts[k];
                    for (int a = 0; a < across.Count; a++)
                    {
                        tube.At(u0, across[a], out Vector3 p, out _);
                        if (a == 0) ctx.BeginFigure(SceneView.Project(p), true); else ctx.LineTo(SceneView.Project(p));
                    }

                    for (int a = across.Count - 1; a >= 0; a--)
                    {
                        tube.At(u1, across[a], out Vector3 p, out _);
                        ctx.LineTo(SceneView.Project(p));
                    }

                    ctx.EndFigure(true);
                    j0 = j1 + 1;
                }
            }
        }

        for (int k = 0; k < 5; k++)
        {
            contexts[k].Dispose();
            dc.DrawGeometry(Palette.ToonPainted(Palette.WormRings, k), null, tones[k]);
        }
    }

    private static double Cross(double a, double b, double fa, double fb) => a + (b - a) * (fa / (fa - fb));

    private static void DrawEyes(DrawingContext dc, Vector3 head, bool squint)
    {
        const double r = Radius;
        var local = Avalonia.Matrix.CreateTranslation(head.X, head.Y);
        foreach (int k in new[] { -1, 1 })
        {
            if (!squint)
            {
                Flat.Circle(dc, Palette.Flat(Palette.White), head.Z + r * 1.32, local, k * r * 0.5, r * 0.2, r * 0.45);
                Flat.Circle(dc, Palette.Flat(Palette.Pupil), head.Z + r * 1.34, local, k * r * 0.5, r * 0.14, r * 0.24);
            }
            else
            {
                foreach (int s in new[] { 1, -1 })
                {
                    Flat.Rect(dc, Palette.Flat(Palette.Pupil), head.Z + r * 1.33, local, k * r * 0.5, r * 0.2 + s * r * 0.1, r * 0.55, r * 0.14, k * s * 0.5);
                }
            }
        }
    }
}
