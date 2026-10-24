namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for the status view (today's sites as a map of time, the enlarged
/// site with its actions, turning strict mode on in one click), the sites you
/// don't track, and what strict mode allows. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    private static readonly string[] StatusKeys =
    {
        "no_browsing_yet", "undo", "strict_off", "today", "turn_on", "len_2h",
        "more_options", "block_site", "block_always", "block_after", "block_part", "rest_of_site",
        "limited_by", "close", "show", "hide", "track_again", "strict_no_edit",
        "strict_can_add", "strict_can_help", "the_rest",
    };

    // Per language, in the order of StatusKeys.
    private static readonly Dictionary<string, string[]> Status = new()
    {
        ["en"] = new[]
        {
            "Nothing yet today. Time with an app, a game or a web page in front shows up here.", "Undo", "Strict mode is off", "Today", "Turn on", "2 hours",
            "More options", "Block {0}", "Always", "After {0} min an hour", "Block this part", "The rest of {0}",
            "Limited · {0}", "Close", "Show", "Hide", "Track again", "Rules can't be edited, switched off or deleted",
            "You can still add rules and switch rules on. Blocking more is always allowed.", "Make things stricter: add a rule, or switch one on.", "The rest",
        },
        ["de"] = new[]
        {
            "Heute noch nichts. Zeit mit einer App, einem Spiel oder einer Webseite im Vordergrund erscheint hier.", "Rückgängig", "Strikter Modus ist aus", "Heute", "Einschalten", "2 Stunden",
            "Weitere Optionen", "{0} blockieren", "Immer", "Nach {0} Min. pro Stunde", "Diesen Teil blockieren", "Der Rest von {0}",
            "Begrenzt · {0}", "Schließen", "Anzeigen", "Ausblenden", "Wieder erfassen", "Regeln lassen sich nicht ändern, ausschalten oder löschen",
            "Du kannst weiterhin Regeln hinzufügen und Regeln einschalten. Mehr blockieren geht immer.", "Strenger werden: eine Regel hinzufügen oder eine einschalten.", "Der Rest",
        },
        ["fr"] = new[]
        {
            "Rien pour l'instant. Le temps passé avec une appli, un jeu ou une page web au premier plan apparaît ici.", "Annuler", "Le mode strict est désactivé", "Aujourd'hui", "Activer", "2 heures",
            "Plus d'options", "Bloquer {0}", "Toujours", "Après {0} min par heure", "Bloquer cette partie", "Le reste de {0}",
            "Limité · {0}", "Fermer", "Afficher", "Masquer", "Suivre à nouveau", "Les règles ne peuvent pas être modifiées, désactivées ni supprimées",
            "Vous pouvez toujours ajouter des règles et en activer. Bloquer davantage est toujours permis.", "Rendez les choses plus strictes : ajoutez une règle ou activez-en une.", "Le reste",
        },
        ["es"] = new[]
        {
            "Nada todavía. El tiempo con una app, un juego o una página web delante aparece aquí.", "Deshacer", "El modo estricto está apagado", "Hoy", "Activar", "2 horas",
            "Más opciones", "Bloquear {0}", "Siempre", "Tras {0} min por hora", "Bloquear esta parte", "El resto de {0}",
            "Limitado · {0}", "Cerrar", "Mostrar", "Ocultar", "Volver a registrar", "Las reglas no se pueden editar, apagar ni eliminar",
            "Aún puedes añadir reglas y encenderlas. Bloquear más siempre está permitido.", "Hazlo más estricto: añade una regla o enciende una.", "El resto",
        },
        ["it"] = new[]
        {
            "Ancora niente oggi. Il tempo con un'app, un gioco o una pagina web in primo piano compare qui.", "Annulla", "La modalità rigida è spenta", "Oggi", "Attiva", "2 ore",
            "Altre opzioni", "Blocca {0}", "Sempre", "Dopo {0} min all'ora", "Blocca questa parte", "Il resto di {0}",
            "Limitato · {0}", "Chiudi", "Mostra", "Nascondi", "Traccia di nuovo", "Le regole non si possono modificare, spegnere o eliminare",
            "Puoi ancora aggiungere regole e accenderle. Bloccare di più è sempre consentito.", "Rendi tutto più rigido: aggiungi una regola o accendine una.", "Il resto",
        },
        ["pt"] = new[]
        {
            "Ainda nada hoje. O tempo com uma app, um jogo ou uma página web à frente aparece aqui.", "Desfazer", "O modo estrito está desligado", "Hoje", "Ativar", "2 horas",
            "Mais opções", "Bloquear {0}", "Sempre", "Após {0} min por hora", "Bloquear esta parte", "O resto de {0}",
            "Limitado · {0}", "Fechar", "Mostrar", "Ocultar", "Voltar a registar", "As regras não podem ser editadas, desligadas nem apagadas",
            "Ainda pode adicionar regras e ligá-las. Bloquear mais é sempre permitido.", "Torne tudo mais rigoroso: adicione uma regra ou ligue uma.", "O resto",
        },
        ["nl"] = new[]
        {
            "Vandaag nog niets. Tijd met een app, game of webpagina op de voorgrond verschijnt hier.", "Ongedaan maken", "Strikte modus staat uit", "Vandaag", "Inschakelen", "2 uur",
            "Meer opties", "{0} blokkeren", "Altijd", "Na {0} min per uur", "Dit deel blokkeren", "De rest van {0}",
            "Beperkt · {0}", "Sluiten", "Tonen", "Verbergen", "Weer bijhouden", "Regels kunnen niet worden bewerkt, uitgezet of verwijderd",
            "Je kunt nog steeds regels toevoegen en aanzetten. Meer blokkeren mag altijd.", "Maak het strenger: voeg een regel toe of zet er een aan.", "De rest",
        },
        ["pl"] = new[]
        {
            "Dziś jeszcze nic. Czas z aplikacją, grą lub stroną na pierwszym planie pojawi się tutaj.", "Cofnij", "Tryb ścisły jest wyłączony", "Dziś", "Włącz", "2 godziny",
            "Więcej opcji", "Zablokuj {0}", "Zawsze", "Po {0} min na godzinę", "Zablokuj tę część", "Reszta {0}",
            "Ograniczone · {0}", "Zamknij", "Pokaż", "Ukryj", "Śledź ponownie", "Reguł nie można edytować, wyłączać ani usuwać",
            "Nadal możesz dodawać reguły i je włączać. Blokowanie więcej jest zawsze dozwolone.", "Zaostrz zasady: dodaj regułę albo włącz którąś.", "Reszta",
        },
        ["sv"] = new[]
        {
            "Inget än i dag. Tid med en app, ett spel eller en webbsida i förgrunden visas här.", "Ångra", "Strikt läge är av", "I dag", "Slå på", "2 timmar",
            "Fler alternativ", "Blockera {0}", "Alltid", "Efter {0} min per timme", "Blockera den här delen", "Resten av {0}",
            "Begränsad · {0}", "Stäng", "Visa", "Dölj", "Spåra igen", "Regler kan inte ändras, stängas av eller tas bort",
            "Du kan fortfarande lägga till regler och slå på regler. Att blockera mer är alltid tillåtet.", "Gör det striktare: lägg till en regel eller slå på en.", "Resten",
        },
        ["da"] = new[]
        {
            "Intet endnu i dag. Tid med en app, et spil eller en webside i forgrunden vises her.", "Fortryd", "Streng tilstand er slået fra", "I dag", "Slå til", "2 timer",
            "Flere muligheder", "Bloker {0}", "Altid", "Efter {0} min i timen", "Bloker denne del", "Resten af {0}",
            "Begrænset · {0}", "Luk", "Vis", "Skjul", "Spor igen", "Regler kan ikke redigeres, slås fra eller slettes",
            "Du kan stadig tilføje regler og slå regler til. Det er altid tilladt at blokere mere.", "Gør det strengere: tilføj en regel, eller slå en til.", "Resten",
        },
        ["fi"] = new[]
        {
            "Ei vielä mitään tänään. Aika, jolloin sovellus, peli tai verkkosivu on edessä, näkyy tässä.", "Kumoa", "Tiukka tila on pois päältä", "Tänään", "Ota käyttöön", "2 tuntia",
            "Lisää vaihtoehtoja", "Estä {0}", "Aina", "{0} min jälkeen tunnissa", "Estä tämä osa", "Muu osa: {0}",
            "Rajoitettu · {0}", "Sulje", "Näytä", "Piilota", "Seuraa taas", "Sääntöjä ei voi muokata, kytkeä pois tai poistaa",
            "Voit yhä lisätä sääntöjä ja kytkeä niitä päälle. Enemmän estäminen on aina sallittua.", "Tiukenna: lisää sääntö tai kytke yksi päälle.", "Muu osa",
        },
        ["cs"] = new[]
        {
            "Dnes zatím nic. Čas s aplikací, hrou nebo webovou stránkou v popředí se zobrazí zde.", "Zpět", "Přísný režim je vypnutý", "Dnes", "Zapnout", "2 hodiny",
            "Další možnosti", "Blokovat {0}", "Vždy", "Po {0} min za hodinu", "Blokovat tuto část", "Zbytek {0}",
            "Omezeno · {0}", "Zavřít", "Zobrazit", "Skrýt", "Znovu sledovat", "Pravidla nelze upravit, vypnout ani smazat",
            "Pravidla můžete dál přidávat a zapínat. Blokovat víc jde vždycky.", "Zpřísněte to: přidejte pravidlo nebo nějaké zapněte.", "Zbytek",
        },
        ["sk"] = new[]
        {
            "Dnes zatiaľ nič. Čas s aplikáciou, hrou alebo webovou stránkou v popredí sa zobrazí tu.", "Späť", "Prísny režim je vypnutý", "Dnes", "Zapnúť", "2 hodiny",
            "Ďalšie možnosti", "Blokovať {0}", "Vždy", "Po {0} min za hodinu", "Blokovať túto časť", "Zvyšok {0}",
            "Obmedzené · {0}", "Zavrieť", "Zobraziť", "Skryť", "Znova sledovať", "Pravidlá nemožno upraviť, vypnúť ani odstrániť",
            "Pravidlá môžete ďalej pridávať a zapínať. Blokovať viac sa dá vždy.", "Sprísnite to: pridajte pravidlo alebo nejaké zapnite.", "Zvyšok",
        },
        ["sl"] = new[]
        {
            "Danes še nič. Čas z aplikacijo, igro ali spletno stranjo v ospredju se prikaže tukaj.", "Razveljavi", "Strogi način je izklopljen", "Danes", "Vklopi", "2 uri",
            "Več možnosti", "Blokiraj {0}", "Vedno", "Po {0} min na uro", "Blokiraj ta del", "Preostanek {0}",
            "Omejeno · {0}", "Zapri", "Pokaži", "Skrij", "Spet spremljaj", "Pravil ni mogoče urejati, izklopiti ali izbrisati",
            "Še vedno lahko dodajate pravila in jih vklapljate. Več blokiranja je vedno dovoljeno.", "Zaostrite: dodajte pravilo ali ga vklopite.", "Preostanek",
        },
        ["hr"] = new[]
        {
            "Danas još ništa. Vrijeme s aplikacijom, igrom ili web-stranicom u prvom planu prikazuje se ovdje.", "Poništi", "Strogi način je isključen", "Danas", "Uključi", "2 sata",
            "Više mogućnosti", "Blokiraj {0}", "Uvijek", "Nakon {0} min na sat", "Blokiraj ovaj dio", "Ostatak {0}",
            "Ograničeno · {0}", "Zatvori", "Prikaži", "Sakrij", "Ponovno prati", "Pravila se ne mogu uređivati, isključiti ni izbrisati",
            "I dalje možete dodavati pravila i uključivati ih. Blokirati više uvijek je dopušteno.", "Postrožite: dodajte pravilo ili uključite neko.", "Ostatak",
        },
        ["ro"] = new[]
        {
            "Nimic încă azi. Timpul cu o aplicație, un joc sau o pagină web în prim-plan apare aici.", "Anulează", "Modul strict este oprit", "Azi", "Pornește", "2 ore",
            "Mai multe opțiuni", "Blochează {0}", "Mereu", "După {0} min pe oră", "Blochează această parte", "Restul din {0}",
            "Limitat · {0}", "Închide", "Arată", "Ascunde", "Urmărește din nou", "Regulile nu pot fi editate, oprite sau șterse",
            "Poți în continuare să adaugi reguli și să le pornești. Să blochezi mai mult e mereu permis.", "Fă-le mai stricte: adaugă o regulă sau pornește una.", "Restul",
        },
        ["hu"] = new[]
        {
            "Ma még semmi. Az idő, amikor egy alkalmazás, játék vagy weboldal van elöl, itt jelenik meg.", "Visszavonás", "A szigorú mód ki van kapcsolva", "Ma", "Bekapcsolás", "2 óra",
            "További lehetőségek", "{0} tiltása", "Mindig", "Óránként {0} perc után", "Ennek a résznek a tiltása", "A(z) {0} többi része",
            "Korlátozva · {0}", "Bezárás", "Megjelenítés", "Elrejtés", "Követés újra", "A szabályok nem szerkeszthetők, nem kapcsolhatók ki és nem törölhetők",
            "Továbbra is adhatsz hozzá szabályokat és kapcsolhatsz be szabályokat. Többet tiltani mindig szabad.", "Szigoríts: adj hozzá egy szabályt, vagy kapcsolj be egyet.", "A többi",
        },
        ["el"] = new[]
        {
            "Τίποτα ακόμη σήμερα. Ο χρόνος με μια εφαρμογή, ένα παιχνίδι ή μια ιστοσελίδα μπροστά εμφανίζεται εδώ.", "Αναίρεση", "Η αυστηρή λειτουργία είναι ανενεργή", "Σήμερα", "Ενεργοποίηση", "2 ώρες",
            "Περισσότερες επιλογές", "Αποκλεισμός {0}", "Πάντα", "Μετά από {0} λεπ. την ώρα", "Αποκλεισμός αυτού του μέρους", "Το υπόλοιπο του {0}",
            "Περιορισμένο · {0}", "Κλείσιμο", "Εμφάνιση", "Απόκρυψη", "Καταγραφή ξανά", "Οι κανόνες δεν μπορούν να αλλάξουν, να απενεργοποιηθούν ή να διαγραφούν",
            "Μπορείτε ακόμα να προσθέτετε κανόνες και να τους ενεργοποιείτε. Ο περισσότερος αποκλεισμός επιτρέπεται πάντα.", "Κάντε τα πιο αυστηρά: προσθέστε έναν κανόνα ή ενεργοποιήστε έναν.", "Το υπόλοιπο",
        },
        ["bg"] = new[]
        {
            "Днес още нищо. Времето с приложение, игра или уеб страница отпред се показва тук.", "Отмяна", "Строгият режим е изключен", "Днес", "Включи", "2 часа",
            "Още опции", "Блокирай {0}", "Винаги", "След {0} мин на час", "Блокирай тази част", "Останалото от {0}",
            "Ограничено · {0}", "Затвори", "Покажи", "Скрий", "Следи отново", "Правилата не могат да се редактират, изключват или изтриват",
            "Все още можете да добавяте правила и да ги включвате. Повече блокиране винаги е позволено.", "Направете го по-строго: добавете правило или включете едно.", "Останалото",
        },
        ["lt"] = new[]
        {
            "Šiandien dar nieko. Laikas, kai priekyje programa, žaidimas ar tinklalapis, rodomas čia.", "Atšaukti", "Griežtasis režimas išjungtas", "Šiandien", "Įjungti", "2 valandos",
            "Daugiau parinkčių", "Blokuoti {0}", "Visada", "Po {0} min per valandą", "Blokuoti šią dalį", "Likusi {0} dalis",
            "Apribota · {0}", "Uždaryti", "Rodyti", "Slėpti", "Vėl sekti", "Taisyklių negalima redaguoti, išjungti ar ištrinti",
            "Vis dar galite pridėti taisyklių ir jas įjungti. Blokuoti daugiau visada leidžiama.", "Sugriežtinkite: pridėkite taisyklę arba įjunkite vieną.", "Likusi dalis",
        },
        ["lv"] = new[]
        {
            "Šodien vēl nekā. Laiks, kad priekšplānā ir lietotne, spēle vai tīmekļa lapa, tiek rādīts šeit.", "Atsaukt", "Stingrais režīms ir izslēgts", "Šodien", "Ieslēgt", "2 stundas",
            "Citas iespējas", "Bloķēt {0}", "Vienmēr", "Pēc {0} min stundā", "Bloķēt šo daļu", "Pārējais no {0}",
            "Ierobežots · {0}", "Aizvērt", "Rādīt", "Paslēpt", "Sekot atkal", "Noteikumus nevar rediģēt, izslēgt vai dzēst",
            "Joprojām varat pievienot noteikumus un tos ieslēgt. Bloķēt vairāk vienmēr ir atļauts.", "Padariet stingrāku: pievienojiet noteikumu vai ieslēdziet kādu.", "Pārējais",
        },
        ["et"] = new[]
        {
            "Täna veel midagi pole. Aeg, mil rakendus, mäng või veebileht on ees, kuvatakse siin.", "Võta tagasi", "Range režiim on väljas", "Täna", "Lülita sisse", "2 tundi",
            "Rohkem valikuid", "Blokeeri {0}", "Alati", "Pärast {0} min tunnis", "Blokeeri see osa", "Ülejäänud {0}",
            "Piiratud · {0}", "Sulge", "Näita", "Peida", "Jälgi uuesti", "Reegleid ei saa muuta, välja lülitada ega kustutada",
            "Saad endiselt reegleid lisada ja sisse lülitada. Rohkem blokeerida on alati lubatud.", "Tee rangemaks: lisa reegel või lülita mõni sisse.", "Ülejäänud",
        },
        ["ga"] = new[]
        {
            "Faic fós inniu. Taispeántar anseo an t-am le haip, cluiche nó leathanach gréasáin chun tosaigh.", "Cealaigh", "Tá an mód docht múchta", "Inniu", "Cas air", "2 uair an chloig",
            "Tuilleadh roghanna", "Blocáil {0}", "I gcónaí", "Tar éis {0} nóim san uair", "Blocáil an chuid seo", "An chuid eile de {0}",
            "Teoranta · {0}", "Dún", "Taispeáin", "Folaigh", "Rianaigh arís", "Ní féidir rialacha a athrú, a mhúchadh ná a scriosadh",
            "Is féidir leat rialacha a chur leis agus a chasadh air fós. Bíonn cead i gcónaí níos mó a bhlocáil.", "Déan níos déine é: cuir riail leis, nó cas ceann air.", "An chuid eile",
        },
        ["mt"] = new[]
        {
            "Xejn s'issa llum. Il-ħin b'app, logħba jew paġna web quddiem jidher hawn.", "Ħassar", "Il-modalità stretta hija mitfija", "Illum", "Ixgħel", "Sagħtejn",
            "Aktar għażliet", "Imblokka {0}", "Dejjem", "Wara {0} min fis-siegħa", "Imblokka din il-parti", "Il-bqija ta' {0}",
            "Limitat · {0}", "Agħlaq", "Uri", "Aħbi", "Erġa' segwi", "Ir-regoli ma jistgħux jinbidlu, jintfew jew jitħassru",
            "Xorta tista' żżid regoli u tixgħelhom. Li timblokka aktar dejjem permess.", "Agħmilha aktar stretta: żid regola, jew ixgħel waħda.", "Il-bqija",
        },
        ["ca"] = new[]
        {
            "Encara res avui. El temps amb una aplicació, un joc o una pàgina web al davant apareix aquí.", "Desfés", "El mode estricte està apagat", "Avui", "Activa", "2 hores",
            "Més opcions", "Bloqueja {0}", "Sempre", "Després de {0} min per hora", "Bloqueja aquesta part", "La resta de {0}",
            "Limitat · {0}", "Tanca", "Mostra", "Amaga", "Torna a registrar", "Les regles no es poden editar, apagar ni eliminar",
            "Encara pots afegir regles i encendre-les. Bloquejar més sempre està permès.", "Fes-ho més estricte: afegeix una regla o encén-ne una.", "La resta",
        },
        ["nb"] = new[]
        {
            "Ingenting ennå i dag. Tid med en app, et spill eller en nettside i forgrunnen vises her.", "Angre", "Streng modus er av", "I dag", "Slå på", "2 timer",
            "Flere alternativer", "Blokker {0}", "Alltid", "Etter {0} min i timen", "Blokker denne delen", "Resten av {0}",
            "Begrenset · {0}", "Lukk", "Vis", "Skjul", "Spor igjen", "Regler kan ikke redigeres, slås av eller slettes",
            "Du kan fortsatt legge til regler og slå regler på. Å blokkere mer er alltid tillatt.", "Gjør det strengere: legg til en regel, eller slå på en.", "Resten",
        },
        ["tr"] = new[]
        {
            "Bugün henüz bir şey yok. Önde bir uygulama, oyun veya web sayfasıyla geçen süre burada görünür.", "Geri al", "Katı mod kapalı", "Bugün", "Aç", "2 saat",
            "Daha fazla seçenek", "{0} engelle", "Her zaman", "Saatte {0} dk sonra", "Bu bölümü engelle", "{0} sitesinin geri kalanı",
            "Sınırlı · {0}", "Kapat", "Göster", "Gizle", "Yeniden takip et", "Kurallar düzenlenemez, kapatılamaz veya silinemez",
            "Hâlâ kural ekleyebilir ve kuralları açabilirsin. Daha fazla engellemek her zaman serbest.", "Daha sıkı yap: bir kural ekle ya da birini aç.", "Geri kalanı",
        },
        ["ru"] = new[]
        {
            "Сегодня пока ничего. Время с приложением, игрой или веб-страницей на переднем плане появится здесь.", "Отменить", "Строгий режим выключен", "Сегодня", "Включить", "2 часа",
            "Другие варианты", "Блокировать {0}", "Всегда", "После {0} мин в час", "Блокировать эту часть", "Остальное на {0}",
            "Ограничено · {0}", "Закрыть", "Показать", "Скрыть", "Снова отслеживать", "Правила нельзя изменить, выключить или удалить",
            "Вы по-прежнему можете добавлять правила и включать их. Блокировать больше можно всегда.", "Сделайте строже: добавьте правило или включите какое-нибудь.", "Остальное",
        },
        ["uk"] = new[]
        {
            "Сьогодні поки нічого. Час із застосунком, грою чи вебсторінкою на передньому плані з'явиться тут.", "Скасувати", "Суворий режим вимкнено", "Сьогодні", "Увімкнути", "2 години",
            "Інші варіанти", "Блокувати {0}", "Завжди", "Після {0} хв на годину", "Блокувати цю частину", "Решта на {0}",
            "Обмежено · {0}", "Закрити", "Показати", "Сховати", "Знову відстежувати", "Правила не можна змінити, вимкнути чи видалити",
            "Ви й далі можете додавати правила та вмикати їх. Блокувати більше можна завжди.", "Зробіть суворіше: додайте правило або ввімкніть якесь.", "Решта",
        },
        ["bs"] = new[]
        {
            "Danas još ništa. Vrijeme s aplikacijom, igrom ili web-stranicom u prvom planu prikazuje se ovdje.", "Poništi", "Strogi način je isključen", "Danas", "Uključi", "2 sata",
            "Više opcija", "Blokiraj {0}", "Uvijek", "Nakon {0} min na sat", "Blokiraj ovaj dio", "Ostatak {0}",
            "Ograničeno · {0}", "Zatvori", "Prikaži", "Sakrij", "Ponovo prati", "Pravila se ne mogu uređivati, isključiti niti izbrisati",
            "I dalje možete dodavati pravila i uključivati ih. Blokirati više je uvijek dozvoljeno.", "Pooštrite: dodajte pravilo ili uključite neko.", "Ostatak",
        },
        ["sr"] = new[]
        {
            "Данас још ништа. Време са апликацијом, игром или веб-страницом у првом плану приказује се овде.", "Поништи", "Строги режим је искључен", "Данас", "Укључи", "2 сата",
            "Још опција", "Блокирај {0}", "Увек", "После {0} мин на сат", "Блокирај овај део", "Остатак {0}",
            "Ограничено · {0}", "Затвори", "Прикажи", "Сакриј", "Поново прати", "Правила се не могу мењати, искључити нити брисати",
            "И даље можете додавати правила и укључивати их. Блокирати више је увек дозвољено.", "Пооштрите: додајте правило или укључите неко.", "Остатак",
        },
    };

    private static void MergeStatus(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in Status)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                for (int i = 0; i < StatusKeys.Length; i++)
                {
                    table[StatusKeys[i]] = text[i];
                }
            }
        }
    }
}
