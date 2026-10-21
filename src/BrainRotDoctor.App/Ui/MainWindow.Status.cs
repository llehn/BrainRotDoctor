using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Apps;
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

    /// <summary>"At night" from the block menu: the hours a new rule blocks.</summary>
    private static readonly TimeOnly NightFrom = new(22, 0);
    private static readonly TimeOnly NightTo = new(7, 0);

    private HashSet<string>? _untracked;
    private Dictionary<string, string>? _untrackedApps;
    private string? _openSite;
    private string? _openApp;
    private string? _justUntracked;
    private (string Key, string Name)? _justUntrackedApp;
    private DispatcherTimer? _activation;

    // Blocks made from the status view in this session, by the place they block: the rule
    // and how it was before (null for a rule made new). Undoable until strict mode.
    private readonly Dictionary<string, (string RuleId, EditableConfiguration.EditableRule? Before)> _madeBlocks = new(StringComparer.Ordinal);

    private Control StatusScreen()
    {
        Control screen = WithSidebar(BuildStatusPage(), scrolls: false);
        if (_openSite is null && _openApp is null)
        {
            return screen;
        }

        ActivityReport report = ActivityReport.Build(_status.Activity);
        Control? sheet = _openSite is not null
            ? report.Sites.FirstOrDefault(s => s.Host == _openSite) is { } site ? SiteSheet(site) : null
            : report.Apps.FirstOrDefault(a => a.Key == _openApp) is { } app ? AppSheet(app) : null;
        if (sheet is null)
        {
            _openSite = null;
            _openApp = null;
            return screen;
        }

        var overlay = new Overlay(screen, sheet);
        overlay.Dismissed += (_, _) =>
        {
            _openSite = null;
            _openApp = null;
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
            top.Children.Add(UntrackedNote(host, () => SetTracked(host, true)));
        }
        else if (_justUntrackedApp is { } app)
        {
            top.Children.Add(UntrackedNote(app.Name, () => SetAppTracked(app.Key, app.Name, true)));
        }

        if (Untracked().Count > 0 || UntrackedApps().Count > 0 || ActivityReport.Build(_status.Activity).Okay > TimeSpan.Zero)
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
    /// Today's sites and apps by time. With everything shown, the okay ones join as one
    /// grey tile in its place by size: how much of the day they take, nothing about which.
    /// </summary>
    private Control SiteMap(ActivityReport report, bool everything)
    {
        bool okay = everything && report.Okay >= ActivityReport.SiteMinimum;
        if (report.Sites.Count == 0 && report.Apps.Count == 0 && !okay)
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

        var shown = report.Sites.Select(s => (s.Time, Site: (ActivitySite?)s, App: (ActivityApp?)null))
            .Concat(report.Apps.Select(a => (a.Time, Site: (ActivitySite?)null, App: (ActivityApp?)a)))
            .OrderByDescending(e => e.Time)
            .Take(MappedSites)
            .ToList();
        for (int i = 0; i < shown.Count; i++)
        {
            if (shown[i].Time < report.Okay)
            {
                AddOkay();
            }

            if (shown[i].App is { } app)
            {
                var appTile = new MapTile(app.App.Name, i, app.Time, badge: RulesBadge(RulesOn(app.App).Select(r => r.Rule)));
                ToolTip.SetTip(appTile, $"{app.App.Name} · {AppCaption(app.App)} · {Minutes(app.Time)}");
                appTile.Click += (_, _) =>
                {
                    _openApp = app.Key;
                    _justUntracked = null;
                    _justUntrackedApp = null;
                    Rerender();
                };
                map.Add(appTile, app.Time.TotalSeconds);
                continue;
            }

            ActivitySite site = shown[i].Site!;

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

            var tile = new MapTile(site.Host, i, site.Time, parts, badge: RulesBadge(RulesOn(site.Host).Select(r => r.Rule)));
            ToolTip.SetTip(tile, $"{site.Host} · {Minutes(site.Time)}");
            tile.Click += (_, _) =>
            {
                _openSite = site.Host;
                _justUntracked = null;
                _justUntrackedApp = null;
                Rerender();
            };
            map.Add(tile, site.Time.TotalSeconds);
        }

        AddOkay();
        return map;
    }

    /// <summary>"Limited · Feeds" (or "Feeds +1") on a site or app some switched-on rule limits, any part of it.</summary>
    private static Control? RulesBadge(IEnumerable<EditableConfiguration.EditableRule> rules)
    {
        List<string> names = rules.Where(r => r.Enabled).Select(RuleName).Distinct().ToList();
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
            $"{s.Host}:{(int)s.Time.TotalMinutes}:" + string.Join(",", s.Places.Select(p => $"{p.Path}={(int)p.Time.TotalMinutes}")))) +
        "|" + string.Join(";", report.Apps.Take(MappedSites).Select(a => $"{a.Key}:{(int)a.Time.TotalMinutes}"));

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
                list.Children.Add(RuleRow(rule, index, BlockedSummary(target)));
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

    // ----- One app, looked at closely -----

    /// <summary>
    /// An app's day, the rules it's in, and blocking it: the app itself, and for Steam
    /// and its games all Steam games at once (the usual wish: no games at night).
    /// </summary>
    private Control AppSheet(ActivityApp app)
    {
        BlockerConfiguration? rules = CurrentRules();

        var close = new IconButton(Glyph.Close, Loc.T("close"));
        close.Click += (_, _) =>
        {
            _openApp = null;
            Rerender();
        };

        var clocks = new ClockFill(app.Time, 26);
        ToolTip.SetTip(clocks, Minutes(app.Time));
        StackPanel title = Layout.Column(Space.S3,
            Layout.Column(Space.S1, new TextLine(app.App.Name, TextStyle.Title), new TextLine(AppCaption(app.App), TextStyle.Small)),
            clocks);
        var head = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(close, Dock.Right);
        close.VerticalAlignment = VerticalAlignment.Top;
        head.Children.Add(close);
        head.Children.Add(title);

        StackPanel body = Layout.Column(Space.S5, head);
        if (_status.Pause is { } pause)
        {
            body.Children.Add(SheetPausedNote(pause));
        }

        List<(EditableConfiguration.EditableRule Rule, int Index, EditableConfiguration.EditableApp App)> touching = RulesOn(app.App);
        if (touching.Count > 0)
        {
            StackPanel list = Layout.Column(Space.S2, new TextLine(Loc.T("in_your_rules"), TextStyle.Overline));
            foreach ((EditableConfiguration.EditableRule rule, int index, EditableConfiguration.EditableApp target) in touching)
            {
                list.Children.Add(RuleRow(rule, index, target.Kind == TargetAppKind.AllSteamGames ? AppLabel(target) : Loc.T("whole_app")));
            }

            body.Children.Add(list);
        }

        string label = Loc.T(app.App.Kind == AppKind.SteamGame ? "block_this_game" : "block_this_app");
        StackPanel actions = Layout.Column(Space.S3, AppBlockAction(app.App, rules, label, ButtonKind.Primary, () => EditableConfiguration.EditableApp.For(app.App)));
        if (app.App.Kind is AppKind.SteamGame or AppKind.Steam)
        {
            actions.Children.Add(AppBlockAction(AnySteamGame, rules, Loc.T("block_all_steam"), ButtonKind.Secondary, EditableConfiguration.EditableApp.AllSteamGames));
        }

        body.Children.Add(actions);

        var stop = new PillButton(Loc.T("this_app_okay"), ButtonKind.Accent);
        stop.Click += (_, _) => SetAppTracked(app.Key, app.App.Name, false);
        body.Children.Add(Layout.Column(Space.S3, new Divider(), stop));

        return new Card(body, CardStyle.Surface, CardPadding.Spacious);
    }

    /// <summary>A Steam game no rule names on its own, so only "All Steam games" covers it.</summary>
    private static readonly AppIdentity AnySteamGame = new(AppKind.SteamGame, "", "", null, "\u0001");

    /// <summary>"Block …" for an app with its choices, or the rule that already limits it.</summary>
    private Control AppBlockAction(AppIdentity app, BlockerConfiguration? rules, string label, ButtonKind kind, Func<EditableConfiguration.EditableApp> entry)
    {
        EditableConfiguration.EditableApp probe = entry();
        return BlockMenu(
            label,
            kind,
            $"app:{AppDocument.KindText(probe.Kind)}:{probe.Folder ?? probe.Exe}",
            rules?.MatchingRules(app).FirstOrDefault()?.Name,
            RulesOn(app).Select(r => r.Rule.Id),
            () =>
            {
                EditableConfiguration.EditableRule rule = NewRule();
                EditableConfiguration.EditableApp added = entry();
                rule.Name = AppLabel(added);
                rule.Apps.Add(added);
                return rule;
            },
            rule => rule.Block(entry()));
    }

    /// <summary>The rules that hold <paramref name="app"/>, switched off ones too, in their order.</summary>
    private List<(EditableConfiguration.EditableRule Rule, int Index, EditableConfiguration.EditableApp App)> RulesOn(AppIdentity app)
    {
        var found = new List<(EditableConfiguration.EditableRule, int, EditableConfiguration.EditableApp)>();
        for (int i = 0; i < _editable.Rules.Count; i++)
        {
            EditableConfiguration.EditableRule rule = _editable.Rules[i];
            if (rule.Apps.FirstOrDefault(a => TargetOf(a)?.Matches(app) == true) is { } entry)
            {
                found.Add((rule, i, entry));
            }
        }

        return found;
    }

    // ----- Blocking from a sheet -----

    /// <summary>"Block …" for a site or a part of it, with its choices, or the rule that already limits it.</summary>
    private Control BlockAction(ActivitySite site, ActivityPlace place, BlockerConfiguration? rules, string label, ButtonKind kind) =>
        BlockMenu(
            label,
            kind,
            place.Label,
            CoveringRule(place, rules),
            RulesOn(site.Host).Select(r => r.Rule.Id),
            () => RuleFor(site, place),
            rule => rule.Block(RuleFor(site, place).Sites[0]));

    /// <summary>
    /// The block button of a sheet. Its menu adds to a rule there is already (not in strict
    /// mode, where rules can only be added) or saves a new rule at once: always, at night,
    /// or after a few minutes an hour. Once a rule limits it: which one, and Undo for a
    /// block made here.
    /// </summary>
    /// <param name="label">The button's text.</param>
    /// <param name="kind">The button's kind.</param>
    /// <param name="made">What a block made here is remembered by, for Undo.</param>
    /// <param name="coveredBy">The name of the rule that limits it already, or null.</param>
    /// <param name="here">The rules it is in already, offered first.</param>
    /// <param name="newRule">A new rule holding just it.</param>
    /// <param name="addTo">Adds it to a rule.</param>
    private Control BlockMenu(
        string label,
        ButtonKind kind,
        string made,
        string? coveredBy,
        IEnumerable<string> here,
        Func<EditableConfiguration.EditableRule> newRule,
        Action<EditableConfiguration.EditableRule> addTo)
    {
        if (coveredBy is not null)
        {
            StackPanel limited = Layout.Row(Space.S2, new Icon(Glyph.Lock, 14, TextTone.Secondary), new TextLine(Loc.T("limited_by", coveredBy), TextStyle.Small));
            if (_madeBlocks.ContainsKey(made) && !_status.StrictMode.IsActive)
            {
                var undo = new PillButton(Loc.T("undo"), ButtonKind.Accent, size: ButtonSize.Small);
                undo.Click += (_, _) => UndoBlock(made);
                limited.Children.Add(undo);
            }

            return limited;
        }

        var block = new PillButton(label, kind, Glyph.Ban) { HorizontalAlignment = HorizontalAlignment.Left };
        var menu = new PopupMenu(260);

        // Strict mode lets rules only be added, so then there's nothing to add to.
        if (!_status.StrictMode.IsActive)
        {
            HashSet<string> first = here.ToHashSet(StringComparer.Ordinal);
            List<EditableConfiguration.EditableRule> targets = _editable.Rules
                .Where(r => r.Enabled)
                .OrderByDescending(r => first.Contains(r.Id))
                .ToList();
            foreach (EditableConfiguration.EditableRule target in targets)
            {
                string id = target.Id;
                menu.AddItem(Loc.T("add_to_rule", RuleName(target)), null, () => AddToRule(id, made, addTo));
            }

            if (targets.Count > 0)
            {
                menu.AddDivider();
                menu.AddHeading(Loc.T("new_rule"));
            }
        }

        menu.AddItem(Loc.T("block_always"), null, () => BlockNow(newRule(), made, null, night: false));
        menu.AddItem(Loc.T("block_at_night", NightFrom.ToString("HH:mm"), NightTo.ToString("HH:mm")), null, () => BlockNow(newRule(), made, null, night: true));
        menu.AddItem(Loc.T("block_after", 5), null, () => BlockNow(newRule(), made, 5, night: false));
        menu.AddItem(Loc.T("block_after", 15), null, () => BlockNow(newRule(), made, 15, night: false));
        menu.AddDivider();
        menu.AddItem(Loc.T("more_options"), null, () => OpenEditor(newRule(), -1, returnTo: Page.Status));
        block.Click += (_, _) => menu.ShowAt(block);
        return block;
    }

    /// <summary>Saves <paramref name="rule"/> as a new rule right away: every day, all day or at night.</summary>
    private void BlockNow(EditableConfiguration.EditableRule rule, string made, int? minutesPerHour, bool night)
    {
        rule.BlockCompletely = minutesPerHour is null;
        rule.AllowanceMinutes = minutesPerHour ?? rule.AllowanceMinutes;
        if (night)
        {
            rule.AllDay = false;
            rule.From = NightFrom;
            rule.To = NightTo;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        candidate.Rules.Add(rule);
        if (_controller.TrySaveConfiguration(candidate, out _))
        {
            _editable = _controller.GetEditableConfiguration();
            _madeBlocks[made] = (rule.Id, null);
        }

        Rerender();
    }

    /// <summary>Adds a place of a site, or an app, to a rule there is already, saved right away.</summary>
    private void AddToRule(string ruleId, string made, Action<EditableConfiguration.EditableRule> addTo)
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
            addTo(rule);
        }
        catch (ConfigurationException)
        {
            return;
        }

        if (_controller.TrySaveConfiguration(candidate, out _))
        {
            _editable = _controller.GetEditableConfiguration();
            _madeBlocks[made] = (ruleId, before);
        }

        Rerender();
    }

    /// <summary>Takes a block made here back: a new rule goes, a rule added to is as it was.</summary>
    private void UndoBlock(string madeKey)
    {
        if (!_madeBlocks.TryGetValue(madeKey, out (string RuleId, EditableConfiguration.EditableRule? Before) made))
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
            _madeBlocks.Remove(madeKey);
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
    /// One rule the site or app is in: what it blocks of it, when, and what it's doing
    /// right now. Opens the rule, except in strict mode; a rule switched off can
    /// be switched on (that only blocks more).
    /// </summary>
    private Control RuleRow(EditableConfiguration.EditableRule rule, int index, string what)
    {
        var chip = new Chip("");
        StackPanel text = Layout.Column(2,
            new TextLine(RuleName(rule), TextStyle.Strong),
            new TextLine($"{what} · {ConditionSummary(rule)}", TextStyle.Small));
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

    /// <summary>Apps marked okay: their key and the name to list them by.</summary>
    private Dictionary<string, string> UntrackedApps() => _untrackedApps ??= _settings.LoadUntrackedApps();

    /// <summary>Marks an app okay (its time joins the okay total, today's included) or tracks it on its own again.</summary>
    private void SetAppTracked(string key, string name, bool tracked)
    {
        Dictionary<string, string> untracked = UntrackedApps();
        if (tracked)
        {
            untracked.Remove(key);
        }
        else
        {
            untracked[key] = name;
        }

        _settings.SaveUntrackedApps(untracked);
        _controller.SetUntrackedApps(untracked.Keys);
        _status = _controller.Status;
        _openApp = null;
        _justUntracked = null;
        _justUntrackedApp = tracked ? null : (key, name);
        Rerender();
    }

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
        _justUntrackedApp = null;
        Rerender();
    }

    private static Control UntrackedNote(string name, Action undoIt)
    {
        var undo = new PillButton(Loc.T("undo"), ButtonKind.Accent, size: ButtonSize.Small);
        undo.Click += (_, _) => undoIt();
        return new Banner(Tone.Neutral, Glyph.Check, Banner.Message(Tone.Neutral, new Phrase(Loc.T("okay_now", name))), undo);
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
