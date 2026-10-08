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
/// If a keyboard attempt had no visible effect and the engine asks again for the
/// same window shortly after, the tab's own close button is pressed through UI
/// Automation instead. Browsers bring their window to the front when that button is
/// pressed, so focus is handed straight back to the window the user was using.
/// </remarks>
internal sealed class BrowserTabCloser : IBrowserTabCloser
{
    // Longest time Ctrl is held for the browser while waiting for the tab to close.
    private static readonly TimeSpan CtrlHoldLimit = TimeSpan.FromMilliseconds(250);

    // A repeated request within this window after an unconfirmed keyboard attempt
    // means the keyboard path did not work, so the close button is used.
    private static readonly TimeSpan FallbackWindow = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan FocusStealWait = TimeSpan.FromMilliseconds(500);

    private readonly Dictionary<IntPtr, DateTimeOffset> _unconfirmedKeyboardAttempts = new();

    public bool CloseSelectedTab(IntPtr windowHandle)
    {
        // Only act on a window that is still on screen (ADR-010). Never restore or
        // re-position it: restoring a snapped window un-snaps it, and activating a
        // window on another virtual desktop would switch the user's desktop.
        if (windowHandle == IntPtr.Zero
            || !NativeMethods.IsWindow(windowHandle)
            || !NativeMethods.IsOnScreen(windowHandle))
        {
            return false;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ForgetStaleAttempts(now);

        if (_unconfirmedKeyboardAttempts.Remove(windowHandle, out DateTimeOffset previous)
            && now - previous < FallbackWindow
            && TryPressCloseButton(windowHandle))
        {
            return true;
        }

        if (!TryPostCtrlW(windowHandle, out bool sawEffect))
        {
            return TryPressCloseButton(windowHandle);
        }

        if (!sawEffect)
        {
            _unconfirmedKeyboardAttempts[windowHandle] = now;
        }

        return true;
    }

    private void ForgetStaleAttempts(DateTimeOffset now)
    {
        foreach (IntPtr handle in _unconfirmedKeyboardAttempts
                     .Where(entry => now - entry.Value >= FallbackWindow)
                     .Select(entry => entry.Key)
                     .ToList())
        {
            _unconfirmedKeyboardAttempts.Remove(handle);
        }
    }

    /// <summary>
    /// Posts Ctrl+W to the window while its thread sees Ctrl as held. Holds Ctrl only
    /// until the window visibly reacts (title changes or the window closes), capped
    /// at <see cref="CtrlHoldLimit"/>.
    /// </summary>
    private static bool TryPostCtrlW(IntPtr windowHandle, out bool sawEffect)
    {
        sawEffect = false;
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
                        sawEffect = true;
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
        AutomationElementCollection tabs = root.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));

        foreach (AutomationElement tab in tabs)
        {
            if (!tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selectionObj)
                || selectionObj is not SelectionItemPattern selection
                || !selection.Current.IsSelected)
            {
                continue;
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

        return null;
    }
}
