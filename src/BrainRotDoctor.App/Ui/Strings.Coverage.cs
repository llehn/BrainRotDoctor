namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for what the rules already do to a site on the status view: the
/// rules it's in, adding it to one of them, and blocking while paused. Merged
/// into the tables by <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    // Per language: heading over the rules a site is in, adding to a rule, a block made while paused.
    private static readonly Dictionary<string, string[]> Coverage = new()
    {
        ["en"] = new[] { "Already in your rules", "Add to {0}", "Anything you block now starts when the pause ends." },
        ["de"] = new[] { "Schon in deinen Regeln", "Zu {0} hinzufügen", "Was du jetzt blockierst, gilt ab dem Ende der Pause." },
        ["fr"] = new[] { "Déjà dans vos règles", "Ajouter à {0}", "Ce que vous bloquez maintenant s'appliquera à la fin de la pause." },
        ["es"] = new[] { "Ya está en tus reglas", "Añadir a {0}", "Lo que bloquees ahora empezará cuando termine la pausa." },
        ["it"] = new[] { "Già nelle tue regole", "Aggiungi a {0}", "Ciò che blocchi ora vale dalla fine della pausa." },
        ["pt"] = new[] { "Já está nas suas regras", "Adicionar a {0}", "O que bloquear agora começa quando a pausa terminar." },
        ["nl"] = new[] { "Al in je regels", "Toevoegen aan {0}", "Wat je nu blokkeert, gaat in zodra de pauze voorbij is." },
        ["pl"] = new[] { "Już w twoich regułach", "Dodaj do {0}", "To, co teraz zablokujesz, zacznie działać po końcu pauzy." },
        ["sv"] = new[] { "Redan i dina regler", "Lägg till i {0}", "Det du blockerar nu börjar gälla när pausen är slut." },
        ["da"] = new[] { "Allerede i dine regler", "Føj til {0}", "Det, du blokerer nu, gælder, når pausen slutter." },
        ["fi"] = new[] { "Jo säännöissäsi", "Lisää sääntöön {0}", "Mitä nyt estät, alkaa tauon päätyttyä." },
        ["cs"] = new[] { "Už ve vašich pravidlech", "Přidat do {0}", "Co teď zablokujete, začne platit po skončení pauzy." },
        ["sk"] = new[] { "Už vo vašich pravidlách", "Pridať do {0}", "Čo teraz zablokujete, začne platiť po skončení pauzy." },
        ["sl"] = new[] { "Že v vaših pravilih", "Dodaj v {0}", "Kar blokirate zdaj, začne veljati, ko se premor konča." },
        ["hr"] = new[] { "Već u vašim pravilima", "Dodaj u {0}", "Ono što sada blokirate počinje vrijediti kad pauza završi." },
        ["ro"] = new[] { "Deja în regulile tale", "Adaugă la {0}", "Ce blochezi acum intră în vigoare când se termină pauza." },
        ["hu"] = new[] { "Már a szabályaidban", "Hozzáadás ehhez: {0}", "Amit most letiltasz, a szünet végén lép életbe." },
        ["el"] = new[] { "Ήδη στους κανόνες σας", "Προσθήκη στο {0}", "Ό,τι μπλοκάρετε τώρα ισχύει μόλις τελειώσει η παύση." },
        ["bg"] = new[] { "Вече в правилата ви", "Добавяне към {0}", "Каквото блокирате сега, започва след края на паузата." },
        ["lt"] = new[] { "Jau jūsų taisyklėse", "Pridėti prie {0}", "Tai, ką dabar užblokuosite, įsigalios pasibaigus pauzei." },
        ["lv"] = new[] { "Jau jūsu noteikumos", "Pievienot: {0}", "Tas, ko bloķējat tagad, sāks darboties, kad pauze beigsies." },
        ["et"] = new[] { "Juba sinu reeglites", "Lisa reeglisse {0}", "Mida praegu blokeerid, hakkab kehtima pausi lõppedes." },
        ["ga"] = new[] { "I do rialacha cheana", "Cuir le {0}", "An rud a bhlocálann tú anois, tosóidh sé nuair a chríochnóidh an sos." },
        ["mt"] = new[] { "Diġà fir-regoli tiegħek", "Żid ma' {0}", "Dak li timblokka issa jibda meta tispiċċa l-pawża." },
        ["ca"] = new[] { "Ja és a les teves regles", "Afegeix a {0}", "El que bloquegis ara començarà quan acabi la pausa." },
        ["nb"] = new[] { "Allerede i reglene dine", "Legg til i {0}", "Det du blokkerer nå, gjelder når pausen er over." },
        ["tr"] = new[] { "Zaten kurallarında", "{0} kuralına ekle", "Şimdi engellediğin şey duraklama bitince başlar." },
        ["ru"] = new[] { "Уже в ваших правилах", "Добавить в «{0}»", "То, что вы заблокируете сейчас, начнёт действовать после паузы." },
        ["uk"] = new[] { "Уже у ваших правилах", "Додати до «{0}»", "Те, що ви заблокуєте зараз, почне діяти після паузи." },
        ["bs"] = new[] { "Već u vašim pravilima", "Dodaj u {0}", "Ono što sada blokirate počinje važiti kad pauza završi." },
        ["sr"] = new[] { "Већ у вашим правилима", "Додај у {0}", "Оно што сада блокирате почиње да важи кад пауза заврши." },
    };

    private static void MergeCoverage(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in Coverage)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                table["in_your_rules"] = text[0];
                table["add_to_rule"] = text[1];
                table["starts_after_pause"] = text[2];
            }
        }
    }
}
