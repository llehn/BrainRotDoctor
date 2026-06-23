using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.Core.Configuration;
using System.IO;

namespace BrainRotDoctor.App.Runtime;

internal sealed class EnforcementController : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(2);

    private BudgetEngine _engine;
    private readonly IBrowserObserver _observer;
    private readonly IBrowserTabCloser _tabCloser;
    private string _configSource;
    private string _configurationJson;
    private string? _configurationFilePath;
    private readonly StrictModeStore _strictModeStore;
    private readonly string? _logPath;
    private readonly System.Threading.Timer _timer;
    private readonly object _sync = new();
    private readonly List<CloseEvent> _recentClosures = new();
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
        string? logPath = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        _tabCloser = tabCloser ?? throw new ArgumentNullException(nameof(tabCloser));
        _configSource = configSource;
        _configurationJson = configurationJson;
        _configurationFilePath = configurationFilePath;
        _strictModeStore = strictModeStore;
        _logPath = logPath;
        _engine = new BudgetEngine(configuration);
        _timer = new System.Threading.Timer(Tick);
        _status = BuildStatus(
            DateTimeOffset.Now,
            Array.Empty<ObservedBrowserWindow>(),
            _engine.GetRuleSnapshots(DateTimeOffset.Now),
            lastError: null);
    }

    public event EventHandler<AppStatus>? StatusChanged;

    /// <summary>Raised on the enforcement thread immediately after a tab is closed.</summary>
    public event EventHandler<CloseEvent>? TabClosed;

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

    public StrictModeSnapshot ActivateStrictMode(TimeSpan duration)
    {
        StrictModeSnapshot snapshot = _strictModeStore.Activate(duration, _configurationJson);
        Publish(BuildStatus(DateTimeOffset.Now, Status.Windows, _engine.GetRuleSnapshots(DateTimeOffset.Now), Status.LastError));
        return snapshot;
    }

    public EditableConfiguration GetEditableConfiguration() =>
        EditableConfiguration.FromJson(_configurationJson);

    public bool TrySaveConfiguration(EditableConfiguration editable, out string? error)
    {
        error = null;
        if (_strictModeStore.GetSnapshot().IsActive)
        {
            error = "Strict mode is active. Configuration is locked until the commitment ends.";
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
            _engine = new BudgetEngine(configuration);
            _configurationJson = json;
            _configSource = _configurationFilePath;
        }

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
            IReadOnlyList<ObservedBrowserWindow> windows = _observer.GetSelectedTabs();
            WriteLog(now, $"observed {windows.Count} window(s): {string.Join(" || ", windows.Select(w => $"{w.BrowserName}:{w.WindowId}:{w.Url?.AbsoluteUri ?? "(null)"}"))}");
            TickResult result = _engine.Tick(
                windows.Select(w => new BrowserWindowState(w.WindowId, w.Url)).ToArray(),
                now);

            foreach (CloseDecision decision in result.CloseDecisions)
            {
                ObservedBrowserWindow? window = windows.FirstOrDefault(w => w.WindowId == decision.WindowId);
                if (window is null)
                {
                    continue;
                }

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

        TabClosed?.Invoke(this, closure);
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
                lastError);
        }
    }

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
