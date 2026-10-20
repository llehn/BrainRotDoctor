using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace BrainRotDoctor.DesignSystem;

/// <summary>
/// Wires the design system into an Avalonia application: fonts, the Fluent base
/// theme pinned to our accent, the palette, and the few global control tweaks.
/// </summary>
public static class DesignTheme
{
    /// <summary>The culture used for case changes (overlines); set it when the UI language changes.</summary>
    public static CultureInfo TextCulture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>Registers the bundled fonts. Call on the app builder.</summary>
    public static AppBuilder UseDesignSystem(this AppBuilder builder) => builder.WithInterFont();

    /// <summary>Installs styles and palette. Call from <see cref="Application.Initialize"/>.</summary>
    public static void Install(Application app)
    {
        app.Styles.Add(new FluentTheme
        {
            Palettes =
            {
                [ThemeVariant.Light] = new ColorPaletteResources { Accent = Color.Parse("#0B6B63") },
                [ThemeVariant.Dark] = new ColorPaletteResources { Accent = Color.Parse("#2A9D8F") },
            },
        });
        app.Styles.Add(GlobalStyles());
        app.Resources.MergedDictionaries.Add(Palette.Build());
    }

    private static Styles GlobalStyles()
    {
        var styles = new Styles();
        foreach (Type t in new[] { typeof(TextBox), typeof(ComboBox) })
        {
            var s = new Style(x => x.OfType(t));
            s.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(9)));
            s.Setters.Add(new Setter(Layoutable.MinHeightProperty, 40.0));
            styles.Add(s);
        }

        var text = new Style(x => x.OfType<TextBox>());
        text.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        styles.Add(text);

        // A dropdown is as wide as what it holds (each sets its own width), not capped by Fluent.
        var flyout = new Style(x => x.OfType<FlyoutPresenter>());
        flyout.Setters.Add(new Setter(Layoutable.MaxWidthProperty, double.PositiveInfinity));
        styles.Add(flyout);
        return styles;
    }
}

/// <summary>Every window of the app: the app background and the sans font.</summary>
public class AppWindow : Window
{
    public AppWindow()
    {
        FontFamily = new FontFamily(Fonts.Sans);
        this[!BackgroundProperty] = Palette.Dyn(Palette.AppBg);
    }

    protected override Type StyleKeyOverride => typeof(Window);
}
