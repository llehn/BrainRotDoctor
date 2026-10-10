using System.Numerics;

namespace BrainRotDoctor.App.Ui.Scene;

internal readonly record struct ScenePose(BrainPose Brain, WormPose Worm, DoctorPose Doctor);

/// <summary>A script: which actor makes which move, and when.</summary>
internal interface ISceneScript
{
    /// <summary>Length of the scene in seconds.</summary>
    double Length { get; }

    ScenePose PoseAt(double t);
}

/// <summary>
/// The main scene, timed as approved: the brain rises (0.1–0.75 s), the worm wiggles,
/// the doctor slides in (1.2–1.85 s), grabs the head (1.95–2.12 s), tugs (2.2–3.0 s),
/// the worm pops out whole at <see cref="PopAt"/> — the moment the tab closes — and
/// everyone leaves (3.55–4.0 s).
/// </summary>
internal sealed class ExtractionScript : ISceneScript
{
    public const double PopAt = 3.0;

    private const double BrainUp = -SceneView.HalfHeight + 0.16;
    private const double BrainDown = -3.7;
    private const double DoctorAway = 4.6;

    private static readonly (double T, double V)[] TugKeys =
    {
        (2.2, 0), (2.35, 0.3), (2.45, 0.2), (2.6, 0.6), (2.7, 0.5), (2.92, 0.95), (3.0, 1),
    };

    private static readonly Vector3 Pull = new(0.45f, 0.2f, 0);

    private readonly Vector3 _grab;
    private readonly Vector3 _pulled;
    private readonly double _hangLength;
    private readonly double _hangAngle;

    public ExtractionScript()
    {
        _grab = HeadTipAt(2.12);
        _pulled = _grab + Pull;
        Vector3 hole = Worm.At(Brain.Holes[3], Brain.Top, BrainUp, 1, 1);
        _hangLength = Vector3.Distance(hole, _pulled);
        _hangAngle = Math.Atan2(hole.Y - _pulled.Y, hole.X - _pulled.X);
    }

    public double Length => 4.0;

    public ScenePose PoseAt(double t)
    {
        // Brain: rise with a little overshoot, squash on the pop, sink.
        double y = BrainDown;
        if (t >= 0.1 && t < 0.75) y = Lerp(BrainDown, BrainUp, EaseBack((t - 0.1) / 0.65));
        else if (t >= 0.75 && t < 3.55) y = BrainUp;
        else if (t >= 3.55 && t < 4) y = Lerp(BrainUp, BrainDown, EaseIn((t - 3.55) / 0.45));

        double sx = 1, sy = 1;
        if (t >= 3.0 && t < 3.4)
        {
            double p = (t - 3) / 0.4;
            sy = 1 - 0.09 * Math.Sin(p * Math.PI * 2) * (1 - p);
            sx = 1 + 0.06 * Math.Sin(p * Math.PI * 2) * (1 - p);
        }

        BrainFace face = t < 2.2 ? BrainFace.Hypnotised : t < 3.0 ? BrainFace.Straining : BrainFace.Happy;
        var brain = new BrainPose(y, sx, sy, face, t, (t - 3) / 0.3);

        IReadOnlyList<Vector3>? line = t >= 4 ? null : t < PopAt ? Threaded(t, y, sx, sy) : Hanging(t);
        var worm = new WormPose(line, t >= 2.12 && t < 3.0);

        (double dx, double dy) = Doctor.StandingFor(TipAt(t));
        double jaw = t < 2.04 || t > 3.9 ? 0.24 : t < 2.12 ? Lerp(0.24, 0, (t - 2.04) / 0.08) : 0;
        var doctor = new DoctorPose(dx, dy, jaw, t < 3.0 ? DoctorFace.Focused : DoctorFace.Happy);

        return new ScenePose(brain, worm, doctor);
    }

    // ---------- the worm's wiggle ----------

    private readonly record struct Wiggle(double TailOut, double LoopOut, double HeadOut, double Lean);

    /// <summary>The worm slides through the brain (head out while the tail goes in, and back); the loop breathes on its own.</summary>
    private static Wiggle WiggleAt(double t)
    {
        double calm = 1 - Clamp((t - 1.75) / 0.35);
        double s = Math.Sin(t * 3.1);
        double lean = -0.38 * EaseInOut(Clamp((t - 1.85) / 0.25));
        return new Wiggle(
            0.2 - 0.09 * s,
            0.3 + 0.07 * Math.Sin(t * 4.3 + 1.2),
            0.5 + 0.09 * s * calm,
            (0.14 * calm + 0.02) * Math.Sin(t * 6.5) + lean);
    }

    /// <summary>The head's line: up out of its hole, a little step, up to the tip, all leaning.</summary>
    private static Vector3[] HeadLine(double t, double brainY, double sx, double sy)
    {
        Wiggle w = WiggleAt(t);
        double c = Math.Cos(w.Lean), s = Math.Sin(w.Lean);
        (double X, double Y)[] shape = { (0, 0), (0, 0.22), (0.17, 0.22), (0.17, w.HeadOut) };
        return shape
            .Select(p => Worm.At(Brain.Holes[3] + p.X * c - p.Y * s, Brain.Top + p.X * s + p.Y * c, brainY, sx, sy))
            .ToArray();
    }

    private static Vector3 HeadTipAt(double t) => HeadLine(t, BrainUp, 1, 1)[3];

    private static double TugAt(double t) => t < 2.2 ? 0 : t < 3 ? Keyed(t, TugKeys) : 1;

    /// <summary>Before the pop: tail stub, loop and head; while tugged, tail and loop are drawn in.</summary>
    private List<Vector3> Threaded(double t, double brainY, double sx, double sy)
    {
        Wiggle w = WiggleAt(t);
        double k = TugAt(t);
        double tail = Lerp(w.TailOut, -0.2, EaseInOut(Clamp(k / 0.55)));
        double loop = Lerp(w.LoopOut, -0.18, EaseInOut(Clamp((k - 0.3) / 0.6)));
        Vector3[] head = HeadLine(Math.Min(t, 2.12), brainY, sx, sy);
        if (t >= 2.12)
        {
            // Held by the forceps: the head follows the tip and straightens toward it.
            Vector3 tip = TipAt(t), moved = tip - _grab;
            float b = (float)EaseInOut(Clamp((t - 2.15) / 0.2));
            Vector3 root = head[0];
            for (int i = 0; i < 4; i++)
            {
                float f = i / 3f;
                head[i] = Vector3.Lerp(head[i] + moved * f, Vector3.Lerp(root, tip, f), b);
            }
        }

        return Worm.Threaded(tail, loop, head, brainY, sx, sy);
    }

    /// <summary>After the pop: the whole worm hangs from the forceps; its tail flicks up clear of the brain, then dangles.</summary>
    private List<Vector3> Hanging(double t)
    {
        Vector3 tip = TipAt(t);
        double q = Clamp((t - 3) / 0.2);
        double angle = Lerp(_hangAngle, -Math.PI / 2, EaseOut(q));
        double length = Lerp(_hangLength, 0.55, 1 - Math.Pow(1 - q, 4));
        if (t > 3.2)
        {
            double u = t - 3.2;
            length = Lerp(0.55, 1.15, EaseBack(Clamp(u / 0.3)));
            angle = -Math.PI / 2 + 0.22 * Math.Sin(u * 10) * Math.Exp(-u * 3.5);
        }

        if (t > 3.55)
        {
            angle -= 0.35 * EaseIn(Clamp((t - 3.55) / 0.45));
        }

        Vector3 tail = tip + new Vector3((float)(Math.Cos(angle) * length), (float)(Math.Sin(angle) * length), 0);
        double wave = Clamp(q * 3) * (0.06 + 0.16 * Clamp((1.15 - length) / 0.6));
        return Worm.Hanging(tail, tip, wave, t);
    }

    // ---------- the forceps tip ----------

    /// <summary>Where the forceps tip is, on the worm's plane.</summary>
    private Vector3 TipAt(double t)
    {
        if (t < 2.12)
        {
            Vector3 head = HeadTipAt(t);
            Vector3 hover = head + new Vector3(0.42f, -0.02f, 0);
            Vector3 away = head + new Vector3((float)DoctorAway, 0, 0);
            if (t < 1.2) return away;
            if (t < 1.85) return Vector3.Lerp(away, hover, (float)EaseOut((t - 1.2) / 0.65));
            if (t < 1.95) return hover;
            return Vector3.Lerp(hover, head, (float)EaseInOut((t - 1.95) / 0.17));
        }

        if (t < 3.0) return _grab + Pull * (float)TugAt(t);
        if (t < 3.12)
        {
            float e = (float)EaseOut((t - 3) / 0.12);
            return _pulled + new Vector3(0.35f * e, 0.08f * e, 0);
        }

        if (t < 3.35)
        {
            float e = (float)EaseInOut((t - 3.12) / 0.23);
            return _pulled + new Vector3(0.35f - 0.03f * e, 0.08f + 0.12f * e, 0);
        }

        if (t < 3.55) return _pulled + new Vector3(0.32f, 0.2f, 0);
        return _pulled + new Vector3((float)(0.32 + DoctorAway * EaseIn(Clamp((t - 3.55) / 0.45))), 0.2f, 0);
    }

    // ---------- easing ----------

    private static double Clamp(double x) => Math.Clamp(x, 0, 1);

    private static double Lerp(double a, double b, double p) => a + (b - a) * p;

    private static double EaseOut(double p) => 1 - Math.Pow(1 - p, 3);

    private static double EaseIn(double p) => p * p * p;

    private static double EaseInOut(double p) => p < 0.5 ? 4 * p * p * p : 1 - Math.Pow(-2 * p + 2, 3) / 2;

    private static double EaseBack(double p)
    {
        const double c1 = 1.7, c3 = c1 + 1;
        return 1 + c3 * Math.Pow(p - 1, 3) + c1 * Math.Pow(p - 1, 2);
    }

    private static double Keyed(double t, (double T, double V)[] keys)
    {
        if (t <= keys[0].T)
        {
            return keys[0].V;
        }

        for (int i = 1; i < keys.Length; i++)
        {
            if (t <= keys[i].T)
            {
                double p = (t - keys[i - 1].T) / (keys[i].T - keys[i - 1].T);
                return Lerp(keys[i - 1].V, keys[i].V, EaseInOut(p));
            }
        }

        return keys[^1].V;
    }
}
