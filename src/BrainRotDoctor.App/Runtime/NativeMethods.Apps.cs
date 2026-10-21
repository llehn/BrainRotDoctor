using System.Runtime.InteropServices;

namespace BrainRotDoctor.App.Runtime;

/// <summary>What finding and closing apps needs from Windows.</summary>
internal static partial class NativeMethods
{
    public const uint WM_CLOSE = 0x0010;
    public const int GWL_EXSTYLE = -20;
    public const long WS_EX_APPWINDOW = 0x00040000;

    /// <summary>Enough to read a process's program file and start time, even of most protected processes.</summary>
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private const uint LWA_ALPHA = 0x2;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLayeredWindowAttributes(IntPtr hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, [Out] char[] lpExeName, ref uint lpdwSize);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessTimes(IntPtr hProcess, out long creation, out long exit, out long kernel, out long user);

    public static long ExtendedStyle(IntPtr hWnd) => GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();

    /// <summary>A layered window drawn fully transparent (an overlay waiting to show something).</summary>
    public static bool IsInvisibleLayer(IntPtr hWnd) =>
        (ExtendedStyle(hWnd) & WS_EX_LAYERED) != 0
        && GetLayeredWindowAttributes(hWnd, out _, out byte alpha, out uint flags)
        && (flags & LWA_ALPHA) != 0
        && alpha == 0;

    /// <summary>The program file and start time of a process; null when Windows won't say (it's gone).</summary>
    public static (string Path, long Started)? ProcessImage(uint processId)
    {
        IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var buffer = new char[1024];
            uint size = (uint)buffer.Length;
            if (!QueryFullProcessImageName(handle, 0, buffer, ref size) || !GetProcessTimes(handle, out long started, out _, out _, out _))
            {
                return null;
            }

            return (new string(buffer, 0, (int)size), started);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    /// <summary>The first child window that belongs to another process than <paramref name="processId"/> (the app a frame host shows).</summary>
    public static uint ChildProcessOtherThan(IntPtr hWnd, uint processId)
    {
        uint found = 0;
        EnumChildWindows(hWnd, (child, _) =>
        {
            GetWindowThreadProcessId(child, out uint pid);
            if (pid != 0 && pid != processId)
            {
                found = pid;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return found;
    }
}
