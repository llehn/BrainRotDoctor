using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- How long: strict mode leans long, a pause leans short ----------

    /// <summary>A custom strict-mode length as picked: an amount of hours or of days.</summary>
    private sealed class CustomLength
    {
        public int Amount { get; set; }

        public bool Days { get; set; }

        public TimeSpan Length => Days ? TimeSpan.FromDays(Amount) : TimeSpan.FromHours(Amount);
    }

    /// <summary>The pause lengths offered up front, in minutes; a longer one goes a minute at a time.</summary>
    private static readonly int[] BreakMinutes = { 5, 10, 15 };

    /// <summary>A pause of more minutes than this has to be held down to start.</summary>
    private const int HoldOver = 15;

    private static readonly TimeSpan HoldTime = TimeSpan.FromSeconds(3);

    /// <summary>From this many minutes on, a longer pause asks whether it's worth it.</summary>
    private const int WorthItFrom = 30;

    /// <summary>Strict mode's choices, by what they are rather than where they sit (the work day comes and goes).</summary>
    private enum StrictChoice { Tomorrow, WorkDay, FourHours, TwoHours }

    private readonly CustomLength _strictCustom = new() { Amount = 5 };

    // Strict mode's choice (null: the recommended one) and whether its custom length is open.
    private StrictChoice? _strictPick;
    private bool _strictCustomOpen;

    // The pause dialog, and its choice: one of the short breaks, or a longer one.
    private bool _pauseOpen;
    private int _pausePick;
    private bool _pauseCustomOpen;
    private int _pauseCustomMinutes = HoldOver;

    /// <summary>
    /// Strict mode's choices, longest first so the long ones set the scale: until
    /// tomorrow morning, the rest of the work day (recommended, while there's enough
    /// of it left), four hours and two. Without a work day left, until tomorrow is
    /// recommended. Each has its end as seen from <paramref name="now"/>, so the
    /// choices are worked out again when one is started.
    /// </summary>
    private static List<(StrictChoice Choice, string Label, string Caption, DateTimeOffset End, bool Recommended)> StrictLengths(DateTimeOffset now)
    {
        DateTime today = now.LocalDateTime.Date;
        DateTimeOffset morning = LocalAt(today.AddDays(1).AddHours(8));
        DateTimeOffset evening = LocalAt(today.AddHours(18));
        bool workDay = evening - now >= TimeSpan.FromHours(2);

        var lengths = new List<(StrictChoice, string, string, DateTimeOffset, bool)>
        {
            (StrictChoice.Tomorrow, Loc.T("len_until_tomorrow"), WhenText(morning), morning, !workDay),
        };
        if (workDay)
        {
            lengths.Add((StrictChoice.WorkDay, Loc.T("len_work_day"), Loc.T("until_time", Clock(evening)), evening, true));
        }

        foreach ((StrictChoice choice, string label, int hours) in new[] { (StrictChoice.FourHours, Loc.T("len_4h"), 4), (StrictChoice.TwoHours, Loc.T("len_2h"), 2) })
        {
            lengths.Add((choice, label, Loc.T("until_time", Clock(now.AddHours(hours))), now.AddHours(hours), false));
        }

        return lengths;
    }

    /// <summary>A local wall-clock time with the offset that applies then, not now (clocks change overnight).</summary>
    private static DateTimeOffset LocalAt(DateTime local) => new(local, TimeZoneInfo.Local.GetUtcOffset(local));

    /// <summary>When something ends: the time alone today, with the day otherwise.</summary>
    private static string EndText(DateTimeOffset end) =>
        end.LocalDateTime.Date == DateTime.Today ? Clock(end) : WhenText(end);

    /// <summary>
    /// Strict mode while it's off: an ink card asking for a long lock. A long choice
    /// is picked already, so one click on the button locks for long; a custom length
    /// is hours or days, never minutes.
    /// </summary>
    private Control StrictCard()
    {
        string help = Loc.T("strict_pitch_help");
        if (_status.IsPaused)
        {
            help += " " + Loc.T("strict_ends_pause");
        }

        StackPanel head = Layout.Column(6,
            Layout.Row(Space.S2, new Icon(Glyph.Lock, 14, TextTone.OnInkMuted), new TextLine(Loc.T("strict_off"), TextStyle.Overline, TextTone.OnInkMuted)),
            new TextLine(Loc.T("strict_pitch"), TextStyle.Heading, TextTone.OnInk),
            new TextLine(help, TextStyle.Small, TextTone.OnInkMuted));

        var host = new ContentControl();

        void Build()
        {
            var lengths = StrictLengths(DateTimeOffset.Now);
            if (_strictPick is { } gone && !lengths.Exists(l => l.Choice == gone))
            {
                _strictPick = null;
            }

            StrictChoice picked = _strictPick ?? lengths.Find(l => l.Recommended).Choice;

            var tiles = new Control[lengths.Count];
            for (int i = 0; i < lengths.Count; i++)
            {
                (StrictChoice choice, string label, string caption, _, bool recommended) = lengths[i];
                var tile = new ChoiceTile(label, caption, !_strictCustomOpen && choice == picked, ChoiceTileStyle.OnInk, recommended ? Loc.T("recommended") : null);
                tile.Click += (_, _) =>
                {
                    _strictPick = choice;
                    _strictCustomOpen = false;
                    Build();
                };
                tiles[i] = tile;
            }

            // The end as of now: the card may have been built long ago (the window hides to the tray).
            DateTimeOffset? End(DateTimeOffset now) => _strictCustomOpen
                ? now + _strictCustom.Length
                : StrictLengths(now).Where(l => l.Choice == picked && l.End > now).Select(l => (DateTimeOffset?)l.End).FirstOrDefault();
            string Label() => End(DateTimeOffset.Now) is { } end ? Loc.T("lock_until_btn", WhenText(end)) : "";

            // Full width, so it keeps its size whatever the end time says.
            var go = new PillButton(Label(), ButtonKind.OnInk, Glyph.Lock, ButtonSize.Large) { Stretch = true };
            go.Click += (_, _) =>
            {
                // A choice that has run out (the work day is over) is shown afresh instead of started.
                if (End(DateTimeOffset.Now) is { } end)
                {
                    BeginStrict(end);
                }
                else
                {
                    Build();
                }
            };

            var note = new TextLine(Loc.T("change_mind", (int)ActivationDelay.TotalSeconds), TextStyle.Caption, TextTone.OnInkMuted)
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            var footer = new DockPanel { LastChildFill = true };
            if (!_strictCustomOpen)
            {
                var another = new PillButton(Loc.T("another_length"), ButtonKind.GhostOnInk, size: ButtonSize.Small);
                another.Click += (_, _) =>
                {
                    _strictCustomOpen = true;
                    Build();
                };
                DockPanel.SetDock(another, Dock.Right);
                footer.Children.Add(another);
            }

            footer.Children.Add(note);

            StackPanel body = Layout.Column(Space.S4, Layout.EqualColumns(Space.S2, tiles));
            if (_strictCustomOpen)
            {
                body.Children.Add(StrictCustomRow(() => go.Text = Label(), Build));
            }

            body.Children.Add(Layout.Column(Space.S2, go, footer));
            host.Content = body;
        }

        Build();
        return new Card(Layout.Column(Space.S5, head, host), CardStyle.Ink, CardPadding.Roomy);
    }

    /// <summary>A custom strict-mode length: hours (up to a day) or days, with no upper end worth reaching.</summary>
    private Control StrictCustomRow(Action changed, Action rebuild)
    {
        CustomLength custom = _strictCustom;
        int most = custom.Days ? 999 : 23;
        custom.Amount = Math.Clamp(custom.Amount, 1, most);

        var amount = new Stepper(custom.Amount, 1, most, Loc.T("shorter"), Loc.T("longer"));
        var unit = new Segmented(new Segment[] { new(Loc.T("hours")), new(Loc.T("days")) }, custom.Days ? 1 : 0, stretch: false);
        amount.Changed += value =>
        {
            custom.Amount = value;
            changed();
        };
        unit.Changed += i =>
        {
            custom.Days = i == 1;
            custom.Amount = custom.Days ? 1 : 5;
            rebuild();
        };

        return Layout.Row(Space.S3, amount, unit);
    }

    private void OpenPause()
    {
        _pauseOpen = true;
        _pausePick = 0;
        _pauseCustomOpen = false;
        _pauseCustomMinutes = HoldOver;
        Rerender();
    }

    private void ClosePause()
    {
        _pauseOpen = false;
        Rerender();
    }

    private void StartPause(int minutes)
    {
        _pauseOpen = false;
        _controller.Pause(TimeSpan.FromMinutes(minutes));
        _status = _controller.Status;
        Rerender();
    }

    /// <summary>
    /// A pause, in a dialog of its own over the screen. It leans short: 5, 10 or 15
    /// minutes with 5 picked, "Keep blocking" as big as the pause, and today's paused
    /// time on top. A longer pause goes up one minute per press (down is quick), and
    /// past 15 minutes the button has to be held down.
    /// </summary>
    private Control PauseSheet()
    {
        var top = new DockPanel { LastChildFill = true };
        TimeSpan pausedToday = _status.Activity.PausedTime;
        if (pausedToday >= TimeSpan.FromMinutes(1))
        {
            string total = pausedToday < TimeSpan.FromHours(1) ? Loc.T("len_n_min", (int)pausedToday.TotalMinutes) : FormatSpan(pausedToday);
            var chip = new Chip(Loc.T("paused_today_total", total), Tone.Warn);
            DockPanel.SetDock(chip, Dock.Right);
            top.Children.Add(chip);
        }

        top.Children.Add(new Icon(Glyph.Pause, 22, TextTone.Warn) { HorizontalAlignment = HorizontalAlignment.Left });
        StackPanel head = Layout.Column(Space.S3, top, Layout.Column(Space.S1,
            new TextLine(Loc.T("take_break"), TextStyle.Heading),
            new TextLine(Loc.T("break_help"), TextStyle.Small)));

        var host = new ContentControl();

        void Build()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            var tiles = new Control[BreakMinutes.Length];
            for (int i = 0; i < BreakMinutes.Length; i++)
            {
                int index = i;
                var tile = new ChoiceTile(
                    Loc.T("len_n_min", BreakMinutes[i]),
                    Loc.T("until_time", EndText(now.AddMinutes(BreakMinutes[i]))),
                    !_pauseCustomOpen && i == _pausePick,
                    ChoiceTileStyle.Warn);
                tile.Click += (_, _) =>
                {
                    _pausePick = index;
                    _pauseCustomOpen = false;
                    Build();
                };
                tiles[i] = tile;
            }

            int Minutes() => _pauseCustomOpen ? _pauseCustomMinutes : BreakMinutes[_pausePick];

            // The start button, full width so it keeps its size: held down for a long pause.
            var action = new ContentControl();
            void ShowAction()
            {
                int minutes = Minutes();
                string until = EndText(DateTimeOffset.Now.AddMinutes(minutes));
                if (minutes > HoldOver)
                {
                    var hold = new HoldButton(Loc.T("hold_to_pause", until), HoldTime, Glyph.Pause);
                    hold.Held += (_, _) => StartPause(minutes);
                    action.Content = hold;
                }
                else
                {
                    var go = new PillButton(Loc.T("pause_until_btn", until), ButtonKind.Primary, Glyph.Pause, ButtonSize.Large) { Stretch = true };
                    go.Click += (_, _) => StartPause(minutes);
                    action.Content = go;
                }
            }

            StackPanel body = Layout.Column(Space.S5, Layout.EqualColumns(Space.S2, tiles));
            if (_pauseCustomOpen)
            {
                int most = (int)PauseState.Longest.TotalMinutes;
                var note = new TextLine(LongerNote(_pauseCustomMinutes, most), TextStyle.Small, TextTone.Warn);
                var minutes = new Stepper(_pauseCustomMinutes, 1, most, Loc.T("shorter"), Loc.T("longer"), repeatUp: false);
                minutes.Changed += value =>
                {
                    _pauseCustomMinutes = value;
                    note.SetText(LongerNote(value, most));
                    ShowAction();
                };
                body.Children.Add(new Card(
                    Layout.Column(Space.S3, Layout.Row(Space.S3, minutes, new TextLine(Loc.T("minutes"))), note),
                    CardStyle.Well,
                    CardPadding.Compact));
            }

            var keep = new PillButton(Loc.T("keep_blocking"), ButtonKind.Secondary, size: ButtonSize.Large) { Stretch = true };
            keep.Click += (_, _) => ClosePause();
            body.Children.Add(Layout.Column(Space.S2, action, keep));

            if (!_pauseCustomOpen)
            {
                var longer = new PillButton(Loc.T("longer_break"), ButtonKind.Ghost, size: ButtonSize.Small) { HorizontalAlignment = HorizontalAlignment.Center };
                longer.Click += (_, _) =>
                {
                    _pauseCustomOpen = true;
                    _pauseCustomMinutes = HoldOver;
                    Build();
                };
                body.Children.Add(longer);
            }

            ShowAction();
            host.Content = body;
        }

        Build();
        return new Card(Layout.Column(Space.S5, head, host), CardStyle.Surface, CardPadding.Roomy) { Width = 440 };
    }

    /// <summary>What a longer pause costs, said more plainly the longer it gets.</summary>
    private static string LongerNote(int minutes, int most) =>
        minutes >= most ? Loc.T("pause_max_note", most / 60)
        : minutes >= WorthItFrom ? Loc.T("break_worth_it", minutes)
        : minutes > HoldOver ? Loc.T("break_hold_note", HoldOver)
        : Loc.T("break_one_minute");
}
