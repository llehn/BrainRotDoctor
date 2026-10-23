using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace BrainRotDoctor.DesignSystem;

/// <summary>What a button does, from most to least prominent.</summary>
public enum ButtonKind
{
    /// <summary>The one main action of a screen or card: filled ink.</summary>
    Primary,

    /// <summary>Other actions: outlined.</summary>
    Secondary,

    /// <summary>Navigation and low-key actions: text only.</summary>
    Ghost,

    /// <summary>A destructive action offered quietly: danger-coloured text.</summary>
    Danger,

    /// <summary>A destructive action being confirmed: filled danger.</summary>
    DangerSolid,

    /// <summary>A light constructive action inside content: accent-coloured text.</summary>
    Accent,

    /// <summary>The way out of a temporary state (e.g. resume): soft amber.</summary>
    Warn,

    /// <summary>The main action on an ink card: filled with the surface colour.</summary>
    OnInk,

    /// <summary>A low-key action on an ink card: muted text.</summary>
    GhostOnInk,
}

public enum ButtonSize
{
    /// <summary>28 high, for inline actions next to a label.</summary>
    Small,

    /// <summary>40 high: the default.</summary>
    Regular,

    /// <summary>48 high, for the final action of a flow.</summary>
    Large,
}

/// <summary>A rounded button with a label and an optional leading icon.</summary>
public sealed class PillButton : Border
{
    private readonly TextLine _label;
    private bool _enabled = true;

    public PillButton(string text, ButtonKind kind = ButtonKind.Secondary, string? glyph = null, ButtonSize size = ButtonSize.Regular)
    {
        Kind = kind;
        TextTone fg = kind switch
        {
            ButtonKind.Primary or ButtonKind.DangerSolid => TextTone.OnInk,
            ButtonKind.Ghost => TextTone.Secondary,
            ButtonKind.Danger => TextTone.Danger,
            ButtonKind.Accent => TextTone.Accent,
            ButtonKind.GhostOnInk => TextTone.OnInkMuted,
            _ => TextTone.Primary,
        };

        bool small = size == ButtonSize.Small;
        _label = new TextLine(text, TextStyle.Strong, fg) { TextWrapping = TextWrapping.NoWrap };
        if (small)
        {
            _label.FontSize = 12.5;
        }

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Space.S2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (glyph is not null)
        {
            row.Children.Add(new Icon(glyph, small ? 14 : 16, fg));
        }

        if (text.Length > 0)
        {
            row.Children.Add(_label);
        }

        Child = row;
        CornerRadius = new CornerRadius(Radius.Control);
        MinHeight = size switch { ButtonSize.Small => 28, ButtonSize.Large => 48, _ => 40 };
        bool textOnly = kind is ButtonKind.Ghost or ButtonKind.Danger or ButtonKind.Accent or ButtonKind.GhostOnInk;
        Padding = small ? new Thickness(6, 0)
            : textOnly ? new Thickness(10, 0)
            : new Thickness(glyph is null ? 18 : 14, 0, 18, 0);
        Cursor = new Cursor(StandardCursorType.Hand);
        VerticalAlignment = VerticalAlignment.Center;
        Background = Brushes.Transparent;

        switch (kind)
        {
            case ButtonKind.Primary:
                this[!BackgroundProperty] = Palette.Dyn(Palette.Ink);
                break;
            case ButtonKind.DangerSolid:
                this[!BackgroundProperty] = Palette.Dyn(Palette.Danger);
                break;
            case ButtonKind.Secondary:
                this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
                BorderThickness = new Thickness(1);
                this[!BorderBrushProperty] = Palette.Dyn(Palette.BorderStrong);
                break;
            case ButtonKind.Warn:
                this[!BackgroundProperty] = Palette.Dyn(Palette.WarnSoft);
                BorderThickness = new Thickness(1);
                this[!BorderBrushProperty] = Palette.Dyn(Palette.WarnBorder);
                break;
            case ButtonKind.OnInk:
                this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
                break;
        }

        PointerEntered += (_, _) => { if (_enabled) Opacity = 0.82; };
        PointerExited += (_, _) => { if (_enabled) Opacity = 1; };
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            if (_enabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public event EventHandler? Click;

    public ButtonKind Kind { get; }

    public string Text
    {
        get => _label.Text ?? "";
        set => _label.SetText(value);
    }

    /// <summary>Greys the button out and ignores taps (kept hit-testable for its tooltip).</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Opacity = value ? 1 : 0.4;
            Cursor = new Cursor(value ? StandardCursorType.Hand : StandardCursorType.Arrow);
        }
    }

    /// <summary>Stretches the button to the full width of its container.</summary>
    public bool Stretch
    {
        get => HorizontalAlignment == HorizontalAlignment.Stretch;
        set => HorizontalAlignment = value ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
    }
}

/// <summary>
/// A main action that only happens when held down: it fills from the left while
/// pressed and raises <see cref="Held"/> once full; letting go early empties it.
/// Looks like a large <see cref="ButtonKind.Primary"/> button filling the width.
/// </summary>
public sealed class HoldButton : Border
{
    private readonly TextLine _label;
    private readonly Border _fill;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly TimeSpan _hold;
    private DateTime _pressedAt;

    public HoldButton(string text, TimeSpan hold, string? glyph = null)
    {
        _hold = hold;
        _label = new TextLine(text, TextStyle.Strong, TextTone.OnInk) { TextWrapping = TextWrapping.NoWrap };
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Space.S2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (glyph is not null)
        {
            row.Children.Add(new Icon(glyph, 16, TextTone.OnInk));
        }

        row.Children.Add(_label);
        _fill = new Border { Width = 0, HorizontalAlignment = HorizontalAlignment.Left, [!BackgroundProperty] = Palette.Dyn(Palette.WarnText) };
        Child = new Grid { Children = { _fill, row } };

        CornerRadius = new CornerRadius(Radius.Control);
        ClipToBounds = true;
        MinHeight = 48;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Cursor = new Cursor(StandardCursorType.Hand);
        this[!BackgroundProperty] = Palette.Dyn(Palette.Ink);

        PointerEntered += (_, _) => Opacity = 0.9;
        PointerExited += (_, _) => Opacity = 1;
        PointerPressed += (_, e) =>
        {
            e.Pointer.Capture(this);
            _pressedAt = DateTime.UtcNow;
            _timer.Start();
        };
        PointerReleased += (_, _) => Empty();
        PointerCaptureLost += (_, _) => Empty();
        DetachedFromVisualTree += (_, _) => Empty();
        _timer.Tick += (_, _) =>
        {
            double held = (DateTime.UtcNow - _pressedAt) / _hold;
            if (held < 1)
            {
                _fill.Width = Bounds.Width * held;
                return;
            }

            Empty();
            Held?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>Raised once the button has been held down for the whole time.</summary>
    public event EventHandler? Held;

    public string Text
    {
        get => _label.Text ?? "";
        set => _label.SetText(value);
    }

    private void Empty()
    {
        _timer.Stop();
        _fill.Width = 0;
    }
}

public enum IconButtonStyle
{
    /// <summary>40×40 with an outline: a standalone action in a row.</summary>
    Outlined,

    /// <summary>28×28, no outline: dismissing or removing the thing it sits on.</summary>
    Bare,
}

/// <summary>A square icon-only button. Always has a tooltip.</summary>
public sealed class IconButton : Border
{
    public IconButton(string glyph, string tip, IconButtonStyle style = IconButtonStyle.Outlined, bool selected = false)
    {
        bool bare = style == IconButtonStyle.Bare;
        Width = bare ? 28 : 40;
        Height = Width;
        CornerRadius = new CornerRadius(bare ? Radius.Small : Radius.Control);
        Cursor = new Cursor(StandardCursorType.Hand);
        VerticalAlignment = VerticalAlignment.Center;
        Child = new Icon(glyph, bare ? 14 : 16) { HorizontalAlignment = HorizontalAlignment.Center };
        Background = Brushes.Transparent;
        if (!bare)
        {
            BorderThickness = new Thickness(1);
            this[!BackgroundProperty] = Palette.Dyn(Palette.Surface);
            this[!BorderBrushProperty] = Palette.Dyn(selected ? Palette.Ink : Palette.BorderStrong);
        }

        ToolTip.SetTip(this, tip);
        PointerEntered += (_, _) => Opacity = 0.82;
        PointerExited += (_, _) => Opacity = 1;
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            e.Handled = true;
            Click?.Invoke(this, EventArgs.Empty);
        });
    }

    public event EventHandler? Click;
}

/// <summary>
/// Makes any content clickable without changing its look (e.g. the header of
/// an expandable card). Clicks on buttons inside it don't reach it.
/// </summary>
public sealed class Pressable : Border
{
    public Pressable(Control content, Thickness padding = default)
    {
        Child = content;
        Padding = padding;
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);
        AddHandler(Gestures.TappedEvent, (_, e) =>
        {
            if (!e.Handled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public event EventHandler? Click;
}
