using System.Diagnostics;
using System.Windows.Automation;

namespace BrainRotDoctor.App.Runtime;

internal sealed class UiAutomationBrowserObserver : IBrowserObserver
{
    private static readonly IReadOnlyDictionary<string, string> SupportedBrowsers =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "Chrome",
            ["firefox"] = "Firefox",
            ["msedge"] = "Edge",
            ["brave"] = "Brave",
            ["vivaldi"] = "Vivaldi",
            ["opera"] = "Opera",
        };

    // One tracker per browser window, so text typed into a window's address bar
    // never counts as the page that window is showing.
    private readonly Dictionary<IntPtr, AddressBarTracker> _trackers = new();

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

            if (!_trackers.TryGetValue(handle, out AddressBarTracker? tracker))
            {
                tracker = new AddressBarTracker();
                _trackers[handle] = tracker;
            }

            Uri? url = TryReadUrl(handle, browserName!, tracker, handle == foreground);
            result.Add(new ObservedBrowserWindow(handle.ToInt64().ToString(), handle, browserName!, url));
        }

        foreach (IntPtr gone in _trackers.Keys.Where(h => result.All(w => w.WindowHandle != h)).ToArray())
        {
            _trackers.Remove(gone);
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

        if (!_trackers.TryGetValue(windowHandle, out AddressBarTracker? tracker))
        {
            tracker = new AddressBarTracker();
            _trackers[windowHandle] = tracker;
        }

        return TryReadUrl(windowHandle, browserName!, tracker, windowHandle == NativeMethods.GetForegroundWindow());
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

    private static bool TryGetBrowserName(IntPtr handle, out string? browserName)
    {
        browserName = null;
        NativeMethods.GetWindowThreadProcessId(handle, out uint pid);
        if (pid == 0)
        {
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById((int)pid);
            return SupportedBrowsers.TryGetValue(process.ProcessName, out browserName);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Uri? TryReadUrl(IntPtr handle, string browserName, AddressBarTracker tracker, bool isForeground)
    {
        try
        {
            AutomationElement root = AutomationElement.FromHandle(handle);
            bool isFirefox = browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase);

            // Firefox reports the loaded page's own address, which is never typed text.
            if (isFirefox && TryReadFirefoxPageAddress(root, out Uri? page))
            {
                return tracker.Confirm(page);
            }

            // Otherwise fall back to the address bar, guarded against unsent typing.
            AddressBarReading? bar = isFirefox ? TryReadFirefoxAddressBar(root) : TryReadChromiumAddressBar(root);
            return tracker.Resolve(
                bar?.Text,
                bar?.HasFocus == true,
                isForeground,
                NativeMethods.GetWindowTitle(handle));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the address of the page loaded in Firefox's selected tab from the
    /// page document itself. False when Firefox has not exposed it (yet).
    /// </summary>
    private static bool TryReadFirefoxPageAddress(AutomationElement root, out Uri? page)
    {
        page = null;
        AutomationElement? panels = root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, "tabbrowser-tabpanels"));
        if (panels is null)
        {
            return false;
        }

        // Every tab has a panel; only the selected tab's panel is on screen.
        AutomationElement? selected = panels
            .FindAll(TreeScope.Children, Condition.TrueCondition)
            .Cast<AutomationElement>()
            .FirstOrDefault(panel => !panel.Current.IsOffscreen);

        // The first document in the panel is the page; frames inside it come later.
        AutomationElement? document = selected?.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document));
        if (document is null
            || !document.TryGetCurrentPattern(ValuePattern.Pattern, out object patternObj)
            || patternObj is not ValuePattern pattern
            || string.IsNullOrWhiteSpace(pattern.Current.Value))
        {
            return false;
        }

        // A non-web page (new tab, settings) is known but matches no rule.
        UrlNormalizer.TryNormalize(pattern.Current.Value, out page);
        return true;
    }

    private static AddressBarReading? TryReadFirefoxAddressBar(AutomationElement root)
    {
        AutomationElement? urlBar = root.FindFirst(
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

    private static AddressBarReading? TryReadChromiumAddressBar(AutomationElement root)
    {
        AutomationElement[] edits = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit))
            .Cast<AutomationElement>()
            .ToArray();
        AutomationElement[] addressLike = edits.Where(LooksLikeAddressBar).ToArray();

        // Chromium's own address bar first; otherwise the top-most address-like
        // field, preferring one that holds a URL; otherwise any field with a URL.
        AutomationElement? bar = edits.FirstOrDefault(e => e.Current.ClassName == "OmniboxViewViews")
            ?? TopMost(addressLike.Where(HoldsUrl))
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
}
