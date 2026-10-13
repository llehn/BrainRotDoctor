using Avalonia;
using Avalonia.Controls;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Strict mode ----------

    private enum StrictLength { OneHour, FourHours, RestOfDay, OneDay, OneWeek, Custom }

    private StrictLength _strictLength = StrictLength.FourHours;
    private int _strictAmount = 3;
    private int _strictUnit = 1; // 0 minutes, 1 hours, 2 days

    private TimeSpan StrictDuration()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return _strictLength switch
        {
            StrictLength.OneHour => TimeSpan.FromHours(1),
            StrictLength.FourHours => TimeSpan.FromHours(4),
            StrictLength.RestOfDay => now.Date.AddDays(1) - now.DateTime,
            StrictLength.OneDay => TimeSpan.FromDays(1),
            StrictLength.OneWeek => TimeSpan.FromDays(7),
            _ => _strictUnit switch
            {
                0 => TimeSpan.FromMinutes(Math.Max(1, _strictAmount)),
                2 => TimeSpan.FromDays(Math.Max(1, _strictAmount)),
                _ => TimeSpan.FromHours(Math.Max(1, _strictAmount)),
            },
        };
    }

    private static string WhenText(DateTimeOffset end)
    {
        DateTime today = DateTime.Today;
        DateTime day = end.LocalDateTime.Date;
        string clock = end.ToLocalTime().ToString("HH:mm", Loc.Culture);
        if (day == today)
        {
            return Loc.T("today_at", clock);
        }

        if (day == today.AddDays(1))
        {
            return Loc.T("tomorrow_at", clock);
        }

        return end.ToLocalTime().ToString("dddd d MMM, HH:mm", Loc.Culture);
    }

    /// <summary>
    /// Strict mode is a flag on until a time, so setting it up is only choosing
    /// how long. It knows nothing about rules; the rules side refuses anything
    /// but tightening while it is on.
    /// </summary>
    private Control BuildStrictSetup()
    {
        var page = new StackPanel { Spacing = Space.S6 };
        page.Children.Add(new PageHeader(Loc.T("strict_mode"), Loc.T("strict_subtitle")));

        var endsText = new TextLine("");

        void Refresh()
        {
            string ends = WhenText(DateTimeOffset.Now + StrictDuration());
            endsText.SetPhrases(new Phrase(Loc.T("unlocks") + " "), new Phrase(ends, Strong: true));
        }

        // How long
        var lengths = new (StrictLength Length, string Label, string Sub)[]
        {
            (StrictLength.OneHour, Loc.T("pause_1h"), ""),
            (StrictLength.FourHours, Loc.T("len_4h"), ""),
            (StrictLength.RestOfDay, Loc.T("len_today"), Loc.T("until_midnight")),
            (StrictLength.OneDay, Loc.T("len_1d"), ""),
            (StrictLength.OneWeek, Loc.T("len_1w"), ""),
            (StrictLength.Custom, Loc.T("len_custom"), Loc.T("pick_a_length")),
        };

        var lengthTiles = new List<Control>();
        var customRow = new StackPanel();
        foreach ((StrictLength length, string label, string sub) in lengths)
        {
            DateTimeOffset end = DateTimeOffset.Now + (length == StrictLength.Custom ? TimeSpan.Zero : DurationOf(length));
            string caption = sub.Length > 0 ? sub : Loc.T("until_time", length == StrictLength.OneWeek ? end.ToString("ddd HH:mm", Loc.Culture) : Clock(end));
            var tile = new ChoiceTile(label, caption, _strictLength == length);
            tile.Click += (_, _) =>
            {
                _strictLength = length;
                Rerender();
            };
            lengthTiles.Add(tile);
        }

        if (_strictLength == StrictLength.Custom)
        {
            var amount = new Stepper(_strictAmount, 1, 999, Loc.T("shorter"), Loc.T("longer"));
            amount.Changed += value => { _strictAmount = value; Refresh(); };
            var unit = new Segmented(new Segment[] { new(Loc.T("minutes")), new(Loc.T("hours")), new(Loc.T("days")) }, _strictUnit, stretch: false);
            unit.Changed += i => { _strictUnit = i; Refresh(); };
            customRow.Children.Add(Layout.Row(10, new TextLine(Loc.T("lock_for")), amount, unit));
        }

        var ends = new Card(Layout.Row(10, new Icon(Glyph.Clock, 18), endsText), CardStyle.Inset, CardPadding.Compact);

        var howLong = new Card(Layout.Column(Space.S4, new TextLine(Loc.T("how_long"), TextStyle.Subheading), Layout.Tiles(lengthTiles, 140, Space.S2), customRow, ends), CardStyle.Surface, CardPadding.Roomy);

        // While it's on + turning it on
        Control Consequence(string glyph, TextTone tone, string text) =>
            Layout.Row(10, new Icon(glyph, 18, tone), new TextLine(text) { MaxWidth = 240 });

        var whileOn = new Card(Layout.Column(14,
            new TextLine(Loc.T("while_on"), TextStyle.Subheading),
            Consequence(Glyph.Ban, TextTone.Danger, Loc.T("strict_no_edit")),
            Consequence(Glyph.Ban, TextTone.Danger, Loc.T("while_no_pause")),
            Consequence(Glyph.Ban, TextTone.Danger, Loc.T("while_no_uninstall")),
            Consequence(Glyph.Check, TextTone.Accent, Loc.T("strict_can_add"))), CardStyle.Surface, CardPadding.Roomy);

        var lockIn = new PillButton(Loc.T("lock_in"), ButtonKind.Secondary, Glyph.Lock, ButtonSize.Large) { Stretch = true };
        lockIn.Click += (_, _) =>
        {
            _controller.ActivateStrictMode(StrictDuration());
            _status = _controller.Status;
            Navigate(Page.Strict);
        };

        var confirm = new Card(
            Layout.Column(Space.S4, new TextLine(Loc.T("no_early_exit"), TextStyle.Heading, TextTone.OnInk), lockIn),
            CardStyle.Ink,
            CardPadding.Roomy);

        Refresh();
        page.Children.Add(Layout.Columns(howLong, Layout.Column(Space.S4, whileOn, confirm), 300, gap: Space.S5, fixedRight: true));
        return page;
    }

    private static TimeSpan DurationOf(StrictLength length)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return length switch
        {
            StrictLength.OneHour => TimeSpan.FromHours(1),
            StrictLength.FourHours => TimeSpan.FromHours(4),
            StrictLength.RestOfDay => now.Date.AddDays(1) - now.DateTime,
            StrictLength.OneDay => TimeSpan.FromDays(1),
            _ => TimeSpan.FromDays(7),
        };
    }

    private Control BuildStrictRunning()
    {
        StrictModeSnapshot strict = _status.StrictMode;

        var countdown = new TextLine("", TextStyle.Display, TextTone.OnInk);
        var progress = new Meter(0, Tone.Ink, onInk: true);

        string until = strict.ActiveUntilLocal is { } end ? WhenText(end) : "";
        StackPanel titleRow = Layout.Row(Space.S2, new Icon(Glyph.Lock, 16, TextTone.OnInkMuted), new TextLine(Loc.T("strict_on"), TextStyle.Label, TextTone.OnInkMuted));
        var unlocks = new TextLine(Loc.T("unlocks") + " " + until, TextStyle.Body, TextTone.OnInkMuted);

        var hero = new Card(Layout.Column(18, Layout.Column(6, titleRow, countdown, unlocks), progress), CardStyle.Ink, CardPadding.Spacious);

        _live.Add(status =>
        {
            TimeSpan left = status.StrictMode.Remaining;
            countdown.SetText(left.TotalDays >= 1
                ? $"{(int)left.TotalDays}d {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}"
                : $"{(int)left.TotalHours}:{left.Minutes:00}:{left.Seconds:00}");
            progress.SetFraction(status.StrictMode.Progress);
        });

        var addRule = new PillButton(Loc.T("add_rule"), ButtonKind.Primary, Glyph.Plus);
        addRule.Click += (_, _) => OpenEditor(NewRule(), -1);
        var stillCanText = Layout.Column(Space.S1,
            new TextLine(Loc.T("you_can_still"), TextStyle.Subheading),
            new TextLine(Loc.T("strict_can_help"), TextStyle.Small));
        var stillCanRow = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(addRule, Dock.Right);
        addRule.Margin = new Thickness(Space.S4, 0, 0, 0);
        stillCanRow.Children.Add(addRule);
        stillCanRow.Children.Add(stillCanText);
        var stillCan = new Card(stillCanRow, CardStyle.Surface, CardPadding.Roomy);

        var page = new StackPanel { Spacing = Space.S6 };
        page.Children.Add(hero);
        page.Children.Add(stillCan);
        return page;
    }
}
