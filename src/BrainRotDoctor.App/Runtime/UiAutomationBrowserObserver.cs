using System.Diagnostics;
using System.IO;
using Interop.UIAutomationClient;

namespace BrainRotDoctor.App.Runtime;

internal sealed class UiAutomationBrowserObserver : IBrowserObserver, IDisposable
{
    internal static readonly IReadOnlyDictionary<string, string> SupportedBrowsers =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "Chrome",
            ["firefox"] = "Firefox",
            ["msedge"] = "Edge",
            ["brave"] = "Brave",
            ["vivaldi"] = "Vivaldi",
            ["opera"] = "Opera",
        };

    // Everything a read needs from an element, fetched in the same call that finds it.
    private static readonly Lazy<IUIAutomationCacheRequest> Fetch = new(() =>
        Uia.CacheRequest(Uia.NameProperty, Uia.ClassNameProperty, Uia.IsOffscreenProperty, Uia.ValueProperty));

    private static readonly Lazy<IUIAutomationCondition> PageCondition = new(() =>
        Uia.Property(Uia.ControlTypeProperty, Uia.DocumentControl));

    private static readonly Lazy<IUIAutomationCondition> OnScreenPageCondition = new(() =>
        Uia.Automation.CreateAndCondition(PageCondition.Value, Uia.Property(Uia.IsOffscreenProperty, false)));

    private static readonly Lazy<IUIAutomationCondition> FirefoxPanelsCondition = new(() =>
        Uia.Property(Uia.AutomationIdProperty, "tabbrowser-tabpanels"));

    private static readonly Lazy<IUIAutomationCondition> FirefoxAddressBarCondition = new(() =>
        Uia.Property(Uia.AutomationIdProperty, "urlbar-input"));

    private static readonly Lazy<IUIAutomationCondition> ChromiumAddressBarCondition = new(() =>
        Uia.Property(Uia.ClassNameProperty, "OmniboxViewViews"));

    private static readonly Lazy<IUIAutomationCondition> ToolBarCondition = new(() =>
        Uia.Property(Uia.ControlTypeProperty, Uia.ToolBarControl));

    private static readonly Lazy<IUIAutomationCondition> ButtonCondition = new(() =>
        Uia.Property(Uia.ControlTypeProperty, Uia.ButtonControl));

    private static readonly Lazy<IUIAutomationCondition> EditCondition = new(() =>
        Uia.Property(Uia.ControlTypeProperty, Uia.EditControl));

    // What is known about each browser window: its address-bar tracker, so text typed
    // into one window's bar never counts as the page it shows, and what never has to
    // be asked again (ADR-016).
    private readonly Dictionary<IntPtr, WindowState> _windows = new();

    public IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs()
    {
        var result = new List<ObservedBrowserWindow>();
        IntPtr foreground = NativeMethods.GetForegroundWindow();
        foreach (IntPtr handle in EnumerateBrowserWindows())
        {
            if (!TryGetBrowserName(handle, out string? browserName))
            {
                continue;
            }

            (Uri? url, bool isPrivate) = TryReadWindow(handle, browserName!, StateOf(handle), handle == foreground);
            result.Add(new ObservedBrowserWindow(
                handle.ToInt64().ToString(),
                handle,
                browserName!,
                url,
                handle == foreground,
                isPrivate));
        }

        foreach (IntPtr gone in _windows.Keys.Where(h => result.All(w => w.WindowHandle != h)).ToArray())
        {
            _windows[gone].Forget();
            _windows.Remove(gone);
        }

        return result;
    }

    /// <summary>Hands everything remembered about every window back to Windows.</summary>
    public void Dispose()
    {
        foreach (WindowState state in _windows.Values)
        {
            state.Forget();
        }

        _windows.Clear();
    }

    public Uri? ReadSelectedUrl(IntPtr windowHandle)
    {
        if (!NativeMethods.IsWindow(windowHandle)
            || !NativeMethods.IsOnScreen(windowHandle)
            || !TryGetBrowserName(windowHandle, out string? browserName))
        {
            return null;
        }

        return TryReadWindow(windowHandle, browserName!, StateOf(windowHandle), windowHandle == NativeMethods.GetForegroundWindow()).Url;
    }

    private WindowState StateOf(IntPtr handle)
    {
        if (!_windows.TryGetValue(handle, out WindowState? state))
        {
            state = new WindowState();
            _windows[handle] = state;
        }

        return state;
    }

    private static IEnumerable<IntPtr> EnumerateBrowserWindows()
    {
        var handles = new List<IntPtr>();
        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (NativeMethods.IsOnScreen(hWnd) && TryGetBrowserName(hWnd, out string? _))
            {
                handles.Add(hWnd);
            }

            return true;
        }, IntPtr.Zero);

        return handles;
    }

    internal static bool TryGetBrowserName(IntPtr handle, out string? browserName)
    {
        browserName = null;
        NativeMethods.GetWindowThreadProcessId(handle, out uint pid);
        if (pid == 0 || NativeMethods.ProcessImage(pid) is not { } image)
        {
            return false;
        }

        browserName = BrowserName(pid, image);
        return browserName is not null;
    }

    /// <summary>The browser process <paramref name="pid"/> is, or null; <paramref name="image"/> is its program file and start time.</summary>
    internal static string? BrowserName(uint pid, (string Path, long Started) image)
    {
        if (!BrowserProcesses.TryGetValue(pid, out (long Started, string? Browser) known) || known.Started != image.Started)
        {
            known = (image.Started, BrowserOf(pid, image.Path));
            BrowserProcesses[pid] = known;
        }

        return known.Browser;
    }

    // Which browser each process is, by id and start time (a reused id is a new process).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<uint, (long Started, string? Browser)> BrowserProcesses = new();

    /// <summary>
    /// The browser a process is, by its program file. When Firefox updated itself while
    /// running, its file was renamed ("moz…"); then the file it was started from says.
    /// Only then is the process looked into: reading other programs, such as games under
    /// anti-cheat, is never needed.
    /// </summary>
    private static string? BrowserOf(uint pid, string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (SupportedBrowsers.TryGetValue(name, out string? browser))
        {
            return browser;
        }

        if (!name.StartsWith("moz", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById((int)pid);
            string? started = process.MainModule?.FileName;
            return started is not null && SupportedBrowsers.TryGetValue(Path.GetFileNameWithoutExtension(started), out browser) ? browser : null;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// One read of one window (ADR-016): the address of the page in its selected tab,
    /// and whether it is private. Only the browser's own frame is looked at, never the
    /// page, and where the page sits is remembered so the next read goes straight there.
    /// Everything found is handed back to Windows when the read ends (ADR-019).
    /// </summary>
    private static (Uri? Url, bool IsPrivate) TryReadWindow(IntPtr handle, string browserName, WindowState state, bool isForeground)
    {
        string title = NativeMethods.GetWindowTitle(handle);
        using var scope = new UiaScope();
        try
        {
            IUIAutomationElement root = state.Root ??= Uia.Automation.ElementFromHandle(handle);
            bool isFirefox = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase);

            // The browser reports the loaded page's own address, which is never typed text.
            IUIAutomationElement? page = FindPage(scope, root, state, browserName, title);
            if (page?.GetCachedPropertyValueEx(Uia.ValueProperty, 1) is string address
                && !string.IsNullOrWhiteSpace(address))
            {
                // A non-web page (new tab, settings) is known but matches no rule.
                UrlNormalizer.TryNormalize(address, out Uri? url);
                return (state.Tracker.Confirm(url), IsPrivateWindow(scope, title, root, state, browserName, frameReady: true));
            }

            // Otherwise fall back to the address bar, guarded against unsent typing.
            AddressBarReading? bar = isFirefox ? TryReadFirefoxAddressBar(scope, root, state) : TryReadChromiumAddressBar(scope, root, state);
            bool isPrivate = IsPrivateWindow(scope, title, root, state, browserName, frameReady: page is not null || bar is not null);
            return (state.Tracker.Resolve(bar?.Text, bar?.HasFocus == true, isForeground, title), isPrivate);
        }
        catch (Exception ex) when (Uia.IsFailure(ex))
        {
            state.Forget();
            return (null, state.FrameSaysPrivate == true || PrivateBrowsing.LooksPrivate(title));
        }
    }

    /// <summary>
    /// The page document of the selected tab, its address fetched with it: first where
    /// it was last time, while that is still the selected tab; else looked up again,
    /// by a search that stops at the page before reaching its contents. A browser
    /// update that moves the page is thus found again, not missed. Only browsers whose
    /// page reports its own address are read this way; the others go by the address bar.
    /// </summary>
    private static IUIAutomationElement? FindPage(UiaScope scope, IUIAutomationElement root, WindowState state, string browserName, string title)
    {
        bool isFirefox = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase);
        if (!isFirefox && !browserName.Equals("Chrome", StringComparison.OrdinalIgnoreCase) && !browserName.Equals("Edge", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (state.PageHolder is { } holder
            && (!isFirefox || state.SelectedPanel is { } panel && IsSelectedPanel(scope.Own(panel.BuildUpdatedCache(Fetch.Value))!))
            && scope.Own(holder.FindFirstBuildCache(TreeScope.TreeScope_Children, PageCondition.Value, Fetch.Value)) is { } known
            && ShowsTitle(known, title))
        {
            return known;
        }

        state.PageHolder = null;
        state.SelectedPanel = null;
        IUIAutomationElement? page;
        if (isFirefox)
        {
            // Every tab has a panel under one known element; the selected tab's is
            // marked, even while the window is covered (then all report off screen).
            state.Panels ??= root.FindFirst(TreeScope.TreeScope_Children, FirefoxPanelsCondition.Value);
            IReadOnlyList<IUIAutomationElement> panels = state.Panels is { } all
                ? scope.All(all.FindAllBuildCache(TreeScope.TreeScope_Children, Uia.TrueCondition, Fetch.Value))
                : [];
            state.SelectedPanel = scope.Keep(panels.FirstOrDefault(IsSelectedPanel) ?? panels.FirstOrDefault(p => p.CachedIsOffscreen == 0));
            page = scope.Own(state.SelectedPanel?.FindFirstBuildCache(TreeScope.TreeScope_Descendants, PageCondition.Value, Fetch.Value))
                ?? (state.Panels is null ? scope.Own(root.FindFirstBuildCache(TreeScope.TreeScope_Descendants, OnScreenPageCondition.Value, Fetch.Value)) : null);
        }
        else
        {
            // Chromium keeps only the selected tab's page in the window.
            page = scope.Own(root.FindFirstBuildCache(TreeScope.TreeScope_Descendants, PageCondition.Value, Fetch.Value));
        }

        if (page is not null)
        {
            state.PageHolder = Uia.ControlView.GetParentElement(page);
        }

        return page;
    }

    private static bool IsSelectedPanel(IUIAutomationElement panel) =>
        (panel.CachedClassName ?? string.Empty).Contains("deck-selected", StringComparison.Ordinal);

    /// <summary>The window title follows the selected tab; a page it doesn't name is another tab's.</summary>
    private static bool ShowsTitle(IUIAutomationElement page, string title) =>
        page.CachedName is not { Length: > 0 } name || title.StartsWith(name, StringComparison.Ordinal);

    /// <summary>
    /// Private sessions are marked on the window chrome, never on the page: the
    /// title, the accessibility name of the window, and the profile button in the
    /// toolbar. A page titled "Incognito" is not a private window. A window is
    /// private or not for its whole life, so the browser is asked once, as soon as
    /// it shows its frame (ADR-016). The title costs the browser nothing and is read
    /// every time: a new window's title is not final at once. Firefox and Edge always
    /// say so in the title, so their toolbars are never searched.
    /// </summary>
    private static bool IsPrivateWindow(UiaScope scope, string title, IUIAutomationElement root, WindowState state, string browserName, bool frameReady)
    {
        if (PrivateBrowsing.LooksPrivate(title))
        {
            return true;
        }

        bool titleSays = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase) || browserName.Equals("Edge", StringComparison.OrdinalIgnoreCase);
        if (state.FrameSaysPrivate is null && frameReady && !titleSays)
        {
            IUIAutomationElement frame = scope.Own(root.BuildUpdatedCache(Fetch.Value))!;
            state.FrameSaysPrivate = PrivateBrowsing.LooksPrivate(frame.CachedName) || ToolbarLooksPrivate(scope, root);
        }

        return state.FrameSaysPrivate == true;
    }

    /// <summary>
    /// Chromium's main toolbar, the first one in its frame, holds its private-session
    /// control (the profile button). The search stops there and never enters a page.
    /// </summary>
    private static bool ToolbarLooksPrivate(UiaScope scope, IUIAutomationElement root) =>
        scope.Own(root.FindFirstBuildCache(TreeScope.TreeScope_Descendants, ToolBarCondition.Value, Fetch.Value)) is { } toolbar
        && scope.All(toolbar.FindAllBuildCache(TreeScope.TreeScope_Children, ButtonCondition.Value, Fetch.Value))
            .Any(button => PrivateBrowsing.LooksPrivateButton(button.CachedName));

    private static AddressBarReading? TryReadFirefoxAddressBar(UiaScope scope, IUIAutomationElement root, WindowState state)
    {
        IUIAutomationElement? urlBar = state.AddressBar ??= root.FindFirst(TreeScope.TreeScope_Descendants, FirefoxAddressBarCondition.Value);
        if (urlBar is null)
        {
            return null;
        }

        // The bar itself first; only when it holds no address, the fields inside it.
        string? text = ReadAddressText(urlBar) is { } own && UrlNormalizer.TryNormalize(own, out _)
            ? own
            : scope.All(urlBar.FindAll(TreeScope.TreeScope_Descendants, Uia.TrueCondition))
                .Select(ReadAddressText)
                .FirstOrDefault(value => UrlNormalizer.TryNormalize(value, out _));
        return new AddressBarReading(text, urlBar.CurrentHasKeyboardFocus != 0);
    }

    /// <summary>
    /// Chromium's own address bar, found by a search that stops at it in the toolbar.
    /// Only a Chromium browser whose bar is something else falls back to looking
    /// through every text field.
    /// </summary>
    private static AddressBarReading? TryReadChromiumAddressBar(UiaScope scope, IUIAutomationElement root, WindowState state)
    {
        IUIAutomationElement? bar = state.AddressBar ??= root.FindFirst(TreeScope.TreeScope_Descendants, ChromiumAddressBarCondition.Value);
        if (bar is not null)
        {
            return new AddressBarReading(ReadAddressText(bar), bar.CurrentHasKeyboardFocus != 0);
        }

        IReadOnlyList<IUIAutomationElement> edits = scope.All(root.FindAll(TreeScope.TreeScope_Descendants, EditCondition.Value));
        IUIAutomationElement[] addressLike = edits.Where(LooksLikeAddressBar).ToArray();

        // The top-most address-like field, preferring one that holds a URL;
        // otherwise any field with a URL.
        bar = TopMost(addressLike.Where(HoldsUrl))
            ?? TopMost(addressLike)
            ?? TopMost(edits.Where(HoldsUrl));

        return bar is null ? null : new AddressBarReading(ReadAddressText(bar), bar.CurrentHasKeyboardFocus != 0);
    }

    private static IUIAutomationElement? TopMost(IEnumerable<IUIAutomationElement> elements) =>
        elements.OrderBy(e => e.CurrentBoundingRectangle.top).FirstOrDefault();

    private static bool HoldsUrl(IUIAutomationElement element) =>
        UrlNormalizer.TryNormalize(ReadAddressText(element), out _);

    /// <summary>The element's first value that reads as a web address, else its first value.</summary>
    private static string? ReadAddressText(IUIAutomationElement element)
    {
        string? first = null;
        foreach (Func<IUIAutomationElement, string?> read in ValueReaders)
        {
            string? value = read(element);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (UrlNormalizer.TryNormalize(value, out _))
            {
                return value;
            }

            first ??= value;
        }

        return first;
    }

    // Where an element may hold its text, cheapest first; each is asked only if needed.
    private static readonly Func<IUIAutomationElement, string?>[] ValueReaders =
    [
        element =>
        {
            object? pattern = element.GetCurrentPattern(Uia.ValuePattern);
            try
            {
                return (pattern as IUIAutomationValuePattern)?.CurrentValue;
            }
            finally
            {
                Uia.Release(pattern);
            }
        },
        element =>
        {
            object? pattern = element.GetCurrentPattern(Uia.TextPattern);
            IUIAutomationTextRange? range = null;
            try
            {
                range = (pattern as IUIAutomationTextPattern)?.DocumentRange;
                return range?.GetText(2048);
            }
            finally
            {
                Uia.Release(range);
                Uia.Release(pattern);
            }
        },
        element => element.CurrentName,
    ];

    private static bool LooksLikeAddressBar(IUIAutomationElement element)
    {
        string text = string.Join(
            ' ',
            element.CurrentName,
            element.CurrentAutomationId,
            element.CurrentClassName);

        return text.Contains("address", StringComparison.OrdinalIgnoreCase)
            || text.Contains("search", StringComparison.OrdinalIgnoreCase)
            || text.Contains("url", StringComparison.OrdinalIgnoreCase)
            || text.Contains("omnibox", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record AddressBarReading(string? Text, bool HasFocus);

    /// <summary>
    /// What is remembered about one browser window between reads. Each remembered
    /// element is handed back to Windows when it is replaced or forgotten.
    /// </summary>
    private sealed class WindowState
    {
        private IUIAutomationElement? _root;
        private IUIAutomationElement? _panels;
        private IUIAutomationElement? _selectedPanel;
        private IUIAutomationElement? _pageHolder;
        private IUIAutomationElement? _addressBar;

        public AddressBarTracker Tracker { get; } = new();

        /// <summary>Whether the window frame marks a private session, once it could be read; it never changes.</summary>
        public bool? FrameSaysPrivate { get; set; }

        public IUIAutomationElement? Root
        {
            get => _root;
            set => Replace(ref _root, value);
        }

        /// <summary>Firefox's panel that holds every tab's page.</summary>
        public IUIAutomationElement? Panels
        {
            get => _panels;
            set => Replace(ref _panels, value);
        }

        /// <summary>Firefox's panel of the tab that was selected last time.</summary>
        public IUIAutomationElement? SelectedPanel
        {
            get => _selectedPanel;
            set => Replace(ref _selectedPanel, value);
        }

        /// <summary>Where the selected tab's page was found last time.</summary>
        public IUIAutomationElement? PageHolder
        {
            get => _pageHolder;
            set => Replace(ref _pageHolder, value);
        }

        public IUIAutomationElement? AddressBar
        {
            get => _addressBar;
            set => Replace(ref _addressBar, value);
        }

        /// <summary>An element went away (the browser rebuilt its frame), or the window did: find everything again.</summary>
        public void Forget()
        {
            Root = null;
            Panels = null;
            SelectedPanel = null;
            PageHolder = null;
            AddressBar = null;
        }

        private static void Replace(ref IUIAutomationElement? field, IUIAutomationElement? value)
        {
            if (!ReferenceEquals(field, value))
            {
                Uia.Release(field);
                field = value;
            }
        }
    }
}
