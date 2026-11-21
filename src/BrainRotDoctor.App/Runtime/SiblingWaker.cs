using System.Diagnostics;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Wakes the other running copies of this app on every check (ADR-014) without
/// listing every process on the machine each time. The copies are found
/// once and then held by handle; each check wakes them by that handle. The machine
/// is looked through again only when a known copy has exited, when none is known
/// (at most once a second), and every ten seconds as a safety net for a copy that
/// appeared while another was still known.
/// </summary>
internal sealed class SiblingWaker : IDisposable
{
    internal static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan SafetyNetInterval = TimeSpan.FromSeconds(10);

    private readonly Func<IReadOnlyList<int>> _findSiblings;
    private readonly Dictionary<int, IntPtr> _known = new();
    private DateTimeOffset? _lastLook;

    /// <param name="findSiblings">The ids of the other running copies; the expensive part.</param>
    public SiblingWaker(Func<IReadOnlyList<int>> findSiblings) => _findSiblings = findSiblings;

    /// <summary>How many times the machine was looked through. For tests.</summary>
    internal int Lookups { get; private set; }

    /// <summary>The copies currently held. For tests.</summary>
    internal IReadOnlyCollection<int> Known => _known.Keys;

    public void Wake(DateTimeOffset now)
    {
        bool lost = ForgetExited();
        if (_lastLook is not { } last
            || lost
            || (_known.Count == 0 && now - last >= RetryInterval)
            || now - last >= SafetyNetInterval)
        {
            Look(now);
        }

        foreach (IntPtr handle in _known.Values)
        {
            // Waking a running copy does nothing.
            NativeMethods.NtResumeProcess(handle);
        }
    }

    /// <summary>The other running copies of the executable named <paramref name="processName"/>.</summary>
    internal static IReadOnlyList<int> OtherInstancesOf(string processName)
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(processName);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return [];
        }

        var ids = new List<int>(processes.Length);
        foreach (Process process in processes)
        {
            using (process)
            {
                if (process.Id != Environment.ProcessId)
                {
                    ids.Add(process.Id);
                }
            }
        }

        return ids;
    }

    public void Dispose()
    {
        foreach (IntPtr handle in _known.Values)
        {
            NativeMethods.CloseHandle(handle);
        }

        _known.Clear();
    }

    private bool ForgetExited()
    {
        List<int>? exited = null;
        foreach ((int id, IntPtr handle) in _known)
        {
            if (NativeMethods.WaitForSingleObject(handle, 0) != NativeMethods.WAIT_TIMEOUT)
            {
                (exited ??= []).Add(id);
            }
        }

        if (exited is null)
        {
            return false;
        }

        foreach (int id in exited)
        {
            NativeMethods.CloseHandle(_known[id]);
            _known.Remove(id);
        }

        return true;
    }

    private void Look(DateTimeOffset now)
    {
        _lastLook = now;
        Lookups++;
        foreach (int id in _findSiblings())
        {
            // A held handle keeps its process id from being reused, so a known id is the same copy.
            if (id <= 0 || id == Environment.ProcessId || _known.ContainsKey(id))
            {
                continue;
            }

            IntPtr handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_SUSPEND_RESUME | NativeMethods.SYNCHRONIZE, false, (uint)id);
            if (handle != IntPtr.Zero)
            {
                _known[id] = handle;
            }
        }
    }
}
