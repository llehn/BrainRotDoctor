using Avalonia;
using Avalonia.Media;

namespace BrainRotDoctor.App.Ui.Scene;

/// <summary>
/// The worm scene: the brain, the worm and the doctor (the actors), played by a
/// script that poses them at every moment. Drawn back to front: brain, worm, doctor.
/// </summary>
internal sealed class WormScene
{
    private readonly Brain _brain;
    private readonly Worm _worm = new();
    private readonly Doctor _doctor = new();

    /// <param name="script">Who does what, when.</param>
    /// <param name="folds">False draws the brain without its folds (the mark at tray sizes).</param>
    public WormScene(ISceneScript script, bool folds = true)
    {
        Script = script;
        _brain = new Brain(folds);
    }

    public ISceneScript Script { get; }

    /// <summary>Builds the parts that never change shape, ahead of the first play.</summary>
    public void Prepare()
    {
        using var target = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(1, 1));
        using (DrawingContext dc = target.CreateDrawingContext())
        {
            Draw(dc, 2.0);
        }
    }

    /// <summary>Draws the scene at time <paramref name="t"/> into a 360 × 220 DIP area.</summary>
    public void Draw(DrawingContext dc, double t)
    {
        ScenePose pose = Script.PoseAt(t);
        using (dc.PushClip(new Rect(0, 0, SceneView.Width, SceneView.Height)))
        {
            _brain.Draw(dc, pose.Brain);
            _worm.Draw(dc, pose.Worm, Brain.OutsideOf(pose.Brain));
            _doctor.Draw(dc, pose.Doctor);
        }
    }
}
