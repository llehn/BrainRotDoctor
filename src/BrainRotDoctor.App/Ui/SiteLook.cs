using BrainRotDoctor.Core.Configuration;
using BrainRotDoctor.DesignSystem;

namespace BrainRotDoctor.App.Ui;

/// <summary>How a website or an app is drawn: its monogram's brand colours and letters.</summary>
internal static class SiteLook
{
    public static Monogram Monogram(string? catalogId, string label, double size = 36)
    {
        (Swatch swatch, string? letters) = catalogId switch
        {
            "instagram" => (new Swatch("#F6DCE8", "#8A1F4E"), "Ig"),
            "youtube" => (new Swatch("#FADBD6", "#8E2214"), "Yt"),
            "tiktok" => (new Swatch("#DDE9EA", "#1B4F54"), "Tt"),
            "facebook" => (new Swatch("#DCE5FA", "#1F3E8C"), "Fb"),
            "x" => (new Swatch("#E3E3E6", "#16171B"), "X"),
            "reddit" => (new Swatch("#FBE2D3", "#8A3A0C"), "Rd"),
            "linkedin" => (new Swatch("#D9E7F5", "#17436E"), "In"),
            "twitch" => (new Swatch("#E6DEF7", "#4A2A8A"), "Tw"),
            "pinterest" => (new Swatch("#F7DADA", "#8C1C1C"), "Pi"),
            _ => (Swatch.Neutral, null),
        };
        return new Monogram(letters ?? DesignSystem.Monogram.InitialsOf(label), swatch, size);
    }

    /// <summary>An app's mark: Steam and its games in Steam's colours, any other program in neutral initials.</summary>
    public static Monogram AppMonogram(TargetAppKind kind, string label, double size = 36) => kind switch
    {
        TargetAppKind.Program => new Monogram(DesignSystem.Monogram.InitialsOf(label), Swatch.Neutral, size),
        TargetAppKind.Steam or TargetAppKind.AllSteamGames => new Monogram("St", Steam, size),
        _ => new Monogram(DesignSystem.Monogram.InitialsOf(label), Steam, size),
    };

    private static readonly Swatch Steam = new("#DCE3EE", "#1B2838");
}
