using System.Globalization;

namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Lightweight localization. Strings are keyed; English (US) is the canonical
/// fallback. The active language follows the Windows UI language by default and
/// can be overridden in Settings; changing it raises <see cref="Changed"/> so the
/// current screen can re-render.
/// </summary>
internal static class Loc
{
    public const string Auto = "auto";

    /// <summary>Selectable languages: the 24 official EU languages, English first.</summary>
    public static readonly IReadOnlyList<(string Code, string Native)> Languages = new[]
    {
        ("en", "English (US)"),
        ("bg", "Български"),
        ("hr", "Hrvatski"),
        ("cs", "Čeština"),
        ("da", "Dansk"),
        ("nl", "Nederlands"),
        ("et", "Eesti"),
        ("fi", "Suomi"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("el", "Ελληνικά"),
        ("hu", "Magyar"),
        ("ga", "Gaeilge"),
        ("it", "Italiano"),
        ("lv", "Latviešu"),
        ("lt", "Lietuvių"),
        ("mt", "Malti"),
        ("pl", "Polski"),
        ("pt", "Português"),
        ("ro", "Română"),
        ("sk", "Slovenčina"),
        ("sl", "Slovenščina"),
        ("es", "Español"),
        ("sv", "Svenska"),
        ("ca", "Català"),
        ("nb", "Norsk bokmål"),
        ("ru", "Русский"),
        ("sr", "Српски"),
        ("bs", "Bosanski"),
        ("tr", "Türkçe"),
        ("uk", "Українська"),
    };

    // Normalize related codes Windows may report to one we ship.
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["no"] = "nb",
        ["nn"] = "nb",
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Tables = Strings.BuildTables();

    private static string _code = "en";

    public static event Action? Changed;

    /// <summary>The resolved two-letter language code currently in use.</summary>
    public static string Active => _code;

    /// <summary>Culture for number/time/day formatting (English maps to en-US).</summary>
    public static CultureInfo Culture =>
        CultureInfo.GetCultureInfo(_code == "en" ? "en-US" : _code);

    public static void Initialize(string preference) => Resolve(preference);

    public static void SetPreference(string preference)
    {
        string before = _code;
        Resolve(preference);
        if (before != _code)
        {
            Changed?.Invoke();
        }
    }

    public static string T(string key)
    {
        if (Tables.TryGetValue(_code, out Dictionary<string, string>? table)
            && table.TryGetValue(key, out string? value))
        {
            return value;
        }

        return Tables["en"].TryGetValue(key, out string? english) ? english : key;
    }

    public static string T(string key, params object[] args) => string.Format(Culture, T(key), args);

    /// <summary>
    /// Localized names for the two rules created on first run. Kept here (not in
    /// the string tables) because they are seed content, not UI chrome — once
    /// written to the config they are ordinary, user-editable rule names.
    /// </summary>
    public static (string ShortVideo, string Feeds) DefaultRuleNames() => _code switch
    {
        "de" => ("Kurzvideos", "Feeds"),
        "fr" => ("Vidéos courtes", "Fils d'actualité"),
        "es" => ("Vídeos cortos", "Feeds"),
        "it" => ("Video brevi", "Feed"),
        "pt" => ("Vídeos curtos", "Feeds"),
        "nl" => ("Korte video's", "Feeds"),
        "pl" => ("Krótkie filmy", "Kanały"),
        "sv" => ("Korta videor", "Flöden"),
        "da" => ("Korte videoer", "Feeds"),
        "fi" => ("Lyhytvideot", "Syötteet"),
        "cs" => ("Krátká videa", "Kanály"),
        "sk" => ("Krátke videá", "Kanály"),
        "sl" => ("Kratki videoposnetki", "Viri"),
        "hr" => ("Kratki videozapisi", "Feedovi"),
        "ro" => ("Videoclipuri scurte", "Fluxuri"),
        "hu" => ("Rövid videók", "Hírfolyamok"),
        "el" => ("Σύντομα βίντεο", "Ροές"),
        "bg" => ("Кратки видеа", "Емисии"),
        "lt" => ("Trumpi vaizdo įrašai", "Srautai"),
        "lv" => ("Īsie video", "Plūsmas"),
        "et" => ("Lühivideod", "Vood"),
        "ga" => ("Físeáin ghearra", "Fothaí"),
        "mt" => ("Vidjows qosra", "Feeds"),
        "ca" => ("Vídeos curts", "Canals"),
        "nb" => ("Korte videoer", "Feeder"),
        "tr" => ("Kısa videolar", "Akışlar"),
        "ru" => ("Короткие видео", "Ленты"),
        "uk" => ("Короткі відео", "Стрічки"),
        "bs" => ("Kratki videozapisi", "Feedovi"),
        "sr" => ("Кратки видео снимци", "Фидови"),
        _ => ("Short video", "Feeds"),
    };

    /// <summary>Compact duration unit abbreviations (hour, minute, second) for the active language.</summary>
    public static (string H, string M, string S) DurationUnits() => _code switch
    {
        "ru" or "bg" or "sr" => ("ч", "мин", "с"),
        "uk" => ("год", "хв", "с"),
        "el" => ("ω", "λ", "δ"),
        "de" => ("Std", "Min", "Sek"),
        "nl" => ("u", "min", "s"),
        "pl" => ("g", "min", "s"),
        "sv" => ("tim", "min", "s"),
        "da" or "nb" or "fi" or "et" => ("t", "min", "s"),
        "lt" => ("val", "min", "s"),
        "lv" => ("st", "min", "s"),
        "hu" => ("ó", "p", "mp"),
        "tr" => ("sa", "dk", "sn"),
        "ga" => ("u", "n", "s"),
        "en" => ("h", "m", "s"),
        // fr, es, it, pt, ca, ro, cs, sk, sl, hr, bs, mt and others
        _ => ("h", "min", "s"),
    };

    private static void Resolve(string preference)
    {
        string code = string.Equals(preference, Auto, StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            : preference;

        if (Aliases.TryGetValue(code, out string? mapped))
        {
            code = mapped;
        }

        _code = Tables.ContainsKey(code) ? code : "en";
        DesignSystem.DesignTheme.TextCulture = Culture;
    }
}
