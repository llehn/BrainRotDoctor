namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for switching a single rule on and off. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    // Per language: chip on the rule card, card footer, strict mode with every rule off.
    private static readonly Dictionary<string, string[]> RuleSwitch = new()
    {
        ["en"] = new[] { "Off", "Switched off — blocks nothing", "All your rules are switched off. Switch one on to lock it." },
        ["de"] = new[] { "Aus", "Ausgeschaltet – blockiert nichts", "Alle deine Regeln sind ausgeschaltet. Schalte eine ein, um sie zu sperren." },
        ["fr"] = new[] { "Désactivée", "Désactivée — ne bloque rien", "Toutes vos règles sont désactivées. Activez-en une pour la verrouiller." },
        ["es"] = new[] { "Apagada", "Apagada: no bloquea nada", "Todas tus reglas están apagadas. Enciende una para bloquearla." },
        ["it"] = new[] { "Spenta", "Spenta — non blocca nulla", "Tutte le tue regole sono spente. Accendine una per bloccarla." },
        ["pt"] = new[] { "Desligada", "Desligada — não bloqueia nada", "Todas as suas regras estão desligadas. Ligue uma para a trancar." },
        ["nl"] = new[] { "Uit", "Uitgeschakeld — blokkeert niets", "Al je regels staan uit. Zet er een aan om hem te vergrendelen." },
        ["pl"] = new[] { "Wył.", "Wyłączona — nic nie blokuje", "Wszystkie reguły są wyłączone. Włącz jedną, aby ją zablokować." },
        ["sv"] = new[] { "Av", "Avstängd – blockerar inget", "Alla dina regler är avstängda. Slå på en för att låsa den." },
        ["da"] = new[] { "Fra", "Slået fra – blokerer intet", "Alle dine regler er slået fra. Slå en til for at låse den." },
        ["fi"] = new[] { "Pois", "Pois päältä – ei estä mitään", "Kaikki sääntösi ovat pois päältä. Kytke yksi päälle lukitaksesi sen." },
        ["cs"] = new[] { "Vyp.", "Vypnuto – nic neblokuje", "Všechna pravidla jsou vypnutá. Zapněte jedno, abyste ho mohli zamknout." },
        ["sk"] = new[] { "Vyp.", "Vypnuté – nič neblokuje", "Všetky pravidlá sú vypnuté. Zapnite jedno, aby ste ho mohli zamknúť." },
        ["sl"] = new[] { "Izklop.", "Izklopljeno – ne blokira ničesar", "Vsa vaša pravila so izklopljena. Vklopite eno, da ga zaklenete." },
        ["hr"] = new[] { "Isklj.", "Isključeno – ništa ne blokira", "Sva su vaša pravila isključena. Uključite jedno da biste ga zaključali." },
        ["ro"] = new[] { "Oprită", "Oprită — nu blochează nimic", "Toate regulile tale sunt oprite. Pornește una ca s-o blochezi." },
        ["hu"] = new[] { "Ki", "Kikapcsolva – semmit sem tilt", "Minden szabályod ki van kapcsolva. Kapcsolj be egyet a zároláshoz." },
        ["el"] = new[] { "Ανενεργός", "Απενεργοποιημένος — δεν μπλοκάρει τίποτα", "Όλοι οι κανόνες σας είναι απενεργοποιημένοι. Ενεργοποιήστε έναν για να τον κλειδώσετε." },
        ["bg"] = new[] { "Изкл.", "Изключено — не блокира нищо", "Всички ваши правила са изключени. Включете едно, за да го заключите." },
        ["lt"] = new[] { "Išj.", "Išjungta – nieko neblokuoja", "Visos jūsų taisyklės išjungtos. Įjunkite vieną, kad galėtumėte ją užrakinti." },
        ["lv"] = new[] { "Izsl.", "Izslēgts — neko nebloķē", "Visi jūsu noteikumi ir izslēgti. Ieslēdziet vienu, lai to bloķētu." },
        ["et"] = new[] { "Väljas", "Välja lülitatud – ei blokeeri midagi", "Kõik su reeglid on välja lülitatud. Lülita üks sisse, et see lukustada." },
        ["ga"] = new[] { "As", "Múchta — ní bhlocálann sé faic", "Tá do rialacha go léir múchta. Cas ceann air chun é a ghlasáil." },
        ["mt"] = new[] { "Mitfija", "Mitfija — ma timblokka xejn", "Ir-regoli kollha tiegħek huma mitfija. Ixgħel waħda biex tissakkarha." },
        ["ca"] = new[] { "Apagada", "Apagada: no bloqueja res", "Totes les teves regles estan apagades. Encén-ne una per bloquejar-la." },
        ["nb"] = new[] { "Av", "Slått av – blokkerer ingenting", "Alle reglene dine er slått av. Slå på én for å låse den." },
        ["tr"] = new[] { "Kapalı", "Kapalı — hiçbir şeyi engellemez", "Tüm kuralların kapalı. Kilitlemek için birini aç." },
        ["ru"] = new[] { "Выкл.", "Выключено — ничего не блокирует", "Все ваши правила выключены. Включите одно, чтобы заблокировать его." },
        ["uk"] = new[] { "Вимк.", "Вимкнено — нічого не блокує", "Усі ваші правила вимкнено. Увімкніть одне, щоб заблокувати його." },
        ["bs"] = new[] { "Isklj.", "Isključeno – ništa ne blokira", "Sva vaša pravila su isključena. Uključite jedno da biste ga zaključali." },
        ["sr"] = new[] { "Искљ.", "Искључено — ништа не блокира", "Сва ваша правила су искључена. Укључите једно да бисте га закључали." },
    };

    private static void MergeRuleSwitch(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in RuleSwitch)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                table["rule_off"] = text[0];
                table["rule_switched_off"] = text[1];
                table["no_rules_on"] = text[2];
            }
        }
    }
}
