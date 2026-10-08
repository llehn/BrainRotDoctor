using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// The app's visual language: warm grey surfaces, near-black primary actions,
/// teal for "allowed" and orange for "blocked", in light and dark variants.
/// Colours are theme-aware resources referenced with
/// <see cref="DynamicResourceExtension"/>; the factory helpers build
/// consistently-styled controls.
/// </summary>
internal static class UiTheme
{
    public const string AppBg = "Brd.AppBg";
    public const string Sidebar = "Brd.Sidebar";
    public const string Surface = "Brd.Surface";
    public const string SurfaceAlt = "Brd.SurfaceAlt";
    public const string Track = "Brd.Track";
    public const string Selected = "Brd.Selected";
    public const string Border_ = "Brd.Border";
    public const string BorderStrong = "Brd.BorderStrong";
    public const string TextPrimary = "Brd.TextPrimary";
    public const string TextSecondary = "Brd.TextSecondary";
    public const string TextTertiary = "Brd.TextTertiary";
    public const string Ink = "Brd.Ink";
    public const string InkText = "Brd.InkText";
    public const string Accent = "Brd.Accent";
    public const string AccentText = "Brd.AccentText";
    public const string AllowSoft = "Brd.AllowSoft";
    public const string Danger = "Brd.Danger";
    public const string BlockSoft = "Brd.BlockSoft";
    public const string Success = "Brd.Success";
    public const string Warn = "Brd.Warn";
    public const string WarnSoft = "Brd.WarnSoft";
    public const string WarnBorder = "Brd.WarnBorder";
    public const string WarnText = "Brd.WarnText";

    public const string MonoFont = "Cascadia Mono, Consolas, Courier New, monospace";

    public static ResourceDictionary BuildPalette()
    {
        var light = new ResourceDictionary
        {
            [AppBg] = B("#F4F3F0"),
            [Sidebar] = B("#ECEBE7"),
            [Surface] = B("#FFFFFF"),
            [SurfaceAlt] = B("#F8F7F4"),
            [Track] = B("#ECEBE7"),
            [Selected] = B("#FFFFFF"),
            [Border_] = B("#E2E0DA"),
            [BorderStrong] = B("#D6D3CC"),
            [TextPrimary] = B("#16171B"),
            [TextSecondary] = B("#4F525A"),
            [TextTertiary] = B("#6B6E76"),
            [Ink] = B("#16171B"),
            [InkText] = B("#FFFFFF"),
            [Accent] = B("#0B6B63"),
            [AccentText] = B("#FFFFFF"),
            [AllowSoft] = B("#E1F0EE"),
            [Danger] = B("#B93D0E"),
            [BlockSoft] = B("#FBE9E1"),
            [Success] = B("#0B6B63"),
            [Warn] = B("#B07400"),
            [WarnSoft] = B("#FBF0D9"),
            [WarnBorder] = B("#EBD3A0"),
            [WarnText] = B("#5C3D00"),
        };

        var dark = new ResourceDictionary
        {
            [AppBg] = B("#17181B"),
            [Sidebar] = B("#1C1D21"),
            [Surface] = B("#232428"),
            [SurfaceAlt] = B("#1F2024"),
            [Track] = B("#2C2D33"),
            [Selected] = B("#3A3B42"),
            [Border_] = B("#313238"),
            [BorderStrong] = B("#44464D"),
            [TextPrimary] = B("#ECECEE"),
            [TextSecondary] = B("#B4B6BD"),
            [TextTertiary] = B("#8E9098"),
            [Ink] = B("#ECECEE"),
            [InkText] = B("#16171B"),
            [Accent] = B("#2A9D8F"),
            [AccentText] = B("#FFFFFF"),
            [AllowSoft] = B("#173A36"),
            [Danger] = B("#D2602F"),
            [BlockSoft] = B("#45261A"),
            [Success] = B("#3DBBA9"),
            [Warn] = B("#E2A93B"),
            [WarnSoft] = B("#3A2E14"),
            [WarnBorder] = B("#5C4920"),
            [WarnText] = B("#F2C76B"),
        };

        var res = new ResourceDictionary();
        res.ThemeDictionaries[ThemeVariant.Dark] = dark;
        res.ThemeDictionaries[ThemeVariant.Light] = light;
        return res;
    }

    /// <summary>
    /// The Fluent control theme with its accent (check boxes, focus rings,
    /// selections) pinned to the app's teal instead of the Windows accent colour.
    /// </summary>
    public static Avalonia.Themes.Fluent.FluentTheme BuildFluentTheme() => new()
    {
        Palettes =
        {
            [ThemeVariant.Light] = new Avalonia.Themes.Fluent.ColorPaletteResources { Accent = Color.Parse("#0B6B63") },
            [ThemeVariant.Dark] = new Avalonia.Themes.Fluent.ColorPaletteResources { Accent = Color.Parse("#2A9D8F") },
        },
    };

    /// <summary>A few global control tweaks (rounded inputs, taller fields).</summary>
    public static Styles BuildStyles()
    {
        var styles = new Styles();
        foreach (Type t in new[] { typeof(TextBox), typeof(NumericUpDown), typeof(ComboBox) })
        {
            var s = new Style(x => x.OfType(t));
            s.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(9)));
            s.Setters.Add(new Setter(Layoutable.MinHeightProperty, 40.0));
            styles.Add(s);
        }

        var text = new Style(x => x.OfType<TextBox>());
        text.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        styles.Add(text);
        return styles;
    }

    private static SolidColorBrush B(string hex) => new(Color.Parse(hex));

    /// <summary>Makes an unchecked check box visible on an ink (inverted) surface.</summary>
    public static void OnInk(CheckBox box)
    {
        foreach ((ThemeVariant variant, string hex) in new[] { (ThemeVariant.Light, "#FFFFFF"), (ThemeVariant.Dark, "#16171B") })
        {
            box.Resources.ThemeDictionaries[variant] = new ResourceDictionary
            {
                ["CheckBoxCheckBackgroundStrokeUnchecked"] = B(hex),
                ["CheckBoxCheckBackgroundStrokeUncheckedPointerOver"] = B(hex),
                ["CheckBoxCheckBackgroundStrokeUncheckedPressed"] = B(hex),
                ["CheckBoxForegroundUnchecked"] = B(hex),
                ["CheckBoxForegroundUncheckedPointerOver"] = B(hex),
                ["CheckBoxForegroundChecked"] = B(hex),
                ["CheckBoxForegroundCheckedPointerOver"] = B(hex),
            };
        }
    }

    public static DynamicResourceExtension Dyn(string key) => new(key);

    // ---- Text ----

    public static TextBlock H1(string text) => Text(text, 26, FontWeight.SemiBold, TextPrimary);

    public static TextBlock H2(string text) => Text(text, 16, FontWeight.SemiBold, TextPrimary);

    public static TextBlock Body(string text) => Text(text, 14, FontWeight.Normal, TextPrimary);

    public static TextBlock Muted(string text) => Text(text, 13, FontWeight.Normal, TextSecondary);

    public static TextBlock Caption(string text) => Text(text, 12.5, FontWeight.Normal, TextTertiary);

    public static TextBlock Strong(string text, double size = 14) => Text(text, size, FontWeight.SemiBold, TextPrimary);

    public static TextBlock Mono(string text, string fgKey = TextPrimary)
    {
        TextBlock t = Text(text, 12.5, FontWeight.Normal, fgKey);
        t.FontFamily = new FontFamily(MonoFont);
        t.TextWrapping = TextWrapping.Wrap;
        return t;
    }

    public static TextBlock SectionLabel(string text)
    {
        TextBlock t = Text(text.ToUpper(Loc.Culture), 11.5, FontWeight.SemiBold, TextTertiary);
        t.LetterSpacing = 0.7;
        return t;
    }

    public static TextBlock Text(string text, double size, FontWeight weight, string fgKey) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = weight,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        [!TextBlock.ForegroundProperty] = Dyn(fgKey),
    };

    // ---- Surfaces ----

    public static Border Card(Control content, double padding = 20) => new()
    {
        Child = content,
        CornerRadius = new CornerRadius(14),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(padding),
        [!Border.BackgroundProperty] = Dyn(Surface),
        [!Border.BorderBrushProperty] = Dyn(Border_),
    };

    public static Border Chip(string text, string bgKey, string fgKey) => new()
    {
        CornerRadius = new CornerRadius(20),
        Padding = new Thickness(9, 3, 9, 4),
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Left,
        [!Border.BackgroundProperty] = Dyn(bgKey),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            [!TextBlock.ForegroundProperty] = Dyn(fgKey),
        },
    };

    public static Border Dot(string colorKey, double size = 8) => new()
    {
        Width = size,
        Height = size,
        CornerRadius = new CornerRadius(size / 2),
        VerticalAlignment = VerticalAlignment.Center,
        [!Border.BackgroundProperty] = Dyn(colorKey),
    };

    public static Border Divider() => new() { Height = 1, [!Border.BackgroundProperty] = Dyn(Border_) };

    /// <summary>A rounded square with a site's initials, standing in for its logo.</summary>
    public static Border Monogram(string? catalogId, string label, double size = 36)
    {
        (string bg, string fg, string text) = MonogramStyle(catalogId, label);
        return new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.28),
            Background = B(bg),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = text,
                FontSize = size * 0.36,
                FontWeight = FontWeight.Bold,
                Foreground = B(fg),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }

    private static (string Bg, string Fg, string Text) MonogramStyle(string? catalogId, string label) => catalogId switch
    {
        "instagram" => ("#F6DCE8", "#8A1F4E", "Ig"),
        "youtube" => ("#FADBD6", "#8E2214", "Yt"),
        "tiktok" => ("#DDE9EA", "#1B4F54", "Tt"),
        "facebook" => ("#DCE5FA", "#1F3E8C", "Fb"),
        "x" => ("#E3E3E6", "#16171B", "X"),
        "reddit" => ("#FBE2D3", "#8A3A0C", "Rd"),
        "linkedin" => ("#D9E7F5", "#17436E", "In"),
        "twitch" => ("#E6DEF7", "#4A2A8A", "Tw"),
        "pinterest" => ("#F7DADA", "#8C1C1C", "Pi"),
        _ => ("#E6E4DE", "#3D3F45", Initials(label)),
    };

    private static string Initials(string label)
    {
        string letters = new(label.Where(char.IsLetterOrDigit).Take(2).ToArray());
        return letters.Length == 0 ? "?" : char.ToUpperInvariant(letters[0]) + letters[1..].ToLowerInvariant();
    }

    // ---- Buttons ----

    public static PillButton Primary(string text, string? icon = null) => new(text, PillKind.Primary, icon);

    public static PillButton Secondary(string text, string? icon = null) => new(text, PillKind.Secondary, icon);

    public static PillButton Ghost(string text, string? icon = null) => new(text, PillKind.Ghost, icon);

    public static PillButton DangerText(string text, string? icon = null) => new(text, PillKind.Danger, icon);

    public static PillButton AccentText_(string text, string? icon = null) => new(text, PillKind.Accent, icon);

    public static PillButton IconButton(string icon, string tip)
    {
        var b = new PillButton("", PillKind.Icon, icon) { Width = 40, Height = 40 };
        ToolTip.SetTip(b, tip);
        return b;
    }

    public static StackPanel HStack(double spacing, params Control[] children)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (Control c in children)
        {
            c.VerticalAlignment = VerticalAlignment.Center;
            s.Children.Add(c);
        }

        return s;
    }

    public static StackPanel VStack(double spacing, params Control[] children)
    {
        var s = new StackPanel { Spacing = spacing };
        foreach (Control c in children)
        {
            s.Children.Add(c);
        }

        return s;
    }
}

internal enum PillKind
{
    Primary,
    Secondary,
    Ghost,
    Danger,
    Accent,
    Icon,
}

/// <summary>A rounded button drawn from a Border, with full control over its colours.</summary>
internal sealed class PillButton : Border
{
    private readonly TextBlock _label;
    private readonly PillKind _kind;
    private bool _enabled = true;

    public PillButton(string text, PillKind kind, string? icon = null)
    {
        _kind = kind;
        string fg = kind switch
        {
            PillKind.Primary => UiTheme.InkText,
            PillKind.Ghost => UiTheme.TextSecondary,
            PillKind.Danger => UiTheme.Danger,
            PillKind.Accent => UiTheme.Accent,
            _ => UiTheme.TextPrimary,
        };

        _label = new TextBlock
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBlock.ForegroundProperty] = UiTheme.Dyn(fg),
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (icon is not null)
        {
            row.Children.Add(Icons.Make(icon, 16, fg));
        }

        if (text.Length > 0)
        {
            row.Children.Add(_label);
        }

        Child = row;
        CornerRadius = new CornerRadius(10);
        MinHeight = 40;
        Padding = kind switch
        {
            PillKind.Icon => new Thickness(0),
            PillKind.Ghost or PillKind.Danger or PillKind.Accent => new Thickness(10, 0),
            _ => new Thickness(icon is null ? 18 : 14, 0, 18, 0),
        };
        Cursor = new Cursor(StandardCursorType.Hand);
        VerticalAlignment = VerticalAlignment.Center;
        Background = Brushes.Transparent;

        switch (kind)
        {
            case PillKind.Primary:
                this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.Ink);
                break;
            case PillKind.Secondary:
            case PillKind.Icon:
                this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface);
                BorderThickness = new Thickness(1);
                this[!BorderBrushProperty] = UiTheme.Dyn(UiTheme.BorderStrong);
                break;
        }

        PointerEntered += (_, _) => { if (_enabled) Opacity = 0.82; };
        PointerExited += (_, _) => { if (_enabled) Opacity = 1; };
        AddHandler(Gestures.TappedEvent, (_, _) =>
        {
            if (_enabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public event EventHandler? Click;

    public string Text
    {
        get => _label.Text ?? "";
        set => _label.Text = value;
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

    public PillKind Kind => _kind;
}

/// <summary>
/// A row of mutually exclusive options on a grey track; the selected option is
/// raised (or filled with its own colour, e.g. orange for Block, teal for Allow).
/// </summary>
internal sealed class Segmented : Border
{
    private readonly List<(Border Cell, TextBlock Label, string? FillKey)> _cells = new();
    private int _selected;

    public Segmented(IReadOnlyList<(string Text, string? FillKey)> options, int selected, bool stretch = true, double height = 38)
    {
        _selected = selected;
        Padding = new Thickness(3);
        CornerRadius = new CornerRadius(10);
        this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.Track);

        var grid = new Grid();
        for (int i = 0; i < options.Count; i++)
        {
            grid.ColumnDefinitions.Add(stretch ? new ColumnDefinition(GridLength.Star) : new ColumnDefinition(GridLength.Auto));
            int index = i;
            var label = new TextBlock
            {
                Text = options[i].Text,
                FontSize = 13.5,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            var cell = new Border
            {
                MinHeight = height - 6,
                MinWidth = 64,
                Padding = new Thickness(10, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(i == 0 ? 0 : 2, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = label,
            };
            cell.AddHandler(Gestures.TappedEvent, (_, _) => Select(index, raise: true));
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
            _cells.Add((cell, label, options[i].FillKey));
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
            (Border cell, TextBlock label, string? fill) = _cells[i];
            bool on = i == _selected;
            if (!on)
            {
                cell.Background = Brushes.Transparent;
                cell.BoxShadow = default;
                label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.TextSecondary);
            }
            else if (fill is not null)
            {
                cell[!Border.BackgroundProperty] = UiTheme.Dyn(fill);
                cell.BoxShadow = default;
                label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.AccentText);
            }
            else
            {
                cell[!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Selected);
                cell.BoxShadow = new BoxShadows(new BoxShadow { OffsetY = 1, Blur = 3, Color = Color.FromArgb(40, 0, 0, 0) });
                label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.TextPrimary);
            }
        }
    }
}
