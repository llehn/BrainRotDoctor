namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// A worm scene about to play because blocked tabs or apps are due to close: its id (to
/// report the pop and the end) and windows of what closes on the pop, to play on their screen.
/// </summary>
internal sealed record BlockScene(long Id, IReadOnlyList<IntPtr> Windows);

/// <summary>
/// An open app a block reaches soon: it closes at <paramref name="At"/>, because of the
/// rule named <paramref name="Rule"/>. <paramref name="Window"/> is one of its windows, to
/// show the heads-up on its screen.
/// </summary>
internal sealed record HeadsUp(string App, DateTimeOffset At, string Rule, IntPtr Window);
