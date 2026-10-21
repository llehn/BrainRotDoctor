using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Home: the rule overview ----------

    private Control BuildHome()
    {
        var page = new StackPanel { Spacing = Space.S6 };
        page.Children.Add(new PageHeader(
            Loc.T("rules"),
            Loc.T("rules_subtitle"),
            NewRuleButton()));

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
        page.Children.Add(Layout.Tiles(tiles, 300, Space.S4));
        return page;
    }

    private PillButton NewRuleButton()
    {
        var button = new PillButton(Loc.T("new_rule"), ButtonKind.Primary, Glyph.Plus);
        button.Click += (_, _) => OpenEditor(NewRule(), -1);
        return button;
    }

    /// <summary>Rules has no pause of its own (that's on Status), so while paused its banner carries the way back.</summary>
    private Control PausedBanner(PauseState pause)
    {
        string until = Loc.T("paused_until", Clock(pause.UntilUtc));
        var resume = new PillButton(Loc.T("resume_now"), ButtonKind.Primary);
        resume.Click += (_, _) => _controller.Resume();
        return new Banner(
            Tone.Warn,
            Glyph.Pause,
            Banner.Message(Tone.Warn, new Phrase(until, Strong: true), new Phrase(" " + Loc.T("paused_explain"))),
            resume);
    }

    private Control RuleCard(EditableConfiguration.EditableRule rule, int index)
    {
        // While strict mode is on, rules only tighten: a rule that's off can still
        // be switched on, but nothing else about any rule can change.
        bool strict = _status.StrictMode.IsActive;
        bool locked = strict && rule.Enabled;

        var chip = new Chip("");
        var onOff = new OnOffSwitch(rule.Enabled) { Margin = new Thickness(10, 0, 0, 0) };
        if (locked)
        {
            onOff.Enabled = false;
        }

        onOff.Toggled += on => SetRuleEnabled(index, on);
        var title = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(onOff, Dock.Right);
        DockPanel.SetDock(chip, Dock.Right);
        title.Children.Add(onOff);
        title.Children.Add(chip);
        var name = new TextLine(string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name, TextStyle.Subheading)
        {
            SingleLine = true,
            Margin = new Thickness(0, 0, 10, 0),
        };
        if (locked)
        {
            var lockIcon = new Icon(Glyph.Lock, 15, TextTone.Tertiary) { Margin = new Thickness(0, 0, Space.S2, 0) };
            DockPanel.SetDock(lockIcon, Dock.Left);
            title.Children.Add(lockIcon);
        }

        title.Children.Add(name);

        var sites = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        foreach (EditableConfiguration.EditableSite site in rule.Sites)
        {
            sites.Children.Add(new InfoChip(SiteLook.Monogram(site.CatalogId, site.DisplayLabel, 18), site.DisplayLabel, BlockedSummary(site)));
        }

        foreach (EditableConfiguration.EditableApp app in rule.Apps)
        {
            sites.Children.Add(new InfoChip(AppMark(app, 18), AppLabel(app)));
        }

        if (rule.Sites.Count == 0 && rule.Apps.Count == 0)
        {
            sites.Children.Add(new TextLine(Loc.T("no_sites"), TextStyle.Caption));
        }

        var footer = new StackPanel { Spacing = 7 };
        StackPanel details = Layout.Column(Space.S3, new TextLine(ConditionSummary(rule), TextStyle.Small), sites);
        if (!rule.Enabled)
        {
            Layout.Inactive(details);
        }

        StackPanel top = Layout.Column(Space.S3, title, details);
        var body = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        footer.Margin = new Thickness(0, Space.S4, 0, 0);
        body.Children.Add(top);
        body.Children.Add(footer);

        var card = new Card(body) { MinHeight = 224 };
        if (!strict)
        {
            card.Click += (_, _) => OpenEditor(rule.Clone(), index);
        }

        _live.Add(status =>
        {
            RuleSnapshot? snapshot = status.Rules.FirstOrDefault(r => r.RuleId == rule.Id);
            UpdateRuleChip(chip, rule, snapshot, status.IsPaused);
            UpdateRuleFooter(footer, rule, snapshot, status, locked);
        });

        return card;
    }

    private void SetRuleEnabled(int index, bool on)
    {
        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        if (index < candidate.Rules.Count)
        {
            candidate.Rules[index].Enabled = on;
            if (_controller.TrySaveConfiguration(candidate, out _))
            {
                _editable = _controller.GetEditableConfiguration();
            }
        }

        Rerender();
    }

    private static void UpdateRuleChip(Chip chip, EditableConfiguration.EditableRule rule, RuleSnapshot? s, bool paused)
    {
        (string text, Tone tone) = !rule.Enabled
            ? (Loc.T("rule_off"), Tone.Neutral)
            : paused
            ? (Loc.T("paused"), Tone.Warn)
            : s is null || !s.IsActive
                ? (Loc.T("off_hours"), Tone.Neutral)
                : s.IsBlocking
                    ? (Loc.T("blocking_now"), Tone.Danger)
                    : (Loc.T("allowed_now"), Tone.Accent);
        chip.SetState(text, tone);
    }

    private static void UpdateRuleFooter(StackPanel footer, EditableConfiguration.EditableRule rule, RuleSnapshot? s, AppStatus status, bool locked)
    {
        footer.Children.Clear();
        if (!rule.Enabled)
        {
            footer.Children.Add(new TextLine(Loc.T("rule_switched_off"), TextStyle.Caption));
        }
        else if (s is not null && s.IsActive && !s.BlocksCompletely)
        {
            double used = Math.Min(s.Consumed.TotalMinutes, s.Allowance.TotalMinutes);
            bool spent = s.IsBlocking;
            TextLine left = TextLine.Rich(
                TextStyle.Caption,
                spent ? TextTone.Danger : TextTone.Secondary,
                new Phrase(Loc.T("used_of", Math.Floor(used), (int)s.Allowance.TotalMinutes), Strong: true),
                new Phrase(" " + (spent ? Loc.T("used_blocked") : Loc.T("used_this_hour"))));

            var row = new DockPanel { LastChildFill = true };
            if (s.HourResetsAt is { } reset)
            {
                var refill = new TextLine(Loc.T("refills_at", Clock(reset)), TextStyle.Caption);
                DockPanel.SetDock(refill, Dock.Right);
                row.Children.Add(refill);
            }

            row.Children.Add(left);
            footer.Children.Add(row);
            footer.Children.Add(new Meter(used / Math.Max(1, s.Allowance.TotalMinutes), spent ? Tone.Danger : Tone.Accent));
        }
        else if (s is not null && s.IsActive)
        {
            string text = s.ActiveWindowEndsAt is { } end ? Loc.T("blocked_until", Clock(end)) : Loc.T("blocked_all_day");
            footer.Children.Add(Layout.Row(Space.S2, new Icon(Glyph.Ban, 15, TextTone.Danger), new TextLine(text, TextStyle.CaptionStrong, TextTone.Danger)));
        }
        else
        {
            string text = rule.AllDay ? Loc.T("not_today") : Loc.T("starts_at", rule.From.ToString("HH:mm"));
            footer.Children.Add(Layout.Row(Space.S2, new Icon(Glyph.Clock, 15, TextTone.Tertiary), new TextLine(text, TextStyle.Caption)));
        }

        if (locked && status.StrictMode.ActiveUntilLocal is { } until)
        {
            footer.Children.Add(Layout.Row(Space.S2, new Icon(Glyph.Lock, 14, TextTone.Tertiary), new TextLine(Loc.T("locked_until", Clock(until)), TextStyle.Caption)));
        }
    }

    private Control AddRuleTile()
    {
        var tile = new PlaceholderTile(Glyph.Plus, Loc.T("new_rule"), Loc.T("new_rule_hint")) { MinHeight = 224 };
        tile.Click += (_, _) => OpenEditor(NewRule(), -1);
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
        var existing = _editable.Rules.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
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
