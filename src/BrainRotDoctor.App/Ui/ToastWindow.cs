using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// A self-contained desktop notification: a borderless, transparent, always-on-top
/// window that slides in from the bottom-right above the tray, then auto-dismisses.
/// It is deliberately *not* a Windows native toast — that path supports no animation,
/// which we need for the planned "doctor pulls a worm out of the brain" sequence. For
/// now it shows static text; the window plumbing here is what the animation will reuse.
///
/// Crucially it never takes focus: we just told the browser to close a tab and brought
/// it to the foreground, so the toast applies WS_EX_NOACTIVATE / WS_EX_TOOLWINDOW and
/// is created with <see cref="Window.ShowActivated"/> = false.
/// </summary>
internal sealed class ToastWindow : Window
{
    /// <summary>Vertical space one toast reserves when stacked, in DIPs.</summary>
    public const double SlotHeight = 96;

    private const double CardMargin = 14;   // transparent padding inside the window (room for shadow + slide)
    private const double EdgeGapPx = 8;      // gap from the screen work-area edge, in physical px
    private const double SlideOffset = 44;   // how far the card travels on the way in/out, in DIPs

    private static readonly TimeSpan SlideIn = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan SlideOut = TimeSpan.FromMilliseconds(220);

    private readonly Border _card;
    private readonly TranslateTransform _slide;
    private readonly DispatcherTimer _life;
    private double _stackOffset;
    private bool _dismissing;

    public ToastWindow(string title, string message, TimeSpan lifetime)
    {
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        SizeToContent = SizeToContent.Manual;
        Width = 360;
        Height = 116;
        Focusable = false;

        var icon = new Image
        {
            Source = ProductIcon.RenderBitmap(40),
            Width = 40,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var titleText = new TextBlock
        {
            Text = title,
            FontFamily = new FontFamily("Inter, $Default"),
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#F4F1FA")),
            TextWrapping = TextWrapping.Wrap,
        };

        var bodyText = new TextBlock
        {
            Text = message,
            FontFamily = new FontFamily("Inter, $Default"),
            FontSize = 12.5,
            FontWeight = FontWeight.Normal,
            Foreground = new SolidColorBrush(Color.Parse("#B9B4C7")),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
        };

        var textStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { titleText, bodyText },
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(icon, 0);
        Grid.SetColumn(textStack, 1);
        textStack.Margin = new Thickness(13, 0, 0, 0);
        grid.Children.Add(icon);
        grid.Children.Add(textStack);

        _slide = new TranslateTransform { X = SlideOffset };
        _slide.Transitions = new Transitions
        {
            new DoubleTransition { Property = TranslateTransform.XProperty, Duration = SlideIn, Easing = new CubicEaseOut() },
        };

        _card = new Border
        {
            Margin = new Thickness(CardMargin),
            Padding = new Thickness(16, 14),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#403A4D")),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#272231"), 0),
                    new GradientStop(Color.Parse("#211D29"), 1),
                },
            },
            BoxShadow = new BoxShadows(new BoxShadow { OffsetX = 0, OffsetY = 8, Blur = 28, Color = Color.FromArgb(150, 0, 0, 0) }),
            Child = grid,
            Opacity = 0,
            RenderTransform = _slide,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        _card.Transitions = new Transitions
        {
            new DoubleTransition { Property = OpacityProperty, Duration = SlideIn, Easing = new CubicEaseOut() },
        };

        Content = _card;

        _card.PointerPressed += (_, _) => BeginDismiss();

        _life = new DispatcherTimer { Interval = lifetime };
        _life.Tick += (_, _) => BeginDismiss();
    }

    /// <summary>Raised once the toast has finished animating out and closed.</summary>
    public event EventHandler? Dismissed;

    /// <summary>Re-anchors the toast to a vertical slot above the tray (in DIPs from the bottom).</summary>
    public void SetStackOffset(double offsetDip)
    {
        _stackOffset = offsetDip;
        Reposition();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (TryGetPlatformHandle() is { } handle)
        {
            NativeMethods.MakeNonActivatingOverlay(handle.Handle);
        }

        Reposition();

        // Play the entrance after layout settles so the transition actually animates.
        Dispatcher.UIThread.Post(() =>
        {
            _card.Opacity = 1;
            _slide.X = 0;
        }, DispatcherPriority.Background);

        _life.Start();
    }

    public void BeginDismiss()
    {
        if (_dismissing)
        {
            return;
        }

        _dismissing = true;
        _life.Stop();

        foreach (Transitions? t in new[] { _card.Transitions, _slide.Transitions })
        {
            if (t?[0] is DoubleTransition d)
            {
                d.Duration = SlideOut;
                d.Easing = new CubicEaseIn();
            }
        }

        _card.Opacity = 0;
        _slide.X = SlideOffset;

        var closeTimer = new DispatcherTimer { Interval = SlideOut + TimeSpan.FromMilliseconds(40) };
        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            Close();
            Dismissed?.Invoke(this, EventArgs.Empty);
        };
        closeTimer.Start();
    }

    private void Reposition()
    {
        Screen? screen = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (screen is null)
        {
            return;
        }

        double scale = screen.Scaling;
        PixelRect work = screen.WorkingArea;
        int wPx = (int)Math.Ceiling(Width * scale);
        int hPx = (int)Math.Ceiling(Height * scale);
        int gap = (int)Math.Round(EdgeGapPx * scale);
        int offset = (int)Math.Round(_stackOffset * scale);

        int x = work.Right - wPx - gap;
        int y = work.Bottom - hPx - gap - offset;
        Position = new PixelPoint(x, y);
    }
}
