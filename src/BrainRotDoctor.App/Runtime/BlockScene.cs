namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// A worm scene about to play because blocked tabs are due to close: its id (to report
/// the pop and the end) and the browser windows whose tabs close on the pop.
/// </summary>
internal sealed record BlockScene(long Id, IReadOnlyList<ObservedBrowserWindow> Windows);
