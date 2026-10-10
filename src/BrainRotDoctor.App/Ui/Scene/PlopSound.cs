using System.IO;
using System.Runtime.InteropServices;
using BrainRotDoctor.App.Runtime;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// The plop on the pop, made exactly as the design made it: a short resonance that
/// jumps up in pitch (like a cork), a soft thump under it, and a wet click on the
/// first milliseconds. Rendered once into memory and played through Windows, so it
/// follows the system volume and mute like any other app sound.
/// </summary>
internal static class PlopSound
{
    private const int Rate = 48000;

    private static readonly Lazy<IntPtr> Wave = new(() =>
    {
        byte[] bytes = Render();
        // The sound plays from this memory after Play returns, so it must never move.
        IntPtr memory = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, memory, bytes.Length);
        return memory;
    });

    /// <summary>Renders the sound ahead of time, so the first pop is not late.</summary>
    public static void Prepare() => _ = Wave.Value;

    public static void Play()
    {
        try
        {
            NativeMethods.PlaySound(Wave.Value, IntPtr.Zero, NativeMethods.SND_MEMORY | NativeMethods.SND_ASYNC | NativeMethods.SND_NODEFAULT);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // No sound system: the scene still plays silently.
        }
    }

    /// <summary>The samples as a 16-bit mono WAV file.</summary>
    private static byte[] Render()
    {
        int count = (int)(Rate * 0.14);
        var mix = new double[count];

        // The plop: a sine sliding 230 → 880 Hz in 45 ms, then to 960 Hz by 100 ms.
        AddTone(mix, t => Ramp(t, (0, 230), (0.045, 880), (0.1, 960)), t => Ramp(t, (0, 0.0001), (0.004, 0.8), (0.12, 0.0001)), 0.13);

        // A soft thump under it: 150 → 60 Hz.
        AddTone(mix, t => Ramp(t, (0, 150), (0.07, 60)), t => Ramp(t, (0, 0.0001), (0.003, 0.5), (0.08, 0.0001)), 0.09);

        // The click: 12 ms of decaying noise through a 2.5 kHz low-pass.
        var random = new Random(7);
        int clickLength = (int)(Rate * 0.012);
        var click = new double[clickLength];
        for (int i = 0; i < clickLength; i++)
        {
            click[i] = (random.NextDouble() * 2 - 1) * Math.Pow(1 - (double)i / clickLength, 3);
        }

        LowPass(click, 2500, Math.Pow(10, 1 / 20.0));
        for (int i = 0; i < clickLength; i++)
        {
            mix[i] += click[i] * 0.35;
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        int dataBytes = count * 2;
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(Rate);
        writer.Write(Rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        foreach (double sample in mix)
        {
            writer.Write((short)Math.Round(Math.Clamp(sample * 0.9, -1, 1) * short.MaxValue));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static void AddTone(double[] mix, Func<double, double> frequency, Func<double, double> gain, double stop)
    {
        double phase = 0;
        int end = Math.Min(mix.Length, (int)(Rate * stop));
        for (int i = 0; i < end; i++)
        {
            double t = (double)i / Rate;
            mix[i] += Math.Sin(phase) * gain(t);
            phase += 2 * Math.PI * frequency(t) / Rate;
        }
    }

    /// <summary>Exponential ramps between the given (time, value) points, held after the last.</summary>
    private static double Ramp(double t, params (double T, double V)[] points)
    {
        for (int i = 1; i < points.Length; i++)
        {
            if (t <= points[i].T)
            {
                double p = (t - points[i - 1].T) / (points[i].T - points[i - 1].T);
                return points[i - 1].V * Math.Pow(points[i].V / points[i - 1].V, p);
            }
        }

        return points[^1].V;
    }

    /// <summary>A two-pole low-pass filter (the web audio biquad), applied in place.</summary>
    private static void LowPass(double[] samples, double cutoff, double q)
    {
        double w = 2 * Math.PI * cutoff / Rate, alpha = Math.Sin(w) / (2 * q), cos = Math.Cos(w);
        double a0 = 1 + alpha;
        double b0 = (1 - cos) / 2 / a0, b1 = (1 - cos) / a0, b2 = b0, a1 = -2 * cos / a0, a2 = (1 - alpha) / a0;
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            double x = samples[i];
            double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1;
            x1 = x;
            y2 = y1;
            y1 = y;
            samples[i] = y;
        }
    }
}
