using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace BrainRotDoctor.DesignSystem;

/// <summary>One option of a <see cref="Segmented"/>; <paramref name="Fill"/> colours it when selected.</summary>
public readonly record struct Segment(string Text, Tone? Fill = null);

/// <summary>
/// A row of mutually exclusive options on a grey track; the selected option is
/// raised, or filled with its tone (e.g. danger for Block, accent for Allow).
/// <c>onInk</c> draws it for an ink card: a dark track, the selected option light.
/// </summary>
public sealed class Segmented : Border
{
    private readonly List<(Border Cell, TextLine Label, Tone? Fill)> _cells = new();
    private readonly bool _onInk;
    private int _selected;

    public Segmented(IReadOnlyList<Segment> options, int selected, bool stretch = true, double height = 38, bool onInk = false)
    {
        _selected = selected;
        _onInk = onInk;
        Padding = new Thickness(3);
        CornerRadius = new CornerRadius(Radius.Control);
        this[!BackgroundProperty] = Palette.Dyn(onInk ? Palette.InkTrack : Palette.Track);

        var grid = new Grid();
        for (int i = 0; i < options.Count; i++)
        {
            grid.ColumnDefinitions.Add(stretch ? new ColumnDefinition(GridLength.Star) : new ColumnDefinition(GridLength.Auto));
            int index = i;
            var label = new TextLine(options[i].Text, TextStyle.Strong) { FontSize = 13.5, Centered = true };
            var cell = new Border
            {
                MinHeight = height - 6,
                MinWidth = 64,
                Padding = new Thickness(10, 3),
                CornerRadius = new CornerRadius(Radius.Small),
                Margin = new Thickness(i == 0 ? 0 : 2, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = label,
            };
            cell.AddHandler(Gestures.TappedEvent, (_, e) =>
            {
                e.Handled = true;
                Select(index, raise: true);
            });
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
            _cells.Add((cell, label, options[i].Fill));
        }

        Child = grid;
        HorizontalAlignment = stretch ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        Paint();
    }

    public event Action<int>? Changed;

    public int SelectedIndex => _selected;

    public void Select(int index, bool raise)
    {
        if (index == _selected)
        {
            return;
        }

        _selected = index;
        Paint();
        if (raise)
        {
            Changed?.Invoke(index);
        }
    }

    private void Paint()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            (Border cell, TextLine label, Tone? fill) = _cells[i];
            bool on = i == _selected;
            if (!on)
            {
                cell.Background = Brushes.Transparent;
                cell.BoxShadow = default;
                label.SetTone(_onInk ? TextTone.OnInkMuted : TextTone.Secondary);
            }
            else if (_onInk)
            {
                cell[!BackgroundProperty] = Palette.Dyn(Palette.InkPick);
                cell.BoxShadow = default;
                label[!TextBlock.ForegroundProperty] = Palette.Dyn(Palette.InkPickText);
            }
            else if (fill is { } tone)
            {
                cell[!BackgroundProperty] = Palette.Dyn(Palette.Solid(tone));
                cell.BoxShadow = default;
                label[!TextBlock.ForegroundProperty] = Palette.Dyn(Palette.AccentText);
            }
            else
            {
                cell[!BackgroundProperty] = Palette.Dyn(Palette.Selected);
                cell.BoxShadow = Elevation.Raised;
                label.SetTone(TextTone.Primary);
            }
        }
    }
}

/// <summary>
/// An on/off switch: accent track with the knob right when on, grey with the
/// knob left when off. Taps on it never reach the control underneath.
/// </summary>
public sealed class OnOffSwitch : Border
{
    private readonly Border _knob;
    private bool _enabled = true;

    public OnOffSwitch(bool isOn)
    {
        IsOn = isOn;
        Width = 40;
        Height = 22;
        CornerRadius = new CornerRadius(11);
        Padding = new Thickness(3);
        VerticalAlignment = VerticalAlignment.Center;
        Cursor = new Cursor(StandardCursorType.Hand);
        _knob = new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(8),
            BoxShadow = Elevation.Knob,
            [!BackgroundProperty] = Palette.Dyn(Palette.Knob),
        };
        Child = _knob;
        Paint();

        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            if (_enabled)
            {
                IsOn = !IsOn;
                Paint();
                Toggled?.Invoke(IsOn);
            }
        });
    }

    public event Action<bool>? Toggled;

    public bool IsOn { get; private set; }

    /// <summary>Greys the switch out and ignores taps (kept hit-testable for its tooltip).</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Opacity = value ? 1 : 0.45;
            Cursor = new Cursor(value ? StandardCursorType.Hand : StandardCursorType.Arrow);
        }
    }

    private void Paint()
    {
        _knob.HorizontalAlignment = IsOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        this[!BackgroundProperty] = Palette.Dyn(IsOn ? Palette.Accent : Palette.BorderStrong);
    }
}

/// <summary>
/// One choice of several that wrap onto multiple lines (radio behaviour is up
/// to the caller: rebuild with the new selection).
/// </summary>
public sealed class RadioPill : Border
{
    public RadioPill(string label, bool selected)
    {
        MinHeight = 40;
        Padding = new Thickness(14, 0);
        CornerRadius = new CornerRadius(9);
        BorderThickness = new Thickness(selected ? 1.5 : 1);
        Cursor = new Cursor(StandardCursorType.Hand);
        var text = new TextLine(label, selected ? TextStyle.Strong : TextStyle.Body) { FontSize = 13.5 };
        Child = text;
        this[!BackgroundProperty] = Palette.Dyn(selected ? Palette.Surface : Palette.AppBg);
        this[!BorderBrushProperty] = Palette.Dyn(selected ? Palette.Ink : Palette.BorderStrong);
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            Click?.Invoke(this, EventArgs.Empty);
        });
    }

    public event EventHandler? Click;
}

/// <summary>A small round on/off toggle with a one- or two-letter label (e.g. weekdays).</summary>
public sealed class RoundToggle : Border
{
    public RoundToggle(string label, bool on, string tip)
    {
        IsOn = on;
        Width = 36;
        Height = 36;
        CornerRadius = new CornerRadius(18);
        BorderThickness = new Thickness(1);
        Cursor = new Cursor(StandardCursorType.Hand);
        Child = new TextLine(label, TextStyle.CaptionStrong, on ? TextTone.Inverse : TextTone.Secondary) { Centered = true };
        this[!BackgroundProperty] = Palette.Dyn(on ? Palette.Ink : Palette.Surface);
        this[!BorderBrushProperty] = Palette.Dyn(on ? Palette.Ink : Palette.BorderStrong);
        ToolTip.SetTip(this, tip);
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            Toggled?.Invoke(!IsOn);
        });
    }

    /// <summary>Raised with the requested new state; rebuild to show it.</summary>
    public event Action<bool>? Toggled;

    public bool IsOn { get; }
}

/// <summary>A check box with optional content. On ink surfaces pass <c>onInk</c> so the empty box stays visible.</summary>
public sealed class CheckOption : CheckBox
{
    public CheckOption(bool isChecked, Control? content = null, bool onInk = false)
    {
        IsChecked = isChecked;
        Content = content;
        VerticalAlignment = content is null ? VerticalAlignment.Center : VerticalAlignment.Top;
        if (onInk)
        {
            foreach ((ThemeVariant variant, string hex) in new[] { (ThemeVariant.Light, "#FFFFFF"), (ThemeVariant.Dark, "#16171B") })
            {
                var brush = new SolidColorBrush(Color.Parse(hex));
                Resources.ThemeDictionaries[variant] = new ResourceDictionary
                {
                    ["CheckBoxCheckBackgroundStrokeUnchecked"] = brush,
                    ["CheckBoxCheckBackgroundStrokeUncheckedPointerOver"] = brush,
                    ["CheckBoxCheckBackgroundStrokeUncheckedPressed"] = brush,
                    ["CheckBoxForegroundUnchecked"] = brush,
                    ["CheckBoxForegroundUncheckedPointerOver"] = brush,
                    ["CheckBoxForegroundChecked"] = brush,
                    ["CheckBoxForegroundCheckedPointerOver"] = brush,
                };
            }
        }

        IsCheckedChanged += (_, _) => Changed?.Invoke(IsChecked == true);
    }

    public event Action<bool>? Changed;

    protected override Type StyleKeyOverride => typeof(CheckBox);
}

/// <summary>An outlined row with a check box, a title and a caption; the whole row toggles.</summary>
public sealed class CheckRow : Border
{
    public CheckRow(string title, string caption, bool isChecked)
    {
        var box = new CheckOption(isChecked);
        box.Changed += on => Changed?.Invoke(on);
        MinHeight = 56;
        Padding = new Thickness(12, 6);
        CornerRadius = new CornerRadius(Radius.Control);
        BorderThickness = new Thickness(1);
        Cursor = new Cursor(StandardCursorType.Hand);
        Background = Brushes.Transparent;
        this[!BorderBrushProperty] = Palette.Dyn(Palette.Border);
        Child = Layout.Row(Space.S1, box, Layout.Column(1, new TextLine(title, TextStyle.Strong), new TextLine(caption, TextStyle.Caption)));
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            if (e.Source is Visual v && box.IsVisualAncestorOf(v))
            {
                return;
            }

            box.IsChecked = box.IsChecked != true;
        });
    }

    public event Action<bool>? Changed;
}

/// <summary>Where a <see cref="ChoiceTile"/> sits and how it shows that it's selected.</summary>
public enum ChoiceTileStyle
{
    /// <summary>On a surface: the selected tile is filled ink.</summary>
    Default,

    /// <summary>On an ink card: raised, outlined dark tiles, the selected one light. Taller, text at the bottom.</summary>
    OnInk,

    /// <summary>On a surface, choosing a temporary state: the selected tile is soft amber.</summary>
    Warn,
}

/// <summary>A tile among several choices, optionally with a badge above its title.</summary>
public sealed class ChoiceTile : Border
{
    public ChoiceTile(string title, string caption, bool selected, ChoiceTileStyle style = ChoiceTileStyle.Default, string? badge = null)
    {
        bool onInk = style == ChoiceTileStyle.OnInk;
        (string surface, string outline, string titleColor, string captionColor) = (style, selected) switch
        {
            (ChoiceTileStyle.OnInk, true) => (Palette.InkPick, Palette.InkPick, Palette.InkPickText, Palette.InkPickMuted),
            (ChoiceTileStyle.OnInk, false) => (Palette.InkRaised, Palette.InkRaisedBorder, Palette.Of(TextTone.OnInk), Palette.Of(TextTone.OnInkMuted)),
            (ChoiceTileStyle.Warn, true) => (Palette.WarnSoft, Palette.Warn, Palette.Of(TextTone.Warn), Palette.Of(TextTone.Warn)),
            (_, true) => (Palette.Ink, Palette.Ink, Palette.Of(TextTone.Inverse), Palette.Of(TextTone.Inverse)),
            _ => (Palette.Surface, Palette.Border, Palette.Of(TextTone.Primary), Palette.Of(TextTone.Tertiary)),
        };

        MinHeight = onInk ? 104 : 64;
        Padding = new Thickness(14, onInk ? 12 : 8);
        CornerRadius = new CornerRadius(Radius.Inset);
        BorderThickness = new Thickness(1.5);
        Cursor = new Cursor(StandardCursorType.Hand);
        var titleLine = new TextLine(title, TextStyle.Label) { [!TextBlock.ForegroundProperty] = Palette.Dyn(titleColor) };
        var captionLine = new TextLine(caption, TextStyle.Caption) { FontSize = 12, [!TextBlock.ForegroundProperty] = Palette.Dyn(captionColor) };
        var text = Layout.Column(2, titleLine, captionLine);
        text.VerticalAlignment = onInk ? VerticalAlignment.Bottom : VerticalAlignment.Center;
        var content = new DockPanel { LastChildFill = true };
        if (badge is not null)
        {
            var chip = new Chip(badge, Tone.Accent) { Margin = new Thickness(0, 0, 0, Space.S2) };
            DockPanel.SetDock(chip, Dock.Top);
            content.Children.Add(chip);
        }

        content.Children.Add(text);
        Child = content;
        this[!BackgroundProperty] = Palette.Dyn(surface);
        this[!BorderBrushProperty] = Palette.Dyn(outline);
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            Click?.Invoke(this, EventArgs.Empty);
        });
    }

    public event EventHandler? Click;
}

/// <summary>
/// A tile that can be picked into a set, with a round check in its corner.
/// <c>locked</c> shows it picked, greyed out and not clickable (already chosen elsewhere).
/// </summary>
public sealed class PickTile : Border
{
    public PickTile(Control content, bool picked, bool locked = false)
    {
        bool on = picked || locked;
        var check = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            BorderThickness = new Thickness(1.5),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = on ? new Icon(Glyph.Check, 13, TextTone.Inverse, 3) { HorizontalAlignment = HorizontalAlignment.Center } : null,
            [!BackgroundProperty] = Palette.Dyn(on ? (locked ? Palette.TextTertiary : Palette.Ink) : Palette.Surface),
            [!BorderBrushProperty] = Palette.Dyn(on ? (locked ? Palette.TextTertiary : Palette.Ink) : Palette.BorderStrong),
        };

        MinHeight = 140;
        Padding = new Thickness(Space.S4);
        CornerRadius = new CornerRadius(Radius.Card);
        BorderThickness = new Thickness(1.5);
        Child = new Grid { Children = { content, check } };
        Opacity = locked ? 0.65 : 1;
        Cursor = new Cursor(locked ? StandardCursorType.Arrow : StandardCursorType.Hand);
        this[!BackgroundProperty] = Palette.Dyn(locked ? Palette.SurfaceAlt : Palette.Surface);
        this[!BorderBrushProperty] = Palette.Dyn(picked && !locked ? Palette.Ink : Palette.Border);
        if (!locked)
        {
            AddHandler(Gestures.TappedEvent, (_, e) =>
            {
                e.Handled = true;
                Click?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    public event EventHandler? Click;
}
