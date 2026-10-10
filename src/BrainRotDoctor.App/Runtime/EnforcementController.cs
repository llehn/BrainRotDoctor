using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Configuration;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

internal sealed class EnforcementController : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(2);

    // If the scene never reports its pop (it failed to show), the tabs close anyway
    // after this long; if it never reports its end, a new scene may start after this.
    private static readonly TimeSpan PopOverdue = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SceneOverdue = TimeSpan.FromSeconds(10);

    private BudgetEngine _engine;
    private readonly IBrowserObserver _observer;
    private readonly IBrowserTabCloser _tabCloser;
    private string _configSource;
    private string _configurationJson;
    private string? _configurationFilePath;
    private readonly StrictModeStore _strictModeStore;
    private readonly UsageStore _usageStore;
    private readonly PauseStore _pauseStore;
    private PauseState? _pause;
    private readonly string? _logPath;
    private readonly System.Threading.Timer _timer;
    private readonly object _sync = new();

    // Serialises everything that talks to the browsers or the engine: the 1-second
    // tick and a scene's pop run on different threads.
    private readonly object _browserLock = new();
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
        PauseStore? pauseStore = null)
    {
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

    /// <summary>Locks the given rules (null = all) for <paramref name="duration"/>; ends any pause.</summary>
    public StrictModeSnapshot ActivateStrictMode(TimeSpan duration, IReadOnlyCollection<string>? lockedRuleIds = null)
    {
        StrictModeSnapshot snapshot = _strictModeStore.Activate(duration, _configurationJson, lockedRuleIds);
        SetPause(null);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
        return snapshot;
    }

    /// <summary>
    /// Stops blocking and allowance counting for <paramref name="duration"/> (null =
    /// until <see cref="Resume"/>). Refused while strict mode is active.
    /// </summary>
    public bool Pause(TimeSpan? duration)
    {
        if (_strictModeStore.GetSnapshot().IsActive)
        {
            return false;
        }

        SetPause(new PauseState(duration is { } d ? DateTimeOffset.UtcNow + d : null));
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
        return true;
    }

    public void Resume()
    {
        SetPause(null);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
    }

    public EditableConfiguration GetEditableConfiguration() =>
        EditableConfiguration.FromJson(_configurationJson);

    /// <summary>
    /// Validates and saves <paramref name="editable"/>. During strict mode a save is
    /// refused only when it changes or removes a locked rule.
    /// </summary>
    public bool TrySaveConfiguration(EditableConfiguration editable, out string? error)
    {
        error = null;
        if (_strictModeStore.FindLockViolations(editable) is { } locked)
        {
            error = $"Locked by strict mode: {string.Join(", ", locked)}";
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
        PersistUsage();
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
            TickResult result;
            lock (_browserLock)
            {
                windows = _observer.GetSelectedTabs();
                WriteLog(now, $"observed {windows.Count} window(s): {string.Join(" || ", windows.Select(w => $"{w.BrowserName}:{w.WindowId}:{w.Url?.AbsoluteUri ?? "(null)"}"))}");

                // While paused the engine still ticks (so hours roll over and the
                // clock stays current) but sees no tabs: nothing is charged or closed.
                result = _engine.Tick(
                    IsPaused(now)
                        ? Array.Empty<BrowserWindowState>()
                        : windows.Select(w => new BrowserWindowState(w.WindowId, w.Url)).ToArray(),
                    now);

                HandleCloseDecisions(now, windows, result.CloseDecisions);
            }

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
    private void HandleCloseDecisions(DateTimeOffset now, IReadOnlyList<ObservedBrowserWindow> windows, IReadOnlyList<CloseDecision> decisions)
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
        }

        var targets = decisions
            .Select(d => (Window: windows.FirstOrDefault(w => w.WindowId == d.WindowId), Decision: d))
            .Where(t => t.Window is not null)
            .Select(t => new SceneTarget(t.Window!, t.Decision))
            .ToList();
        if (targets.Count == 0)
        {
            return;
        }

        if (BlockSceneStarting is null)
        {
            foreach (SceneTarget target in targets)
            {
                Close(now, target);
            }

            return;
        }

        PendingScene started;
        lock (_sync)
        {
            if (_scene is not null)
            {
                return;
            }

            started = new PendingScene(++_sceneIds, now, targets);
            _scene = started;
        }

        WriteLog(now, $"scene {started.Id} starts for {string.Join(", ", targets.Select(t => $"{t.Window.BrowserName}:{t.Window.WindowId}:{t.Decision.RuleId}"))}");
        BlockSceneStarting.Invoke(this, new BlockScene(started.Id, targets.Select(t => t.Window).ToArray()));
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

    private sealed class PendingScene
    {
        public PendingScene(long id, DateTimeOffset startedAt, IReadOnlyList<SceneTarget> targets)
        {
            Id = id;
            StartedAt = startedAt;
            Targets = targets;
        }

        public long Id { get; }

        public DateTimeOffset StartedAt { get; }

        public IReadOnlyList<SceneTarget> Targets { get; }

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
                lastError);
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

    private void Publish(AppStatus status)
    {
        lock (_sync)
        {
            _status = status;
        }

        StatusChanged?.Invoke(this, status);
    }

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
