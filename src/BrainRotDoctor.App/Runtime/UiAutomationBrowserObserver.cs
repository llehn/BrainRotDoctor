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

    public IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs()
    {
        var result = new List<ObservedBrowserWindow>();
        foreach (IntPtr handle in EnumerateBrowserWindows())
        {
            if (!TryGetBrowserName(handle, out string? browserName))
            {
                continue;
            }

            Uri? url = TryReadUrl(handle, browserName!);
            result.Add(new ObservedBrowserWindow(handle.ToInt64().ToString(), handle, browserName!, url));
        }

        return result;
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

    private static Uri? TryReadUrl(IntPtr handle, string browserName)
    {
        try
        {
            AutomationElement root = AutomationElement.FromHandle(handle);
            if (browserName.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
            {
                return TryReadFirefoxUrl(root);
            }

            return TryReadChromiumUrl(root);
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

    private static Uri? TryReadFirefoxUrl(AutomationElement root)
    {
        AutomationElement? urlBar = root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, "urlbar-input"));

        if (urlBar is null)
        {
            return null;
        }

        foreach (AutomationElement element in EnumerateSelfAndDescendants(urlBar))
        {
            if (TryReadUriFromElement(element, out Uri? uri))
            {
                return uri;
            }
        }

        return null;
    }

    private static Uri? TryReadChromiumUrl(AutomationElement root)
    {
        var candidates = new List<AddressBarCandidate>();
        AutomationElementCollection edits = root.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));

        foreach (AutomationElement edit in edits)
        {
            if (!LooksLikeAddressBar(edit))
            {
                continue;
            }

            if (TryReadUriFromElement(edit, out Uri? uri) && uri is not null)
            {
                candidates.Add(new AddressBarCandidate(uri, edit.Current.BoundingRectangle.Top, Preferred: true));
            }
        }

        if (candidates.Count == 0)
        {
            foreach (AutomationElement edit in edits)
            {
                if (TryReadUriFromElement(edit, out Uri? uri) && uri is not null)
                {
                    candidates.Add(new AddressBarCandidate(uri, edit.Current.BoundingRectangle.Top, Preferred: false));
                }
            }
        }

        return candidates
            .OrderByDescending(c => c.Preferred)
            .ThenBy(c => c.Top)
            .Select(c => c.Uri)
            .FirstOrDefault();
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

    private static bool TryReadUriFromElement(AutomationElement element, out Uri? uri)
    {
        uri = null;

        foreach (string? value in ReadElementValues(element))
        {
            if (UrlNormalizer.TryNormalize(value, out Uri? parsed) && parsed is not null)
            {
                uri = parsed;
                return true;
            }
        }

        return false;
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

    private sealed record AddressBarCandidate(Uri Uri, double Top, bool Preferred);
}
