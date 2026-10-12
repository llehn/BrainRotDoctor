using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// A 24-hour time that can only be picked, never typed: the field shows "09:00";
/// clicking it opens an hour column and a minute column (5-minute steps).
/// <see cref="Changed"/> fires once when the picker closes with a new time.
/// </summary>
internal sealed class TimeField : Border
{
    private const int MinuteStep = 5;
    private const double ItemHeight = 34;

    private readonly TextBlock _text;
    private TimeOnly _value;

    public TimeField(TimeOnly value)
    {
        _value = value;
        _text = UiTheme.Text(Format(value), 15, FontWeight.SemiBold, UiTheme.TextPrimary);
        Control clock = Icons.Make(Icons.Clock, 16, UiTheme.TextTertiary);
        var row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(clock, Dock.Right);
        row.Children.Add(clock);
        row.Children.Add(_text);

        Child = row;
        MinHeight = 40;
        Padding = new Thickness(12, 0);
        CornerRadius = new CornerRadius(9);
        BorderThickness = new Thickness(1);
        Cursor = new Cursor(StandardCursorType.Hand);
        this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface);
        this[!BorderBrushProperty] = UiTheme.Dyn(UiTheme.BorderStrong);
        PointerEntered += (_, _) => this[!BorderBrushProperty] = UiTheme.Dyn(UiTheme.TextTertiary);
        PointerExited += (_, _) => this[!BorderBrushProperty] = UiTheme.Dyn(UiTheme.BorderStrong);
        AddHandler(Gestures.TappedEvent, (_, _) => Open());
    }

    public event Action<TimeOnly>? Changed;

    private static string Format(TimeOnly t) => t.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Opens the picker and returns its content (used by the UI snapshots).</summary>
    internal Control Open()
    {
        int hour = _value.Hour;
        int minute = _value.Minute;
        var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };

        ScrollViewer hours = Column(Enumerable.Range(0, 24), () => hour, h => hour = h);
        ScrollViewer minutes = Column(Enumerable.Range(0, 60 / MinuteStep).Select(i => i * MinuteStep), () => minute, m => minute = m);
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*") };
        columns.Children.Add(hours);
        Grid.SetColumn(minutes, 2);
        columns.Children.Add(minutes);

        PillButton done = UiTheme.Primary(Loc.T("done"));
        done.HorizontalAlignment = HorizontalAlignment.Stretch;
        done.Click += (_, _) => flyout.Hide();

        var content = new StackPanel { Width = 180, Spacing = 10, Children = { columns, done } };
        flyout.Content = content;
        flyout.Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            // Show the picked numbers in the middle of their columns.
            hours.Offset = new Vector(0, Math.Max(0, (hour - 2) * ItemHeight));
            minutes.Offset = new Vector(0, Math.Max(0, (minute / MinuteStep - 2) * ItemHeight));
        }, DispatcherPriority.Background);
        flyout.Closed += (_, _) =>
        {
            var picked = new TimeOnly(hour, minute);
            if (picked != _value)
            {
                _value = picked;
                _text.Text = Format(picked);
                Changed?.Invoke(picked);
            }
        };
        flyout.ShowAt(this);
        return content;
    }

    /// <summary>A scrolling list of two-digit numbers; the current one is filled.</summary>
    private static ScrollViewer Column(IEnumerable<int> numbers, Func<int> current, Action<int> pick)
    {
        var list = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        var cells = new List<(int Number, Border Cell, TextBlock Label)>();

        void Paint()
        {
            foreach ((int number, Border cell, TextBlock label) in cells)
            {
                bool on = number == current();
                if (on)
                {
                    cell[!BackgroundProperty] = UiTheme.Dyn(UiTheme.Ink);
                }
                else
                {
                    cell.Background = Brushes.Transparent;
                }

                label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(on ? UiTheme.InkText : UiTheme.TextPrimary);
            }
        }

        foreach (int number in numbers)
        {
            TextBlock label = UiTheme.Text(number.ToString("00", System.Globalization.CultureInfo.InvariantCulture), 14, FontWeight.SemiBold, UiTheme.TextPrimary);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            var cell = new Border
            {
                Height = ItemHeight,
                CornerRadius = new CornerRadius(8),
                Cursor = new Cursor(StandardCursorType.Hand),
                Background = Brushes.Transparent,
                Child = label,
            };
            cell.AddHandler(Gestures.TappedEvent, (_, _) =>
            {
                pick(number);
                Paint();
            });
            cells.Add((number, cell, label));
            list.Children.Add(cell);
        }

        Paint();
        return new ScrollViewer { Height = ItemHeight * 5, Content = list, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
