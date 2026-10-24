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

    /// <summary>A longer pause starts here, at the longest short one.</summary>
    private const int LongerFrom = 15;

    /// <summary>From this many minutes on, a longer pause asks whether it's worth it.</summary>
    private const int WorthItFrom = 30;

    /// <summary>Strict mode's choices, by what they are rather than where they sit (the work day comes and goes).</summary>
    private enum StrictChoice { Tomorrow, WorkDay, RestOfToday, FourHours, TwoHours }

    private readonly CustomLength _strictCustom = new() { Amount = 5 };

    // Strict mode's choice (null: the recommended one) and whether its custom length is open.
    private StrictChoice? _strictPick;
    private bool _strictCustomOpen;

    // The pause dialog, and its choice: one of the short breaks, or a longer one.
    private bool _pauseOpen;
    private int _pausePick;
    private bool _pauseCustomOpen;
    private int _pauseCustomMinutes = LongerFrom;

    /// <summary>
    /// Strict mode's choices, longest first so the long ones set the scale: until
    /// tomorrow morning, then the rest of the work day (recommended) while at least
    /// two hours of it are left, else the rest of today (recommended) while at least
    /// two hours of it are left, then four hours and two. Late at night, until
    /// tomorrow is recommended. Each has its end as seen from <paramref name="now"/>,
    /// so the choices are worked out again when one is started.
    /// </summary>
    private static List<(StrictChoice Choice, string Label, string Caption, DateTimeOffset End, bool Recommended)> StrictLengths(DateTimeOffset now)
    {
        DateTime today = now.LocalDateTime.Date;
        DateTimeOffset morning = LocalAt(today.AddDays(1).AddHours(8));
        DateTimeOffset evening = LocalAt(today.AddHours(18));
        DateTimeOffset midnight = LocalAt(today.AddDays(1));
        TimeSpan enough = TimeSpan.FromHours(2);
        bool workDay = evening - now >= enough;
        bool restOfToday = !workDay && midnight - now >= enough;

        var lengths = new List<(StrictChoice, string, string, DateTimeOffset, bool)>
        {
            (StrictChoice.Tomorrow, Loc.T("len_until_tomorrow"), Loc.T("tomorrow_at", Clock(morning)), morning, !workDay && !restOfToday),
        };
        if (workDay)
        {
            lengths.Add((StrictChoice.WorkDay, Loc.T("len_work_day"), Loc.T("until_time", Clock(evening)), evening, true));
        }
        else if (restOfToday)
        {
            lengths.Add((StrictChoice.RestOfToday, Loc.T("len_today"), Loc.T("until_midnight"), midnight, true));
        }

        foreach ((StrictChoice choice, string label, int hours) in new[] { (StrictChoice.FourHours, Loc.T("len_4h"), 4), (StrictChoice.TwoHours, Loc.T("len_2h"), 2) })
        {
            lengths.Add((choice, label, Loc.T("until_time", Clock(now.AddHours(hours))), now.AddHours(hours), false));
        }

        return lengths;
    }

    /// <summary>A local wall-clock time with the offset that applies then, not now (clocks change overnight).</summary>
    private static DateTimeOffset LocalAt(DateTime local) => new(local, TimeZoneInfo.Local.GetUtcOffset(local));

    /// <summary>
    /// When something ends, on a button: the time alone today, with the short day
    /// name within the week ("Sun 08:00"), and with the date beyond it ("Tue 3 Nov 14:00").
    /// </summary>
    private static string EndText(DateTimeOffset end)
    {
        DateTime day = end.LocalDateTime.Date;
        return day == DateTime.Today ? Clock(end)
            : day < DateTime.Today.AddDays(6) ? end.ToLocalTime().ToString("ddd HH:mm", Loc.Culture)
            : end.ToLocalTime().ToString("ddd d MMM HH:mm", Loc.Culture);
    }

    /// <summary>
    /// Strict mode while it's off: an ink card asking for a long lock. A long choice
    /// is picked already, so one click on the button locks for long; a custom length
    /// is hours or days, never minutes. After the click the card stays as it is for a
    /// few seconds, the button turned into Cancel right under the pointer; it only
    /// goes once strict mode is on.
    /// </summary>
    private Control StrictCard()
    {
        bool starting = _activation is not null;
        string help = Loc.T("strict_pitch_help");
        if (_status.IsPaused)
        {
            help += " " + Loc.T("strict_ends_pause");
        }

        var state = new TextLine(starting ? StartsIn() : Loc.T("strict_off"), TextStyle.Overline, TextTone.OnInkMuted);
        StackPanel head = Layout.Column(6,
            Layout.Row(Space.S2, new Icon(Glyph.Lock, 14, TextTone.OnInkAccent), state),
            new TextLine(Loc.T("strict_pitch"), TextStyle.Heading, TextTone.OnInk),
            new TextLine(help, TextStyle.Small, TextTone.OnInkMuted));

        var host = new ContentControl();
        string builtFor = "";

        void Build()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            builtFor = Clock(now);
            var lengths = StrictLengths(now);
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
            DateTimeOffset? End(DateTimeOffset at) => _strictCustomOpen
                ? at + _strictCustom.Length
                : StrictLengths(at).Where(l => l.Choice == picked && l.End > at).Select(l => (DateTimeOffset?)l.End).FirstOrDefault();
            string Label() => End(DateTimeOffset.Now) is { } end ? Loc.T("lock_until_btn", EndText(end)) : "";

            // Full width, so it keeps its size whatever the end time says; Cancel takes its place while starting.
            PillButton go;
            if (starting)
            {
                go = new PillButton(Loc.T("cancel"), ButtonKind.SecondaryOnInk, size: ButtonSize.Large) { Stretch = true };
                go.Click += (_, _) => CancelStrict();
            }
            else
            {
                go = new PillButton(Label(), ButtonKind.OnInk, Glyph.Lock, ButtonSize.Large) { Stretch = true };
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
            }

            // While starting, the choices stay where they are but can't be changed.
            StackPanel choices = Layout.Column(Space.S4, Layout.EqualColumns(Space.S2, tiles));
            if (_strictCustomOpen)
            {
                choices.Children.Add(StrictCustomRow(() => go.Text = Label(), Build));
            }

            choices.IsHitTestVisible = !starting;
            StackPanel body = Layout.Column(Space.S4, choices, go);
            if (!_strictCustomOpen)
            {
                var another = new PillButton(Loc.T("another_length"), ButtonKind.GhostOnInk, size: ButtonSize.Small)
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Enabled = !starting,
                };
                another.Click += (_, _) =>
                {
                    _strictCustomOpen = true;
                    Build();
                };
                body.Children.Add(another);
            }

            host.Content = body;
        }

        Build();
        _live.Add(_ =>
        {
            if (_activation is not null)
            {
                state.SetText(StartsIn());
            }
            else if (Clock(DateTimeOffset.Now) != builtFor)
            {
                // A new minute: every "until" moves on with it.
                Build();
            }
        });
        return new Card(Layout.Column(Space.S5, head, host), CardStyle.Ink, CardPadding.Roomy);
    }

    /// <summary>"Strict mode starts in 3 s", counting down while it can still be cancelled.</summary>
    private string StartsIn()
    {
        double left = Math.Ceiling((_activationAt - DateTimeOffset.Now).TotalSeconds);
        return Loc.T("strict_starts_in", (int)Math.Clamp(left, 1, ActivationDelay.TotalSeconds));
    }

    /// <summary>A custom strict-mode length: hours (up to a day) or days, with no upper end worth reaching.</summary>
    private Control StrictCustomRow(Action changed, Action rebuild)
    {
        CustomLength custom = _strictCustom;
        int most = custom.Days ? 999 : 23;
        custom.Amount = Math.Clamp(custom.Amount, 1, most);
        string unit = custom.Days ? Loc.DayUnit() : Loc.DurationUnits().H;

        var amount = new Stepper(custom.Amount, 1, most, Loc.T("shorter"), Loc.T("longer"), style: StepperStyle.OnInk, format: v => $"{v} {unit}");
        var units = new Segmented(new Segment[] { new(Loc.T("hours")), new(Loc.T("days")) }, custom.Days ? 1 : 0, stretch: false, onInk: true);
        amount.Changed += value =>
        {
            custom.Amount = value;
            changed();
        };
        units.Changed += i =>
        {
            custom.Days = i == 1;
            custom.Amount = custom.Days ? 1 : 5;
            rebuild();
        };

        return new Card(Layout.Row(Space.S5, amount, units), CardStyle.InkWell, CardPadding.Compact);
    }

    private void OpenPause()
    {
        _pauseOpen = true;
        _pausePick = 0;
        _pauseCustomOpen = false;
        _pauseCustomMinutes = LongerFrom;
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
    /// A pause, in a dialog of its own in the middle of the screen. It leans short:
    /// 5, 10 or 15 minutes with 5 picked, "Keep blocking" as big as the pause, and
    /// today's paused time on top. A longer pause goes up one minute per press (down
    /// is quick), and from 30 minutes on it asks whether it's worth it.
    /// </summary>
    private Control PauseSheet()
    {
        var top = new DockPanel { LastChildFill = true };
        TimeSpan pausedToday = _status.Activity.PausedTime;
        if (pausedToday >= TimeSpan.FromMinutes(1))
        {
            string total = pausedToday < TimeSpan.FromHours(1) ? Loc.T("len_n_min", (int)pausedToday.TotalMinutes) : FormatSpan(pausedToday);
            var badge = new Badge(Loc.T("paused_today_total", total), Tone.Warn);
            DockPanel.SetDock(badge, Dock.Right);
            top.Children.Add(badge);
        }

        top.Children.Add(new IconDisc(Glyph.Pause, Tone.Warn));
        StackPanel head = Layout.Column(10, top, Layout.Column(Space.S1,
            new TextLine(Loc.T("take_break"), TextStyle.Heading),
            new TextLine(Loc.T("break_help"), TextStyle.Small)));

        var host = new ContentControl();
        string builtFor = "";

        void Build()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            builtFor = Clock(now);
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
            string Label() => Loc.T("pause_until_btn", EndText(DateTimeOffset.Now.AddMinutes(Minutes())));

            // Full width, so it keeps its size whatever the end time says.
            var go = new PillButton(Label(), ButtonKind.Primary, size: ButtonSize.Large) { Stretch = true };
            go.Click += (_, _) => StartPause(Minutes());

            StackPanel body = Layout.Column(Space.S5, Layout.EqualColumns(Space.S2, tiles));
            if (_pauseCustomOpen)
            {
                int most = (int)PauseState.Longest.TotalMinutes;
                var note = new TextLine("", TextStyle.Small, TextTone.Warn);
                void Note(int minutes)
                {
                    note.SetText(Loc.T("break_worth_it", minutes));
                    note.IsVisible = minutes >= WorthItFrom;
                }

                var minutes = new Stepper(_pauseCustomMinutes, 1, most, Loc.T("shorter"), Loc.T("longer"), repeatUp: false,
                    StepperStyle.Warn, v => Loc.T("len_n_min", v), stretch: true);
                minutes.Changed += value =>
                {
                    _pauseCustomMinutes = value;
                    Note(value);
                    go.Text = Label();
                };
                Note(_pauseCustomMinutes);
                body.Children.Add(new Card(Layout.Column(10, minutes, note), CardStyle.WarnWell, CardPadding.Compact));
            }

            var keep = new PillButton(Loc.T("keep_blocking"), ButtonKind.Secondary, size: ButtonSize.Large) { Stretch = true };
            keep.Click += (_, _) => ClosePause();
            body.Children.Add(Layout.Column(Space.S2, go, keep));

            if (!_pauseCustomOpen)
            {
                var longer = new PillButton(Loc.T("longer_break"), ButtonKind.Ghost, size: ButtonSize.Small) { HorizontalAlignment = HorizontalAlignment.Center };
                longer.Click += (_, _) =>
                {
                    _pauseCustomOpen = true;
                    _pauseCustomMinutes = LongerFrom;
                    Build();
                };
                body.Children.Add(longer);
            }

            host.Content = body;
        }

        Build();
        _live.Add(_ =>
        {
            // A new minute: every "until" moves on with it.
            if (Clock(DateTimeOffset.Now) != builtFor)
            {
                Build();
            }
        });
        return new Card(Layout.Column(Space.S5, head, host), CardStyle.WarnSurface, CardPadding.Roomy) { Width = 440 };
    }
}
