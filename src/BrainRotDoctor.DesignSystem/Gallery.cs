using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace BrainRotDoctor.DesignSystem;

/// <summary>
/// A living catalogue of the design system: every token and every control in
/// its variants and states, built from the real components. Rendered to the
/// gallery images in docs/ (light and dark) by the app's DEBUG snapshot tool.
/// </summary>
public static class Gallery
{
    /// <summary>The whole catalogue as one page.</summary>
    public static Control Build()
    {
        var page = new StackPanel { Spacing = Space.S8 };
        page.Children.Add(new PageHeader("Design system", "Every token and control, in its variants and states."));
        page.Children.Add(Section("Colour", Colours()));
        page.Children.Add(Section("Type", TypeScale()));
        page.Children.Add(Section("Icons", Icons()));
        page.Children.Add(Section("Buttons", Buttons()));
        page.Children.Add(Section("Selection", Selection()));
        page.Children.Add(Section("Input", Inputs()));
        page.Children.Add(Section("Surfaces", Surfaces()));
        page.Children.Add(Section("Status", Indicators()));
        page.Children.Add(Section("Maps of time", Maps()));
        page.Children.Add(Section("Navigation and lists", NavigationAndLists()));
        return new Border { Padding = new Thickness(Space.S8), Child = page, [!Border.BackgroundProperty] = Palette.Dyn(Palette.AppBg) };
    }

    private static Control Section(string title, Control content) =>
        Layout.Column(Space.S3, new TextLine(title, TextStyle.Overline), content);

    private static Control Labeled(string label, Control control) =>
        Layout.Column(6, new TextLine(label, TextStyle.Caption), control);

    private static WrapPanel Wrap(double gap, params Control[] items)
    {
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = gap, LineSpacing = gap };
        foreach (Control item in items)
        {
            item.VerticalAlignment = VerticalAlignment.Top;
            wrap.Children.Add(item);
        }

        return wrap;
    }

    private static Control Colours()
    {
        var swatches = new List<Control>();
        foreach ((string key, _, _) in Palette.Values)
        {
            var chip = new Border
            {
                Width = 120,
                Height = 44,
                CornerRadius = new CornerRadius(Radius.Small),
                BorderThickness = new Thickness(1),
                [!Border.BackgroundProperty] = Palette.Dyn(key),
                [!Border.BorderBrushProperty] = Palette.Dyn(Palette.Border),
            };
            swatches.Add(Layout.Column(Space.S1, chip, new TextLine(key["Ds.".Length..], TextStyle.Caption)));
        }

        return Wrap(Space.S3, swatches.ToArray());
    }

    private static Control TypeScale() => new Card(Layout.Column(Space.S3,
        Enum.GetValues<TextStyle>().Select(style => (Control)Layout.Row(Space.S4,
            new TextLine(style.ToString(), TextStyle.Caption) { Width = 110 },
            new TextLine(style == TextStyle.Display ? "3:59:12" : "The quick brown fox", style))).ToArray()));

    private static Control Icons() => Wrap(Space.S4, Glyph.All.Select(g => (Control)Layout.Column(Space.S1,
        new Icon(g.Data, 22) { HorizontalAlignment = HorizontalAlignment.Center },
        new TextLine(g.Name, TextStyle.Caption) { Centered = true, Width = 80 })).ToArray());

    private static Control Buttons()
    {
        Control[] kinds = Enum.GetValues<ButtonKind>()
            .Where(k => k is not (ButtonKind.OnInk or ButtonKind.GhostOnInk))
            .Select(k => Labeled(k.ToString(), new PillButton(k.ToString(), k, k == ButtonKind.Primary ? Glyph.Plus : null))).ToArray();
        var disabled = new PillButton("Disabled", ButtonKind.Primary) { Enabled = false };
        var onInk = new Card(Layout.Row(Space.S3, new PillButton("OnInk", ButtonKind.OnInk, Glyph.Lock), new PillButton("GhostOnInk", ButtonKind.GhostOnInk, size: ButtonSize.Small)), CardStyle.Ink, CardPadding.Compact);
        return Layout.Column(Space.S4,
            Wrap(Space.S4, kinds.Append(Labeled("OnInk, GhostOnInk", onInk)).ToArray()),
            Labeled("HoldButton", new HoldButton("Hold to confirm", TimeSpan.FromSeconds(3)) { Width = 320 }),
            Wrap(Space.S4,
                Labeled("Small", new PillButton("Small", ButtonKind.Accent, size: ButtonSize.Small)),
                Labeled("Regular", new PillButton("Regular")),
                Labeled("Large", new PillButton("Large", ButtonKind.Secondary, Glyph.Lock, ButtonSize.Large)),
                Labeled("Disabled", disabled),
                Labeled("IconButton", new IconButton(Glyph.Pencil, "Edit")),
                Labeled("IconButton selected", new IconButton(Glyph.Pencil, "Edit", selected: true)),
                Labeled("IconButton bare", new IconButton(Glyph.Close, "Remove", IconButtonStyle.Bare))));
    }

    private static Control Selection()
    {
        var disabledSwitch = new OnOffSwitch(true) { Enabled = false };
        return Layout.Column(Space.S4,
            Wrap(Space.S6,
                Labeled("Segmented", new Segmented(new Segment[] { new("All day"), new("Set hours") }, 1, stretch: false)),
                Labeled("Segmented, toned", new Segmented(new Segment[] { new("Block", Tone.Danger), new("Allow", Tone.Accent) }, 0, stretch: false, height: 36)),
                Labeled("OnOffSwitch", Layout.Row(Space.S3, new OnOffSwitch(true), new OnOffSwitch(false), disabledSwitch))),
            Wrap(Space.S6,
                Labeled("RadioPill", Layout.Row(Space.S2, new RadioPill("Only this page", true), new RadioPill("Everything under it", false))),
                Labeled("RoundToggle", Layout.Row(5, new RoundToggle("Mo", true, "Monday"), new RoundToggle("Tu", true, "Tuesday"), new RoundToggle("Sa", false, "Saturday"))),
                Labeled("CheckOption", Layout.Row(Space.S3, new CheckOption(true, new TextLine("Checked")), new CheckOption(false, new TextLine("Unchecked"))))),
            Wrap(Space.S6,
                Labeled("CheckRow", new CheckRow("Feeds", "5 min / hour · All day", true) { Width = 280 }),
                Labeled("ChoiceTile", Layout.Row(Space.S2, new ChoiceTile("4 hours", "until 15:00", true) { Width = 150 }, new ChoiceTile("1 day", "until tomorrow", false) { Width = 150 })),
                Labeled("ChoiceTile, warn", Layout.Row(Space.S2, new ChoiceTile("5 min", "until 14:05", true, ChoiceTileStyle.Warn) { Width = 130 }, new ChoiceTile("10 min", "until 14:10", false, ChoiceTileStyle.Warn) { Width = 130 })),
                Labeled("ChoiceTile, on ink, badge", new Card(Layout.Row(Space.S2,
                    new ChoiceTile("A long one", "until 18:00", true, ChoiceTileStyle.OnInk, "Badge") { Width = 150 },
                    new ChoiceTile("A short one", "until 15:00", false, ChoiceTileStyle.OnInk) { Width = 150 }), CardStyle.Ink, CardPadding.Compact)),
                Labeled("PickTile", Layout.Row(Space.S3,
                    new PickTile(new TextLine("Picked", TextStyle.Strong), picked: true) { Width = 150 },
                    new PickTile(new TextLine("Not picked", TextStyle.Strong), picked: false) { Width = 150 },
                    new PickTile(new TextLine("Locked", TextStyle.Strong), picked: false, locked: true) { Width = 150 }))));
    }

    private static Control Inputs() => Wrap(Space.S6,
        Labeled("TextField", new TextField("", "Placeholder") { Width = 220 }),
        Labeled("TextField, code + prefix", new TextField("/reels/", style: TextFieldStyle.Code, prefix: "example.com") { Width = 260 }),
        Labeled("TextField, large + icon", new TextField("", "Search", TextFieldStyle.Large, Glyph.Search) { Width = 260 }),
        Labeled("TextField, title", new TextField("Editable title", style: TextFieldStyle.Title) { Width = 220 }),
        Labeled("Dropdown", new Dropdown(new[] { "Automatic", "English", "Deutsch" }, 1) { MinWidth = 200 }),
        Labeled("Stepper", new Stepper(5, 1, 59, "Fewer", "More")),
        Labeled("Stepper, slow up", new Stepper(15, 1, 1440, "Fewer", "More", repeatUp: false)),
        Labeled("TimeField", new TimeField(new TimeOnly(9, 0), "Done") { Width = 140 }));

    private static Control Surfaces() => Layout.Column(Space.S4,
        Wrap(Space.S4, Enum.GetValues<CardStyle>().Select(style => (Control)new Card(
            new TextLine(style.ToString(), TextStyle.Strong, style == CardStyle.Ink ? TextTone.OnInk : TextTone.Primary),
            style) { Width = 180 }).ToArray()),
        new Card(Layout.Column(0,
            new Band(new TextLine("Band"), new Thickness(Space.S5, Space.S3)),
            new Divider(),
            new Band(new TextLine("Band, shaded"), new Thickness(Space.S5, Space.S3), shaded: true)), CardStyle.Surface, CardPadding.None),
        new Banner(Tone.Warn, Glyph.Pause, Banner.Message(Tone.Warn, new Phrase("Paused until 15:00.", true), new Phrase(" A temporary state.")), new PillButton("Resume now", ButtonKind.Primary)),
        new Banner(Tone.Danger, Glyph.Ban, Banner.Message(Tone.Danger, new Phrase("Couldn't save: an error.", true))),
        Wrap(Space.S4,
            new Callout(Tone.Danger, "Blocked", "What happened and why.") { Width = 260 },
            new Callout(Tone.Accent, "Allowed", "What happened and why.") { Width = 260 },
            new Callout(Tone.Neutral, "Not covered", "What happened and why.") { Width = 260 }),
        new PlaceholderTile(Glyph.Plus, "New item", "Start something new") { Width = 260, Height = 160 });

    private static Control Indicators()
    {
        Tone[] tones = Enum.GetValues<Tone>();
        var status = new StatusPanel { Width = 220 };
        status.SetState(Tone.Success, "Protected", "2 of 2 rules active", inverted: false);
        var statusInk = new StatusPanel { Width = 220 };
        statusInk.SetState(Tone.Ink, "Strict mode", "3 h 59 min left", inverted: true);
        return Layout.Column(Space.S4,
            Wrap(Space.S3, tones.Select(t => (Control)new Chip(t.ToString(), t)).ToArray()),
            Wrap(Space.S4, tones.Select(t => (Control)Layout.Row(6, new Dot(t), new TextLine(t.ToString(), TextStyle.Caption))).ToArray()),
            Wrap(Space.S6,
                Labeled("Meter", new Meter(0.6, Tone.Accent) { Width = 220 }),
                Labeled("Meter, spent", new Meter(1, Tone.Danger) { Width = 220 }),
                Labeled("Meter, on ink", new Card(new Meter(0.3, Tone.Ink, onInk: true) { Width = 200 }, CardStyle.Ink, CardPadding.Compact))),
            Wrap(Space.S6,
                Labeled("Monogram", Layout.Row(Space.S2, new Monogram("Ab", Swatch.Neutral), new Monogram("Cd", new Swatch("#DCE5FA", "#1F3E8C")), new Monogram("Ef", Swatch.Neutral, 18))),
                Labeled("InfoChip", new InfoChip(new Monogram("Ab", Swatch.Neutral, 18), "Label", "detail")),
                Labeled("SuggestionChip", new SuggestionChip("example.com/page/")),
                Labeled("RemovableChip", new RemovableChip("key = value", "Remove")),
                Labeled("StatusPanel", Layout.Row(Space.S3, status, statusInk))));
    }

    private static Control Maps()
    {
        var parts = new TileMap();
        parts.Add(new MapTile("/section", 0, TimeSpan.FromMinutes(70), nested: true), 70);
        parts.Add(new MapTile("The rest", 0, TimeSpan.FromMinutes(25), nested: true), 25);

        var map = new TileMap { Width = 900, Height = 360 };
        map.Add(new MapTile("Two hours", 0, TimeSpan.FromMinutes(134), parts), 134);
        map.Add(new MapTile("About an hour and a half", 1, TimeSpan.FromMinutes(96)), 96);
        map.Add(new MapTile("Forty minutes", 2, TimeSpan.FromMinutes(41)), 41);
        map.Add(new MapTile("Twenty", 3, TimeSpan.FromMinutes(20)), 20);
        map.Add(new MapTile("Eight", 4, TimeSpan.FromMinutes(8)), 8);

        return Layout.Column(Space.S4,
            Wrap(Space.S6, new[] { 5, 25, 45, 90, 150, 200 }.Select(m =>
                (Control)Labeled($"ClockFill, {m} min", new ClockFill(TimeSpan.FromMinutes(m))))
                .Append(Labeled("Overlay", new Overlay(
                    new Card(new TextLine("The screen behind"), CardStyle.Surface, CardPadding.Roomy),
                    new Card(new TextLine("Sheet", TextStyle.Strong), CardStyle.Surface, CardPadding.Roomy)) { Width = 320, Height = 200 }))
                .ToArray()),
            Labeled("TileMap of MapTiles, one with nested tiles", map));
    }

    private static Control NavigationAndLists()
    {
        var nav = Layout.Column(Space.S1,
            new NavItem(Glyph.List, "Active item", true),
            new NavItem(Glyph.Lock, "With badge", false, "ON"),
            new NavItem(Glyph.Gear, "Inactive item", false));
        var navPanel = new Border { Width = 236, Padding = new Thickness(14), Child = nav, [!Border.BackgroundProperty] = Palette.Dyn(Palette.Sidebar) };

        return Layout.Column(Space.S4,
            Wrap(Space.S6,
                Labeled("NavItem", navPanel),
                Labeled("InfoRow", new InfoRow(Glyph.Lock, "Title", "Caption", new TextLine("Status", TextStyle.CaptionStrong, TextTone.Accent)) { Width = 360 })),
            Labeled("TopBar", new TopBar(new PillButton("Back", ButtonKind.Ghost, Glyph.ArrowLeft), new TextLine("Screen title", TextStyle.Heading), new PillButton("Save", ButtonKind.Primary))),
            Labeled("Group + FormRow", new Group("Section",
                new FormRow("Setting", "What it changes.", new Segmented(new Segment[] { new("One"), new("Two") }, 0, stretch: false)),
                new FormRow("Another", "What it changes.", new OnOffSwitch(true)))));
    }
}
