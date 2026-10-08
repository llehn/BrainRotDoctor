using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Home: the rule overview ----------

    private Control BuildHome()
    {
        var page = new StackPanel { Spacing = 24 };
        page.Children.Add(PageHeader(
            Loc.T("rules"),
            Loc.T("rules_subtitle"),
            UiTheme.HStack(10, PauseControl(), NewRuleButton())));

        if (_status.Pause is { } pause)
        {
            page.Children.Add(PausedBanner(pause));
        }

        var tiles = new List<Control>();
        for (int i = 0; i < _editable.Rules.Count; i++)
        {
            tiles.Add(RuleCard(_editable.Rules[i], i));
        }

        tiles.Add(AddRuleTile());
        page.Children.Add(TileGrid(tiles, 300, 16));
        return page;
    }

    private PillButton NewRuleButton()
    {
        PillButton button = UiTheme.Primary(Loc.T("new_rule"), Icons.Plus);
        button.Click += (_, _) => OpenEditor(NewRule(), -1);
        return button;
    }

    private Control PauseControl()
    {
        if (_status.IsPaused)
        {
            PillButton resume = UiTheme.Secondary(Loc.T("resume"), Icons.Play);
            resume[!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.WarnSoft);
            resume[!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.WarnBorder);
            resume.Click += (_, _) => _controller.Resume();
            return resume;
        }

        PillButton pause = UiTheme.Secondary(Loc.T("pause"), Icons.Pause);
        if (_status.StrictMode.IsActive)
        {
            pause.Enabled = false;
            ToolTip.SetTip(pause, Loc.T("pause_strict_note"));
            return pause;
        }

        var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedRight };
        var menu = new StackPanel { Width = 280, Spacing = 2 };
        TextBlock heading = UiTheme.SectionLabel(Loc.T("pause_for"));
        heading.Margin = new Thickness(10, 6, 10, 6);
        menu.Children.Add(heading);

        DateTimeOffset now = DateTimeOffset.Now;
        TimeSpan untilMidnight = now.Date.AddDays(1) - now.DateTime;
        menu.Children.Add(PauseOption(flyout, Loc.T("pause_15m"), TimeSpan.FromMinutes(15)));
        menu.Children.Add(PauseOption(flyout, Loc.T("pause_1h"), TimeSpan.FromHours(1)));
        menu.Children.Add(PauseOption(flyout, Loc.T("pause_today"), untilMidnight));
        menu.Children.Add(PauseOption(flyout, Loc.T("pause_manual"), null));
        Border divider = UiTheme.Divider();
        divider.Margin = new Thickness(4, 6);
        menu.Children.Add(divider);
        Control lockIcon = Icons.Make(Icons.Lock, 14, UiTheme.TextTertiary);
        TextBlock note = UiTheme.Caption(Loc.T("pause_strict_note"));
        StackPanel noteRow = UiTheme.HStack(8, lockIcon, note);
        noteRow.Margin = new Thickness(10, 2, 10, 6);
        note.MaxWidth = 230;
        menu.Children.Add(noteRow);
        flyout.Content = menu;

        pause.Click += (_, _) => flyout.ShowAt(pause);
        return pause;
    }

    private Control PauseOption(Flyout flyout, string label, TimeSpan? duration)
    {
        var row = new DockPanel { LastChildFill = true };
        if (duration is { } d)
        {
            TextBlock until = UiTheme.Caption(Loc.T("until_time", Clock(DateTimeOffset.Now + d)));
            DockPanel.SetDock(until, Dock.Right);
            row.Children.Add(until);
        }

        row.Children.Add(UiTheme.Body(label));
        var item = new Border
        {
            Height = 42,
            Padding = new Thickness(10, 0),
            CornerRadius = new CornerRadius(8),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = row,
        };
        item.PointerEntered += (_, _) => item[!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Track);
        item.PointerExited += (_, _) => item.Background = Brushes.Transparent;
        item.AddHandler(Gestures.TappedEvent, (_, _) =>
        {
            flyout.Hide();
            _controller.Pause(duration);
        });
        return item;
    }

    private Control PausedBanner(PauseState pause)
    {
        string until = pause.UntilUtc is { } u ? Loc.T("paused_until", Clock(u)) : Loc.T("paused_manual");
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
        text.Inlines = new Avalonia.Controls.Documents.InlineCollection
        {
            new Avalonia.Controls.Documents.Run(until) { FontWeight = FontWeight.SemiBold },
            new Avalonia.Controls.Documents.Run(" " + Loc.T("paused_explain")),
        };
        text[!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.WarnText);

        PillButton resume = UiTheme.Primary(Loc.T("resume_now"));
        resume.Click += (_, _) => _controller.Resume();

        Control icon = Icons.Make(Icons.Pause, 20, UiTheme.WarnText);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        icon.Margin = new Thickness(0, 0, 14, 0);
        grid.Children.Add(icon);
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        resume.Margin = new Thickness(14, 0, 0, 0);
        Grid.SetColumn(resume, 2);
        grid.Children.Add(resume);

        return new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 12),
            Child = grid,
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.WarnSoft),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.WarnBorder),
        };
    }

    private Control RuleCard(EditableConfiguration.EditableRule rule, int index)
    {
        bool locked = _status.StrictMode.IsLocked(rule.Id);

        Border chip = UiTheme.Chip("", UiTheme.Track, UiTheme.TextSecondary);
        var title = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(chip, Dock.Right);
        title.Children.Add(chip);
        var name = UiTheme.Text(string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name, 17, FontWeight.SemiBold, UiTheme.TextPrimary);
        name.TextTrimming = TextTrimming.CharacterEllipsis;
        name.TextWrapping = TextWrapping.NoWrap;
        name.Margin = new Thickness(0, 0, 10, 0);
        if (locked)
        {
            Control lockIcon = Icons.Make(Icons.Lock, 15, UiTheme.TextTertiary);
            lockIcon.Margin = new Thickness(0, 0, 8, 0);
            DockPanel.SetDock(lockIcon, Dock.Left);
            title.Children.Add(lockIcon);
        }

        title.Children.Add(name);

        var sites = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        foreach (EditableConfiguration.EditableSite site in rule.Sites)
        {
            sites.Children.Add(SiteChip(site));
        }

        if (rule.Sites.Count == 0)
        {
            sites.Children.Add(UiTheme.Caption(Loc.T("no_sites")));
        }

        var footer = new StackPanel { Spacing = 7 };
        var top = UiTheme.VStack(12, title, UiTheme.Muted(ConditionSummary(rule)), sites);
        var body = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        footer.Margin = new Thickness(0, 16, 0, 0);
        body.Children.Add(top);
        body.Children.Add(footer);

        Border card = UiTheme.Card(body);
        card.MinHeight = 224;
        if (!locked)
        {
            card.Cursor = new Cursor(StandardCursorType.Hand);
            card.AddHandler(Gestures.TappedEvent, (_, _) => OpenEditor(rule.Clone(), index));
            card.PointerEntered += (_, _) => card[!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.BorderStrong);
            card.PointerExited += (_, _) => card[!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_);
        }

        _live.Add(status =>
        {
            RuleSnapshot? snapshot = status.Rules.FirstOrDefault(r => r.RuleId == rule.Id);
            UpdateRuleChip(chip, rule, snapshot, status.IsPaused);
            UpdateRuleFooter(footer, rule, snapshot, status, locked);
        });

        return card;
    }

    private static Control SiteChip(EditableConfiguration.EditableSite site)
    {
        TextBlock label = UiTheme.Text(site.DisplayLabel, 12.5, FontWeight.Medium, UiTheme.TextPrimary);
        TextBlock what = UiTheme.Text(BlockedSummary(site), 12.5, FontWeight.Normal, UiTheme.TextTertiary);
        label.TextWrapping = TextWrapping.NoWrap;
        what.TextWrapping = TextWrapping.NoWrap;
        return new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(3, 3, 9, 3),
            Child = UiTheme.HStack(6, UiTheme.Monogram(site.CatalogId, site.DisplayLabel, 18), label, what),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
        };
    }

    private static void UpdateRuleChip(Border chip, EditableConfiguration.EditableRule rule, RuleSnapshot? s, bool paused)
    {
        (string text, string bg, string fg) = paused
            ? (Loc.T("paused"), UiTheme.WarnSoft, UiTheme.WarnText)
            : s is null || !s.IsActive
                ? (Loc.T("off_hours"), UiTheme.Track, UiTheme.TextSecondary)
                : s.IsBlocking
                    ? (Loc.T("blocking_now"), UiTheme.BlockSoft, UiTheme.Danger)
                    : (Loc.T("allowed_now"), UiTheme.AllowSoft, UiTheme.Accent);

        chip[!Border.BackgroundProperty] = UiTheme.Dyn(bg);
        if (chip.Child is TextBlock label)
        {
            label.Text = text;
            label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(fg);
        }
    }

    private static void UpdateRuleFooter(StackPanel footer, EditableConfiguration.EditableRule rule, RuleSnapshot? s, AppStatus status, bool locked)
    {
        footer.Children.Clear();
        if (s is not null && s.IsActive && !s.BlocksCompletely)
        {
            double used = Math.Min(s.Consumed.TotalMinutes, s.Allowance.TotalMinutes);
            bool spent = s.IsBlocking;
            var left = new TextBlock { FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
            left.Inlines = new Avalonia.Controls.Documents.InlineCollection
            {
                new Avalonia.Controls.Documents.Run(Loc.T("used_of", Math.Floor(used), (int)s.Allowance.TotalMinutes)) { FontWeight = FontWeight.SemiBold },
                new Avalonia.Controls.Documents.Run(" " + (spent ? Loc.T("used_blocked") : Loc.T("used_this_hour"))),
            };
            left[!TextBlock.ForegroundProperty] = UiTheme.Dyn(spent ? UiTheme.Danger : UiTheme.TextSecondary);

            var row = new DockPanel { LastChildFill = true };
            if (s.HourResetsAt is { } reset)
            {
                TextBlock refill = UiTheme.Caption(Loc.T("refills_at", Clock(reset)));
                DockPanel.SetDock(refill, Dock.Right);
                row.Children.Add(refill);
            }

            row.Children.Add(left);
            footer.Children.Add(row);
            footer.Children.Add(Meter(used / Math.Max(1, s.Allowance.TotalMinutes), spent ? UiTheme.Danger : UiTheme.Accent));
        }
        else if (s is not null && s.IsActive)
        {
            string text = s.ActiveWindowEndsAt is { } end ? Loc.T("blocked_until", Clock(end)) : Loc.T("blocked_all_day");
            footer.Children.Add(UiTheme.HStack(8, Icons.Make(Icons.Ban, 15, UiTheme.Danger), UiTheme.Text(text, 12.5, FontWeight.Medium, UiTheme.Danger)));
        }
        else
        {
            string text = rule.AllDay ? Loc.T("not_today") : Loc.T("starts_at", rule.From.ToString("HH:mm"));
            footer.Children.Add(UiTheme.HStack(8, Icons.Make(Icons.Clock, 15, UiTheme.TextTertiary), UiTheme.Caption(text)));
        }

        if (locked && status.StrictMode.ActiveUntilLocal is { } until)
        {
            footer.Children.Add(UiTheme.HStack(8, Icons.Make(Icons.Lock, 14, UiTheme.TextTertiary), UiTheme.Caption(Loc.T("locked_until", Clock(until)))));
        }
    }

    private static Control Meter(double fraction, string fillKey)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        var grid = new Grid
        {
            Height = 6,
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(fraction, GridUnitType.Star),
                new ColumnDefinition(1 - fraction, GridUnitType.Star),
            },
        };
        grid.Children.Add(new Border { CornerRadius = new CornerRadius(3), [!Border.BackgroundProperty] = UiTheme.Dyn(fillKey) });

        return new Border
        {
            Height = 6,
            CornerRadius = new CornerRadius(3),
            ClipToBounds = true,
            Child = grid,
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Track),
        };
    }

    private Control AddRuleTile()
    {
        var dash = new Rectangle
        {
            RadiusX = 14,
            RadiusY = 14,
            StrokeThickness = 1.5,
            StrokeDashArray = new AvaloniaList<double> { 4, 3 },
            [!Shape.StrokeProperty] = UiTheme.Dyn(UiTheme.BorderStrong),
        };

        var plus = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Border { HorizontalAlignment = HorizontalAlignment.Center, Child = Icons.Make(Icons.Plus, 20) },
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Track),
        };
        TextBlock title = UiTheme.Strong(Loc.T("new_rule"), 15);
        TextBlock sub = UiTheme.Muted(Loc.T("new_rule_hint"));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        var content = UiTheme.VStack(10, plus, title, sub);
        content.VerticalAlignment = VerticalAlignment.Center;

        var tile = new Grid
        {
            MinHeight = 224,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Children = { dash, content },
        };
        tile.AddHandler(Gestures.TappedEvent, (_, _) => OpenEditor(NewRule(), -1));
        return tile;
    }

    private EditableConfiguration.EditableRule NewRule() => new()
    {
        Id = NextId(),
        Name = Loc.T("new_rule"),
        BlockCompletely = false,
        AllowanceMinutes = 5,
        AllDay = true,
    };

    private string NextId()
    {
        var existing = _editable.Rules.Select(r => r.Id)
            .Concat(_status.StrictMode.LockedRuleIds)
            .ToHashSet(StringComparer.Ordinal);
        for (int i = 1; ; i++)
        {
            string candidate = $"rule-{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
