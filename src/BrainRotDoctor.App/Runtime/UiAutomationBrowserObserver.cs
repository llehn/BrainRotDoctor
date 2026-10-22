using System.Diagnostics;
using System.IO;
using System.Windows.Automation;

namespace BrainRotDoctor.App.Runtime;

internal sealed class UiAutomationBrowserObserver : IBrowserObserver
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
    private static readonly CacheRequest Fetch = NewFetch();

    private static readonly Condition PageCondition =
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);

    private static readonly Condition OnScreenPageCondition = new AndCondition(
        PageCondition,
        new PropertyCondition(AutomationElement.IsOffscreenProperty, false));

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
            _windows.Remove(gone);
        }

        return result;
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

    private static CacheRequest NewFetch()
    {
        var fetch = new CacheRequest();
        fetch.Add(AutomationElement.NameProperty);
        fetch.Add(AutomationElement.ClassNameProperty);
        fetch.Add(AutomationElement.IsOffscreenProperty);
        fetch.Add(ValuePattern.ValueProperty);
        return fetch;
    }

    /// <summary>
    /// One read of one window (ADR-016): the address of the page in its selected tab,
    /// and whether it is private. Only the browser's own frame is looked at, never the
    /// page, and where the page sits is remembered so the next read goes straight there.
    /// </summary>
    private static (Uri? Url, bool IsPrivate) TryReadWindow(IntPtr handle, string browserName, WindowState state, bool isForeground)
    {
        string title = NativeMethods.GetWindowTitle(handle);
        try
        {
            AutomationElement root = state.Root ??= AutomationElement.FromHandle(handle);
            bool isFirefox = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase);

            // The browser reports the loaded page's own address, which is never typed text.
            AutomationElement? page = FindPage(root, state, browserName, title);
            if (page?.GetCachedPropertyValue(ValuePattern.ValueProperty, ignoreDefaultValue: true) is string address
                && !string.IsNullOrWhiteSpace(address))
            {
                // A non-web page (new tab, settings) is known but matches no rule.
                UrlNormalizer.TryNormalize(address, out Uri? url);
                return (state.Tracker.Confirm(url), IsPrivateWindow(title, root, state, browserName, frameReady: true));
            }

            // Otherwise fall back to the address bar, guarded against unsent typing.
            AddressBarReading? bar = isFirefox ? TryReadFirefoxAddressBar(root, state) : TryReadChromiumAddressBar(root, state);
            bool isPrivate = IsPrivateWindow(title, root, state, browserName, frameReady: page is not null || bar is not null);
            return (state.Tracker.Resolve(bar?.Text, bar?.HasFocus == true, isForeground, title), isPrivate);
        }
        catch (ElementNotAvailableException)
        {
            state.Forget();
            return (null, state.FrameSaysPrivate == true || PrivateBrowsing.LooksPrivate(title));
        }
        catch (InvalidOperationException)
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
    private static AutomationElement? FindPage(AutomationElement root, WindowState state, string browserName, string title)
    {
        bool isFirefox = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase);
        if (!isFirefox && !browserName.Equals("Chrome", StringComparison.OrdinalIgnoreCase) && !browserName.Equals("Edge", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        using (Fetch.Activate())
        {
            if (state.PageHolder is { } holder
                && (!isFirefox || state.SelectedPanel is { } panel && IsSelectedPanel(panel.GetUpdatedCache(Fetch)))
                && holder.FindFirst(TreeScope.Children, PageCondition) is { } known
                && ShowsTitle(known, title))
            {
                return known;
            }

            state.PageHolder = null;
            state.SelectedPanel = null;
            AutomationElement? page;
            if (isFirefox)
            {
                // Every tab has a panel under one known element; the selected tab's is
                // marked, even while the window is covered (then all report off screen).
                state.Panels ??= root.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "tabbrowser-tabpanels"));
                AutomationElement[] panels = state.Panels?
                    .FindAll(TreeScope.Children, Condition.TrueCondition)
                    .Cast<AutomationElement>()
                    .ToArray() ?? [];
                state.SelectedPanel = panels.FirstOrDefault(IsSelectedPanel) ?? panels.FirstOrDefault(p => !p.Cached.IsOffscreen);
                page = state.SelectedPanel?.FindFirst(TreeScope.Descendants, PageCondition)
                    ?? (state.Panels is null ? root.FindFirst(TreeScope.Descendants, OnScreenPageCondition) : null);
            }
            else
            {
                // Chromium keeps only the selected tab's page in the window.
                page = root.FindFirst(TreeScope.Descendants, PageCondition);
            }

            if (page is not null)
            {
                state.PageHolder = TreeWalker.ControlViewWalker.GetParent(page);
            }

            return page;
        }
    }

    private static bool IsSelectedPanel(AutomationElement panel) =>
        panel.Cached.ClassName.Contains("deck-selected", StringComparison.Ordinal);

    /// <summary>The window title follows the selected tab; a page it doesn't name is another tab's.</summary>
    private static bool ShowsTitle(AutomationElement page, string title) =>
        page.Cached.Name is not { Length: > 0 } name || title.StartsWith(name, StringComparison.Ordinal);

    /// <summary>
    /// Private sessions are marked on the window chrome, never on the page: the
    /// title, the accessibility name of the window, and the profile button in the
    /// toolbar. A page titled "Incognito" is not a private window. A window is
    /// private or not for its whole life, so the browser is asked once, as soon as
    /// it shows its frame (ADR-016). The title costs the browser nothing and is read
    /// every time: a new window's title is not final at once. Firefox and Edge always
    /// say so in the title, so their toolbars are never searched.
    /// </summary>
    private static bool IsPrivateWindow(string title, AutomationElement root, WindowState state, string browserName, bool frameReady)
    {
        if (PrivateBrowsing.LooksPrivate(title))
        {
            return true;
        }

        bool titleSays = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase) || browserName.Equals("Edge", StringComparison.OrdinalIgnoreCase);
        if (state.FrameSaysPrivate is null && frameReady && !titleSays)
        {
            using (Fetch.Activate())
            {
                state.FrameSaysPrivate = PrivateBrowsing.LooksPrivate(root.GetUpdatedCache(Fetch).Cached.Name) || ToolbarLooksPrivate(root);
            }
        }

        return state.FrameSaysPrivate == true;
    }

    /// <summary>
    /// Chromium's main toolbar, the first one in its frame, holds its private-session
    /// control (the profile button). The search stops there and never enters a page.
    /// </summary>
    private static bool ToolbarLooksPrivate(AutomationElement root) =>
        root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar)) is { } toolbar
        && toolbar
            .FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
            .Cast<AutomationElement>()
            .Any(button => PrivateBrowsing.LooksPrivateButton(button.Cached.Name));

    private static AddressBarReading? TryReadFirefoxAddressBar(AutomationElement root, WindowState state)
    {
        AutomationElement? urlBar = state.AddressBar ??= root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, "urlbar-input"));

        if (urlBar is null)
        {
            return null;
        }

        string? text = EnumerateSelfAndDescendants(urlBar)
            .Select(ReadAddressText)
            .FirstOrDefault(value => UrlNormalizer.TryNormalize(value, out _));
        return new AddressBarReading(text, urlBar.Current.HasKeyboardFocus);
    }

    /// <summary>
    /// Chromium's own address bar, found by a search that stops at it in the toolbar.
    /// Only a Chromium browser whose bar is something else falls back to looking
    /// through every text field.
    /// </summary>
    private static AddressBarReading? TryReadChromiumAddressBar(AutomationElement root, WindowState state)
    {
        AutomationElement? bar = state.AddressBar ??= root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ClassNameProperty, "OmniboxViewViews"));
        if (bar is not null)
        {
            return new AddressBarReading(ReadAddressText(bar), bar.Current.HasKeyboardFocus);
        }

        AutomationElement[] edits = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit))
            .Cast<AutomationElement>()
            .ToArray();
        AutomationElement[] addressLike = edits.Where(LooksLikeAddressBar).ToArray();

        // The top-most address-like field, preferring one that holds a URL;
        // otherwise any field with a URL.
        bar = TopMost(addressLike.Where(HoldsUrl))
            ?? TopMost(addressLike)
            ?? TopMost(edits.Where(HoldsUrl));

        return bar is null ? null : new AddressBarReading(ReadAddressText(bar), bar.Current.HasKeyboardFocus);
    }

    private static AutomationElement? TopMost(IEnumerable<AutomationElement> elements) =>
        elements.OrderBy(e => e.Current.BoundingRectangle.Top).FirstOrDefault();

    private static bool HoldsUrl(AutomationElement element) =>
        UrlNormalizer.TryNormalize(ReadAddressText(element), out _);

    /// <summary>The element's first value that reads as a web address, else its first value.</summary>
    private static string? ReadAddressText(AutomationElement element)
    {
        string? first = null;
        foreach (string? value in ReadElementValues(element))
        {
            if (UrlNormalizer.TryNormalize(value, out _))
            {
                return value;
            }

            first ??= value;
        }

        return first;
    }

    private static bool LooksLikeAddressBar(AutomationElement element)
    {
        string text = string.Join(
            ' ',
            element.Current.Name,
            element.Current.AutomationId,
            element.Current.ClassName);

        return text.Contains("address", StringComparison.OrdinalIgnoreCase)
            || text.Contains("search", StringComparison.OrdinalIgnoreCase)
            || text.Contains("url", StringComparison.OrdinalIgnoreCase)
            || text.Contains("omnibox", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<AutomationElement> EnumerateSelfAndDescendants(AutomationElement root)
    {
        yield return root;

        AutomationElementCollection descendants = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        foreach (AutomationElement descendant in descendants)
        {
            yield return descendant;
        }
    }

    private static IEnumerable<string?> ReadElementValues(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj)
            && valuePatternObj is ValuePattern valuePattern)
        {
            string value = valuePattern.Current.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }

        if (element.TryGetCurrentPattern(TextPattern.Pattern, out object textPatternObj)
            && textPatternObj is TextPattern textPattern)
        {
            string value = textPattern.DocumentRange.GetText(2048);
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }

        string name = element.Current.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            yield return name;
        }
    }

    private sealed record AddressBarReading(string? Text, bool HasFocus);

    /// <summary>What is remembered about one browser window between reads.</summary>
    private sealed class WindowState
    {
        public AddressBarTracker Tracker { get; } = new();

        /// <summary>Whether the window frame marks a private session, once it could be read; it never changes.</summary>
        public bool? FrameSaysPrivate { get; set; }

        public AutomationElement? Root { get; set; }

        /// <summary>Firefox's panel that holds every tab's page.</summary>
        public AutomationElement? Panels { get; set; }

        /// <summary>Firefox's panel of the tab that was selected last time.</summary>
        public AutomationElement? SelectedPanel { get; set; }

        /// <summary>Where the selected tab's page was found last time.</summary>
        public AutomationElement? PageHolder { get; set; }

        public AutomationElement? AddressBar { get; set; }

        /// <summary>An element went away (the browser rebuilt its frame): find everything again.</summary>
        public void Forget()
        {
            Root = null;
            Panels = null;
            SelectedPanel = null;
            PageHolder = null;
            AddressBar = null;
        }
    }
}
