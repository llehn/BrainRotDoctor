using Avalonia.Controls;
using Avalonia.Layout;
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

    /// <summary>The picker's width everywhere: three choices across, wide enough for the longest names.</summary>
    private const double PickerWidth = 456;

#if DEBUG
    /// <summary>For the snapshots: pickers start with Custom open.</summary>
    internal bool PreviewCustomOpen { get; set; }
#else
    private const bool PreviewCustomOpen = false;
#endif

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
    /// A pause can't outlast <see cref="PauseState.Longest"/>: a longer choice isn't
    /// offered, and a custom length stops there, saying so once it gets there.
    /// </summary>
    private Control DurationPicker(DurationUse use, Action<TimeSpan> start)
    {
        bool pause = use == DurationUse.Pause;
        CustomLength custom = pause ? _pauseCustom : _strictCustom;
        TimeSpan? longest = pause ? PauseState.Longest : null;

        // One size wherever it is: three tiles across, in a menu, a banner or a card alike.
        var host = new ContentControl { Width = PickerWidth, HorizontalAlignment = HorizontalAlignment.Left };
        bool customOpen = PreviewCustomOpen;

        void Build()
        {
            var tiles = new List<Control>();
            foreach ((string label, string caption, TimeSpan length) in Lengths(DateTimeOffset.Now))
            {
                if (longest is { } max && length > max)
                {
                    continue;
                }

                var tile = new ChoiceTile(label, caption, false);
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

        // The button says when it ends; it is as wide as its words, like every button.
        string Label()
        {
            DateTimeOffset end = DateTimeOffset.Now + custom.Length;
            return Loc.T(pause ? "pause_until_btn" : "lock_until_btn", WhenText(end));
        }

        var go = new PillButton(Label(), ButtonKind.Primary, pause ? Glyph.Pause : Glyph.Lock) { HorizontalAlignment = HorizontalAlignment.Left };

        // The limit is mentioned only to someone who runs into it.
        var limit = new TextLine(longest is { } most ? Loc.T("pause_max_note", (int)most.TotalHours) : "", TextStyle.Caption);
        void Refresh()
        {
            go.Text = Label();
            limit.IsVisible = longest is not null && custom.Amount >= Most(custom.Unit);
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
        return Layout.Column(Space.S3, Layout.Row(10, amount, unit), go, limit);
    }
}
