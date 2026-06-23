using Avalonia.Threading;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Shows and arranges <see cref="ToastWindow"/> notifications. Toasts stack upward
/// from the tray corner, newest at the bottom, and reflow as they dismiss. All access
/// is marshalled onto the UI thread, so callers may invoke <see cref="Show(string, string)"/>
/// from any thread (the enforcement loop raises tab-close events on a timer thread).
/// </summary>
internal sealed class ToastNotifier
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(5);
    private const int MaxVisible = 4;

    private readonly List<ToastWindow> _active = new();

    public void Show(string title, string message) => Show(title, message, DefaultLifetime);

    public void Show(string title, string message, TimeSpan lifetime)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ShowCore(title, message, lifetime);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ShowCore(title, message, lifetime));
        }
    }

    private void ShowCore(string title, string message, TimeSpan lifetime)
    {
        while (_active.Count >= MaxVisible)
        {
            _active[0].BeginDismiss();
            _active.RemoveAt(0);
        }

        var toast = new ToastWindow(title, message, lifetime);
        toast.Dismissed += (_, _) =>
        {
            _active.Remove(toast);
            Reflow();
        };

        _active.Add(toast);
        toast.Show();
        Reflow();
    }

    private void Reflow()
    {
        // Newest (last) sits at the bottom with no offset; older toasts move up.
        for (int i = 0; i < _active.Count; i++)
        {
            double offset = (_active.Count - 1 - i) * ToastWindow.SlotHeight;
            _active[i].SetStackOffset(offset);
        }
    }
}
