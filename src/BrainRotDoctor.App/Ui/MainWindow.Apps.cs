using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using BrainRotDoctor.App.Runtime;
using BrainRotDoctor.Core.Apps;
using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

internal sealed partial class MainWindow
{
    // ---------- Apps in rules: names, captions and rows shared by the screens ----------

    /// <summary>The name an app entry goes by: "All Steam games", "Steam", or its own.</summary>
    private static string AppLabel(EditableConfiguration.EditableApp app) => app.Kind switch
    {
        TargetAppKind.AllSteamGames => Loc.T("all_steam_games"),
        TargetAppKind.Steam => "Steam",
        _ => string.IsNullOrWhiteSpace(app.Label) ? app.Exe ?? app.Folder ?? "?" : app.Label,
    };

    /// <summary>What an app entry covers, in a few words: "Steam game", "Program · discord.exe".</summary>
    private static string AppCaption(EditableConfiguration.EditableApp app) => app.Kind switch
    {
        TargetAppKind.AllSteamGames => Loc.T("all_steam_games_caption"),
        TargetAppKind.Steam => Loc.T("steam_caption"),
        TargetAppKind.SteamGame => Loc.T("steam_game"),
        _ => app.Exe is { Length: > 0 } exe ? $"{Loc.T("program")} · {exe}" : Loc.T("program"),
    };

    /// <summary>The same, for an app as it runs.</summary>
    private static string AppCaption(AppIdentity app) => AppCaption(EditableConfiguration.EditableApp.For(app));

    private static Monogram AppMark(EditableConfiguration.EditableApp app, double size = 36) => SiteLook.AppMonogram(app.Kind, AppLabel(app), size);

    /// <summary>One app in a list: its mark, name and caption, and a × to take it out.</summary>
    private static Control AppLine(EditableConfiguration.EditableApp app, Action remove, double mark = 36)
    {
        var removeButton = new IconButton(Glyph.Close, Loc.T("remove"), IconButtonStyle.Bare) { VerticalAlignment = VerticalAlignment.Center };
        removeButton.Click += (_, _) => remove();
        Monogram monogram = AppMark(app, mark);
        monogram.Margin = new Thickness(0, 0, Space.S3, 0);
        var line = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(monogram, Dock.Left);
        DockPanel.SetDock(removeButton, Dock.Right);
        line.Children.Add(monogram);
        line.Children.Add(removeButton);
        StackPanel text = Layout.Column(2, new TextLine(AppLabel(app), TextStyle.Strong) { SingleLine = true }, new TextLine(AppCaption(app), TextStyle.Caption) { SingleLine = true });
        text.VerticalAlignment = VerticalAlignment.Center;
        line.Children.Add(text);
        return line;
    }

    /// <summary>The validated app entry, or null when it can't be one (then it matches nothing).</summary>
    private static TargetApp? TargetOf(EditableConfiguration.EditableApp app)
    {
        try
        {
            return app.ToTargetApp();
        }
        catch (ConfigurationException)
        {
            return null;
        }
    }
}
