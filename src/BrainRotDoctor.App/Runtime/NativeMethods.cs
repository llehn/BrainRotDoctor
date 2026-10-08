using System.Runtime.InteropServices;

namespace BrainRotDoctor.App.Runtime;

internal static partial class NativeMethods
{
    // DWM window attribute (dwmapi.h): non-zero when the window is cloaked, e.g.
    // because it lives on a virtual desktop other than the current one.
    private const int DWMWA_CLOAKED = 14;

    // Virtual-key codes (winuser.h).
    public const byte VK_CONTROL = 0x11;
    public const byte VK_W = 0x57;
    public const byte VK_LCONTROL = 0xA2;

    // Keyboard-state flag: the key is down.
    public const byte KEY_DOWN = 0x80;

    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint SCAN_W = 0x11;

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(IntPtr hWnd);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    /// <summary>
    /// True when the window is on the user's current screen (ADR-010): shown, not
    /// minimized, and not cloaked onto another virtual desktop. Being covered by
    /// other windows does not matter.
    /// </summary>
    public static bool IsOnScreen(IntPtr hWnd)
    {
        if (!IsWindowVisible(hWnd) || IsIconic(hWnd))
        {
            return false;
        }

        return DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) != 0 || cloaked == 0;
    }

    [LibraryImport("user32.dll")]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetKeyboardState([Out] byte[] lpKeyState);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetKeyboardState(byte[] lpKeyState);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetWindowText(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var buffer = new char[512];
        int length = GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer, 0, Math.Max(length, 0));
    }

    /// <summary>
    /// Brings <paramref name="hWnd"/> to the front by borrowing the input state of the
    /// thread that currently owns the foreground, which the foreground lock allows.
    /// Used only to hand focus back to the window the user was already using.
    /// </summary>
    public static void ReturnFocusTo(IntPtr hWnd)
    {
        uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        uint currentThread = GetCurrentThreadId();
        bool attached = foregroundThread != 0
            && foregroundThread != currentThread
            && AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            SetForegroundWindow(hWnd);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(IntPtr hIcon);

    public const uint MB_ICONINFORMATION = 0x40;
    public const uint MB_ICONERROR = 0x10;

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    // Extended window styles (winuser.h) used to make the toast a non-activating,
    // tool-style overlay: it never steals focus from the browser we just acted on
    // and never appears in Alt-Tab or the taskbar.
    public const int GWL_EXSTYLE = -20;
    public const long WS_EX_TOOLWINDOW = 0x00000080;
    public const long WS_EX_NOACTIVATE = 0x08000000;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static partial nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    public static partial nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    /// <summary>Adds the no-activate / tool-window extended styles to a window.</summary>
    public static void MakeNonActivatingOverlay(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        nint current = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
        nint updated = current | (nint)WS_EX_NOACTIVATE | (nint)WS_EX_TOOLWINDOW;
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, updated);
    }
}
