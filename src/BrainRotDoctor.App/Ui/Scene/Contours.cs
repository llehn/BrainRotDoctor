using Avalonia;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// Traces the outline of the area where a sampled value is ≥ 0 (marching squares on
/// a rectilinear grid) and writes it as closed figures. Everything outside the grid
/// counts as outside, so every outline closes; an outline that runs along the grid
/// border follows it exactly.
/// </summary>
internal static class Contours
{
    /// <param name="us">Grid positions along the first parameter (ascending).</param>
    /// <param name="vs">Grid positions along the second parameter (ascending).</param>
    /// <param name="values">Sampled values, indexed <c>j * us.Length + i</c>.</param>
    /// <param name="map">Maps a parameter pair to its on-screen point.</param>
    /// <param name="output">Receives the outlines as closed figures.</param>
    /// <returns>False when the area is empty.</returns>
    public static bool Trace(double[] us, double[] vs, double[] values, Func<double, double, Point> map, StreamGeometryContext output)
    {
        int nu = us.Length + 2, nv = vs.Length + 2;
        var u = new double[nu];
        var v = new double[nv];
        var f = new double[nu * nv];
        Array.Copy(us, 0, u, 1, us.Length);
        Array.Copy(vs, 0, v, 1, vs.Length);
        u[0] = us[0];
        u[nu - 1] = us[^1];
        v[0] = vs[0];
        v[nv - 1] = vs[^1];
        bool any = false;
        for (int j = 0; j < nv; j++)
        {
            for (int i = 0; i < nu; i++)
            {
                bool border = i == 0 || j == 0 || i == nu - 1 || j == nv - 1;
                double value = border ? -1 : values[(j - 1) * us.Length + (i - 1)];
                f[j * nu + i] = value;
                any |= value >= 0;
            }
        }

        if (!any)
        {
            return false;
        }

        // Edge keys: horizontal edge (i,j)-(i+1,j) = 2·(j·nu+i), vertical (i,j)-(i,j+1) = +1.
        int keys = nu * nv * 2;
        var link0 = new int[keys];
        var link1 = new int[keys];
        Array.Fill(link0, -1);
        Array.Fill(link1, -1);

        void Link(int a, int b)
        {
            if (link0[a] < 0) link0[a] = b; else link1[a] = b;
            if (link0[b] < 0) link0[b] = a; else link1[b] = a;
        }

        for (int j = 0; j < nv - 1; j++)
        {
            for (int i = 0; i < nu - 1; i++)
            {
                double a = f[j * nu + i], b = f[j * nu + i + 1], c = f[(j + 1) * nu + i + 1], d = f[(j + 1) * nu + i];
                int index = (a >= 0 ? 1 : 0) | (b >= 0 ? 2 : 0) | (c >= 0 ? 4 : 0) | (d >= 0 ? 8 : 0);
                if (index is 0 or 15)
                {
                    continue;
                }

                int e0 = 2 * (j * nu + i), e1 = 2 * (j * nu + i + 1) + 1, e2 = 2 * ((j + 1) * nu + i), e3 = 2 * (j * nu + i) + 1;
                bool centreIn = a + b + c + d >= 0;
                switch (index)
                {
                    case 1: case 14: Link(e3, e0); break;
                    case 2: case 13: Link(e0, e1); break;
                    case 3: case 12: Link(e3, e1); break;
                    case 4: case 11: Link(e1, e2); break;
                    case 6: case 9: Link(e0, e2); break;
                    case 7: case 8: Link(e3, e2); break;
                    case 5:
                        if (centreIn) { Link(e0, e1); Link(e2, e3); } else { Link(e3, e0); Link(e1, e2); }
                        break;
                    case 10:
                        if (centreIn) { Link(e3, e0); Link(e1, e2); } else { Link(e0, e1); Link(e2, e3); }
                        break;
                }
            }
        }

        Point PointOf(int key)
        {
            int node = key >> 1;
            int i = node % nu, j = node / nu;
            if ((key & 1) == 0)
            {
                double fa = f[j * nu + i], fb = f[j * nu + i + 1];
                double t = fa / (fa - fb);
                return map(u[i] + (u[i + 1] - u[i]) * t, v[j]);
            }
            else
            {
                double fa = f[j * nu + i], fb = f[(j + 1) * nu + i];
                double t = fa / (fa - fb);
                return map(u[i], v[j] + (v[j + 1] - v[j]) * t);
            }
        }

        var visited = new bool[keys];
        bool drew = false;
        for (int start = 0; start < keys; start++)
        {
            if (link0[start] < 0 || visited[start])
            {
                continue;
            }

            output.BeginFigure(PointOf(start), true);
            visited[start] = true;
            int current = start;
            while (true)
            {
                int next = !visited[link0[current]] ? link0[current]
                    : link1[current] >= 0 && !visited[link1[current]] ? link1[current]
                    : -1;
                if (next < 0)
                {
                    break;
                }

                visited[next] = true;
                output.LineTo(PointOf(next));
                current = next;
            }

            output.EndFigure(true);
            drew = true;
        }

        return drew;
    }
}
