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

    // Rules made from the status view in this session, by the place they block: undoable until strict mode.
    private readonly Dictionary<string, string> _madeRules = new(StringComparer.Ordinal);

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

        var menu = new PopupMenu(420);
        menu.AddHeading(Loc.T("pause_for"));
        Control picker = DurationPicker(DurationUse.Pause, length =>
        {
            menu.Hide();
            _controller.Pause(length);
        });
        menu.AddContent(picker);
        pause.Click += (_, _) => menu.ShowAt(pause);
        _pauseMenu = (menu, pause, picker);
        return pause;
    }

    // The pause choices of the current screen, for the DEBUG snapshots.
    private (PopupMenu Menu, Control Anchor, Control Content)? _pauseMenu;

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

            var tile = new MapTile(site.Host, i, site.Time, parts);
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
        StackPanel body = Layout.Column(Space.S5, head, BlockAction(site, whole, rules, Loc.T("block_site", site.Host), ButtonKind.Primary));

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
            if (_madeRules.TryGetValue(place.Label, out string? madeId) && !_status.StrictMode.IsActive)
            {
                var undo = new PillButton(Loc.T("undo"), ButtonKind.Accent, size: ButtonSize.Small);
                undo.Click += (_, _) => RemoveMadeRule(place.Label, madeId);
                limited.Children.Add(undo);
            }

            return limited;
        }

        var block = new PillButton(label, kind, Glyph.Ban) { HorizontalAlignment = HorizontalAlignment.Left };
        var menu = new PopupMenu(240);
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
            _madeRules[place.Label] = rule.Id;
        }

        Rerender();
    }

    private void RemoveMadeRule(string placeLabel, string ruleId)
    {
        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        candidate.Rules.RemoveAll(r => r.Id == ruleId);
        if (_controller.TrySaveConfiguration(candidate, out _))
        {
            _editable = _controller.GetEditableConfiguration();
            _madeRules.Remove(placeLabel);
        }

        Rerender();
    }

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
