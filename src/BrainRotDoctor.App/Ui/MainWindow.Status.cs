using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Status: today's sites as a map of time, pause, and strict mode in one click ----------

    /// <summary>A shortcut turns strict mode on after this long, so a slip of the mouse can still be cancelled.</summary>
    private static readonly TimeSpan ActivationDelay = TimeSpan.FromSeconds(3);

    /// <summary>The map shows at most this many sites; the rest are too small to see anyway.</summary>
    private const int MappedSites = 12;

    private HashSet<string>? _untracked;
    private string? _openSite;
    private string? _justUntracked;
    private DispatcherTimer? _activation;

    // Blocks made from the status view in this session, by the place they block: the rule
    // and how it was before (null for a rule made new). Undoable until strict mode.
    private readonly Dictionary<string, (string RuleId, EditableConfiguration.EditableRule? Before)> _madeBlocks = new(StringComparer.Ordinal);

    private Control StatusScreen()
    {
        Control screen = WithSidebar(BuildStatusPage(), scrolls: false);
        if (_openSite is null)
        {
            return screen;
        }

        ActivitySite? site = ActivityReport.Build(_status.Activity).Sites.FirstOrDefault(s => s.Host == _openSite);
        if (site is null)
        {
            _openSite = null;
            return screen;
        }

        var overlay = new Overlay(screen, SiteSheet(site));
        overlay.Dismissed += (_, _) =>
        {
            _openSite = null;
            Rerender();
        };
        return overlay;
    }

    private Control BuildStatusPage()
    {
        StackPanel top = Layout.Column(Space.S4, new PageHeader(Loc.T("today"), null, PauseControl()));
        if (_status.Pause is { } pause)
        {
            top.Children.Add(PausedNote(pause));
        }

        top.Children.Add(StrictBar());
        if (_justUntracked is { } host)
        {
            top.Children.Add(UntrackedNote(host));
        }

        if (Untracked().Count > 0 || ActivityReport.Build(_status.Activity).Okay > TimeSpan.Zero)
        {
            top.Children.Add(ShowEverythingOption());
        }

        top.Margin = new Thickness(0, 0, 0, Space.S5);
        var page = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(top, Dock.Top);
        page.Children.Add(top);

        var map = new ContentControl();
        page.Children.Add(map);

        string? shown = null;
        _live.Add(status =>
        {
            ActivityReport report = ActivityReport.Build(status.Activity);
            bool everything = ShowEverything();
            string signature = Signature(report, everything);
            if (signature != shown)
            {
                shown = signature;
                map.Content = SiteMap(report, everything);
            }
        });

        return page;
    }

    // ----- Pause -----

    /// <summary>Pause (the same choices as strict mode), Resume while paused, unavailable in strict mode.</summary>
    private Control PauseControl()
    {
        if (_status.IsPaused)
        {
            var resume = new PillButton(Loc.T("resume"), ButtonKind.Warn, Glyph.Play);
            resume.Click += (_, _) => _controller.Resume();
            return resume;
        }

        var pause = new PillButton(Loc.T("pause"), ButtonKind.Secondary, Glyph.Pause);
        if (_status.StrictMode.IsActive)
        {
            pause.Enabled = false;
            ToolTip.SetTip(pause, Loc.T("pause_strict_note"));
            return pause;
        }

        var menu = new PopupMenu(PickerWidth + 12);
        menu.AddHeading(Loc.T("pause_for"));
        Control picker = DurationPicker(DurationUse.Pause, length =>
        {
            menu.Hide();
            _controller.Pause(length);
        });
        menu.AddContent(picker);
        pause.Click += (_, _) => menu.ShowAt(pause);
#if DEBUG
        _pauseMenu = (menu, pause, picker);
#endif
        return pause;
    }

#if DEBUG
    // The pause choices of the current screen, for the snapshots.
    private (PopupMenu Menu, Control Anchor, Control Content)? _pauseMenu;
#endif

    /// <summary>What a pause means and how long it has left; Resume is the button in the header.</summary>
    private Control PausedNote(PauseState pause)
    {
        TextLine message = Banner.Message(Tone.Warn);
        _live.Add(status =>
        {
            TimeSpan left = pause.UntilUtc - DateTimeOffset.UtcNow;
            message.SetPhrases(
                new Phrase(Loc.T("paused_until", Clock(pause.UntilUtc)), Strong: true),
                new Phrase($" {Loc.T("paused_explain")} {Loc.T("time_left", FormatSpan(left))}"));
        });
        return new Banner(Tone.Warn, Glyph.Pause, message);
    }

    // ----- Strict mode -----

    /// <summary>Off: needs attention, with one-click shortcuts. Activating: a moment to cancel. On: calm.</summary>
    private Control StrictBar()
    {
        if (_status.StrictMode.IsActive)
        {
            TextLine message = Banner.Message(Tone.Accent);
            _live.Add(status =>
            {
                if (status.StrictMode.ActiveUntilLocal is { } until)
                {
                    message.SetPhrases(
                        new Phrase(Loc.T("strict_on"), Strong: true),
                        new Phrase($" {Loc.T("until_time", Clock(until))} · {Loc.T("time_left", FormatSpan(status.StrictMode.Remaining))}"));
                }
            });
            return new Banner(Tone.Accent, Glyph.Lock, message);
        }

        if (_activation is not null)
        {
            var cancel = new PillButton(Loc.T("cancel"), ButtonKind.Ghost, size: ButtonSize.Small);
            cancel.Click += (_, _) => CancelStrict();
            return new Banner(Tone.Accent, Glyph.Lock, Banner.Message(Tone.Accent, new Phrase(Loc.T("strict_activating"), Strong: true)), cancel);
        }

        TextLine off = _status.IsPaused
            ? Banner.Message(Tone.Warn, new Phrase(Loc.T("strict_off"), Strong: true), new Phrase(" · " + Loc.T("strict_ends_pause")))
            : Banner.Message(Tone.Warn, new Phrase(Loc.T("strict_off"), Strong: true));
        Control picker = DurationPicker(DurationUse.Strict, length => BeginStrict(DateTimeOffset.Now + length));
        return new Banner(Tone.Warn, Glyph.Lock, Layout.Column(Space.S3, off, picker));
    }

    private void BeginStrict(DateTimeOffset end)
    {
        CancelStrict();
        _activation = new DispatcherTimer { Interval = ActivationDelay };
        _activation.Tick += (_, _) =>
        {
            _activation?.Stop();
            _activation = null;
            _controller.ActivateStrictMode(end - DateTimeOffset.Now);
            _status = _controller.Status;
            if (_page is Page.Status or Page.Strict)
            {
                Rerender();
            }
        };
        _activation.Start();
        Rerender();
    }

    private void CancelStrict()
    {
        if (_activation is null)
        {
            return;
        }

        _activation.Stop();
        _activation = null;
        if (_page is Page.Status or Page.Strict)
        {
            Rerender();
        }
    }

    // ----- The map -----

    /// <summary>
    /// Today's sites by time. With everything shown, the okay sites join as one grey
    /// tile in its place by size: how much of the day they take, nothing about which.
    /// </summary>
    private Control SiteMap(ActivityReport report, bool everything)
    {
        bool okay = everything && report.Okay >= ActivityReport.SiteMinimum;
        if (report.Sites.Count == 0 && !okay)
        {
            return new Card(new TextLine(Loc.T("no_browsing_yet"), TextStyle.Small) { MaxWidth = 420 }, CardStyle.Outline, CardPadding.Roomy)
            {
                VerticalAlignment = VerticalAlignment.Top,
            };
        }

        var map = new TileMap();
        void AddOkay()
        {
            if (okay)
            {
                okay = false;
                var tile = new MapTile(Loc.T("okay_sites"), 0, report.Okay, muted: true);
                ToolTip.SetTip(tile, $"{Loc.T("okay_sites")} · {Minutes(report.Okay)}");
                map.Add(tile, report.Okay.TotalSeconds);
            }
        }

        for (int i = 0; i < Math.Min(MappedSites, report.Sites.Count); i++)
        {
            ActivitySite site = report.Sites[i];
            if (site.Time < report.Okay)
            {
                AddOkay();
            }

            TileMap? parts = null;
            if (site.Places.Count > 1)
            {
                parts = new TileMap { Gap = 6 };
                foreach (ActivityPlace place in site.Places)
                {
                    var part = new MapTile(place.IsRest ? Loc.T("the_rest") : "/" + place.Path, i, place.Time, nested: true);
                    ToolTip.SetTip(part, $"{PlaceName(place)} · {Minutes(place.Time)}");
                    parts.Add(part, place.Time.TotalSeconds);
                }
            }

            var tile = new MapTile(site.Host, i, site.Time, parts, badge: RulesBadge(site.Host));
            ToolTip.SetTip(tile, $"{site.Host} · {Minutes(site.Time)}");
            tile.Click += (_, _) =>
            {
                _openSite = site.Host;
                _justUntracked = null;
                Rerender();
            };
            map.Add(tile, site.Time.TotalSeconds);
        }

        AddOkay();
        return map;
    }

    /// <summary>"Limited · Feeds" (or "Feeds +1") on a site some switched-on rule limits, any part of it.</summary>
    private Control? RulesBadge(string host)
    {
        List<string> names = RulesOn(host).Where(r => r.Rule.Enabled).Select(r => RuleName(r.Rule)).Distinct().ToList();
        if (names.Count == 0)
        {
            return null;
        }

        string text = names.Count == 1 ? names[0] : $"{names[0]} +{names.Count - 1}";
        var chip = new Chip(Loc.T("limited_by", text));
        ToolTip.SetTip(chip, string.Join(", ", names));
        return chip;
    }

    /// <summary>What the map shows, to the minute: it's rebuilt only when this changes.</summary>
    private static string Signature(ActivityReport report, bool everything) =>
        (everything ? $"okay:{(int)report.Okay.TotalMinutes};" : "") +
        string.Join(";", report.Sites.Take(MappedSites).Select(s =>
            $"{s.Host}:{(int)s.Time.TotalMinutes}:" + string.Join(",", s.Places.Select(p => $"{p.Path}={(int)p.Time.TotalMinutes}"))));

    private bool? _showEverything;

    private bool ShowEverything() => _showEverything ??= _settings.LoadShowEverything();

    /// <summary>"Show everything": the map with the okay sites too. Remembered.</summary>
    private Control ShowEverythingOption()
    {
        var option = new CheckOption(ShowEverything(), new TextLine(Loc.T("show_everything"), TextStyle.Small))
        {
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        option.Changed += on =>
        {
            _showEverything = on;
            _settings.SaveShowEverything(on);
            Rerender();
        };
        return option;
    }

    // ----- One site, looked at closely -----

    private Control SiteSheet(ActivitySite site)
    {
        BlockerConfiguration? rules = CurrentRules();

        var close = new IconButton(Glyph.Close, Loc.T("close"));
        close.Click += (_, _) =>
        {
            _openSite = null;
            Rerender();
        };

        var clocks = new ClockFill(site.Time, 26);
        ToolTip.SetTip(clocks, Minutes(site.Time));
        StackPanel title = Layout.Column(Space.S3, new TextLine(site.Host, TextStyle.Title), clocks);
        var head = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(close, Dock.Right);
        close.VerticalAlignment = VerticalAlignment.Top;
        head.Children.Add(close);
        head.Children.Add(title);

        var whole = new ActivityPlace(site.Host, "", site.Time, IsRest: false);
        StackPanel body = Layout.Column(Space.S5, head);
        if (_status.Pause is { } pause)
        {
            body.Children.Add(SheetPausedNote(pause));
        }

        List<(EditableConfiguration.EditableRule Rule, int Index, EditableConfiguration.EditableSite Site)> touching = RulesOn(site.Host);
        if (touching.Count > 0)
        {
            StackPanel list = Layout.Column(Space.S2, new TextLine(Loc.T("in_your_rules"), TextStyle.Overline));
            foreach ((EditableConfiguration.EditableRule rule, int index, EditableConfiguration.EditableSite target) in touching)
            {
                list.Children.Add(RuleRow(rule, index, target));
            }

            body.Children.Add(list);
        }

        body.Children.Add(BlockAction(site, whole, rules, Loc.T("block_site", site.Host), ButtonKind.Primary));

        if (site.Places.Count > 1)
        {
            var parts = new List<Control>();
            foreach (ActivityPlace place in site.Places)
            {
                var partClocks = new ClockFill(place.Time, 18);
                ToolTip.SetTip(partClocks, Minutes(place.Time));
                Control action = BlockAction(site, place, rules, Loc.T("block_part"), ButtonKind.Secondary);
                action.Margin = new Thickness(0, Space.S2, 0, 0);
                parts.Add(new Card(Layout.Column(Space.S2,
                    new TextLine(place.IsRest ? Loc.T("rest_of_site", site.Host) : "/" + place.Path, TextStyle.Code),
                    partClocks,
                    action), CardStyle.Well, CardPadding.Compact));
            }

            body.Children.Add(Layout.Tiles(parts, 220, Space.S3));
        }

        var stop = new PillButton(Loc.T("this_site_okay"), ButtonKind.Accent);
        stop.Click += (_, _) => SetTracked(site.Host, false);
        body.Children.Add(Layout.Column(Space.S3, new Divider(), stop));

        return new Card(body, CardStyle.Surface, CardPadding.Spacious);
    }

    /// <summary>"Block …" with its choices, or the rule that already limits this place.</summary>
    private Control BlockAction(ActivitySite site, ActivityPlace place, BlockerConfiguration? rules, string label, ButtonKind kind)
    {
        if (CoveringRule(place, rules) is { } rule)
        {
            StackPanel limited = Layout.Row(Space.S2, new Icon(Glyph.Lock, 14, TextTone.Secondary), new TextLine(Loc.T("limited_by", rule), TextStyle.Small));
            if (_madeBlocks.ContainsKey(place.Label) && !_status.StrictMode.IsActive)
            {
                var undo = new PillButton(Loc.T("undo"), ButtonKind.Accent, size: ButtonSize.Small);
                undo.Click += (_, _) => UndoBlock(place.Label);
                limited.Children.Add(undo);
            }

            return limited;
        }

        var block = new PillButton(label, kind, Glyph.Ban) { HorizontalAlignment = HorizontalAlignment.Left };
        var menu = new PopupMenu(260);

        // Strict mode lets rules only be added, so then there's nothing to add to.
        if (!_status.StrictMode.IsActive)
        {
            HashSet<string> here = RulesOn(site.Host).Select(r => r.Rule.Id).ToHashSet(StringComparer.Ordinal);
            List<EditableConfiguration.EditableRule> targets = _editable.Rules
                .Where(r => r.Enabled)
                .OrderByDescending(r => here.Contains(r.Id))
                .ToList();
            foreach (EditableConfiguration.EditableRule target in targets)
            {
                string id = target.Id;
                menu.AddItem(Loc.T("add_to_rule", RuleName(target)), null, () => AddToRule(site, place, id));
            }

            if (targets.Count > 0)
            {
                menu.AddDivider();
                menu.AddHeading(Loc.T("new_rule"));
            }
        }

        menu.AddItem(Loc.T("block_always"), null, () => BlockNow(site, place, null));
        menu.AddItem(Loc.T("block_after", 5), null, () => BlockNow(site, place, 5));
        menu.AddItem(Loc.T("block_after", 15), null, () => BlockNow(site, place, 15));
        menu.AddDivider();
        menu.AddItem(Loc.T("more_options"), null, () => OpenEditor(RuleFor(site, place), -1, returnTo: Page.Status));
        block.Click += (_, _) => menu.ShowAt(block);
        return block;
    }

    /// <summary>Saves a new rule for this place right away: all day, every day.</summary>
    private void BlockNow(ActivitySite site, ActivityPlace place, int? minutesPerHour)
    {
        EditableConfiguration.EditableRule rule = RuleFor(site, place);
        rule.BlockCompletely = minutesPerHour is null;
        rule.AllowanceMinutes = minutesPerHour ?? rule.AllowanceMinutes;

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        candidate.Rules.Add(rule);
        if (_controller.TrySaveConfiguration(candidate, out _))
        {
            _editable = _controller.GetEditableConfiguration();
            _madeBlocks[place.Label] = (rule.Id, null);
        }

        Rerender();
    }

    /// <summary>Adds this place to a rule there is already, saved right away.</summary>
    private void AddToRule(ActivitySite site, ActivityPlace place, string ruleId)
    {
        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        EditableConfiguration.EditableRule? rule = candidate.Rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null)
        {
            return;
        }

        EditableConfiguration.EditableRule before = rule.Clone();
        try
        {
            rule.Block(RuleFor(site, place).Sites[0]);
        }
        catch (ConfigurationException)
        {
            return;
        }

        if (_controller.TrySaveConfiguration(candidate, out _))
        {
            _editable = _controller.GetEditableConfiguration();
            _madeBlocks[place.Label] = (ruleId, before);
        }

        Rerender();
    }

    /// <summary>Takes a block made here back: a new rule goes, a rule added to is as it was.</summary>
    private void UndoBlock(string placeLabel)
    {
        if (!_madeBlocks.TryGetValue(placeLabel, out (string RuleId, EditableConfiguration.EditableRule? Before) made))
        {
            return;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        int index = candidate.Rules.FindIndex(r => r.Id == made.RuleId);
        if (made.Before is null)
        {
            candidate.Rules.RemoveAll(r => r.Id == made.RuleId);
        }
        else if (index >= 0)
        {
            candidate.Rules[index] = made.Before;
        }

        if (_controller.TrySaveConfiguration(candidate, out _))
        {
            _editable = _controller.GetEditableConfiguration();
            _madeBlocks.Remove(placeLabel);
        }

        Rerender();
    }

    // ----- The rules a site is already in -----

    /// <summary>The rules that block anything on this host, switched off ones too, in their order.</summary>
    private List<(EditableConfiguration.EditableRule Rule, int Index, EditableConfiguration.EditableSite Site)> RulesOn(string host)
    {
        var found = new List<(EditableConfiguration.EditableRule, int, EditableConfiguration.EditableSite)>();
        if (!Uri.TryCreate($"https://{host}/", UriKind.Absolute, out Uri? probe))
        {
            return found;
        }

        for (int i = 0; i < _editable.Rules.Count; i++)
        {
            EditableConfiguration.EditableRule rule = _editable.Rules[i];
            foreach (EditableConfiguration.EditableSite site in rule.Sites)
            {
                if ((site.BlockEverythingElse || site.Pages.Any(p => p.Block)) && OnHost(site, probe))
                {
                    found.Add((rule, i, site));
                    break;
                }
            }
        }

        return found;
    }

    private static bool OnHost(EditableConfiguration.EditableSite site, Uri probe)
    {
        try
        {
            return site.ToTargetSite().HostMatches(probe);
        }
        catch (ConfigurationException)
        {
            return false;
        }
    }

    /// <summary>
    /// One rule the site is in: what it blocks here, when, and what it's doing
    /// right now. Opens the rule, except in strict mode; a rule switched off can
    /// be switched on (that only blocks more).
    /// </summary>
    private Control RuleRow(EditableConfiguration.EditableRule rule, int index, EditableConfiguration.EditableSite site)
    {
        var chip = new Chip("");
        StackPanel text = Layout.Column(2,
            new TextLine(RuleName(rule), TextStyle.Strong),
            new TextLine($"{BlockedSummary(site)} · {ConditionSummary(rule)}", TextStyle.Small));
        if (!rule.Enabled)
        {
            Layout.Inactive(text);
        }

        var icon = new Icon(Glyph.Lock, 16, TextTone.Secondary) { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Space.S3, 0) };
        StackPanel end = Layout.Row(Space.S2, chip);
        end.VerticalAlignment = VerticalAlignment.Center;
        end.Margin = new Thickness(Space.S3, 0, 0, 0);
        if (!rule.Enabled)
        {
            var on = new PillButton(Loc.T("turn_on"), ButtonKind.Accent, size: ButtonSize.Small);
            on.Click += (_, _) => SetRuleEnabled(index, true);
            end.Children.Add(on);
        }

        var row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(icon, Dock.Left);
        DockPanel.SetDock(end, Dock.Right);
        row.Children.Add(icon);
        row.Children.Add(end);
        row.Children.Add(text);

        var card = new Card(row, CardStyle.Well, CardPadding.Compact);
        if (!_status.StrictMode.IsActive)
        {
            card.Click += (_, _) => OpenEditor(rule.Clone(), index, returnTo: Page.Status);
        }

        _live.Add(status => UpdateRuleChip(chip, rule, status.Rules.FirstOrDefault(r => r.RuleId == rule.Id), status.IsPaused));
        return card;
    }

    /// <summary>While paused nothing is blocked, the site's rules included, and a block made now waits too.</summary>
    private Control SheetPausedNote(PauseState pause)
    {
        var resume = new PillButton(Loc.T("resume_now"), ButtonKind.Primary, size: ButtonSize.Small);
        resume.Click += (_, _) => _controller.Resume();
        return new Banner(
            Tone.Warn,
            Glyph.Pause,
            Banner.Message(Tone.Warn, new Phrase(Loc.T("paused_until", Clock(pause.UntilUtc)), Strong: true), new Phrase(" " + Loc.T("starts_after_pause"))),
            resume);
    }

    private static string RuleName(EditableConfiguration.EditableRule rule) =>
        string.IsNullOrWhiteSpace(rule.Name) ? Loc.T("untitled") : rule.Name;

    /// <summary>
    /// A new rule holding just this place. For "the rest" the rule blocks the
    /// site but keeps the parts split out beside it open: exactly what it stands for.
    /// </summary>
    private EditableConfiguration.EditableRule RuleFor(ActivitySite site, ActivityPlace place)
    {
        EditableConfiguration.EditableSite target = EditableConfiguration.EditableSite.FromDocument(SiteMigration.FromUrl(place.Label, place.Url, includeSubpaths: true));
        if (place.IsRest)
        {
            foreach (ActivityPlace kept in site.Places.Where(p => !p.IsRest))
            {
                target.Pages.Add(new EditableConfiguration.EditablePage { Name = "/" + kept.Path, Path = "/" + kept.Path, Match = PageMatch.Under, Block = false });
            }
        }

        EditableConfiguration.EditableRule rule = NewRule();
        rule.Name = PlaceName(place);
        rule.Sites.Add(target);
        return rule;
    }

    private static string PlaceName(ActivityPlace place) => place.IsRest ? Loc.T("rest_of_site", place.Host) : place.Label;

    /// <summary>The rule already limiting this place: for a section its own address, for a site or its rest any page of it.</summary>
    private static string? CoveringRule(ActivityPlace place, BlockerConfiguration? rules)
    {
        if (rules is null)
        {
            return null;
        }

        string probe = place.Path.Length > 0 ? place.Url : $"https://{place.Host}/any-page";
        return Uri.TryCreate(probe, UriKind.Absolute, out Uri? url)
            ? rules.MatchingRules(url).FirstOrDefault()?.Name
            : null;
    }

    private BlockerConfiguration? CurrentRules()
    {
        try
        {
            return _editable.ToBlockerConfiguration();
        }
        catch (ConfigurationException)
        {
            return null;
        }
    }

    // ----- Not tracked -----

    private HashSet<string> Untracked() => _untracked ??= _settings.LoadUntrackedSites();

    /// <summary>Marks a site okay (its time joins the okay total, today's included) or tracks it on its own again.</summary>
    private void SetTracked(string host, bool tracked)
    {
        HashSet<string> untracked = Untracked();
        if (tracked)
        {
            untracked.Remove(host);
        }
        else
        {
            untracked.Add(host);
        }

        _settings.SaveUntrackedSites(untracked);
        _controller.SetUntrackedSites(untracked);
        _status = _controller.Status;
        _openSite = null;
        _justUntracked = tracked ? null : host;
        Rerender();
    }

    private Control UntrackedNote(string host)
    {
        var undo = new PillButton(Loc.T("undo"), ButtonKind.Accent, size: ButtonSize.Small);
        undo.Click += (_, _) => SetTracked(host, true);
        return new Banner(Tone.Neutral, Glyph.Check, Banner.Message(Tone.Neutral, new Phrase(Loc.T("okay_now", host))), undo);
    }

    // ----- Formatting -----

    /// <summary>"0 min", "&lt;1 min", "42 min", "1 h", "1 h 5 min".</summary>
    private static string Minutes(TimeSpan span)
    {
        (string h, string m, _) = Loc.DurationUnits();
        int total = (int)span.TotalMinutes;
        if (total < 1)
        {
            return span > TimeSpan.Zero ? $"<1 {m}" : $"0 {m}";
        }

        return total < 60 ? $"{total} {m}"
            : total % 60 == 0 ? $"{total / 60} {h}"
            : $"{total / 60} {h} {total % 60} {m}";
    }
}
