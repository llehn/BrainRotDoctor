using Avalonia;
using Avalonia.Controls;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Accounting;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Strict mode ----------

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
    /// how long, with the same card as on Status. It knows nothing about rules;
    /// the rules side refuses anything but tightening while it is on.
    /// </summary>
    private Control BuildStrictSetup()
    {
        var page = new StackPanel { Spacing = Space.S6 };
        page.Children.Add(new PageHeader(Loc.T("strict_mode"), Loc.T("strict_subtitle")));

        // How long: picking starts the same short countdown as on Status, cancelled here too.
        page.Children.Add(StrictBar());

        // While it's on + turning it on
        Control Consequence(string glyph, TextTone tone, string text) =>
            Layout.Row(10, new Icon(glyph, 18, tone), new TextLine(text));

        var whileOn = new Card(Layout.Column(14,
            new TextLine(Loc.T("while_on"), TextStyle.Subheading),
            Consequence(Glyph.Ban, TextTone.Danger, Loc.T("strict_no_edit")),
            Consequence(Glyph.Ban, TextTone.Danger, Loc.T("while_no_pause")),
            Consequence(Glyph.Ban, TextTone.Danger, Loc.T("while_no_uninstall")),
            Consequence(Glyph.Check, TextTone.Accent, Loc.T("strict_can_add")),
            new Divider(),
            new TextLine(Loc.T("no_early_exit"), TextStyle.Strong)), CardStyle.Surface, CardPadding.Roomy);

        page.Children.Add(whileOn);
        return page;
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
