# BrainRotDoctor design system

The design system is the only source of how BrainRotDoctor looks. It holds the
**tokens** (colour, type, spacing, shape) and every **control** a screen may
use. Screens in the app compose these controls; they never draw or style
anything themselves.

It is a separate library on purpose: it knows nothing about rules, websites or
strict mode, and it never references the app or `BrainRotDoctor.Core`.

| Document | What's in it |
|---|---|
| [Foundations](docs/foundations.md) | Colour palette (light and dark), type scale, spacing, shape, elevation, icons, layout rules |
| [Components](docs/components.md) | Every control: what it's for, its variants and states, when to use it and when not |
| [Gallery, light](docs/gallery-light.png) · [dark](docs/gallery-dark.png) | Every token and control rendered from the real code |

## The rules

1. **Screens only use design-system controls.** Besides them, screens may use
   Avalonia's plain layout panels (`Grid`, `StackPanel`, `DockPanel`,
   `WrapPanel`, `Panel`, `ScrollViewer`, `ContentControl`) and `Image`. Nothing
   else: no `Border`, `TextBlock`, `TextBox`, `CheckBox`, `ComboBox`, `Flyout`…
2. **Screens never style.** They don't create brushes, colours, fonts, cursors
   or shadows, don't set `Background`, `Foreground`, `BorderBrush`,
   `BorderThickness`, `CornerRadius`, `FontSize`, `FontWeight`, `FontFamily`,
   `Opacity` or `Cursor`, don't bind to theme resources, and don't wire pointer
   or tap events. They pick a control's *variant* and *tone* instead, and react
   to its own events (`Click`, `Changed`, `Toggled`…).
3. **Screens may lay out.** Margins, paddings, widths, heights, alignment and
   spacing are layout and stay with the screen. Use the [spacing scale](docs/foundations.md#spacing).
4. **The design system is domain-agnostic.** Names describe what a thing is
   (`PickTile`, `Glyph.List`), never where the app uses it. Domain knowledge
   (a website's brand colours, the words on a button) is passed in as data.
5. **Everything public is documented** in [components.md](docs/components.md).

Rules 1, 2, 4 (no app references) and 5 are enforced by tests in
`tests/BrainRotDoctor.App.Tests/DesignSystemRulesTests.cs`, which inspect the
compiled app. Breaking one fails the build, with a message naming the method
and what it did. The illustration code (the worm scene and the product icon)
and the DEBUG snapshot tool are exempt: they are drawings, not screens.

## Needing something that isn't there

Don't work around the rules in the screen. Instead:

1. Check [components.md](docs/components.md): often an existing control has
   the variant you need, or a composition of two does.
2. If not, **extend the design system**: add a variant to an existing control
   or a new control, named for what it is. Build it from the tokens only.
3. Document it in `docs/components.md` (purpose, variants, states, do / don't)
   and add it to the gallery (`Gallery.cs`).
4. Regenerate the gallery images (below) and use the new control in the screen.

## Using it

```csharp
// Startup
AppBuilder.Configure(...).UsePlatformDetect().UseDesignSystem();

// Application.Initialize
DesignTheme.Install(this);

// A screen
var save = new PillButton(Loc.T("save"), ButtonKind.Primary);
save.Click += (_, _) => Save();
var card = new Card(Layout.Column(Space.S3, new TextLine(title, TextStyle.Subheading), save));
```

Windows derive from `AppWindow`. When the UI language changes, set
`DesignTheme.TextCulture` (the app's `Loc` does this).

## Regenerating the gallery

A DEBUG build renders every screen and both gallery images:

```
dotnet run --project src/BrainRotDoctor.App -- --ui-snapshots <folder>
```

Copy `gallery-light.png` and `gallery-dark.png` from that folder to `docs/`.
