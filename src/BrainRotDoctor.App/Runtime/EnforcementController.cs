using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Apps;
using BrainRotDoctor.Core.Configuration;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

internal sealed class EnforcementController : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ActivitySaveInterval = TimeSpan.FromSeconds(30);

    // If the scene never reports its pop (it failed to show), the tabs close anyway
    // after this long; if it never reports its end, a new scene may start after this.
    private static readonly TimeSpan PopOverdue = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SceneOverdue = TimeSpan.FromSeconds(10);

    // An app that couldn't be closed (administrator rights, a Store app's frame) isn't tried again for this long.
    private static readonly TimeSpan RetryRefused = TimeSpan.FromMinutes(1);

    private BudgetEngine _engine;
    private readonly IBrowserObserver _observer;
    private readonly IBrowserTabCloser _tabCloser;
    private readonly IAppObserver? _appObserver;
    private readonly IAppCloser? _appCloser;
    private string _configSource;
    private string _configurationJson;
    private string? _configurationFilePath;
    private readonly StrictModeStore _strictModeStore;
    private readonly UsageStore _usageStore;
    private readonly PauseStore _pauseStore;
    private readonly ActivityStore _activityStore;
    private readonly RuleBaselineStore _ruleBaseline;
    private readonly ActivityTally _activity = new();
    private DateTimeOffset _activitySavedAt;
    private PauseState? _pause;
    private readonly string? _logPath;
    private readonly System.Threading.Timer _timer;
    private readonly object _sync = new();

    // Serialises everything that talks to the browsers, the app observer or the engine:
    // the 1-second tick and a scene's pop run on different threads.
    private readonly object _browserLock = new();

    // When open apps close, and per app (by key) until when a refused close isn't tried again.
    private readonly BlockCountdown _countdown = new();
    private readonly Dictionary<string, DateTimeOffset> _appRefusedUntil = new(StringComparer.Ordinal);
    private readonly HashSet<string> _appsClosing = new(StringComparer.Ordinal);
    private IReadOnlyList<ObservedApp> _apps = Array.Empty<ObservedApp>();
    private IReadOnlyList<HeadsUp> _headsUps = Array.Empty<HeadsUp>();
    private readonly List<CloseEvent> _recentClosures = new();
    private PendingScene? _scene;
    private long _sceneIds;
    private AppStatus _status;
    private bool _isRunning;
    private bool _isTicking;

    public EnforcementController(
        BlockerConfiguration configuration,
        IBrowserObserver observer,
        IBrowserTabCloser tabCloser,
        string configSource,
        string configurationJson,
        string? configurationFilePath,
        StrictModeStore strictModeStore,
        string? logPath = null,
        UsageStore? usageStore = null,
        PauseStore? pauseStore = null,
        ActivityStore? activityStore = null,
        RuleBaselineStore? ruleBaseline = null,
        IAppObserver? appObserver = null,
        IAppCloser? appCloser = null)
    {
        _appObserver = appObserver;
        _appCloser = appCloser;
        _ruleBaseline = ruleBaseline ?? new RuleBaselineStore();
        ArgumentNullException.ThrowIfNull(configuration);
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        _tabCloser = tabCloser ?? throw new ArgumentNullException(nameof(tabCloser));
        _configSource = configSource;
        _configurationJson = configurationJson;
        _configurationFilePath = configurationFilePath;
        _strictModeStore = strictModeStore;
        _usageStore = usageStore ?? new UsageStore();
        _pauseStore = pauseStore ?? new PauseStore();
        _pause = _strictModeStore.GetSnapshot().IsActive ? null : _pauseStore.Load(DateTimeOffset.UtcNow);
        _logPath = logPath;
        _engine = new BudgetEngine(configuration);
        // Pick up the hour's usage left by a previous run / save / update swap so a
        // restart can't be used to wipe a limit. A record from a past hour is
        // ignored automatically on the first tick.
        _engine.RestoreUsage(_usageStore.Load());
        _activityStore = activityStore ?? new ActivityStore();
        _activity.Restore(_activityStore.Load(), DateTimeOffset.Now);
        _activitySavedAt = DateTimeOffset.Now;
        _timer = new System.Threading.Timer(Tick);
        _status = BuildStatus(
            DateTimeOffset.Now,
            Array.Empty<ObservedBrowserWindow>(),
            _engine.GetRuleSnapshots(DateTimeOffset.Now),
            lastError: null);
    }

    public event EventHandler<AppStatus>? StatusChanged;

    /// <summary>
    /// Raised on the enforcement thread when blocked tabs are due to close. The handler
    /// plays the worm scene, calls <see cref="ClosePendingTabs"/> on its pop and
    /// <see cref="EndScene"/> when it is over. Without a handler, tabs close at once.
    /// </summary>
    public event EventHandler<BlockScene>? BlockSceneStarting;

    public AppStatus Status
    {
        get
        {
            lock (_sync)
            {
                return _status;
            }
        }
    }

    /// <summary>Turns strict mode on for <paramref name="duration"/>; ends any pause.</summary>
    public StrictModeSnapshot ActivateStrictMode(TimeSpan duration)
    {
        StrictModeSnapshot snapshot = _strictModeStore.Activate(duration);
        SetPause(null);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
        return snapshot;
    }

    /// <summary>
    /// Stops blocking and allowance counting for <paramref name="duration"/>, at most
    /// <see cref="PauseState.Longest"/>. Refused while strict mode is active.
    /// </summary>
    public bool Pause(TimeSpan duration)
    {
        if (_strictModeStore.GetSnapshot().IsActive || duration <= TimeSpan.Zero)
        {
            return false;
        }

        TimeSpan length = duration < PauseState.Longest ? duration : PauseState.Longest;
        SetPause(new PauseState(DateTimeOffset.UtcNow + length));
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
        return true;
    }

    public void Resume()
    {
        SetPause(null);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
    }

    /// <summary>
    /// Apps marked as okay (by <see cref="AppIdentity.Key"/>): like okay sites, their time
    /// is recorded only as the one shared total.
    /// </summary>
    public void SetUntrackedApps(IEnumerable<string> keys)
    {
        lock (_sync)
        {
            _activity.SetUntrackedApps(keys);
        }

        PersistActivity(DateTimeOffset.Now);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, Status.Rules, Status.LastError));
    }

    public EditableConfiguration GetEditableConfiguration() =>
        EditableConfiguration.FromJson(_configurationJson);

    /// <summary>
    /// Sites marked as okay: their time is recorded only as one shared total, and
    /// what today's statistics already hold for them joins it at once.
    /// </summary>
    public void SetUntrackedSites(IEnumerable<string> hosts)
    {
        lock (_sync)
        {
            _activity.SetUntracked(hosts);
        }

        PersistActivity(DateTimeOffset.Now);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, Status.Rules, Status.LastError));
    }

    /// <summary>
    /// Validates and saves <paramref name="editable"/>. While strict mode is on, a
    /// save is refused when it changes, switches off or removes an existing rule.
    /// </summary>
    public bool TrySaveConfiguration(EditableConfiguration editable, out string? error)
    {
        error = null;
        if (_strictModeStore.GetSnapshot().IsActive
            && RuleGuard.Loosened(GetEditableConfiguration(), editable) is { Count: > 0 } loosened)
        {
            error = $"Locked by strict mode: {string.Join(", ", loosened)}";
            return false;
        }

        string json;
        BrainRotDoctor.Core.Configuration.BlockerConfiguration configuration;
        try
        {
            json = editable.ToJson();
            configuration = BrainRotDoctor.Core.Configuration.ConfigurationLoader.Load(json);
        }
        catch (BrainRotDoctor.Core.Configuration.ConfigurationException ex)
        {
            error = ex.Message;
            return false;
        }

        if (string.IsNullOrWhiteSpace(_configurationFilePath))
        {
            error = "No writable configuration file is available.";
            return false;
        }

        try
        {
            File.WriteAllText(_configurationFilePath, json);
        }
        catch (IOException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            error = ex.Message;
            return false;
        }

        lock (_sync)
        {
            // Carry the hour's usage into the rebuilt engine so saving a rule —
            // even with no real change — can't reset the time already spent.
            IReadOnlyList<RuleUsage> carried = _engine.ExportUsage();
            var rebuilt = new BudgetEngine(configuration);
            rebuilt.RestoreUsage(carried);
            _engine = rebuilt;
            _configurationJson = json;
            _configSource = _configurationFilePath;
        }

        _ruleBaseline.Save(json);
        PersistUsage();
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
        return true;
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
        }

        _timer.Change(TimeSpan.Zero, PollInterval);
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
        }

        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        ClearHeadsUps();
        PersistUsage();
        PersistActivity(DateTimeOffset.Now);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, Status.Rules, Status.LastError));
    }

    public void Dispose() => _timer.Dispose();

    private void Tick(object? state)
    {
        lock (_sync)
        {
            if (!_isRunning || _isTicking)
            {
                return;
            }

            _isTicking = true;
        }

        try
        {
            DateTimeOffset now = DateTimeOffset.Now;
            if (_pause is { } pause && !pause.IsActiveAt(now))
            {
                SetPause(null);
            }

            IReadOnlyList<ObservedBrowserWindow> windows;
            IReadOnlyList<ObservedApp> apps;
            TickResult result;
            lock (_browserLock)
            {
                windows = _observer.GetSelectedTabs();
                apps = _appObserver?.GetApps() ?? Array.Empty<ObservedApp>();
                WriteLog(now, $"observed {windows.Count} window(s): {string.Join(" || ", windows.Select(Describe))}");

                // While paused the engine still ticks (so hours roll over and the
                // clock stays current) but sees no tabs or apps: nothing is charged or closed.
                PauseState? paused = CurrentPause(now);
                List<ObservedApp> counting = apps.Where(a => a.Counts).ToList();
                result = _engine.Tick(
                    paused is not null
                        ? Array.Empty<BrowserWindowState>()
                        : windows.Select(w => new BrowserWindowState(w.WindowId, w.Url)).ToArray(),
                    paused is not null ? Array.Empty<AppIdentity>() : counting.Select(a => a.Identity).ToArray(),
                    now);

                IReadOnlyList<AppTarget> dueApps = DueApps(now, counting, result, paused);
                lock (_sync)
                {
                    _apps = apps;
                }

                HandleCloseDecisions(now, windows, result.CloseDecisions, dueApps);
            }

            RecordActivity(now, windows, apps);

            // Only an active (charged) tick changes the numbers, so persist then —
            // not every idle second. The hour's usage thus survives a later restart.
            if (result.Rules.Any(rule => rule.WasActiveThisTick))
            {
                PersistUsage();
            }

            Publish(BuildStatus(now, windows, result.Rules, lastError: null));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            ClearHeadsUps();
            Publish(BuildStatus(DateTimeOffset.Now, Array.Empty<ObservedBrowserWindow>(), _engine.GetRuleSnapshots(DateTimeOffset.Now), ex.Message));
            WriteLog(DateTimeOffset.Now, $"error {ex}");
            _timer.Change(ErrorBackoff, PollInterval);
        }
        finally
        {
            lock (_sync)
            {
                _isTicking = false;
            }
        }
    }

    /// <summary>
    /// A blocked tab does not close at once: the worm scene starts, and the tab closes
    /// on its pop (<see cref="ClosePendingTabs"/>). While a scene plays, further
    /// decisions wait; the engine repeats them every tick for as long as a blocked page
    /// stays open, so a tab still open after the scene starts the next one.
    /// </summary>
    private void HandleCloseDecisions(DateTimeOffset now, IReadOnlyList<ObservedBrowserWindow> windows, IReadOnlyList<CloseDecision> decisions, IReadOnlyList<AppTarget> apps)
    {
        PendingScene? overdue = null;
        lock (_sync)
        {
            if (_scene is { } scene)
            {
                if (!scene.PopReached && now - scene.StartedAt > PopOverdue)
                {
                    scene.PopReached = true;
                    overdue = scene;
                }
                else if (now - scene.StartedAt > SceneOverdue)
                {
                    _scene = null;
                }
            }
        }

        if (overdue is not null)
        {
            WriteLog(now, $"scene {overdue.Id} never reached its pop; closing now");
            CloseIfStillBlocked(now, overdue.Targets);
            CloseAppsIfStillBlocked(overdue.Apps);
        }

        var targets = decisions
            .Select(d => (Window: windows.FirstOrDefault(w => w.WindowId == d.WindowId), Decision: d))
            .Where(t => t.Window is not null)
            .Select(t => new SceneTarget(t.Window!, t.Decision))
            .ToList();
        if (targets.Count == 0 && apps.Count == 0)
        {
            return;
        }

        if (BlockSceneStarting is null)
        {
            foreach (SceneTarget target in targets)
            {
                Close(now, target);
            }

            CloseAppsIfStillBlocked(apps);
            return;
        }

        PendingScene started;
        lock (_sync)
        {
            if (_scene is not null)
            {
                return;
            }

            started = new PendingScene(++_sceneIds, now, targets, apps);
            _scene = started;
        }

        WriteLog(now, $"scene {started.Id} starts for {string.Join(", ", targets.Select(t => $"{t.Window.BrowserName}:{t.Window.WindowId}:{t.Decision.RuleId}").Concat(apps.Select(a => $"{a.App.Identity.Key}:{a.RuleId}")))}");
        BlockSceneStarting.Invoke(this, new BlockScene(
            started.Id,
            targets.Select(t => t.Window.WindowHandle).Concat(apps.SelectMany(a => a.App.Windows.Take(1))).ToArray()));
    }

    /// <summary>
    /// The apps to close now, and the heads-up for those a block reaches within a minute
    /// (<see cref="BlockCountdown"/> decides). While paused nothing is coming, except a
    /// block the pause ends into within the minute.
    /// </summary>
    private IReadOnlyList<AppTarget> DueApps(DateTimeOffset now, IReadOnlyList<ObservedApp> counting, TickResult result, PauseState? pause)
    {
        var apps = new List<CountingApp>();
        foreach (ObservedApp app in counting)
        {
            string key = app.Identity.Key;
            BlockAhead? ahead = pause is null
                ? _engine.BlockAheadFor(app.Identity, now, BlockCountdown.Lead, counting: true)
                : pause.UntilUtc - now <= BlockCountdown.Lead
                    ? _engine.BlockAheadFor(app.Identity, pause.UntilUtc, TimeSpan.Zero, counting: false)
                    : null;
            bool held = (_appRefusedUntil.TryGetValue(key, out DateTimeOffset refused) && refused > now) || _appsClosing.Contains(key);
            apps.Add(new CountingApp(app.Identity, ahead, held));
        }

        CountdownResult countdown = _countdown.Tick(apps, now);
        var headsUps = new List<HeadsUp>();
        foreach (AppWarning warning in countdown.Warn)
        {
            if (!_headsUps.Any(h => h.App == warning.App.Name))
            {
                WriteLog(now, $"heads-up {warning.App.Key}:{warning.RuleId} closes at {warning.At:HH:mm:ss}");
            }

            string rule = result.Rules.FirstOrDefault(r => r.RuleId == warning.RuleId)?.Name ?? "";
            IntPtr window = counting.First(a => a.Identity.Key == warning.App.Key).Windows.FirstOrDefault();
            headsUps.Add(new HeadsUp(warning.App.Name, warning.At, rule, window));
        }

        lock (_sync)
        {
            _headsUps = headsUps;
        }

        return countdown.Close
            .Select(c => new AppTarget(counting.First(a => a.Identity.Key == c.App.Key), c.RuleId))
            .ToList();
    }

    /// <summary>
    /// Closes the apps a scene was started for, each only if it still counts and is still
    /// blocked; one that's gone or allowed by now is left alone. Runs off the tick: a
    /// close may wait a few seconds for an app to go.
    /// </summary>
    private void CloseAppsIfStillBlocked(IReadOnlyList<AppTarget> targets)
    {
        if (targets.Count == 0 || _appCloser is not { } closer || _appObserver is not { } observer)
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                List<ObservedApp> closing;
                DateTimeOffset now = DateTimeOffset.Now;
                var keys = targets.Select(t => t.App.Identity.Key).ToHashSet(StringComparer.Ordinal);
                lock (_browserLock)
                {
                    closing = IsPaused(now)
                        ? new List<ObservedApp>()
                        : observer.GetApps()
                            .Where(a => keys.Contains(a.Identity.Key) && !_appsClosing.Contains(a.Identity.Key)
                                && a.Counts && _engine.BlockingRuleFor(a.Identity, now) is not null)
                            .ToList();
                    _appsClosing.UnionWith(closing.Select(a => a.Identity.Key));
                }

                foreach (ObservedApp app in closing)
                {
                    AppCloseResult closed = AppCloseResult.Closed;
                    try
                    {
                        closed = closer.Close(app);
                    }
                    finally
                    {
                        lock (_browserLock)
                        {
                            _appsClosing.Remove(app.Identity.Key);
                            if (closed is AppCloseResult.Denied or AppCloseResult.NotClosed)
                            {
                                _appRefusedUntil[app.Identity.Key] = DateTimeOffset.Now + RetryRefused;
                            }
                        }
                    }

                    WriteLog(DateTimeOffset.Now, $"app {closed.ToString().ToLowerInvariant()} {app.Identity.Key}");
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                WriteLog(DateTimeOffset.Now, $"error closing apps {ex}");
            }
        });
    }

    /// <summary>
    /// The scene's pop: closes the tabs it was started for, each only if that window
    /// still shows a blocked page in front. A tab the user has already left is never
    /// closed, and no other tab is closed instead. Safe to call from any thread.
    /// </summary>
    public void ClosePendingTabs(long sceneId)
    {
        PendingScene? scene;
        lock (_sync)
        {
            scene = _scene is { Id: var id } current && id == sceneId && !current.PopReached ? current : null;
            if (scene is not null)
            {
                scene.PopReached = true;
            }
        }

        if (scene is not null)
        {
            ThreadPool.QueueUserWorkItem(_ => CloseIfStillBlocked(DateTimeOffset.Now, scene.Targets));
            CloseAppsIfStillBlocked(scene.Apps);
        }
    }

    /// <summary>The scene is over; the next blocked tab may start a new one.</summary>
    public void EndScene(long sceneId)
    {
        lock (_sync)
        {
            if (_scene?.Id == sceneId)
            {
                _scene = null;
            }
        }
    }

    private void CloseIfStillBlocked(DateTimeOffset now, IReadOnlyList<SceneTarget> targets)
    {
        try
        {
            lock (_browserLock)
            {
                foreach (SceneTarget target in targets)
                {
                    Uri? url = IsPaused(now) ? null : _observer.ReadSelectedUrl(target.Window.WindowHandle);
                    string? ruleId = url is null ? null : _engine.BlockingRuleFor(url, now);
                    if (ruleId is null)
                    {
                        WriteLog(now, $"left before the pop {target.Window.BrowserName}:{target.Window.WindowId}:{url?.AbsoluteUri ?? "(null)"}");
                        continue;
                    }

                    Close(now, target with { Decision = new CloseDecision(target.Window.WindowId, url!, ruleId) });
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            WriteLog(DateTimeOffset.Now, $"error at the pop {ex}");
        }
    }

    private void Close(DateTimeOffset now, SceneTarget target)
    {
        ObservedBrowserWindow window = target.Window;
        CloseDecision decision = target.Decision;
        if (_tabCloser.CloseSelectedTab(window.WindowHandle))
        {
            WriteLog(now, $"closed {window.BrowserName}:{window.WindowId}:{decision.Url.AbsoluteUri}:{decision.RuleId}");
            AddClosure(now, window, decision);
        }
        else
        {
            WriteLog(now, $"close failed {window.BrowserName}:{window.WindowId}:{decision.Url.AbsoluteUri}:{decision.RuleId}");
        }
    }

    private void AddClosure(DateTimeOffset now, ObservedBrowserWindow window, CloseDecision decision)
    {
        var closure = new CloseEvent(now, window.BrowserName, decision.Url, decision.RuleId);
        lock (_sync)
        {
            _recentClosures.Insert(0, closure);
            if (_recentClosures.Count > 12)
            {
                _recentClosures.RemoveRange(12, _recentClosures.Count - 12);
            }
        }
    }

    private sealed record SceneTarget(ObservedBrowserWindow Window, CloseDecision Decision);

    private sealed record AppTarget(ObservedApp App, string RuleId);

    private sealed class PendingScene
    {
        public PendingScene(long id, DateTimeOffset startedAt, IReadOnlyList<SceneTarget> targets, IReadOnlyList<AppTarget> apps)
        {
            Id = id;
            StartedAt = startedAt;
            Targets = targets;
            Apps = apps;
        }

        public long Id { get; }

        public DateTimeOffset StartedAt { get; }

        public IReadOnlyList<SceneTarget> Targets { get; }

        public IReadOnlyList<AppTarget> Apps { get; }

        public bool PopReached { get; set; }
    }

    private AppStatus BuildStatus(
        DateTimeOffset now,
        IReadOnlyList<ObservedBrowserWindow> windows,
        IReadOnlyList<RuleSnapshot> rules,
        string? lastError)
    {
        lock (_sync)
        {
            return new AppStatus(
                now,
                _isRunning,
                _configSource,
                windows,
                rules,
                _recentClosures.ToArray(),
                _strictModeStore.GetSnapshot(),
                _pause,
                lastError,
                _activity.Snapshot(),
                _apps,
                _headsUps);
        }
    }

    /// <summary>No heads-up stays on screen while nothing counts it down.</summary>
    private void ClearHeadsUps()
    {
        lock (_sync)
        {
            _headsUps = Array.Empty<HeadsUp>();
        }
    }

    private PauseState? CurrentPause(DateTimeOffset now)
    {
        lock (_sync)
        {
            return _pause?.IsActiveAt(now) == true ? _pause : null;
        }
    }

    private bool IsPaused(DateTimeOffset now)
    {
        lock (_sync)
        {
            return _pause?.IsActiveAt(now) == true;
        }
    }

    private void SetPause(PauseState? pause)
    {
        lock (_sync)
        {
            _pause = pause;
        }

        _pauseStore.Save(pause);
    }

    private void PersistUsage() => _usageStore.Save(_engine.ExportUsage());

    /// <summary>
    /// Counts the page or app in front toward today's statistics — also while paused,
    /// since they describe what you did, not what was enforced — and stores them now and then.
    /// A private window is never counted: Status is a diary, and private browsing
    /// is the user saying this visit did not happen. Blocking still sees that window.
    /// </summary>
    private void RecordActivity(DateTimeOffset now, IReadOnlyList<ObservedBrowserWindow> windows, IReadOnlyList<ObservedApp> apps)
    {
        ObservedBrowserWindow? front = windows.FirstOrDefault(w => w.IsForeground);
        Uri? url = front is { IsPrivate: false } ? front.Url : null;
        AppIdentity? app = front is null ? apps.FirstOrDefault(a => a.IsForeground)?.Identity : null;
        lock (_sync)
        {
            _activity.Tick(url, app, now);
            if (_pause?.IsActiveAt(now) == true)
            {
                _activity.MarkPaused(now);
            }

            if (_strictModeStore.GetSnapshot().IsActive)
            {
                _activity.MarkStrict(now);
            }
        }

        if (now - _activitySavedAt >= ActivitySaveInterval)
        {
            PersistActivity(now);
        }
    }

    private void PersistActivity(DateTimeOffset now)
    {
        ActivityRecord record;
        lock (_sync)
        {
            record = _activity.Snapshot();
            _activitySavedAt = now;
        }

        _activityStore.Save(record);
    }

    private void Publish(AppStatus status)
    {
        lock (_sync)
        {
            _status = status;
        }

        StatusChanged?.Invoke(this, status);
    }

    private static string Describe(ObservedBrowserWindow window) =>
        window.IsPrivate
            ? $"{window.BrowserName}:{window.WindowId}:(private)"
            : $"{window.BrowserName}:{window.WindowId}:{window.Url?.AbsoluteUri ?? "(null)"}";

    private void WriteLog(DateTimeOffset now, string message)
    {
        if (string.IsNullOrWhiteSpace(_logPath))
        {
            return;
        }

        try
        {
            File.AppendAllText(_logPath, $"{now:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
