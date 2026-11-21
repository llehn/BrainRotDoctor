using System.Runtime.InteropServices;
using Interop.UIAutomationClient;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Windows UI Automation through its own COM interface (ADR-019). The older .NET
/// wrapper left one record behind in Windows for every element it found in another
/// program, about half a gigabyte after a few days of reading browsers every second.
/// Every element found here is handed back to Windows as soon as it is done with.
/// </summary>
internal static class Uia
{
    public const int ControlTypeProperty = 30003;
    public const int NameProperty = 30005;
    public const int HasKeyboardFocusProperty = 30008;
    public const int AutomationIdProperty = 30011;
    public const int ClassNameProperty = 30012;
    public const int IsOffscreenProperty = 30022;
    public const int ValueProperty = 30045;

    public const int InvokePattern = 10000;
    public const int ValuePattern = 10002;
    public const int SelectionItemPattern = 10010;
    public const int TextPattern = 10014;

    public const int ButtonControl = 50000;
    public const int EditControl = 50004;
    public const int TabItemControl = 50019;
    public const int ToolBarControl = 50021;
    public const int DocumentControl = 50030;

    // Created off the UI thread, in the apartment the readers run in.
    private static readonly Lazy<IUIAutomation> Instance = new(Create, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<IUIAutomationCondition> True = new(() => Automation.CreateTrueCondition());

    private static readonly Lazy<IUIAutomationTreeWalker> ControlViewWalker = new(() => Automation.ControlViewWalker);

    public static IUIAutomation Automation => Instance.Value;

    /// <summary>Matches every element.</summary>
    public static IUIAutomationCondition TrueCondition => True.Value;

    /// <summary>Walks the elements people see and use, as screen readers do.</summary>
    public static IUIAutomationTreeWalker ControlView => ControlViewWalker.Value;

    private static IUIAutomation Create()
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            return Task.Run(Create).GetAwaiter().GetResult();
        }

        try
        {
            return new CUIAutomation8();
        }
        catch (COMException)
        {
            return new CUIAutomation();
        }
    }

    public static IUIAutomationCondition Property(int property, object value) =>
        Automation.CreatePropertyCondition(property, value);

    public static IUIAutomationCacheRequest CacheRequest(params int[] properties)
    {
        IUIAutomationCacheRequest request = Automation.CreateCacheRequest();
        foreach (int property in properties)
        {
            request.AddProperty(property);
        }

        return request;
    }

    /// <summary>Hands a COM reference back to Windows; null is ignored.</summary>
    public static void Release(object? com)
    {
        if (com is not null && Marshal.IsComObject(com))
        {
            Marshal.ReleaseComObject(com);
        }
    }

    /// <summary>
    /// What Windows reports when an element went away, its program is busy or
    /// closing, or it refuses a request. The read is then given up, never the app.
    /// </summary>
    public static bool IsFailure(Exception ex) =>
        ex is COMException or TimeoutException or UnauthorizedAccessException or InvalidCastException or InvalidComObjectException;
}

/// <summary>
/// Everything one read found, handed back to Windows together at its end. An element
/// that is to be remembered for the next read is taken out with <see cref="Keep{T}"/>.
/// </summary>
internal sealed class UiaScope : IDisposable
{
    private readonly List<object> _owned = [];

    /// <summary>Owns <paramref name="com"/> until the scope ends; returns it.</summary>
    public T? Own<T>(T? com)
        where T : class
    {
        if (com is not null)
        {
            _owned.Add(com);
        }

        return com;
    }

    /// <summary>Takes <paramref name="com"/> out of the scope: whoever keeps it releases it.</summary>
    public T? Keep<T>(T? com)
        where T : class
    {
        if (com is not null)
        {
            int at = _owned.FindLastIndex(owned => ReferenceEquals(owned, com));
            if (at >= 0)
            {
                _owned.RemoveAt(at);
            }
        }

        return com;
    }

    /// <summary>The elements of <paramref name="array"/>, each owned by the scope.</summary>
    public IReadOnlyList<IUIAutomationElement> All(IUIAutomationElementArray? array)
    {
        if (array is null)
        {
            return [];
        }

        try
        {
            int length = array.Length;
            var elements = new List<IUIAutomationElement>(length);
            for (int i = 0; i < length; i++)
            {
                if (Own(array.GetElement(i)) is { } element)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }
        finally
        {
            Uia.Release(array);
        }
    }

    public void Dispose()
    {
        for (int i = _owned.Count - 1; i >= 0; i--)
        {
            Uia.Release(_owned[i]);
        }

        _owned.Clear();
    }
}
