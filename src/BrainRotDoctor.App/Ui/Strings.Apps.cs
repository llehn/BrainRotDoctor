namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for apps and games as rule targets: adding them to a rule, their sheet on
/// the status view, and the heads-up before one is closed. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    private static readonly string[] AppKeys =
    {
        "websites", "apps_games", "search_apps", "all_steam_games", "all_steam_games_caption", "steam_caption",
        "steam_game", "program", "programs", "open_now", "no_programs", "other_program",
        "other_program_help", "choose_program", "cant_limit_program", "show_all_n", "covered_by_all", "add_n",
        "block_at_night", "block_this_game", "block_this_app", "block_all_steam", "whole_app", "this_app_okay",
        "closes_in", "apps_close_in", "save_now",
    };

    // Per language, in the order of AppKeys.
    private static readonly Dictionary<string, string[]> Apps = new()
    {
        ["en"] = new[]
        {
            "Websites", "Apps & games", "Search apps and games", "All Steam games", "Also games installed later", "Store, library and community",
            "Steam game", "Program", "Programs", "Open now", "Nothing open now. Start it, then come back, or choose its .exe below.", "Any other program",
            "Not listed? Start it once, or choose its .exe file.", "Choose a program…", "This program can't be limited on its own.", "Show all {0}", "Covered by All Steam games", "Add {0}",
            "At night ({0}–{1})", "Block this game", "Block this app", "Block all Steam games", "Whole app", "This app is okay",
            "{0} closes in {1}", "Several apps close in {0}", "Save your progress now.",
        },
        ["de"] = new[]
        {
            "Websites", "Apps & Spiele", "Apps und Spiele suchen", "Alle Steam-Spiele", "Auch später installierte Spiele", "Shop, Bibliothek und Community",
            "Steam-Spiel", "Programm", "Programme", "Gerade offen", "Gerade ist nichts offen. Starte es und komm zurück, oder wähle unten seine .exe.", "Ein anderes Programm",
            "Nicht dabei? Starte es einmal, oder wähle seine .exe-Datei.", "Programm wählen…", "Dieses Programm lässt sich nicht einzeln begrenzen.", "Alle {0} anzeigen", "Durch „Alle Steam-Spiele“ abgedeckt", "{0} hinzufügen",
            "Nachts ({0}–{1})", "Dieses Spiel blockieren", "Diese App blockieren", "Alle Steam-Spiele blockieren", "Ganze App", "Diese App ist unbedenklich",
            "{0} schließt in {1}", "Mehrere Apps schließen in {0}", "Speichere jetzt deinen Fortschritt.",
        },
        ["fr"] = new[]
        {
            "Sites", "Applis et jeux", "Rechercher des applis et des jeux", "Tous les jeux Steam", "Aussi les jeux installés plus tard", "Magasin, bibliothèque et communauté",
            "Jeu Steam", "Programme", "Programmes", "Ouvert", "Rien d'ouvert. Lancez-le puis revenez, ou choisissez son .exe ci-dessous.", "Un autre programme",
            "Absent de la liste ? Lancez-le une fois, ou choisissez son fichier .exe.", "Choisir un programme…", "Ce programme ne peut pas être limité seul.", "Tout afficher ({0})", "Couvert par Tous les jeux Steam", "Ajouter {0}",
            "La nuit ({0}–{1})", "Bloquer ce jeu", "Bloquer cette appli", "Bloquer tous les jeux Steam", "Appli entière", "Cette appli est acceptable",
            "{0} se ferme dans {1}", "Plusieurs applis se ferment dans {0}", "Sauvegardez votre progression maintenant.",
        },
        ["es"] = new[]
        {
            "Sitios", "Apps y juegos", "Buscar apps y juegos", "Todos los juegos de Steam", "También los que instales después", "Tienda, biblioteca y comunidad",
            "Juego de Steam", "Programa", "Programas", "Abierto ahora", "No hay nada abierto. Ábrelo y vuelve, o elige su .exe abajo.", "Otro programa",
            "¿No aparece? Ábrelo una vez o elige su archivo .exe.", "Elegir un programa…", "Este programa no se puede limitar por separado.", "Mostrar los {0}", "Incluido en Todos los juegos de Steam", "Añadir {0}",
            "Por la noche ({0}–{1})", "Bloquear este juego", "Bloquear esta app", "Bloquear todos los juegos de Steam", "App entera", "Esta app es aceptable",
            "{0} se cierra en {1}", "Varias apps se cierran en {0}", "Guarda tu progreso ahora.",
        },
        ["it"] = new[]
        {
            "Siti", "App e giochi", "Cerca app e giochi", "Tutti i giochi Steam", "Anche quelli installati in seguito", "Negozio, libreria e comunità",
            "Gioco Steam", "Programma", "Programmi", "Aperto ora", "Niente di aperto. Avvialo e torna qui, o scegli il suo .exe qui sotto.", "Un altro programma",
            "Non c'è? Avvialo una volta o scegli il suo file .exe.", "Scegli un programma…", "Questo programma non si può limitare da solo.", "Mostra tutti ({0})", "Incluso in Tutti i giochi Steam", "Aggiungi {0}",
            "Di notte ({0}–{1})", "Blocca questo gioco", "Blocca questa app", "Blocca tutti i giochi Steam", "App intera", "Questa app è accettabile",
            "{0} si chiude tra {1}", "Più app si chiudono tra {0}", "Salva i tuoi progressi ora.",
        },
        ["pt"] = new[]
        {
            "Sites", "Apps e jogos", "Pesquisar apps e jogos", "Todos os jogos Steam", "Também os instalados depois", "Loja, biblioteca e comunidade",
            "Jogo Steam", "Programa", "Programas", "Aberto agora", "Nada aberto. Abra-o e volte, ou escolha o .exe abaixo.", "Outro programa",
            "Não aparece? Abra-o uma vez ou escolha o ficheiro .exe.", "Escolher um programa…", "Este programa não pode ser limitado sozinho.", "Mostrar todos ({0})", "Incluído em Todos os jogos Steam", "Adicionar {0}",
            "À noite ({0}–{1})", "Bloquear este jogo", "Bloquear esta app", "Bloquear todos os jogos Steam", "App inteira", "Esta app é aceitável",
            "{0} fecha dentro de {1}", "Várias apps fecham dentro de {0}", "Guarde o seu progresso agora.",
        },
        ["nl"] = new[]
        {
            "Websites", "Apps en games", "Apps en games zoeken", "Alle Steam-games", "Ook games die je later installeert", "Winkel, bibliotheek en community",
            "Steam-game", "Programma", "Programma's", "Nu open", "Er is niets open. Start het en kom terug, of kies hieronder de .exe.", "Een ander programma",
            "Staat het er niet bij? Start het een keer, of kies het .exe-bestand.", "Programma kiezen…", "Dit programma kan niet apart worden beperkt.", "Alle {0} tonen", "Valt onder Alle Steam-games", "{0} toevoegen",
            "'s Nachts ({0}–{1})", "Deze game blokkeren", "Deze app blokkeren", "Alle Steam-games blokkeren", "Hele app", "Deze app is oké",
            "{0} sluit over {1}", "Meerdere apps sluiten over {0}", "Sla je voortgang nu op.",
        },
        ["pl"] = new[]
        {
            "Strony", "Aplikacje i gry", "Szukaj aplikacji i gier", "Wszystkie gry Steam", "Także gry zainstalowane później", "Sklep, biblioteka i społeczność",
            "Gra Steam", "Program", "Programy", "Teraz otwarte", "Nic nie jest otwarte. Uruchom go i wróć albo wybierz jego plik .exe poniżej.", "Inny program",
            "Nie ma go na liście? Uruchom go raz albo wybierz jego plik .exe.", "Wybierz program…", "Tego programu nie da się ograniczyć osobno.", "Pokaż wszystkie ({0})", "Objęte przez Wszystkie gry Steam", "Dodaj {0}",
            "W nocy ({0}–{1})", "Zablokuj tę grę", "Zablokuj tę aplikację", "Zablokuj wszystkie gry Steam", "Cała aplikacja", "Ta aplikacja jest w porządku",
            "{0} zamknie się za {1}", "Kilka aplikacji zamknie się za {0}", "Zapisz teraz swoje postępy.",
        },
        ["sv"] = new[]
        {
            "Webbplatser", "Appar och spel", "Sök appar och spel", "Alla Steam-spel", "Även spel som installeras senare", "Butik, bibliotek och community",
            "Steam-spel", "Program", "Program", "Öppet nu", "Inget är öppet. Starta det och kom tillbaka, eller välj dess .exe nedan.", "Ett annat program",
            "Saknas det? Starta det en gång, eller välj dess .exe-fil.", "Välj ett program…", "Det här programmet kan inte begränsas för sig.", "Visa alla {0}", "Ingår i Alla Steam-spel", "Lägg till {0}",
            "På natten ({0}–{1})", "Blockera det här spelet", "Blockera den här appen", "Blockera alla Steam-spel", "Hela appen", "Den här appen är okej",
            "{0} stängs om {1}", "Flera appar stängs om {0}", "Spara dina framsteg nu.",
        },
        ["da"] = new[]
        {
            "Websites", "Apps og spil", "Søg efter apps og spil", "Alle Steam-spil", "Også spil, der installeres senere", "Butik, bibliotek og fællesskab",
            "Steam-spil", "Program", "Programmer", "Åben nu", "Intet er åbent. Start det, og kom tilbage, eller vælg dets .exe nedenfor.", "Et andet program",
            "Er det ikke på listen? Start det én gang, eller vælg dets .exe-fil.", "Vælg et program…", "Dette program kan ikke begrænses for sig selv.", "Vis alle {0}", "Dækket af Alle Steam-spil", "Tilføj {0}",
            "Om natten ({0}–{1})", "Bloker dette spil", "Bloker denne app", "Bloker alle Steam-spil", "Hele appen", "Denne app er okay",
            "{0} lukker om {1}", "Flere apps lukker om {0}", "Gem dine fremskridt nu.",
        },
        ["fi"] = new[]
        {
            "Sivustot", "Sovellukset ja pelit", "Hae sovelluksia ja pelejä", "Kaikki Steam-pelit", "Myös myöhemmin asennetut pelit", "Kauppa, kirjasto ja yhteisö",
            "Steam-peli", "Ohjelma", "Ohjelmat", "Auki nyt", "Mitään ei ole auki. Käynnistä se ja palaa, tai valitse sen .exe alta.", "Muu ohjelma",
            "Eikö listalla? Käynnistä se kerran tai valitse sen .exe-tiedosto.", "Valitse ohjelma…", "Tätä ohjelmaa ei voi rajoittaa erikseen.", "Näytä kaikki {0}", "Kuuluu Kaikkiin Steam-peleihin", "Lisää {0}",
            "Yöllä ({0}–{1})", "Estä tämä peli", "Estä tämä sovellus", "Estä kaikki Steam-pelit", "Koko sovellus", "Tämä sovellus on hyväksytty",
            "{0} sulkeutuu {1} kuluttua", "Useita sovelluksia sulkeutuu {0} kuluttua", "Tallenna edistymisesi nyt.",
        },
        ["cs"] = new[]
        {
            "Weby", "Aplikace a hry", "Hledat aplikace a hry", "Všechny hry ze Steamu", "I hry nainstalované později", "Obchod, knihovna a komunita",
            "Hra ze Steamu", "Program", "Programy", "Teď otevřené", "Nic není otevřené. Spusťte ho a vraťte se, nebo níže vyberte jeho .exe.", "Jiný program",
            "Není v seznamu? Jednou ho spusťte, nebo vyberte jeho soubor .exe.", "Vybrat program…", "Tento program nelze omezit samostatně.", "Zobrazit všechny ({0})", "Zahrnuto ve Všech hrách ze Steamu", "Přidat {0}",
            "V noci ({0}–{1})", "Blokovat tuto hru", "Blokovat tuto aplikaci", "Blokovat všechny hry ze Steamu", "Celá aplikace", "Tato aplikace je v pořádku",
            "{0} se zavře za {1}", "Několik aplikací se zavře za {0}", "Uložte teď svůj postup.",
        },
        ["sk"] = new[]
        {
            "Weby", "Aplikácie a hry", "Hľadať aplikácie a hry", "Všetky hry zo Steamu", "Aj hry nainštalované neskôr", "Obchod, knižnica a komunita",
            "Hra zo Steamu", "Program", "Programy", "Teraz otvorené", "Nič nie je otvorené. Spustite ho a vráťte sa, alebo nižšie vyberte jeho .exe.", "Iný program",
            "Nie je v zozname? Raz ho spustite alebo vyberte jeho súbor .exe.", "Vybrať program…", "Tento program sa nedá obmedziť samostatne.", "Zobraziť všetky ({0})", "Zahrnuté vo Všetkých hrách zo Steamu", "Pridať {0}",
            "V noci ({0}–{1})", "Blokovať túto hru", "Blokovať túto aplikáciu", "Blokovať všetky hry zo Steamu", "Celá aplikácia", "Táto aplikácia je v poriadku",
            "{0} sa zatvorí o {1}", "Niekoľko aplikácií sa zatvorí o {0}", "Uložte teraz svoj postup.",
        },
        ["sl"] = new[]
        {
            "Spletna mesta", "Aplikacije in igre", "Iskanje aplikacij in iger", "Vse igre s Steama", "Tudi igre, nameščene pozneje", "Trgovina, knjižnica in skupnost",
            "Igra s Steama", "Program", "Programi", "Zdaj odprto", "Nič ni odprto. Zaženite ga in se vrnite ali spodaj izberite njegov .exe.", "Drug program",
            "Ga ni na seznamu? Enkrat ga zaženite ali izberite njegovo datoteko .exe.", "Izberi program…", "Tega programa ni mogoče omejiti posebej.", "Pokaži vse ({0})", "Zajeto v Vse igre s Steama", "Dodaj {0}",
            "Ponoči ({0}–{1})", "Blokiraj to igro", "Blokiraj to aplikacijo", "Blokiraj vse igre s Steama", "Celotna aplikacija", "Ta aplikacija je v redu",
            "{0} se zapre čez {1}", "Več aplikacij se zapre čez {0}", "Zdaj shranite svoj napredek.",
        },
        ["hr"] = new[]
        {
            "Web-stranice", "Aplikacije i igre", "Traži aplikacije i igre", "Sve Steam igre", "I igre instalirane kasnije", "Trgovina, biblioteka i zajednica",
            "Steam igra", "Program", "Programi", "Sada otvoreno", "Ništa nije otvoreno. Pokrenite ga i vratite se ili ispod odaberite njegov .exe.", "Drugi program",
            "Nije na popisu? Pokrenite ga jednom ili odaberite njegovu .exe datoteku.", "Odaberi program…", "Ovaj se program ne može ograničiti zasebno.", "Prikaži sve ({0})", "Obuhvaćeno sa Sve Steam igre", "Dodaj {0}",
            "Noću ({0}–{1})", "Blokiraj ovu igru", "Blokiraj ovu aplikaciju", "Blokiraj sve Steam igre", "Cijela aplikacija", "Ova aplikacija je u redu",
            "{0} se zatvara za {1}", "Više aplikacija zatvara se za {0}", "Spremite napredak sada.",
        },
        ["ro"] = new[]
        {
            "Site-uri", "Aplicații și jocuri", "Caută aplicații și jocuri", "Toate jocurile Steam", "Și jocurile instalate mai târziu", "Magazin, bibliotecă și comunitate",
            "Joc Steam", "Program", "Programe", "Deschis acum", "Nimic deschis. Pornește-l și revino sau alege mai jos fișierul .exe.", "Alt program",
            "Nu e în listă? Pornește-l o dată sau alege fișierul .exe.", "Alege un program…", "Acest program nu poate fi limitat separat.", "Arată toate ({0})", "Inclus în Toate jocurile Steam", "Adaugă {0}",
            "Noaptea ({0}–{1})", "Blochează acest joc", "Blochează această aplicație", "Blochează toate jocurile Steam", "Toată aplicația", "Aplicația aceasta e în regulă",
            "{0} se închide în {1}", "Mai multe aplicații se închid în {0}", "Salvează-ți progresul acum.",
        },
        ["hu"] = new[]
        {
            "Oldalak", "Alkalmazások és játékok", "Alkalmazások és játékok keresése", "Minden Steam-játék", "A később telepített játékok is", "Áruház, könyvtár és közösség",
            "Steam-játék", "Program", "Programok", "Most nyitva", "Semmi sincs nyitva. Indítsd el, és gyere vissza, vagy válaszd ki lent a .exe fájlját.", "Más program",
            "Nincs a listán? Indítsd el egyszer, vagy válaszd ki a .exe fájlját.", "Program kiválasztása…", "Ez a program önmagában nem korlátozható.", "Mind a(z) {0} mutatása", "A Minden Steam-játék lefedi", "{0} hozzáadása",
            "Éjjel ({0}–{1})", "Játék tiltása", "Alkalmazás tiltása", "Minden Steam-játék tiltása", "Teljes alkalmazás", "Ez az alkalmazás rendben van",
            "{0} bezárul: {1}", "Több alkalmazás bezárul: {0}", "Mentsd most a haladásod.",
        },
        ["el"] = new[]
        {
            "Ιστότοποι", "Εφαρμογές και παιχνίδια", "Αναζήτηση εφαρμογών και παιχνιδιών", "Όλα τα παιχνίδια Steam", "Και όσα εγκατασταθούν αργότερα", "Κατάστημα, βιβλιοθήκη και κοινότητα",
            "Παιχνίδι Steam", "Πρόγραμμα", "Προγράμματα", "Ανοιχτό τώρα", "Τίποτα ανοιχτό. Ανοίξτε το και επιστρέψτε, ή επιλέξτε το .exe του παρακάτω.", "Άλλο πρόγραμμα",
            "Δεν υπάρχει στη λίστα; Ανοίξτε το μία φορά ή επιλέξτε το αρχείο .exe.", "Επιλογή προγράμματος…", "Αυτό το πρόγραμμα δεν μπορεί να περιοριστεί μόνο του.", "Εμφάνιση όλων ({0})", "Καλύπτεται από τα Όλα τα παιχνίδια Steam", "Προσθήκη {0}",
            "Τη νύχτα ({0}–{1})", "Αποκλεισμός παιχνιδιού", "Αποκλεισμός εφαρμογής", "Αποκλεισμός όλων των παιχνιδιών Steam", "Ολόκληρη η εφαρμογή", "Αυτή η εφαρμογή είναι αποδεκτή",
            "Το {0} κλείνει σε {1}", "Πολλές εφαρμογές κλείνουν σε {0}", "Αποθηκεύστε την πρόοδό σας τώρα.",
        },
        ["bg"] = new[]
        {
            "Сайтове", "Приложения и игри", "Търсене на приложения и игри", "Всички игри в Steam", "И инсталираните по-късно", "Магазин, библиотека и общност",
            "Игра в Steam", "Програма", "Програми", "Отворено сега", "Нищо не е отворено. Стартирайте го и се върнете или изберете .exe файла му по-долу.", "Друга програма",
            "Не е в списъка? Стартирайте го веднъж или изберете .exe файла му.", "Избор на програма…", "Тази програма не може да се ограничи сама.", "Покажи всички ({0})", "Обхванато от Всички игри в Steam", "Добави {0}",
            "През нощта ({0}–{1})", "Блокирай тази игра", "Блокирай това приложение", "Блокирай всички игри в Steam", "Цялото приложение", "Това приложение е приемливо",
            "{0} се затваря след {1}", "Няколко приложения се затварят след {0}", "Запазете напредъка си сега.",
        },
        ["lt"] = new[]
        {
            "Svetainės", "Programos ir žaidimai", "Ieškoti programų ir žaidimų", "Visi „Steam“ žaidimai", "Ir vėliau įdiegti žaidimai", "Parduotuvė, biblioteka ir bendruomenė",
            "„Steam“ žaidimas", "Programa", "Programos", "Dabar atidaryta", "Nieko neatidaryta. Paleiskite ją ir grįžkite arba žemiau pasirinkite jos .exe.", "Kita programa",
            "Nėra sąraše? Paleiskite ją kartą arba pasirinkite jos .exe failą.", "Pasirinkti programą…", "Šios programos negalima riboti atskirai.", "Rodyti visus ({0})", "Apima „Visi „Steam“ žaidimai“", "Pridėti {0}",
            "Naktį ({0}–{1})", "Blokuoti šį žaidimą", "Blokuoti šią programą", "Blokuoti visus „Steam“ žaidimus", "Visa programa", "Ši programa tinkama",
            "{0} užsidarys po {1}", "Kelios programos užsidarys po {0}", "Išsaugokite savo pažangą dabar.",
        },
        ["lv"] = new[]
        {
            "Vietnes", "Lietotnes un spēles", "Meklēt lietotnes un spēles", "Visas Steam spēles", "Arī vēlāk instalētās spēles", "Veikals, bibliotēka un kopiena",
            "Steam spēle", "Programma", "Programmas", "Tagad atvērts", "Nekas nav atvērts. Palaidiet to un atgriezieties vai zemāk izvēlieties tās .exe.", "Cita programma",
            "Nav sarakstā? Palaidiet to vienreiz vai izvēlieties tās .exe failu.", "Izvēlēties programmu…", "Šo programmu nevar ierobežot atsevišķi.", "Rādīt visas ({0})", "Ietverts sadaļā Visas Steam spēles", "Pievienot {0}",
            "Naktī ({0}–{1})", "Bloķēt šo spēli", "Bloķēt šo lietotni", "Bloķēt visas Steam spēles", "Visa lietotne", "Šī lietotne ir pieņemama",
            "{0} aizvērsies pēc {1}", "Vairākas lietotnes aizvērsies pēc {0}", "Saglabājiet savu progresu tagad.",
        },
        ["et"] = new[]
        {
            "Saidid", "Rakendused ja mängud", "Otsi rakendusi ja mänge", "Kõik Steami mängud", "Ka hiljem paigaldatud mängud", "Pood, kogu ja kogukond",
            "Steami mäng", "Programm", "Programmid", "Praegu avatud", "Midagi pole avatud. Käivita see ja tule tagasi või vali allpool selle .exe.", "Muu programm",
            "Pole loendis? Käivita see korra või vali selle .exe-fail.", "Vali programm…", "Seda programmi ei saa eraldi piirata.", "Näita kõiki ({0})", "Kuulub alla Kõik Steami mängud", "Lisa {0}",
            "Öösel ({0}–{1})", "Blokeeri see mäng", "Blokeeri see rakendus", "Blokeeri kõik Steami mängud", "Kogu rakendus", "See rakendus sobib",
            "{0} suletakse {1} pärast", "Mitu rakendust suletakse {0} pärast", "Salvesta oma edenemine kohe.",
        },
        ["ga"] = new[]
        {
            "Suíomhanna", "Aipeanna agus cluichí", "Cuardaigh aipeanna agus cluichí", "Gach cluiche Steam", "Cluichí a shuiteálfar níos déanaí freisin", "Siopa, leabharlann agus pobal",
            "Cluiche Steam", "Ríomhchlár", "Ríomhchláir", "Oscailte anois", "Níl aon rud oscailte. Tosaigh é agus fill ar ais, nó roghnaigh a .exe thíos.", "Ríomhchlár eile",
            "Níl sé ar an liosta? Tosaigh é uair amháin, nó roghnaigh a chomhad .exe.", "Roghnaigh ríomhchlár…", "Ní féidir an ríomhchlár seo a theorannú leis féin.", "Taispeáin iad uile ({0})", "Clúdaithe ag Gach cluiche Steam", "Cuir {0} leis",
            "San oíche ({0}–{1})", "Blocáil an cluiche seo", "Blocáil an aip seo", "Blocáil gach cluiche Steam", "An aip ar fad", "Tá an aip seo ceart go leor",
            "Dúnfaidh {0} i gceann {1}", "Dúnfaidh roinnt aipeanna i gceann {0}", "Sábháil do dhul chun cinn anois.",
        },
        ["mt"] = new[]
        {
            "Siti", "Apps u logħob", "Fittex apps u logħob", "Il-logħob kollu ta' Steam", "Anki l-logħob installat wara", "Ħanut, librerija u komunità",
            "Logħba ta' Steam", "Programm", "Programmi", "Miftuħ issa", "Xejn miftuħ. Iftħu u erġa' lura, jew agħżel il-.exe tiegħu hawn taħt.", "Programm ieħor",
            "Mhux fil-lista? Iftħu darba, jew agħżel il-fajl .exe tiegħu.", "Agħżel programm…", "Dan il-programm ma jistax jiġi llimitat waħdu.", "Uri kollha ({0})", "Inkluż f'Il-logħob kollu ta' Steam", "Żid {0}",
            "Bil-lejl ({0}–{1})", "Imblokka din il-logħba", "Imblokka din l-app", "Imblokka l-logħob kollu ta' Steam", "L-app kollha", "Din l-app hija aċċettabbli",
            "{0} tingħalaq fi {1}", "Diversi apps jingħalqu fi {0}", "Issejvja l-progress tiegħek issa.",
        },
        ["ca"] = new[]
        {
            "Llocs web", "Aplicacions i jocs", "Cerca aplicacions i jocs", "Tots els jocs de Steam", "També els jocs instal·lats més tard", "Botiga, biblioteca i comunitat",
            "Joc de Steam", "Programa", "Programes", "Obert ara", "No hi ha res obert. Obre'l i torna, o tria el seu .exe a sota.", "Un altre programa",
            "No hi és? Obre'l una vegada o tria el seu fitxer .exe.", "Tria un programa…", "Aquest programa no es pot limitar per separat.", "Mostra'ls tots ({0})", "Inclòs a Tots els jocs de Steam", "Afegeix {0}",
            "A la nit ({0}–{1})", "Bloqueja aquest joc", "Bloqueja aquesta aplicació", "Bloqueja tots els jocs de Steam", "Aplicació sencera", "Aquesta aplicació és acceptable",
            "{0} es tanca d'aquí a {1}", "Diverses aplicacions es tanquen d'aquí a {0}", "Desa el teu progrés ara.",
        },
        ["nb"] = new[]
        {
            "Nettsteder", "Apper og spill", "Søk etter apper og spill", "Alle Steam-spill", "Også spill som installeres senere", "Butikk, bibliotek og fellesskap",
            "Steam-spill", "Program", "Programmer", "Åpent nå", "Ingenting er åpent. Start det og kom tilbake, eller velg .exe-filen nedenfor.", "Et annet program",
            "Står det ikke her? Start det én gang, eller velg .exe-filen.", "Velg et program…", "Dette programmet kan ikke begrenses alene.", "Vis alle {0}", "Dekket av Alle Steam-spill", "Legg til {0}",
            "Om natten ({0}–{1})", "Blokker dette spillet", "Blokker denne appen", "Blokker alle Steam-spill", "Hele appen", "Denne appen er godkjent",
            "{0} lukkes om {1}", "Flere apper lukkes om {0}", "Lagre fremgangen din nå.",
        },
        ["tr"] = new[]
        {
            "Siteler", "Uygulamalar ve oyunlar", "Uygulama ve oyun ara", "Tüm Steam oyunları", "Sonradan kurulan oyunlar da", "Mağaza, kütüphane ve topluluk",
            "Steam oyunu", "Program", "Programlar", "Şu an açık", "Açık bir şey yok. Başlatıp geri gel veya aşağıdan .exe dosyasını seç.", "Başka bir program",
            "Listede yok mu? Bir kez başlat veya .exe dosyasını seç.", "Program seç…", "Bu program tek başına sınırlanamaz.", "Tümünü göster ({0})", "Tüm Steam oyunları kapsamında", "{0} ekle",
            "Geceleri ({0}–{1})", "Bu oyunu engelle", "Bu uygulamayı engelle", "Tüm Steam oyunlarını engelle", "Uygulamanın tamamı", "Bu uygulama uygun",
            "{0} {1} içinde kapanacak", "Birkaç uygulama {0} içinde kapanacak", "İlerlemeni şimdi kaydet.",
        },
        ["ru"] = new[]
        {
            "Сайты", "Приложения и игры", "Поиск приложений и игр", "Все игры Steam", "И игры, установленные позже", "Магазин, библиотека и сообщество",
            "Игра Steam", "Программа", "Программы", "Открыто сейчас", "Сейчас ничего не открыто. Запустите программу и вернитесь или выберите её .exe ниже.", "Другая программа",
            "Нет в списке? Запустите её один раз или выберите её файл .exe.", "Выбрать программу…", "Эту программу нельзя ограничить отдельно.", "Показать все ({0})", "Входит во «Все игры Steam»", "Добавить ({0})",
            "Ночью ({0}–{1})", "Блокировать эту игру", "Блокировать это приложение", "Блокировать все игры Steam", "Всё приложение", "Это приложение допустимо",
            "{0} закроется через {1}", "Несколько приложений закроются через {0}", "Сохраните прогресс сейчас.",
        },
        ["uk"] = new[]
        {
            "Сайти", "Застосунки й ігри", "Пошук застосунків та ігор", "Усі ігри Steam", "І ігри, встановлені пізніше", "Крамниця, бібліотека та спільнота",
            "Гра Steam", "Програма", "Програми", "Відкрито зараз", "Зараз нічого не відкрито. Запустіть програму й поверніться або виберіть її .exe нижче.", "Інша програма",
            "Немає в списку? Запустіть її один раз або виберіть її файл .exe.", "Вибрати програму…", "Цю програму не можна обмежити окремо.", "Показати всі ({0})", "Входить до «Усі ігри Steam»", "Додати ({0})",
            "Уночі ({0}–{1})", "Блокувати цю гру", "Блокувати цей застосунок", "Блокувати всі ігри Steam", "Увесь застосунок", "Цей застосунок прийнятний",
            "{0} закриється через {1}", "Кілька застосунків закриються через {0}", "Збережіть прогрес зараз.",
        },
        ["bs"] = new[]
        {
            "Stranice", "Aplikacije i igre", "Traži aplikacije i igre", "Sve Steam igre", "I igre instalirane kasnije", "Prodavnica, biblioteka i zajednica",
            "Steam igra", "Program", "Programi", "Sada otvoreno", "Ništa nije otvoreno. Pokrenite ga i vratite se ili ispod odaberite njegov .exe.", "Drugi program",
            "Nije na listi? Pokrenite ga jednom ili odaberite njegovu .exe datoteku.", "Odaberi program…", "Ovaj program se ne može ograničiti zasebno.", "Prikaži sve ({0})", "Obuhvaćeno sa Sve Steam igre", "Dodaj {0}",
            "Noću ({0}–{1})", "Blokiraj ovu igru", "Blokiraj ovu aplikaciju", "Blokiraj sve Steam igre", "Cijela aplikacija", "Ova aplikacija je prihvatljiva",
            "{0} se zatvara za {1}", "Više aplikacija se zatvara za {0}", "Sačuvajte napredak sada.",
        },
        ["sr"] = new[]
        {
            "Сајтови", "Апликације и игре", "Претражи апликације и игре", "Све Steam игре", "И игре инсталиране касније", "Продавница, библиотека и заједница",
            "Steam игра", "Програм", "Програми", "Сада отворено", "Ништа није отворено. Покрените га и вратите се или испод изаберите његов .exe.", "Други програм",
            "Није на листи? Покрените га једном или изаберите његову .exe датотеку.", "Изабери програм…", "Овај програм не може да се ограничи засебно.", "Прикажи све ({0})", "Обухваћено са Све Steam игре", "Додај {0}",
            "Ноћу ({0}–{1})", "Блокирај ову игру", "Блокирај ову апликацију", "Блокирај све Steam игре", "Цела апликација", "Ова апликација је прихватљива",
            "{0} се затвара за {1}", "Више апликација се затвара за {0}", "Сачувајте напредак сада.",
        },
    };

    private static void MergeApps(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in Apps)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                for (int i = 0; i < AppKeys.Length; i++)
                {
                    table[AppKeys[i]] = text[i];
                }
            }
        }
    }
}
