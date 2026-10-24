namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for switching a single rule on and off. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    // Per language: chip on the rule card, card footer.
    private static readonly Dictionary<string, string[]> RuleSwitch = new()
    {
        ["en"] = new[] { "Off", "Switched off — blocks nothing" },
        ["de"] = new[] { "Aus", "Ausgeschaltet – blockiert nichts" },
        ["fr"] = new[] { "Désactivée", "Désactivée — ne bloque rien" },
        ["es"] = new[] { "Apagada", "Apagada: no bloquea nada" },
        ["it"] = new[] { "Spenta", "Spenta — non blocca nulla" },
        ["pt"] = new[] { "Desligada", "Desligada — não bloqueia nada" },
        ["nl"] = new[] { "Uit", "Uitgeschakeld — blokkeert niets" },
        ["pl"] = new[] { "Wył.", "Wyłączona — nic nie blokuje" },
        ["sv"] = new[] { "Av", "Avstängd – blockerar inget" },
        ["da"] = new[] { "Fra", "Slået fra – blokerer intet" },
        ["fi"] = new[] { "Pois", "Pois päältä – ei estä mitään" },
        ["cs"] = new[] { "Vyp.", "Vypnuto – nic neblokuje" },
        ["sk"] = new[] { "Vyp.", "Vypnuté – nič neblokuje" },
        ["sl"] = new[] { "Izklop.", "Izklopljeno – ne blokira ničesar" },
        ["hr"] = new[] { "Isklj.", "Isključeno – ništa ne blokira" },
        ["ro"] = new[] { "Oprită", "Oprită — nu blochează nimic" },
        ["hu"] = new[] { "Ki", "Kikapcsolva – semmit sem tilt" },
        ["el"] = new[] { "Ανενεργός", "Απενεργοποιημένος — δεν μπλοκάρει τίποτα" },
        ["bg"] = new[] { "Изкл.", "Изключено — не блокира нищо" },
        ["lt"] = new[] { "Išj.", "Išjungta – nieko neblokuoja" },
        ["lv"] = new[] { "Izsl.", "Izslēgts — neko nebloķē" },
        ["et"] = new[] { "Väljas", "Välja lülitatud – ei blokeeri midagi" },
        ["ga"] = new[] { "As", "Múchta — ní bhlocálann sé faic" },
        ["mt"] = new[] { "Mitfija", "Mitfija — ma timblokka xejn" },
        ["ca"] = new[] { "Apagada", "Apagada: no bloqueja res" },
        ["nb"] = new[] { "Av", "Slått av – blokkerer ingenting" },
        ["tr"] = new[] { "Kapalı", "Kapalı — hiçbir şeyi engellemez" },
        ["ru"] = new[] { "Выкл.", "Выключено — ничего не блокирует" },
        ["uk"] = new[] { "Вимк.", "Вимкнено — нічого не блокує" },
        ["bs"] = new[] { "Isklj.", "Isključeno – ništa ne blokira" },
        ["sr"] = new[] { "Искљ.", "Искључено — ништа не блокира" },
    };

    private static void MergeRuleSwitch(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in RuleSwitch)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                table["rule_off"] = text[0];
                table["rule_switched_off"] = text[1];
            }
        }
    }
}
