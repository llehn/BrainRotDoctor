#if DEBUG
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Globalization;
using System.IO;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// Dev-only (DEBUG builds) tools for the worm scene.
/// <c>--scene-frames &lt;file.png&gt; [--times t1,t2,…]</c> renders the scene at the
/// given moments into one PNG, two frames per row at twice the real size, over the
/// design page's background, to compare it with the approved design frame by frame;
/// it also measures the cost of a frame.
/// <c>--scene-clip &lt;folder&gt;</c> renders every frame of the scene (30 a second,
/// twice the real size, transparent) as numbered PNGs, for the website's clip.
/// <c>--scene-preview [count]</c> plays the real overlay on the main screen.
/// </summary>
internal static class SceneFrames
{
    public static void Run(string path, string? times)
    {
        AppBuilder.Configure<Application>().UsePlatformDetect().SetupWithoutStarting();
        double[] moments = (times ?? "0.5,1.0,1.6,2.0,2.4,2.8,3.1,3.3,3.7,2.95")
            .Split(',')
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
            .ToArray();

        var scene = new WormScene(new ExtractionScript());
        var grid = new UniformGrid { Columns = 2, Background = new SolidColorBrush(Color.Parse("#ECEAF2")) };
        foreach (double t in moments)
        {
            grid.Children.Add(new View(scene) { Time = t });
        }

        int rows = (moments.Length + 1) / 2;
        var size = new Size(SceneView.Width * 2, SceneView.Height * rows);
        grid.Measure(size);
        grid.Arrange(new Rect(size));
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width * 2, (int)size.Height * 2), new Vector(192, 192));
        bitmap.Render(grid);
        bitmap.Save(path);

        // Frame cost over the whole scene at 60 frames a second, drawn at 2× (a 200% display).
        var view = new View(scene);
        view.Measure(new Size(SceneView.Width, SceneView.Height));
        view.Arrange(new Rect(0, 0, SceneView.Width, SceneView.Height));
        using var frame = new RenderTargetBitmap(new PixelSize(720, 440), new Vector(192, 192));
        var costs = new List<double>();
        for (int i = 0; i < 240; i++)
        {
            view.Time = i / 60.0;
            var one = System.Diagnostics.Stopwatch.StartNew();
            frame.Render(view);
            costs.Add(one.Elapsed.TotalMilliseconds);
        }

        costs.Sort();
        Console.WriteLine($"frame cost: median {costs[120]:F1} ms, 95% {costs[228]:F1} ms, worst {costs[^1]:F1} ms");
    }

    public static void Clip(string dir)
    {
        AppBuilder.Configure<Application>().UsePlatformDetect().SetupWithoutStarting();
        Directory.CreateDirectory(dir);
        var scene = new WormScene(new ExtractionScript());
        const int fps = 30;
        int count = (int)Math.Round(scene.Script.Length * fps);
        for (int i = 0; i < count; i++)
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)SceneView.Width * 2, (int)SceneView.Height * 2), new Vector(192, 192));
            using (DrawingContext dc = bitmap.CreateDrawingContext())
            {
                scene.Draw(dc, (double)i / fps);
            }

            bitmap.Save(Path.Combine(dir, $"frame{i:D3}.png"));
        }
    }

    /// <summary>
    /// Plays the real overlay <paramref name="times"/> times in a row on the main screen,
    /// reporting when each pop happens, how evenly frames arrive, and whether clicks
    /// pass through the overlay.
    /// </summary>
    public static void Preview(int times, string[] args) =>
        AppBuilder.Configure(() => new PreviewApp(times)).UsePlatformDetect().StartWithClassicDesktopLifetime(args);

    private sealed class View : Control
    {
        private readonly WormScene _scene;

        public View(WormScene scene)
        {
            _scene = scene;
            Width = SceneView.Width;
            Height = SceneView.Height;
        }

        public double Time { get; set; }

        public override void Render(DrawingContext context) => _scene.Draw(context, Time);
    }

    private sealed class PreviewApp : Application
    {
        private readonly int _times;
        private readonly WormScene _scene = new(new ExtractionScript());
        private int _played;

        public PreviewApp(int times) => _times = times;

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                _scene.Prepare();
                PlopSound.Prepare();
                PlayNext();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void PlayNext()
        {
            if (_played++ >= _times)
            {
                Environment.Exit(0);
            }

            var overlay = new SceneOverlay(_scene, ExtractionScript.PopAt, null);
            var shown = System.Diagnostics.Stopwatch.StartNew();
            overlay.Popped += (_, _) =>
            {
                PlopSound.Play();
                Console.WriteLine($"pop at {shown.Elapsed.TotalSeconds:F3} s after start");
                var probe = new Runtime.NativeMethods.POINT { X = overlay.Position.X + 40, Y = overlay.Position.Y + 40 };
                Console.WriteLine($"clicks pass through: {Runtime.NativeMethods.WindowFromPoint(probe) != overlay.Handle}");
            };
            overlay.Finished += (_, _) =>
            {
                Console.WriteLine($"finished at {shown.Elapsed.TotalSeconds:F3} s, {overlay.Frames} frames, longest gap {overlay.LongestGap:F1} ms");
                Avalonia.Threading.Dispatcher.UIThread.Post(PlayNext);
            };
            overlay.Start();
        }
    }
}
#endif
