namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for choosing how long (strict mode and pause), the pause limit, and
/// the okay sites (counted together, shown on request).
/// Merged into the tables by <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    private static readonly string[] DurationKeys =
    {
        "len_30m", "show_everything", "okay_sites", "over_hours", "pause_until_btn", "lock_until_btn",
        "pause_max_note", "strict_ends_pause", "this_site_okay", "okay_now", "okay_help",
    };

    // Per language, in the order of DurationKeys.
    private static readonly Dictionary<string, string[]> Durations = new()
    {
        ["en"] = new[]
        {
            "30 min", "Show everything", "Okay sites and apps", "over {0} h", "Pause until {0}", "Lock until {0}",
            "A pause lasts at most {0} hours.", "Turning strict mode on ends the pause.", "This site is okay",
            "{0} is okay now. Its time is counted with your other okay sites and apps.", "Their time is counted together, not one by one.",
        },
        ["de"] = new[]
        {
            "30 Min.", "Alles anzeigen", "Unbedenkliche Websites und Apps", "über {0} Std.", "Pausieren bis {0}", "Sperren bis {0}",
            "Eine Pause dauert höchstens {0} Stunden.", "Der strikte Modus beendet die Pause.", "Diese Website ist unbedenklich",
            "{0} ist jetzt unbedenklich. Die Zeit zählt zusammen mit deinen anderen unbedenklichen Websites und Apps.", "Ihre Zeit wird zusammen gezählt, nicht einzeln.",
        },
        ["fr"] = new[]
        {
            "30 min", "Tout afficher", "Sites et applis acceptables", "plus de {0} h", "Pause jusqu'à {0}", "Verrouiller jusqu'à {0}",
            "Une pause dure au plus {0} heures.", "Activer le mode strict met fin à la pause.", "Ce site est acceptable",
            "{0} est maintenant acceptable. Son temps est compté avec vos autres sites et applis acceptables.", "Leur temps est compté ensemble, pas un par un.",
        },
        ["es"] = new[]
        {
            "30 min", "Mostrar todo", "Sitios y apps aceptables", "más de {0} h", "Pausar hasta las {0}", "Bloquear hasta las {0}",
            "Una pausa dura como máximo {0} horas.", "Activar el modo estricto termina la pausa.", "Este sitio es aceptable",
            "{0} ya es aceptable. Su tiempo se cuenta junto con tus otros sitios y apps aceptables.", "Su tiempo se cuenta junto, no uno por uno.",
        },
        ["it"] = new[]
        {
            "30 min", "Mostra tutto", "Siti e app accettabili", "oltre {0} h", "Pausa fino alle {0}", "Blocca fino alle {0}",
            "Una pausa dura al massimo {0} ore.", "Attivare la modalità rigida termina la pausa.", "Questo sito è accettabile",
            "{0} ora è accettabile. Il suo tempo si somma a quello degli altri siti e app accettabili.", "Il loro tempo è contato insieme, non uno per uno.",
        },
        ["pt"] = new[]
        {
            "30 min", "Mostrar tudo", "Sites e apps aceitáveis", "mais de {0} h", "Pausar até às {0}", "Trancar até às {0}",
            "Uma pausa dura no máximo {0} horas.", "Ligar o modo estrito termina a pausa.", "Este site é aceitável",
            "{0} agora é aceitável. O tempo conta junto com os seus outros sites e apps aceitáveis.", "O tempo deles é contado em conjunto, não um a um.",
        },
        ["nl"] = new[]
        {
            "30 min", "Alles tonen", "Oké-sites en -apps", "meer dan {0} u", "Pauzeren tot {0}", "Vergrendelen tot {0}",
            "Een pauze duurt hoogstens {0} uur.", "Strikte modus aanzetten beëindigt de pauze.", "Deze site is oké",
            "{0} is nu oké. De tijd telt mee met je andere oké-sites en -apps.", "Hun tijd wordt samen geteld, niet apart.",
        },
        ["pl"] = new[]
        {
            "30 min", "Pokaż wszystko", "Strony i aplikacje w porządku", "ponad {0} godz.", "Wstrzymaj do {0}", "Zablokuj do {0}",
            "Pauza trwa najwyżej {0} godzin.", "Włączenie trybu ścisłego kończy pauzę.", "Ta strona jest w porządku",
            "{0} jest teraz w porządku. Czas liczy się razem z innymi stronami i aplikacjami w porządku.", "Ich czas jest liczony razem, a nie osobno.",
        },
        ["sv"] = new[]
        {
            "30 min", "Visa allt", "Okej-sajter och -appar", "över {0} h", "Pausa till {0}", "Lås till {0}",
            "En paus varar högst {0} timmar.", "Att slå på strikt läge avslutar pausen.", "Den här sajten är okej",
            "{0} är okej nu. Tiden räknas ihop med dina andra okej-sajter och -appar.", "Deras tid räknas ihop, inte var för sig.",
        },
        ["da"] = new[]
        {
            "30 min", "Vis alt", "Okay-sider og -apps", "over {0} t", "Pause til {0}", "Lås til {0}",
            "En pause varer højst {0} timer.", "At slå streng tilstand til afslutter pausen.", "Denne side er okay",
            "{0} er okay nu. Tiden tælles sammen med dine andre okay-sider og -apps.", "Deres tid tælles samlet, ikke hver for sig.",
        },
        ["fi"] = new[]
        {
            "30 min", "Näytä kaikki", "Hyväksytyt sivustot ja sovellukset", "yli {0} t", "Tauko klo {0} asti", "Lukitse klo {0} asti",
            "Tauko kestää enintään {0} tuntia.", "Tiukan tilan käynnistäminen päättää tauon.", "Tämä sivusto on hyväksytty",
            "{0} on nyt hyväksytty. Sen aika lasketaan yhteen muiden hyväksyttyjen sivustojen ja sovellusten kanssa.", "Niiden aika lasketaan yhteen, ei erikseen.",
        },
        ["cs"] = new[]
        {
            "30 min", "Zobrazit vše", "Weby a aplikace v pořádku", "přes {0} h", "Pozastavit do {0}", "Zamknout do {0}",
            "Pauza trvá nejvýše {0} hodin.", "Zapnutí přísného režimu pauzu ukončí.", "Tento web je v pořádku",
            "{0} je teď v pořádku. Čas se počítá spolu s ostatními weby a aplikacemi v pořádku.", "Jejich čas se počítá dohromady, ne zvlášť.",
        },
        ["sk"] = new[]
        {
            "30 min", "Zobraziť všetko", "Stránky a aplikácie v poriadku", "viac ako {0} h", "Pozastaviť do {0}", "Zamknúť do {0}",
            "Pauza trvá najviac {0} hodín.", "Zapnutie prísneho režimu pauzu ukončí.", "Táto stránka je v poriadku",
            "{0} je teraz v poriadku. Čas sa počíta spolu s ostatnými stránkami a aplikáciami v poriadku.", "Ich čas sa počíta spolu, nie zvlášť.",
        },
        ["sl"] = new[]
        {
            "30 min", "Pokaži vse", "Spletna mesta in aplikacije v redu", "več kot {0} h", "Premor do {0}", "Zakleni do {0}",
            "Premor traja največ {0} ur.", "Vklop strogega načina konča premor.", "To spletno mesto je v redu",
            "{0} je zdaj v redu. Čas se šteje skupaj z drugimi spletnimi mesti in aplikacijami v redu.", "Njihov čas se šteje skupaj, ne posamezno.",
        },
        ["hr"] = new[]
        {
            "30 min", "Prikaži sve", "Stranice i aplikacije u redu", "više od {0} h", "Pauziraj do {0}", "Zaključaj do {0}",
            "Pauza traje najviše {0} sati.", "Uključivanje strogog načina završava pauzu.", "Ova stranica je u redu",
            "{0} je sada u redu. Vrijeme se broji zajedno s ostalim stranicama i aplikacijama u redu.", "Njihovo se vrijeme broji zajedno, ne zasebno.",
        },
        ["ro"] = new[]
        {
            "30 min", "Arată tot", "Site-uri și aplicații în regulă", "peste {0} h", "Pauză până la {0}", "Blochează până la {0}",
            "O pauză durează cel mult {0} ore.", "Pornirea modului strict încheie pauza.", "Site-ul acesta e în regulă",
            "{0} e acum în regulă. Timpul se adună cu celelalte site-uri și aplicații în regulă.", "Timpul lor se numără împreună, nu separat.",
        },
        ["hu"] = new[]
        {
            "30 perc", "Minden mutatása", "Rendben lévő oldalak és alkalmazások", "több mint {0} óra", "Szünet eddig: {0}", "Zárolás eddig: {0}",
            "Egy szünet legfeljebb {0} óráig tart.", "A szigorú mód bekapcsolása véget vet a szünetnek.", "Ez az oldal rendben van",
            "{0} mostantól rendben van. Az ideje a többi rendben lévő oldallal és alkalmazással együtt számít.", "Az idejüket együtt számoljuk, nem egyenként.",
        },
        ["el"] = new[]
        {
            "30 λεπ.", "Εμφάνιση όλων", "Αποδεκτοί ιστότοποι και εφαρμογές", "πάνω από {0} ώ.", "Παύση έως {0}", "Κλείδωμα έως {0}",
            "Μια παύση διαρκεί το πολύ {0} ώρες.", "Η ενεργοποίηση της αυστηρής λειτουργίας τερματίζει την παύση.", "Αυτός ο ιστότοπος είναι αποδεκτός",
            "{0}: πλέον αποδεκτό. Ο χρόνος μετράει μαζί με τους άλλους αποδεκτούς ιστότοπους και εφαρμογές.", "Ο χρόνος τους μετράει συνολικά, όχι χωριστά.",
        },
        ["bg"] = new[]
        {
            "30 мин", "Покажи всичко", "Приемливи сайтове и приложения", "над {0} ч", "Пауза до {0}", "Заключи до {0}",
            "Паузата трае най-много {0} часа.", "Включването на строгия режим прекратява паузата.", "Този сайт е приемлив",
            "{0} вече е приемлив. Времето се брои заедно с другите приемливи сайтове и приложения.", "Времето им се брои общо, а не поотделно.",
        },
        ["et"] = new[]
        {
            "30 min", "Näita kõike", "Sobivad saidid ja rakendused", "üle {0} h", "Paus kuni {0}", "Lukusta kuni {0}",
            "Paus kestab kõige rohkem {0} tundi.", "Range režiimi sisselülitamine lõpetab pausi.", "See sait sobib",
            "{0} sobib nüüd. Selle aeg loetakse kokku teiste sobivate saitide ja rakendustega.", "Nende aeg loetakse kokku, mitte eraldi.",
        },
        ["ga"] = new[]
        {
            "30 nóim.", "Taispeáin gach rud", "Suíomhanna agus aipeanna atá ceart go leor", "os cionn {0} u", "Sos go dtí {0}", "Glasáil go dtí {0}",
            "Maireann sos {0} uair an chloig ar a mhéad.", "Cuireann an mód docht deireadh leis an sos.", "Tá an suíomh seo ceart go leor",
            "Tá {0} ceart go leor anois. Áirítear a chuid ama le do shuíomhanna agus d'aipeanna eile atá ceart go leor.", "Áirítear a gcuid ama le chéile, ní ceann ar cheann.",
        },
        ["lv"] = new[]
        {
            "30 min", "Rādīt visu", "Pieņemamās vietnes un lietotnes", "vairāk par {0} h", "Pauze līdz {0}", "Bloķēt līdz {0}",
            "Pauze ilgst ne vairāk kā {0} stundas.", "Stingrā režīma ieslēgšana beidz pauzi.", "Šī vietne ir pieņemama",
            "{0} tagad ir pieņemama. Laiks tiek skaitīts kopā ar citām pieņemamajām vietnēm un lietotnēm.", "To laiks tiek skaitīts kopā, nevis atsevišķi.",
        },
        ["lt"] = new[]
        {
            "30 min", "Rodyti viską", "Tinkamos svetainės ir programos", "daugiau nei {0} val.", "Pristabdyti iki {0}", "Užrakinti iki {0}",
            "Pauzė trunka ne ilgiau kaip {0} valandų.", "Įjungus griežtąjį režimą pauzė baigiasi.", "Ši svetainė tinkama",
            "{0} dabar tinkama. Laikas skaičiuojamas kartu su kitomis tinkamomis svetainėmis ir programomis.", "Jų laikas skaičiuojamas kartu, o ne atskirai.",
        },
        ["mt"] = new[]
        {
            "30 min", "Uri kollox", "Siti u apps aċċettabbli", "aktar minn {0} siegħa", "Waqfa sa {0}", "Sakkar sa {0}",
            "Waqfa ddum l-aktar {0} sigħat.", "Meta tixgħel il-modalità stretta, il-waqfa tieqaf.", "Dan is-sit huwa aċċettabbli",
            "{0} issa huwa aċċettabbli. Il-ħin jingħadd flimkien mas-siti u l-apps aċċettabbli l-oħra.", "Il-ħin tagħhom jingħadd flimkien, mhux wieħed wieħed.",
        },
        ["ca"] = new[]
        {
            "30 min", "Mostra-ho tot", "Llocs i aplicacions acceptables", "més de {0} h", "Pausa fins a les {0}", "Bloqueja fins a les {0}",
            "Una pausa dura com a màxim {0} hores.", "Activar el mode estricte acaba la pausa.", "Aquest lloc és acceptable",
            "{0} ara és acceptable. El seu temps es compta amb els altres llocs i aplicacions acceptables.", "El seu temps es compta junt, no un per un.",
        },
        ["nb"] = new[]
        {
            "30 min", "Vis alt", "Godkjente nettsteder og apper", "over {0} t", "Pause til {0}", "Lås til {0}",
            "En pause varer høyst {0} timer.", "Å slå på streng modus avslutter pausen.", "Dette nettstedet er godkjent",
            "{0} er godkjent nå. Tiden telles sammen med de andre godkjente nettstedene og appene.", "Tiden deres telles samlet, ikke hver for seg.",
        },
        ["ru"] = new[]
        {
            "30 мин", "Показать всё", "Допустимые сайты и приложения", "больше {0} ч", "Пауза до {0}", "Заблокировать до {0}",
            "Пауза длится не больше {0} часов.", "Включение строгого режима завершает паузу.", "Этот сайт допустим",
            "{0} теперь допустим. Его время считается вместе с другими допустимыми сайтами и приложениями.", "Их время считается вместе, а не по отдельности.",
        },
        ["sr"] = new[]
        {
            "30 мин", "Прикажи све", "Прихватљиви сајтови и апликације", "више од {0} ч", "Пауза до {0}", "Закључај до {0}",
            "Пауза траје највише {0} сати.", "Укључивање строгог режима завршава паузу.", "Овај сајт је прихватљив",
            "{0} је сада прихватљив. Време се рачуна заједно са осталим прихватљивим сајтовима и апликацијама.", "Њихово време се рачуна заједно, не засебно.",
        },
        ["bs"] = new[]
        {
            "30 min", "Prikaži sve", "Prihvatljive stranice i aplikacije", "više od {0} h", "Pauza do {0}", "Zaključaj do {0}",
            "Pauza traje najviše {0} sati.", "Uključivanje strogog načina završava pauzu.", "Ova stranica je prihvatljiva",
            "{0} je sada prihvatljiv. Vrijeme se računa zajedno s ostalim prihvatljivim stranicama i aplikacijama.", "Njihovo vrijeme se računa zajedno, ne zasebno.",
        },
        ["tr"] = new[]
        {
            "30 dk", "Her şeyi göster", "Uygun siteler ve uygulamalar", "{0} saatten fazla", "Duraklat: {0} saatine kadar", "Kilitle: {0} saatine kadar",
            "Duraklatma en fazla {0} saat sürer.", "Katı modu açmak duraklatmayı bitirir.", "Bu site uygun",
            "{0} artık uygun. Süresi diğer uygun siteler ve uygulamalarla birlikte sayılır.", "Süreleri tek tek değil, birlikte sayılır.",
        },
        ["uk"] = new[]
        {
            "30 хв", "Показати все", "Прийнятні сайти й застосунки", "понад {0} год", "Пауза до {0}", "Заблокувати до {0}",
            "Пауза триває не більше {0} годин.", "Увімкнення суворого режиму завершує паузу.", "Цей сайт прийнятний",
            "{0} тепер прийнятний. Його час рахується разом з іншими прийнятними сайтами й застосунками.", "Їхній час рахується разом, а не окремо.",
        },
    };

    private static void MergeDurations(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in Durations)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                for (int i = 0; i < DurationKeys.Length; i++)
                {
                    table[DurationKeys[i]] = text[i];
                }
            }
        }
    }
}
