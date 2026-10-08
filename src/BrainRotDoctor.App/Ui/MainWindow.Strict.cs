using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Strict mode ----------

    private enum StrictLength { OneHour, FourHours, RestOfDay, OneDay, OneWeek, Custom }

    private StrictLength _strictLength = StrictLength.FourHours;
    private int _strictAmount = 3;
    private int _strictUnit = 1; // 0 minutes, 1 hours, 2 days
    private HashSet<string>? _strictRules; // null until first shown: all rules
    private bool _strictAck;

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

    private Control BuildStrictSetup()
    {
        _strictRules ??= _editable.Rules.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        _strictRules.IntersectWith(_editable.Rules.Select(r => r.Id));

        var page = new StackPanel { Spacing = 24 };
        page.Children.Add(PageHeader(Loc.T("strict_mode"), Loc.T("strict_subtitle"), null));

        var endsText = UiTheme.Body("");
        var lockSummary = UiTheme.Text("", 13, FontWeight.Normal, UiTheme.InkText);
        lockSummary.Opacity = 0.75;
        var ackText = UiTheme.Text("", 13.5, FontWeight.Normal, UiTheme.InkText);

        void Refresh()
        {
            string ends = WhenText(DateTimeOffset.Now + StrictDuration());
            endsText.Inlines = new Avalonia.Controls.Documents.InlineCollection
            {
                new Avalonia.Controls.Documents.Run(Loc.T("unlocks") + " "),
                new Avalonia.Controls.Documents.Run(ends) { FontWeight = FontWeight.SemiBold },
            };
            lockSummary.Text = Loc.T("rules_to_lock", _strictRules!.Count);
            ackText.Text = Loc.T("strict_ack_until", ends);
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
            bool on = _strictLength == length;
            DateTimeOffset end = DateTimeOffset.Now + (length == StrictLength.Custom ? TimeSpan.Zero : DurationOf(length));
            string caption = sub.Length > 0 ? sub : Loc.T("until_time", length == StrictLength.OneWeek ? end.ToString("ddd HH:mm", Loc.Culture) : Clock(end));
            var tile = new Border
            {
                Height = 64,
                Padding = new Thickness(14, 0),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1.5),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = UiTheme.VStack(2,
                    UiTheme.Text(label, 15, FontWeight.SemiBold, on ? UiTheme.InkText : UiTheme.TextPrimary),
                    UiTheme.Text(caption, 12, FontWeight.Normal, on ? UiTheme.InkText : UiTheme.TextTertiary)),
                [!Border.BackgroundProperty] = UiTheme.Dyn(on ? UiTheme.Ink : UiTheme.Surface),
                [!Border.BorderBrushProperty] = UiTheme.Dyn(on ? UiTheme.Ink : UiTheme.Border_),
            };
            ((StackPanel)tile.Child).VerticalAlignment = VerticalAlignment.Center;
            tile.AddHandler(Gestures.TappedEvent, (_, _) =>
            {
                _strictLength = length;
                Rerender();
            });
            lengthTiles.Add(tile);
        }

        if (_strictLength == StrictLength.Custom)
        {
            var amount = new NumericUpDown
            {
                Value = _strictAmount,
                Minimum = 1,
                Maximum = 999,
                Increment = 1,
                FormatString = "0",
                Width = 120,
            };
            amount.ValueChanged += (_, _) => { _strictAmount = (int)(amount.Value ?? 1); Refresh(); };
            var unit = new ComboBox
            {
                Width = 150,
                ItemsSource = new[] { Loc.T("minutes"), Loc.T("hours"), Loc.T("days") },
                SelectedIndex = _strictUnit,
            };
            unit.SelectionChanged += (_, _) => { _strictUnit = Math.Max(0, unit.SelectedIndex); Refresh(); };
            customRow.Children.Add(UiTheme.HStack(10, UiTheme.Body(Loc.T("lock_for")), amount, unit));
        }

        var ends = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12),
            Child = UiTheme.HStack(10, Icons.Make(Icons.Clock, 18), endsText),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
        };

        Control howLong = UiTheme.Card(UiTheme.VStack(16, UiTheme.H2(Loc.T("how_long")), TileGrid(lengthTiles, 140, 8), customRow, ends), 22);

        // Which rules
        var ruleList = UiTheme.VStack(8,
            UiTheme.VStack(4, UiTheme.H2(Loc.T("which_rules")), UiTheme.Muted(Loc.T("which_rules_help"))));
        foreach (EditableConfiguration.EditableRule rule in _editable.Rules)
        {
            var box = new CheckBox { IsChecked = _strictRules.Contains(rule.Id), VerticalAlignment = VerticalAlignment.Center };
            box.IsCheckedChanged += (_, _) =>
            {
                if (box.IsChecked == true)
                {
                    _strictRules.Add(rule.Id);
                }
                else
                {
                    _strictRules.Remove(rule.Id);
                }

                Refresh();
            };
            var text = UiTheme.VStack(1, UiTheme.Strong(string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name), UiTheme.Caption(ConditionSummary(rule)));
            var row = new Border
            {
                MinHeight = 56,
                Padding = new Thickness(12, 6),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = UiTheme.HStack(4, box, text),
                Background = Brushes.Transparent,
                [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
            };
            row.AddHandler(Gestures.TappedEvent, (_, e) =>
            {
                if (e.Source is Visual v && box.IsVisualAncestorOf(v))
                {
                    return;
                }

                box.IsChecked = box.IsChecked != true;
            });
            ruleList.Children.Add(row);
        }

        if (_editable.Rules.Count == 0)
        {
            ruleList.Children.Add(UiTheme.Muted(Loc.T("no_rules_to_lock")));
        }

        Control leftColumn = UiTheme.VStack(20, howLong, UiTheme.Card(ruleList, 22));

        // While it's on + confirmation
        Control Consequence(string icon, string iconKey, string text) =>
            UiTheme.HStack(10, Icons.Make(icon, 18, iconKey), Wrapping(UiTheme.Body(text), 240));

        var whileOn = UiTheme.Card(UiTheme.VStack(14,
            UiTheme.H2(Loc.T("while_on")),
            Consequence(Icons.Ban, UiTheme.Danger, Loc.T("while_no_edit")),
            Consequence(Icons.Ban, UiTheme.Danger, Loc.T("while_no_pause")),
            Consequence(Icons.Ban, UiTheme.Danger, Loc.T("while_no_uninstall")),
            Consequence(Icons.Check, UiTheme.Accent, Loc.T("while_can_add"))), 22);

        var ack = new CheckBox { IsChecked = _strictAck, Content = ackText, VerticalAlignment = VerticalAlignment.Top };
        UiTheme.OnInk(ack);
        PillButton lockIn = UiTheme.Secondary(Loc.T("lock_in"), Icons.Lock);
        lockIn.HorizontalAlignment = HorizontalAlignment.Stretch;
        lockIn.MinHeight = 48;
        lockIn.Enabled = _strictAck && _strictRules.Count > 0;
        ack.IsCheckedChanged += (_, _) =>
        {
            _strictAck = ack.IsChecked == true;
            lockIn.Enabled = _strictAck && _strictRules.Count > 0;
        };
        lockIn.Click += (_, _) =>
        {
            if (!_strictAck || _strictRules.Count == 0)
            {
                return;
            }

            _controller.ActivateStrictMode(StrictDuration(), _strictRules.ToArray());
            _strictAck = false;
            _status = _controller.Status;
            Navigate(Page.Strict);
        };

        var confirm = new Border
        {
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(22),
            Child = UiTheme.VStack(16,
                UiTheme.VStack(4, lockSummary, UiTheme.Text(Loc.T("no_early_exit"), 18, FontWeight.SemiBold, UiTheme.InkText)),
                ack,
                lockIn),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Ink),
        };

        Refresh();
        page.Children.Add(Columns(leftColumn, UiTheme.VStack(16, whileOn, confirm), 300, 760, gap: 20, fixedRight: true));
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

    private static TextBlock Wrapping(TextBlock text, double maxWidth)
    {
        text.MaxWidth = maxWidth;
        return text;
    }

    private Control BuildStrictRunning()
    {
        StrictModeSnapshot strict = _status.StrictMode;

        var countdown = UiTheme.Text("", 60, FontWeight.SemiBold, UiTheme.InkText);
        var progressFill = new ColumnDefinition(0, GridUnitType.Star);
        var progressRest = new ColumnDefinition(1, GridUnitType.Star);
        var progress = new Grid { Height = 8, ColumnDefinitions = new ColumnDefinitions { progressFill, progressRest } };
        progress.Children.Add(new Border { CornerRadius = new CornerRadius(4), [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.InkText) });

        string until = strict.ActiveUntilLocal is { } end ? WhenText(end) : "";
        var titleRow = UiTheme.HStack(8, Icons.Make(Icons.Lock, 16, UiTheme.InkText), UiTheme.Text(Loc.T("strict_on"), 15, FontWeight.SemiBold, UiTheme.InkText));
        titleRow.Opacity = 0.75;
        TextBlock unlocks = UiTheme.Text(Loc.T("unlocks") + " " + until, 15, FontWeight.Normal, UiTheme.InkText);
        unlocks.Opacity = 0.85;

        var hero = new Border
        {
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(32),
            Child = UiTheme.VStack(18,
                UiTheme.VStack(6, titleRow, countdown, unlocks),
                new Border
                {
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    ClipToBounds = true,
                    Child = progress,
                    Background = new SolidColorBrush(Color.FromArgb(70, 128, 128, 128)),
                }),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Ink),
        };

        _live.Add(status =>
        {
            TimeSpan left = status.StrictMode.Remaining;
            countdown.Text = left.TotalDays >= 1
                ? $"{(int)left.TotalDays}d {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}"
                : $"{(int)left.TotalHours}:{left.Minutes:00}:{left.Seconds:00}";
            double p = status.StrictMode.Progress;
            progressFill.Width = new GridLength(p, GridUnitType.Star);
            progressRest.Width = new GridLength(1 - p, GridUnitType.Star);
        });

        // Locked rules
        var locked = UiTheme.VStack(10, UiTheme.H2(Loc.T("locked_rules")));
        foreach (EditableConfiguration.EditableRule rule in _editable.Rules.Where(r => strict.IsLocked(r.Id)))
        {
            TextBlock state = UiTheme.Text("", 12.5, FontWeight.SemiBold, UiTheme.TextSecondary);
            _live.Add(status =>
            {
                RuleSnapshot? s = status.Rules.FirstOrDefault(r => r.RuleId == rule.Id);
                (string text, string key) = s is null || !s.IsActive
                    ? (Loc.T("off_hours"), UiTheme.TextSecondary)
                    : s.IsBlocking
                        ? (Loc.T("blocking_now"), UiTheme.Danger)
                        : (Loc.T("time_left", FormatSpan(s.Remaining)), UiTheme.Accent);
                state.Text = text;
                state[!TextBlock.ForegroundProperty] = UiTheme.Dyn(key);
            });

            var row = new DockPanel { LastChildFill = true };
            Control icon = Icons.Make(Icons.Lock, 16, UiTheme.TextTertiary);
            icon.Margin = new Thickness(0, 0, 12, 0);
            DockPanel.SetDock(icon, Dock.Left);
            DockPanel.SetDock(state, Dock.Right);
            row.Children.Add(icon);
            row.Children.Add(state);
            row.Children.Add(UiTheme.VStack(1, UiTheme.Strong(rule.Name), UiTheme.Caption(ConditionSummary(rule))));
            locked.Children.Add(new Border
            {
                MinHeight = 56,
                Padding = new Thickness(14, 8),
                CornerRadius = new CornerRadius(10),
                Child = row,
                [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
            });
        }

        locked.Children.Add(UiTheme.Caption(Loc.T("locked_note", until)));

        PillButton addRule = UiTheme.Primary(Loc.T("add_rule"), Icons.Plus);
        addRule.HorizontalAlignment = HorizontalAlignment.Stretch;
        addRule.Click += (_, _) => OpenEditor(NewRule(), -1);
        var stillCan = UiTheme.Card(UiTheme.VStack(14,
            UiTheme.H2(Loc.T("you_can_still")),
            UiTheme.Muted(Loc.T("you_can_still_help")),
            addRule,
            UiTheme.Divider(),
            UiTheme.HStack(10, Icons.Make(Icons.Pause, 16, UiTheme.TextTertiary), Wrapping(UiTheme.Caption(Loc.T("pause_off_strict")), 220))), 22);

        var page = new StackPanel { Spacing = 24 };
        page.Children.Add(hero);
        page.Children.Add(Columns(UiTheme.Card(locked, 22), stillCan, 290, 740, gap: 20, fixedRight: true));
        return page;
    }
}
