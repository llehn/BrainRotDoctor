namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Tooltips of the strict-mode length stepper. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    // Per language: shorter, longer.
    private static readonly Dictionary<string, string[]> StrictStepper = new()
    {
        ["en"] = new[] { "Shorter", "Longer" },
        ["de"] = new[] { "Kürzer", "Länger" },
        ["fr"] = new[] { "Plus court", "Plus long" },
        ["es"] = new[] { "Más corto", "Más largo" },
        ["it"] = new[] { "Più breve", "Più lungo" },
        ["pt"] = new[] { "Mais curto", "Mais longo" },
        ["nl"] = new[] { "Korter", "Langer" },
        ["pl"] = new[] { "Krócej", "Dłużej" },
        ["sv"] = new[] { "Kortare", "Längre" },
        ["da"] = new[] { "Kortere", "Længere" },
        ["fi"] = new[] { "Lyhyempi", "Pidempi" },
        ["cs"] = new[] { "Kratší", "Delší" },
        ["sk"] = new[] { "Kratšie", "Dlhšie" },
        ["sl"] = new[] { "Krajše", "Daljše" },
        ["hr"] = new[] { "Kraće", "Duže" },
        ["ro"] = new[] { "Mai scurt", "Mai lung" },
        ["hu"] = new[] { "Rövidebb", "Hosszabb" },
        ["el"] = new[] { "Συντομότερα", "Περισσότερο" },
        ["bg"] = new[] { "По-кратко", "По-дълго" },
        ["lt"] = new[] { "Trumpiau", "Ilgiau" },
        ["lv"] = new[] { "Īsāk", "Ilgāk" },
        ["et"] = new[] { "Lühem", "Pikem" },
        ["ga"] = new[] { "Níos giorra", "Níos faide" },
        ["mt"] = new[] { "Iqsar", "Itwal" },
        ["ca"] = new[] { "Més curt", "Més llarg" },
        ["nb"] = new[] { "Kortere", "Lengre" },
        ["tr"] = new[] { "Daha kısa", "Daha uzun" },
        ["ru"] = new[] { "Короче", "Дольше" },
        ["uk"] = new[] { "Коротше", "Довше" },
        ["bs"] = new[] { "Kraće", "Duže" },
        ["sr"] = new[] { "Краће", "Дуже" },
    };

    private static void MergeStrictStepper(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in StrictStepper)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                table["shorter"] = text[0];
                table["longer"] = text[1];
            }
        }
    }
}
