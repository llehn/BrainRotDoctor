using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace BrainRotDoctor.DesignSystem;

public enum TextFieldStyle
{
    /// <summary>A normal input.</summary>
    Regular,

    /// <summary>A taller input with larger text, for the main search of a screen.</summary>
    Large,

    /// <summary>Monospace, for addresses and other literal values.</summary>
    Code,

    /// <summary>No frame, heading-sized: an editable title in a top bar.</summary>
    Title,
}

/// <summary>
/// Free text input. Only for text that really is free-form (names, addresses,
/// search); numbers, times and choices have their own controls.
/// </summary>
public sealed class TextField : TextBox
{
    public TextField(string text = "", string? placeholder = null, TextFieldStyle style = TextFieldStyle.Regular, string? leadingGlyph = null, string? prefix = null)
    {
        Text = text;
        Watermark = placeholder;
        switch (style)
        {
            case TextFieldStyle.Large:
                FontSize = 15;
                MinHeight = 46;
                break;
            case TextFieldStyle.Code:
                FontFamily = new FontFamily(Fonts.Mono);
                break;
            case TextFieldStyle.Title:
                FontSize = 20;
                FontWeight = FontWeight.SemiBold;
                BorderThickness = new Thickness(0);
                Background = Brushes.Transparent;
                MinHeight = 44;
                break;
        }

        if (leadingGlyph is not null)
        {
            InnerLeftContent = new Border
            {
                Margin = new Thickness(style == TextFieldStyle.Large ? 14 : 12, 0, 4, 0),
                Child = new Icon(leadingGlyph, style == TextFieldStyle.Large ? 18 : 16, TextTone.Tertiary),
            };
        }
        else if (prefix is not null)
        {
            InnerLeftContent = new TextLine(prefix, TextStyle.Code, TextTone.Tertiary)
            {
                FontSize = 13,
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.NoWrap,
            };
        }

        TextChanged += (_, _) => Changed?.Invoke(Text ?? "");
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Submitted?.Invoke();
            }
        };
    }

    /// <summary>Raised on every edit with the new text.</summary>
    public event Action<string>? Changed;

    /// <summary>Raised when Enter is pressed.</summary>
    public event Action? Submitted;

    protected override Type StyleKeyOverride => typeof(TextBox);
}

/// <summary>A choice from a long list (more options than fit a <see cref="Segmented"/>).</summary>
public sealed class Dropdown : ComboBox
{
    public Dropdown(IReadOnlyList<string> items, int selected)
    {
        ItemsSource = items;
        SelectedIndex = selected;
        SelectionChanged += (_, _) => Changed?.Invoke(Math.Max(0, SelectedIndex));
    }

    public event Action<int>? Changed;

    protected override Type StyleKeyOverride => typeof(ComboBox);
}

/// <summary>
/// A whole number changed with − and + only, never typed. Holding a button
/// repeats, and speeds up for long ranges.
/// </summary>
public sealed class Stepper : Border
{
    private readonly int _min;
    private readonly int _max;
    private readonly TextLine _value;
    private readonly DispatcherTimer _repeat = new();
    private int _delta;
    private int _repeats;

    public Stepper(int value, int min, int max, string fewerTip, string moreTip)
    {
        _min = min;
        _max = max;
        Value = Math.Clamp(value, min, max);
        _value = new TextLine(Format(Value), TextStyle.Subheading) { FontSize = 18, Centered = true, MinWidth = 44 };

        CornerRadius = new CornerRadius(Radius.Control);
        BorderThickness = new Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Left;
        this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
        this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
        Child = Layout.Row(0, Step(Glyph.Minus, -1, fewerTip), _value, Step(Glyph.Plus, 1, moreTip));

        _repeat.Tick += (_, _) =>
        {
            _repeats++;
            _repeat.Interval = TimeSpan.FromMilliseconds(70);
            Apply(_delta * (_repeats > 20 ? 10 : _repeats > 8 ? 5 : 1));
        };
    }

    /// <summary>Raised with the new value after every step.</summary>
    public event Action<int>? Changed;

    public int Value { get; private set; }

    private static string Format(int v) => v.ToString(CultureInfo.CurrentCulture);

    private Border Step(string glyph, int delta, string tip)
    {
        var b = new Border
        {
            Width = 44,
            Height = 42,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = new Icon(glyph, 16) { HorizontalAlignment = HorizontalAlignment.Center },
        };
        ToolTip.SetTip(b, tip);
        b.PointerPressed += (_, e) =>
        {
            e.Pointer.Capture(b);
            _delta = delta;
            _repeats = 0;
            Apply(delta);
            _repeat.Interval = TimeSpan.FromMilliseconds(400);
            _repeat.Start();
        };
        b.PointerReleased += (_, _) => _repeat.Stop();
        b.PointerCaptureLost += (_, _) => _repeat.Stop();
        return b;
    }

    private void Apply(int delta)
    {
        int next = Math.Clamp(Value + delta, _min, _max);
        if (next == Value)
        {
            _repeat.Stop();
            return;
        }

        Value = next;
        _value.SetText(Format(next));
        Changed?.Invoke(next);
    }
}

/// <summary>
/// A 24-hour time that can only be picked, never typed: the field shows "09:00";
/// clicking it opens an hour column and a minute column (5-minute steps).
/// <see cref="Changed"/> fires once when the picker closes with a new time.
/// </summary>
public sealed class TimeField : Border
{
    private const int MinuteStep = 5;
    private const double ItemHeight = 34;

    private readonly TextLine _text;
    private readonly string _doneLabel;
    private TimeOnly _value;

    public TimeField(TimeOnly value, string doneLabel)
    {
        _value = value;
        _doneLabel = doneLabel;
        _text = new TextLine(Format(value), TextStyle.Label);
        var clock = new Icon(Glyph.Clock, 16, TextTone.Tertiary);
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
        this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
        this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
        PointerEntered += (_, _) => this[!BorderBrushProperty] = Palette.Dyn(Palette.TextTertiary);
        PointerExited += (_, _) => this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
        AddHandler(Gestures.TappedEvent, (_, _) => Open());
    }

    public event Action<TimeOnly>? Changed;

    private static string Format(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Opens the picker and returns its content (used to render the gallery).</summary>
    public Control Open()
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

        var done = new PillButton(_doneLabel, ButtonKind.Primary) { Stretch = true };
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
                _text.SetText(Format(picked));
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
        var cells = new List<(int Number, Border Cell, TextLine Label)>();

        void Paint()
        {
            foreach ((int number, Border cell, TextLine label) in cells)
            {
                bool on = number == current();
                if (on)
                {
                    cell[!BackgroundProperty] = Palette.Dyn(Palette.Ink);
                }
                else
                {
                    cell.Background = Brushes.Transparent;
                }

                label.SetTone(on ? TextTone.OnInk : TextTone.Primary);
            }
        }

        foreach (int number in numbers)
        {
            var label = new TextLine(number.ToString("00", CultureInfo.InvariantCulture), TextStyle.Strong) { Centered = true };
            var cell = new Border
            {
                Height = ItemHeight,
                CornerRadius = new CornerRadius(Radius.Small),
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
