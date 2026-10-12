using Avalonia;
using Avalonia.Controls;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Activity;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Status: today's browsing, strict mode, achievements ----------

    /// <summary>Under this much brain rot, the day still counts as clean.</summary>
    private static readonly TimeSpan CleanLimit = TimeSpan.FromMinutes(15);

    private const int ListedSites = 12;

    private HashSet<string>? _fineSites;

    private Control BuildStatusPage()
    {
        _fineSites ??= _settings.LoadFineSites();
        BlockerConfiguration? rules = CurrentRules();

        var page = new StackPanel { Spacing = Space.S6 };
        page.Children.Add(new PageHeader(Loc.T("status"), Loc.T("status_subtitle")));
        if (_status.Pause is { } pause)
        {
            page.Children.Add(PausedBanner(pause));
        }

        // Summary
        var rotTime = new TextLine("", TextStyle.Display);
        var ofTotal = new TextLine("", TextStyle.Small);
        var rotShare = new Meter(0, Tone.Danger) { Margin = new Thickness(0, Space.S2, 0, 0) };
        var summary = new Card(
            Layout.Column(Space.S2, new TextLine(Loc.T("rot_today"), TextStyle.Overline), rotTime, ofTotal, rotShare),
            CardStyle.Surface,
            CardPadding.Roomy);

        // Where the time went
        var sitesHost = new ContentControl();
        var where = new Card(
            Layout.Column(0,
                new Band(Layout.Column(Space.S1, new TextLine(Loc.T("where_time_went"), TextStyle.Subheading), new TextLine(Loc.T("where_time_went_help"), TextStyle.Small)), new Thickness(Space.S6, Space.S5)),
                new Divider(),
                sitesHost),
            CardStyle.Surface,
            CardPadding.None);

        // Achievements
        var achievementsHost = new ContentControl();
        var achievements = new Card(
            Layout.Column(Space.S4, new TextLine(Loc.T("achievements"), TextStyle.Subheading), achievementsHost),
            CardStyle.Surface,
            CardPadding.Roomy);

        page.Children.Add(Layout.Columns(summary, StrictAtAGlance(), 340, gap: Space.S5, fixedRight: true));
        page.Children.Add(Layout.Columns(where, achievements, 340, gap: Space.S5, fixedRight: true));

        string? shownSites = null;
        string? shownAchievements = null;
        _live.Add(status =>
        {
            ActivityReport report = ActivityReport.Build(status.Activity, _fineSites);

            rotTime.SetText(Minutes(report.BrainRot));
            ofTotal.SetText(Loc.T("rot_of_total", Minutes(report.Total)));
            rotShare.SetFraction(report.Total > TimeSpan.Zero ? report.BrainRot / report.Total : 0);

            string sites = SitesSignature(report);
            if (sites != shownSites)
            {
                shownSites = sites;
                sitesHost.Content = SiteList(report, rules);
            }

            bool[] earned = Earned(report, status);
            string badges = string.Concat(earned.Select(e => e ? '1' : '0'));
            if (badges != shownAchievements)
            {
                shownAchievements = badges;
                achievementsHost.Content = AchievementList(earned);
            }
        });

        return page;
    }

    // ----- Strict mode at a glance -----

    private Control StrictAtAGlance()
    {
        if (!_status.StrictMode.IsActive)
        {
            var turnOn = new PillButton(Loc.T("turn_on_strict"), ButtonKind.Primary, Glyph.Lock) { Stretch = true };
            turnOn.Click += (_, _) => Navigate(Page.Strict);
            return new Card(
                Layout.Column(Space.S4,
                    Layout.Column(Space.S2,
                        Layout.Row(Space.S2, new Icon(Glyph.Lock, 18), new TextLine(Loc.T("strict_off"), TextStyle.Subheading)),
                        new TextLine(Loc.T("strict_off_help"), TextStyle.Small)),
                    turnOn),
                CardStyle.Surface,
                CardPadding.Roomy);
        }

        var countdown = new TextLine("", TextStyle.Heading, TextTone.OnInk);
        var progress = new Meter(0, Tone.Ink, onInk: true);
        string until = _status.StrictMode.ActiveUntilLocal is { } end ? WhenText(end) : "";
        var card = new Card(
            Layout.Column(Space.S3,
                Layout.Row(Space.S2, new Icon(Glyph.Lock, 16, TextTone.OnInkMuted), new TextLine(Loc.T("strict_on"), TextStyle.Label, TextTone.OnInkMuted)),
                countdown,
                new TextLine(Loc.T("unlocks") + " " + until, TextStyle.Small, TextTone.OnInkMuted),
                progress),
            CardStyle.Ink,
            CardPadding.Roomy);
        card.Click += (_, _) => Navigate(Page.Strict);

        _live.Add(status =>
        {
            countdown.SetText(FormatSpan(status.StrictMode.Remaining));
            progress.SetFraction(status.StrictMode.Progress);
        });

        return card;
    }

    // ----- Where the time went -----

    private Control SiteList(ActivityReport report, BlockerConfiguration? rules)
    {
        if (report.Sites.Count == 0)
        {
            return new Band(new TextLine(Loc.T("no_browsing_yet"), TextStyle.Small), new Thickness(Space.S6, Space.S5));
        }

        TimeSpan longest = report.Sites[0].Time;
        var list = new StackPanel();
        foreach (ActivitySite site in report.Sites.Take(ListedSites))
        {
            if (list.Children.Count > 0)
            {
                list.Children.Add(new Divider());
            }

            list.Children.Add(SiteRow(site, longest, rules));
        }

        int unlisted = report.ShortSites + Math.Max(0, report.Sites.Count - ListedSites);
        if (unlisted > 0)
        {
            list.Children.Add(new Divider());
            list.Children.Add(new Band(new TextLine(Loc.T("other_sites", unlisted), TextStyle.Caption), new Thickness(Space.S6, Space.S3), shaded: true));
        }

        return list;
    }

    private Control SiteRow(ActivitySite site, TimeSpan longest, BlockerConfiguration? rules)
    {
        (string verdict, Tone tone) = site.Verdict switch
        {
            Verdict.BrainRot => (Loc.T("verdict_rot"), Tone.Danger),
            Verdict.WatchOut => (Loc.T("verdict_watch"), Tone.Warn),
            _ => (site.MarkedFine ? Loc.T("verdict_your_call") : Loc.T("verdict_fine"), Tone.Accent),
        };

        var chip = new Chip(verdict, tone);
        ToolTip.SetTip(chip, Loc.T("verdict_explain", (int)ActivityReport.RotTime.TotalMinutes, ActivityReport.RotVisits, (int)ActivityReport.RotCheckingTime.TotalMinutes));

        StackPanel right = Layout.Row(Space.S3);
        if (site.MarkedFine)
        {
            var undo = new PillButton(Loc.T("undo"), ButtonKind.Ghost, size: ButtonSize.Small);
            undo.Click += (_, _) => SetFine(site.Host, false);
            right.Children.Add(undo);
        }
        else if (site.Verdict != Verdict.Fine)
        {
            var fine = new PillButton(Loc.T("not_rot"), ButtonKind.Ghost, size: ButtonSize.Small);
            fine.Click += (_, _) => SetFine(site.Host, true);
            right.Children.Add(fine);
        }

        right.Children.Add(chip);
        right.Children.Add(new TextLine(Minutes(site.Time), TextStyle.Strong) { MinWidth = 72, TextAlignment = Avalonia.Media.TextAlignment.Right });

        Monogram mark = SiteLook.Monogram(CatalogIdOf(site.Host), site.Host, 32);
        mark.Margin = new Thickness(0, 0, Space.S3, 0);
        right.Margin = new Thickness(Space.S3, 0, 0, 0);
        var head = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(right, Dock.Right);
        DockPanel.SetDock(mark, Dock.Left);
        head.Children.Add(right);
        head.Children.Add(mark);
        head.Children.Add(Layout.Column(Space.S0,
            new TextLine(site.Host, TextStyle.Strong) { SingleLine = true },
            new TextLine(Loc.T("opened_times", site.Visits), TextStyle.Caption)));

        Tone bar = site.Verdict switch
        {
            Verdict.BrainRot => Tone.Danger,
            Verdict.WatchOut => Tone.Warn,
            _ => Tone.Accent,
        };

        StackPanel row = Layout.Column(Space.S3, head, new Meter(longest > TimeSpan.Zero ? site.Time / longest : 0, bar));
        foreach (ActivityPlace place in site.Places)
        {
            row.Children.Add(PlaceRow(site, place, rules));
        }

        return new Band(row, new Thickness(Space.S6, Space.S4));
    }

    private Control PlaceRow(ActivitySite site, ActivityPlace place, BlockerConfiguration? rules)
    {
        Control action;
        if (CoveringRule(place, rules) is { } rule)
        {
            action = new Chip(Loc.T("in_rule", rule), Tone.Neutral);
        }
        else
        {
            var block = new PillButton(Loc.T("block"), ButtonKind.Secondary, Glyph.Ban, ButtonSize.Small);
            block.Click += (_, _) => BlockPlace(site, place);
            action = block;
        }

        StackPanel right = Layout.Row(Space.S3, new TextLine(Minutes(place.Time), TextStyle.Caption), action);
        right.Margin = new Thickness(Space.S3, 0, 0, 0);
        var line = new DockPanel { LastChildFill = true, Margin = new Thickness(44, 0, 0, 0) };
        DockPanel.SetDock(right, Dock.Right);
        line.Children.Add(right);
        line.Children.Add(place.Path.Length > 0
            ? new TextLine("/" + place.Path, TextStyle.Code) { SingleLine = true }
            : new TextLine(place.IsRest ? Loc.T("everything_else") : Loc.T("whole_site"), TextStyle.Small));
        return line;
    }

    /// <summary>
    /// Opens a new rule holding just this place; you choose when it applies. For
    /// "everything else" the rule blocks the site but keeps the sections listed
    /// above it open — exactly what the line stands for.
    /// </summary>
    private void BlockPlace(ActivitySite site, ActivityPlace place)
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
        rule.Name = place.Label;
        rule.Sites.Add(target);
        OpenEditor(rule, -1, returnTo: Page.Status);
    }

    private void SetFine(string host, bool fine)
    {
        _fineSites ??= _settings.LoadFineSites();
        if (fine)
        {
            _fineSites.Add(host);
        }
        else
        {
            _fineSites.Remove(host);
        }

        _settings.SaveFineSites(_fineSites);
        Rerender();
    }

    /// <summary>The rule already covering this place: for a section its own address, for a whole site any page of it.</summary>
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

    private static string? CatalogIdOf(string host) =>
        SiteCatalog.Sites.FirstOrDefault(s => host == s.Host || host.EndsWith("." + s.Host, StringComparison.Ordinal))?.Id;

    /// <summary>What the list shows, to the minute: it's rebuilt only when this changes.</summary>
    private static string SitesSignature(ActivityReport report) =>
        string.Join(";", report.Sites.Take(ListedSites).Select(s =>
            $"{s.Host}:{(int)s.Time.TotalMinutes}:{s.Visits}:{s.Verdict}:{s.MarkedFine}:" +
            string.Join(",", s.Places.Select(p => $"{p.Path}={(int)p.Time.TotalMinutes}"))))
        + $"|{report.ShortSites}|{report.Sites.Count}";

    // ----- Achievements -----

    private static bool[] Earned(ActivityReport report, AppStatus status) => new[]
    {
        report.BrainRot < CleanLimit,
        report.Sites.All(s => s.MarkedFine || s.Time < ActivityReport.RotTime),
        !status.Activity.PausedToday && !status.IsPaused,
        status.Activity.StrictToday || status.StrictMode.IsActive,
    };

    private static Control AchievementList(bool[] earned)
    {
        var items = new (string Title, string Help)[]
        {
            (Loc.T("ach_clean"), Loc.T("ach_clean_help", (int)CleanLimit.TotalMinutes)),
            (Loc.T("ach_no_binge"), Loc.T("ach_no_binge_help", (int)ActivityReport.RotTime.TotalMinutes)),
            (Loc.T("ach_no_pause"), Loc.T("ach_no_pause_help")),
            (Loc.T("ach_locked"), Loc.T("ach_locked_help")),
        };

        var list = new StackPanel { Spacing = Space.S4 };
        for (int i = 0; i < items.Length; i++)
        {
            StackPanel badge = Layout.Row(Space.S3,
                new Icon(Glyph.Award, 22, earned[i] ? TextTone.Success : TextTone.Tertiary),
                Layout.Column(Space.S0, new TextLine(items[i].Title, TextStyle.Strong), new TextLine(items[i].Help, TextStyle.Caption)));
            list.Children.Add(earned[i] ? badge : Layout.Inactive(badge));
        }

        return list;
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
