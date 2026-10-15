using System.Windows.Automation;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Closes the selected tab of a browser window without stealing focus and without
/// moving the window (ADR-010, ADR-011).
/// </summary>
/// <remarks>
/// The primary path posts Ctrl+W straight into the browser window's message queue.
/// Browsers read modifier keys from their own thread's keyboard state rather than
/// from the message, so for that moment the browser thread's state is marked
/// "Ctrl down". Nothing is sent to the window the user is typing in.
///
/// Every attempt is checked: if the same tab is still selected a moment later, the
/// tab's own close button is pressed through UI Automation, and so on for a few
/// quick attempts, all within this one call so the worm scene is not replayed.
/// Browsers bring their window to the front when that button is pressed, so focus
/// is handed straight back to the window the user was using.
/// </remarks>
internal sealed class BrowserTabCloser : IBrowserTabCloser
{
    // Longest time Ctrl is held for the browser while waiting for the tab to close.
    private static readonly TimeSpan CtrlHoldLimit = TimeSpan.FromMilliseconds(250);

    // How long an attempt may take to show before the tab counts as still open.
    // Long enough that a slow browser's close is not mistaken for a miss, which
    // would make the next attempt close the neighbouring tab.
    private static readonly TimeSpan ConfirmWait = TimeSpan.FromMilliseconds(800);

    private const int MaxAttempts = 4;

    private static readonly TimeSpan FocusStealWait = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Closes the selected tab, retrying quickly while that same tab stays open.
    /// True once it is confirmed gone.
    /// </summary>
    public bool CloseSelectedTab(IntPtr windowHandle)
    {
        // Only act on a window that is still on screen (ADR-010). Never restore or
        // re-position it: restoring a snapped window un-snaps it, and activating a
        // window on another virtual desktop would switch the user's desktop.
        if (!IsVisible(windowHandle))
        {
            return false;
        }

        string title = NativeMethods.GetWindowTitle(windowHandle);
        int[]? tab = SelectedTabId(windowHandle);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            // Keyboard first; after a miss the close button, which acts on the
            // exact tab; whichever is unavailable, the other.
            bool sent = attempt == 0
                ? TryPostCtrlW(windowHandle) || TryPressCloseButton(windowHandle)
                : TryPressCloseButton(windowHandle) || TryPostCtrlW(windowHandle);

            if (sent && WaitUntilGone(windowHandle, title, tab))
            {
                return true;
            }

            if (!IsVisible(windowHandle) || !IsSameTab(windowHandle, title, tab))
            {
                // Minimized, moved away, or the user switched tabs: stop here.
                return false;
            }
        }

        return false;
    }

    private static bool IsVisible(IntPtr windowHandle) =>
        windowHandle != IntPtr.Zero
        && NativeMethods.IsWindow(windowHandle)
        && NativeMethods.IsOnScreen(windowHandle);

    /// <summary>True once the window closed or shows something other than the tab.</summary>
    private static bool WaitUntilGone(IntPtr windowHandle, string title, int[]? tab)
    {
        var wait = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            if (!NativeMethods.IsWindow(windowHandle)
                || NativeMethods.GetWindowTitle(windowHandle) != title)
            {
                return true;
            }

            if (wait.Elapsed >= ConfirmWait)
            {
                // Same title; a neighbour with an identical title would still be
                // a different tab element.
                return !IsSameTab(windowHandle, title, tab);
            }

            Thread.Sleep(20);
        }
    }

    private static bool IsSameTab(IntPtr windowHandle, string title, int[]? tab) =>
        NativeMethods.IsWindow(windowHandle)
        && NativeMethods.GetWindowTitle(windowHandle) == title
        && (tab is null || SelectedTabId(windowHandle) is not { } now || now.SequenceEqual(tab));

    /// <summary>
    /// Posts Ctrl+W to the window while its thread sees Ctrl as held. Holds Ctrl only
    /// until the window visibly reacts (title changes or the window closes), capped
    /// at <see cref="CtrlHoldLimit"/>.
    /// </summary>
    private static bool TryPostCtrlW(IntPtr windowHandle)
    {
        uint browserThread = NativeMethods.GetWindowThreadProcessId(windowHandle, out _);
        uint currentThread = NativeMethods.GetCurrentThreadId();
        if (browserThread == 0 || !NativeMethods.AttachThreadInput(currentThread, browserThread, true))
        {
            return false;
        }

        try
        {
            var state = new byte[256];
            if (!NativeMethods.GetKeyboardState(state))
            {
                return false;
            }

            bool ctrlWasDown = (state[NativeMethods.VK_CONTROL] & NativeMethods.KEY_DOWN) != 0;
            bool leftCtrlWasDown = (state[NativeMethods.VK_LCONTROL] & NativeMethods.KEY_DOWN) != 0;
            state[NativeMethods.VK_CONTROL] |= NativeMethods.KEY_DOWN;
            state[NativeMethods.VK_LCONTROL] |= NativeMethods.KEY_DOWN;
            if (!NativeMethods.SetKeyboardState(state))
            {
                return false;
            }

            try
            {
                string titleBefore = NativeMethods.GetWindowTitle(windowHandle);
                NativeMethods.PostMessage(windowHandle, NativeMethods.WM_KEYDOWN, (IntPtr)NativeMethods.VK_W, KeyLParam(NativeMethods.SCAN_W, keyUp: false));
                NativeMethods.PostMessage(windowHandle, NativeMethods.WM_KEYUP, (IntPtr)NativeMethods.VK_W, KeyLParam(NativeMethods.SCAN_W, keyUp: true));

                var hold = System.Diagnostics.Stopwatch.StartNew();
                while (hold.Elapsed < CtrlHoldLimit)
                {
                    Thread.Sleep(10);
                    if (!NativeMethods.IsWindow(windowHandle)
                        || NativeMethods.GetWindowTitle(windowHandle) != titleBefore)
                    {
                        break;
                    }
                }
            }
            finally
            {
                // Release only what was faked; keep any real Ctrl press intact.
                if (NativeMethods.GetKeyboardState(state))
                {
                    if (!ctrlWasDown)
                    {
                        state[NativeMethods.VK_CONTROL] &= unchecked((byte)~NativeMethods.KEY_DOWN);
                    }

                    if (!leftCtrlWasDown)
                    {
                        state[NativeMethods.VK_LCONTROL] &= unchecked((byte)~NativeMethods.KEY_DOWN);
                    }

                    NativeMethods.SetKeyboardState(state);
                }
            }

            return true;
        }
        finally
        {
            NativeMethods.AttachThreadInput(currentThread, browserThread, false);
        }
    }

    private static IntPtr KeyLParam(uint scanCode, bool keyUp)
    {
        uint value = 1 | (scanCode << 16);
        if (keyUp)
        {
            value |= 0xC0000000;
        }

        return unchecked((IntPtr)(int)value);
    }

    /// <summary>
    /// Presses the selected tab's close button through UI Automation, then hands
    /// focus back if the browser pulled its window to the front.
    /// </summary>
    private static bool TryPressCloseButton(IntPtr windowHandle)
    {
        try
        {
            AutomationElement? closeButton = FindSelectedTabCloseButton(AutomationElement.FromHandle(windowHandle));
            if (closeButton is null
                || !closeButton.TryGetCurrentPattern(InvokePattern.Pattern, out object patternObj)
                || patternObj is not InvokePattern invoke)
            {
                return false;
            }

            IntPtr userWindow = NativeMethods.GetForegroundWindow();
            invoke.Invoke();

            if (userWindow != IntPtr.Zero && userWindow != windowHandle)
            {
                var wait = System.Diagnostics.Stopwatch.StartNew();
                while (NativeMethods.GetForegroundWindow() != windowHandle && wait.Elapsed < FocusStealWait)
                {
                    Thread.Sleep(5);
                }

                if (NativeMethods.GetForegroundWindow() == windowHandle)
                {
                    NativeMethods.ReturnFocusTo(userWindow);
                }
            }

            return true;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static AutomationElement? FindSelectedTabCloseButton(AutomationElement root)
    {
        AutomationElement? tab = FindSelectedTab(root);
        if (tab is null)
        {
            return null;
        }

        // Matched by class, not by (localized) name: Firefox "tab-close-button",
        // Chrome "TabCloseButton", Edge "EdgeTabCloseButton".
        AutomationElementCollection buttons = tab.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        foreach (AutomationElement button in buttons)
        {
            string className = button.Current.ClassName ?? string.Empty;
            if (className.Contains("CloseButton", StringComparison.OrdinalIgnoreCase)
                || className.Contains("close-button", StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }

    private static AutomationElement? FindSelectedTab(AutomationElement root)
    {
        AutomationElementCollection tabs = root.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));

        foreach (AutomationElement tab in tabs)
        {
            if (tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selectionObj)
                && selectionObj is SelectionItemPattern selection
                && selection.Current.IsSelected)
            {
                return tab;
            }
        }

        return null;
    }

    /// <summary>The selected tab's identity, or null when the browser does not expose it.</summary>
    private static int[]? SelectedTabId(IntPtr windowHandle)
    {
        try
        {
            return FindSelectedTab(AutomationElement.FromHandle(windowHandle))?.GetRuntimeId();
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
}
