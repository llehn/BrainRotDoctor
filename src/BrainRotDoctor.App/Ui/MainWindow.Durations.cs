using Avalonia.Controls;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- How long: one picker for strict mode and for a pause ----------

    private enum DurationUse { Strict, Pause }

    /// <summary>A custom length as picked: an amount of a unit (0 minutes, 1 hours, 2 days).</summary>
    private sealed class CustomLength
    {
        public int Amount { get; set; }

        public int Unit { get; set; }

        public TimeSpan Length => Unit switch
        {
            0 => TimeSpan.FromMinutes(Amount),
            2 => TimeSpan.FromDays(Amount),
            _ => TimeSpan.FromHours(Amount),
        };
    }

    private readonly CustomLength _strictCustom = new() { Amount = 3, Unit = 1 };
    private readonly CustomLength _pauseCustom = new() { Amount = 45, Unit = 0 };

    /// <summary>
    /// The same choices wherever a length is picked: a short one, an hour, two, the
    /// work day (while there's enough of it left) and the rest of the day.
    /// </summary>
    private static IEnumerable<(string Label, string Caption, TimeSpan Length)> Lengths(DateTimeOffset now)
    {
        foreach ((string label, TimeSpan length) in new[]
        {
            (Loc.T("len_30m"), TimeSpan.FromMinutes(30)),
            (Loc.T("pause_1h"), TimeSpan.FromHours(1)),
            (Loc.T("len_2h"), TimeSpan.FromHours(2)),
        })
        {
            yield return (label, Loc.T("until_time", Clock(now + length)), length);
        }

        var evening = new DateTimeOffset(now.Date.AddHours(18), now.Offset);
        if (evening - now >= TimeSpan.FromHours(2))
        {
            yield return (Loc.T("until_clock", Clock(evening)), Minutes(evening - now), evening - now);
        }

        yield return (Loc.T("len_today"), Loc.T("until_midnight"), new DateTimeOffset(now.Date.AddDays(1), now.Offset) - now);
    }

    /// <summary>
    /// The choices as tiles, plus Custom, which opens an amount and a unit with a
    /// button of its own. A pick calls <paramref name="start"/> with its length.
    /// A pause can't outlast <see cref="PauseState.Longest"/>: longer choices show
    /// but can't be picked, and a custom length stops there.
    /// </summary>
    private Control DurationPicker(DurationUse use, Action<TimeSpan> start)
    {
        bool pause = use == DurationUse.Pause;
        CustomLength custom = pause ? _pauseCustom : _strictCustom;
        TimeSpan? longest = pause ? PauseState.Longest : null;
        var host = new ContentControl();
        bool customOpen = false;

        void Build()
        {
            var tiles = new List<Control>();
            foreach ((string label, string caption, TimeSpan length) in Lengths(DateTimeOffset.Now))
            {
                bool fits = longest is not { } max || length <= max;
                var tile = new ChoiceTile(label, fits ? caption : Loc.T("over_hours", (int)longest!.Value.TotalHours), selected: false, available: fits);
                tile.Click += (_, _) => start(length);
                tiles.Add(tile);
            }

            var customTile = new ChoiceTile(Loc.T("len_custom"), Loc.T("pick_a_length"), customOpen);
            customTile.Click += (_, _) =>
            {
                customOpen = !customOpen;
                Build();
            };
            tiles.Add(customTile);

            StackPanel body = Layout.Column(Space.S3, Layout.Tiles(tiles, 112, Space.S2));
            if (customOpen)
            {
                body.Children.Add(CustomLengthRow(use, custom, longest, start, Build));
            }

            host.Content = body;
        }

        Build();
        return host;
    }

    private static Control CustomLengthRow(DurationUse use, CustomLength custom, TimeSpan? longest, Action<TimeSpan> start, Action rebuild)
    {
        bool pause = use == DurationUse.Pause;
        int Most(int unit) => longest is not { } max ? 999 : unit == 0 ? (int)max.TotalMinutes : (int)max.TotalHours;
        custom.Amount = Math.Clamp(custom.Amount, 1, Most(custom.Unit));

        var amount = new Stepper(custom.Amount, 1, Most(custom.Unit), Loc.T("shorter"), Loc.T("longer"));
        var units = new List<Segment> { new(Loc.T("minutes")), new(Loc.T("hours")) };
        if (!pause)
        {
            units.Add(new(Loc.T("days")));
        }

        var unit = new Segmented(units, custom.Unit, stretch: false);
        var ends = new TextLine("");
        var go = new PillButton("", ButtonKind.Primary, pause ? Glyph.Pause : Glyph.Lock);

        void Refresh()
        {
            DateTimeOffset end = DateTimeOffset.Now + custom.Length;
            if (pause)
            {
                ends.SetText(Loc.T("resumes_at", Clock(end)));
                go.Text = Loc.T("pause_until_btn", Clock(end));
            }
            else
            {
                ends.SetPhrases(new Phrase(Loc.T("unlocks") + " "), new Phrase(WhenText(end), Strong: true));
                go.Text = Loc.T("lock_until_btn", WhenText(end));
            }
        }

        amount.Changed += value =>
        {
            custom.Amount = value;
            Refresh();
        };
        unit.Changed += i =>
        {
            custom.Unit = i;
            rebuild();
        };
        go.Click += (_, _) => start(custom.Length);

        Refresh();
        StackPanel column = Layout.Column(Space.S3,
            Layout.Row(10, amount, unit),
            new Card(Layout.Row(10, new Icon(Glyph.Clock, 18), ends), CardStyle.Inset, CardPadding.Compact),
            go);
        if (longest is { } max)
        {
            column.Children.Add(new TextLine(Loc.T("pause_max_note", (int)max.TotalHours), TextStyle.Caption));
        }

        return column;
    }
}
