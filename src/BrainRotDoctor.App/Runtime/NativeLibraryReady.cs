using System.IO;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Waits until every native library in this run's unpack folder can load,
/// then the caller shows the window. Does not start this program again.
/// If the bound elapses, we still return so the window is attempted.
/// </summary>
internal static class NativeLibraryReady
{
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    public static void Wait() =>
        Wait(FindNativeLibraries, TryLoad, Limit, RetryDelay, static ms => Thread.Sleep(ms));

    internal static bool Wait(
        Func<IReadOnlyList<string>> findLibraries,
        Func<string, bool> tryLoad,
        TimeSpan limit,
        TimeSpan delay,
        Action<int> sleep)
    {
        IReadOnlyList<string> paths = findLibraries();
        if (paths.Count == 0)
        {
            return true;
        }

        var pending = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
        DateTime deadline = DateTime.UtcNow + limit;
        while (true)
        {
            foreach (string path in pending.ToArray())
            {
                if (tryLoad(path))
                {
                    pending.Remove(path);
                }
            }

            if (pending.Count == 0)
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            sleep((int)delay.TotalMilliseconds);
        }
    }

    internal static IReadOnlyList<string> FindNativeLibraries()
    {
        if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is not string dirs
            || string.IsNullOrWhiteSpace(dirs))
        {
            return [];
        }

        var files = new List<string>();
        foreach (string dir in dirs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            try
            {
                files.AddRange(Directory.GetFiles(dir, "*.dll"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        return files;
    }

    internal static bool TryLoad(string path)
    {
        IntPtr handle = NativeMethods.LoadLibraryExW(path, IntPtr.Zero, NativeMethods.LOAD_WITH_ALTERED_SEARCH_PATH);
        return handle != IntPtr.Zero;
    }
}
