namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for the status view: today's browser time, the brain rot verdicts,
/// strict mode at a glance and today's achievements. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    private static readonly string[] StatusKeys =
    {
        "status", "status_subtitle", "rot_today", "rot_of_total", "no_browsing_yet",
        "where_time_went", "where_time_went_help", "verdict_rot", "verdict_watch", "verdict_fine",
        "verdict_your_call", "verdict_explain", "opened_times", "other_sites", "in_rule",
        "not_rot", "undo", "strict_off", "strict_off_help", "turn_on_strict",
        "achievements", "ach_clean", "ach_clean_help", "ach_no_binge", "ach_no_binge_help",
        "ach_no_pause", "ach_no_pause_help", "ach_locked", "ach_locked_help",
    };

    // Per language, in the order of StatusKeys.
    private static readonly Dictionary<string, string[]> Status = new()
    {
        ["en"] = new[]
        {
            "Status", "Where your browser time goes today. Only totals per site, kept on this PC and cleared at midnight.",
            "Brain rot today", "of {0} in the browser today",
            "Nothing yet today. Time with a page in front in Chrome, Edge, Firefox, Brave, Vivaldi or Opera shows up here.",
            "Where your time went", "Grouped by site and split further where most of the time goes.",
            "Brain rot", "Watch out", "Fine", "Fine — your call",
            "Brain rot: over {0} min today, or opened {1} times or more and over {2} min.",
            "Opened {0}×", "Other sites, under a minute each: {0}", "In “{0}”", "Not brain rot", "Undo",
            "Strict mode is off", "Lock your rules for a while, so you can't loosen, pause or remove them.", "Turn on strict mode",
            "Today's achievements", "Clean so far", "Less than {0} min of brain rot today", "No binge", "No site over {0} min",
            "Stayed on", "Blocking not paused today", "Locked in", "Strict mode on today",
        },
        ["de"] = new[]
        {
            "Status", "Wohin deine Browserzeit heute geht. Nur Summen pro Website, auf diesem PC gespeichert und um Mitternacht gelöscht.",
            "Brain Rot heute", "von {0} im Browser heute",
            "Heute noch nichts. Zeit mit einer Seite im Vordergrund in Chrome, Edge, Firefox, Brave, Vivaldi oder Opera erscheint hier.",
            "Wohin deine Zeit ging", "Nach Website gruppiert und weiter aufgeteilt, wo die meiste Zeit hingeht.",
            "Brain Rot", "Vorsicht", "In Ordnung", "In Ordnung – deine Wahl",
            "Brain Rot: heute über {0} Min., oder {1}-mal oder öfter geöffnet und über {2} Min.",
            "{0}× geöffnet", "Weitere Websites, je unter einer Minute: {0}", "In „{0}“", "Kein Brain Rot", "Rückgängig",
            "Strikter Modus ist aus", "Sperre deine Regeln für eine Weile, damit du sie nicht lockern, pausieren oder löschen kannst.", "Strikten Modus einschalten",
            "Heutige Erfolge", "Bisher sauber", "Heute unter {0} Min. Brain Rot", "Kein Binge", "Keine Website über {0} Min.",
            "Drangeblieben", "Blockieren heute nicht pausiert", "Eingeschlossen", "Strikter Modus heute aktiv",
        },
        ["fr"] = new[]
        {
            "Statut", "Où va votre temps de navigation aujourd'hui. Seulement des totaux par site, gardés sur ce PC et effacés à minuit.",
            "Brain rot aujourd'hui", "sur {0} dans le navigateur aujourd'hui",
            "Rien pour l'instant. Le temps passé avec une page au premier plan dans Chrome, Edge, Firefox, Brave, Vivaldi ou Opera apparaît ici.",
            "Où est passé votre temps", "Regroupé par site et détaillé là où va l'essentiel du temps.",
            "Brain rot", "Attention", "OK", "OK — votre choix",
            "Brain rot : plus de {0} min aujourd'hui, ou ouvert {1} fois ou plus et plus de {2} min.",
            "Ouvert {0}×", "Autres sites, moins d'une minute chacun : {0}", "Dans « {0} »", "Pas du brain rot", "Annuler",
            "Le mode strict est désactivé", "Verrouillez vos règles un moment pour ne pas pouvoir les assouplir, les mettre en pause ou les supprimer.", "Activer le mode strict",
            "Succès du jour", "Propre jusqu'ici", "Moins de {0} min de brain rot aujourd'hui", "Pas de binge", "Aucun site au-delà de {0} min",
            "Tenu bon", "Blocage pas mis en pause aujourd'hui", "Verrouillé", "Mode strict actif aujourd'hui",
        },
        ["es"] = new[]
        {
            "Estado", "A dónde va hoy tu tiempo en el navegador. Solo totales por sitio, guardados en este PC y borrados a medianoche.",
            "Brain rot hoy", "de {0} en el navegador hoy",
            "Nada todavía. El tiempo con una página delante en Chrome, Edge, Firefox, Brave, Vivaldi u Opera aparece aquí.",
            "A dónde fue tu tiempo", "Agrupado por sitio y desglosado donde se va la mayor parte del tiempo.",
            "Brain rot", "Cuidado", "Bien", "Bien: tú decides",
            "Brain rot: más de {0} min hoy, o abierto {1} veces o más y más de {2} min.",
            "Abierto {0}×", "Otros sitios, menos de un minuto cada uno: {0}", "En «{0}»", "No es brain rot", "Deshacer",
            "El modo estricto está apagado", "Bloquea tus reglas un tiempo para no poder suavizarlas, pausarlas ni quitarlas.", "Activar el modo estricto",
            "Logros de hoy", "Limpio por ahora", "Menos de {0} min de brain rot hoy", "Sin atracones", "Ningún sitio de más de {0} min",
            "Firme", "El bloqueo no se pausó hoy", "Bajo llave", "Modo estricto activo hoy",
        },
        ["it"] = new[]
        {
            "Stato", "Dove va oggi il tuo tempo nel browser. Solo totali per sito, salvati su questo PC e cancellati a mezzanotte.",
            "Brain rot oggi", "su {0} nel browser oggi",
            "Ancora niente oggi. Il tempo con una pagina in primo piano in Chrome, Edge, Firefox, Brave, Vivaldi o Opera compare qui.",
            "Dove è andato il tuo tempo", "Raggruppato per sito e suddiviso dove va la maggior parte del tempo.",
            "Brain rot", "Attenzione", "Ok", "Ok — scelta tua",
            "Brain rot: oltre {0} min oggi, oppure aperto {1} volte o più e oltre {2} min.",
            "Aperto {0}×", "Altri siti, meno di un minuto ciascuno: {0}", "In «{0}»", "Non è brain rot", "Annulla",
            "La modalità rigida è spenta", "Blocca le tue regole per un po', così non puoi allentarle, sospenderle o rimuoverle.", "Attiva la modalità rigida",
            "Traguardi di oggi", "Pulito finora", "Meno di {0} min di brain rot oggi", "Niente abbuffate", "Nessun sito oltre {0} min",
            "Tenuto duro", "Blocco mai sospeso oggi", "Blindato", "Modalità rigida attiva oggi",
        },
        ["pt"] = new[]
        {
            "Estado", "Para onde vai hoje o seu tempo no navegador. Só totais por site, guardados neste PC e apagados à meia-noite.",
            "Brain rot hoje", "de {0} no navegador hoje",
            "Ainda nada hoje. O tempo com uma página à frente no Chrome, Edge, Firefox, Brave, Vivaldi ou Opera aparece aqui.",
            "Para onde foi o seu tempo", "Agrupado por site e detalhado onde vai a maior parte do tempo.",
            "Brain rot", "Atenção", "Bem", "Bem — decisão sua",
            "Brain rot: mais de {0} min hoje, ou aberto {1} vezes ou mais e mais de {2} min.",
            "Aberto {0}×", "Outros sites, menos de um minuto cada: {0}", "Em «{0}»", "Não é brain rot", "Desfazer",
            "O modo estrito está desligado", "Tranque as suas regras durante algum tempo para não as poder aliviar, pausar ou remover.", "Ligar o modo estrito",
            "Conquistas de hoje", "Limpo até agora", "Menos de {0} min de brain rot hoje", "Sem maratonas", "Nenhum site com mais de {0} min",
            "Firme", "Bloqueio não pausado hoje", "Trancado", "Modo estrito ativo hoje",
        },
        ["nl"] = new[]
        {
            "Status", "Waar je browsertijd vandaag heen gaat. Alleen totalen per site, bewaard op deze pc en om middernacht gewist.",
            "Brain rot vandaag", "van {0} in de browser vandaag",
            "Vandaag nog niets. Tijd met een pagina op de voorgrond in Chrome, Edge, Firefox, Brave, Vivaldi of Opera verschijnt hier.",
            "Waar je tijd heen ging", "Gegroepeerd per site en verder opgesplitst waar de meeste tijd heen gaat.",
            "Brain rot", "Opgelet", "Prima", "Prima — jouw keuze",
            "Brain rot: vandaag meer dan {0} min, of {1} keer of vaker geopend en meer dan {2} min.",
            "{0}× geopend", "Andere sites, elk minder dan een minuut: {0}", "In ‘{0}’", "Geen brain rot", "Ongedaan maken",
            "Strikte modus staat uit", "Vergrendel je regels een tijdje, zodat je ze niet kunt versoepelen, pauzeren of verwijderen.", "Strikte modus aanzetten",
            "Prestaties van vandaag", "Tot nu toe schoon", "Minder dan {0} min brain rot vandaag", "Geen binge", "Geen site boven {0} min",
            "Volgehouden", "Blokkeren vandaag niet gepauzeerd", "Vergrendeld", "Strikte modus vandaag aan",
        },
        ["pl"] = new[]
        {
            "Stan", "Na co dziś idzie Twój czas w przeglądarce. Tylko sumy na stronę, przechowywane na tym komputerze i kasowane o północy.",
            "Brain rot dzisiaj", "z {0} w przeglądarce dzisiaj",
            "Dziś jeszcze nic. Czas ze stroną na pierwszym planie w Chrome, Edge, Firefox, Brave, Vivaldi lub Opera pojawi się tutaj.",
            "Na co poszedł Twój czas", "Pogrupowane według stron i rozbite tam, gdzie idzie najwięcej czasu.",
            "Brain rot", "Uwaga", "W porządku", "W porządku — Twój wybór",
            "Brain rot: dziś ponad {0} min albo otwarte {1} razy lub więcej i ponad {2} min.",
            "Otwarto {0}×", "Inne strony, każda poniżej minuty: {0}", "W „{0}”", "To nie brain rot", "Cofnij",
            "Tryb ścisły jest wyłączony", "Zablokuj reguły na jakiś czas, by nie móc ich złagodzić, wstrzymać ani usunąć.", "Włącz tryb ścisły",
            "Dzisiejsze osiągnięcia", "Na razie czysto", "Mniej niż {0} min brain rotu dzisiaj", "Bez maratonów", "Żadna strona ponad {0} min",
            "Wytrwałość", "Blokowanie dziś niewstrzymane", "Pod kluczem", "Tryb ścisły dziś włączony",
        },
        ["sv"] = new[]
        {
            "Status", "Vart din webbläsartid går i dag. Bara summor per webbplats, sparade på den här datorn och raderade vid midnatt.",
            "Brain rot i dag", "av {0} i webbläsaren i dag",
            "Inget än i dag. Tid med en sida i förgrunden i Chrome, Edge, Firefox, Brave, Vivaldi eller Opera visas här.",
            "Vart din tid tog vägen", "Grupperat per webbplats och uppdelat där det mesta av tiden går.",
            "Brain rot", "Se upp", "Okej", "Okej – ditt val",
            "Brain rot: över {0} min i dag, eller öppnad {1} gånger eller fler och över {2} min.",
            "Öppnad {0}×", "Andra webbplatser, under en minut var: {0}", "I ”{0}”", "Inte brain rot", "Ångra",
            "Strikt läge är av", "Lås dina regler en stund så att du inte kan lätta på, pausa eller ta bort dem.", "Slå på strikt läge",
            "Dagens prestationer", "Rent hittills", "Mindre än {0} min brain rot i dag", "Inget hetsande", "Ingen webbplats över {0} min",
            "Höll ut", "Blockeringen pausades inte i dag", "Inlåst", "Strikt läge på i dag",
        },
        ["da"] = new[]
        {
            "Status", "Hvor din browsertid går hen i dag. Kun totaler pr. websted, gemt på denne pc og slettet ved midnat.",
            "Brain rot i dag", "af {0} i browseren i dag",
            "Intet endnu i dag. Tid med en side i forgrunden i Chrome, Edge, Firefox, Brave, Vivaldi eller Opera vises her.",
            "Hvor din tid blev af", "Grupperet efter websted og delt yderligere op, hvor det meste af tiden går.",
            "Brain rot", "Pas på", "Fint", "Fint – dit valg",
            "Brain rot: over {0} min i dag, eller åbnet {1} gange eller flere og over {2} min.",
            "Åbnet {0}×", "Andre websteder, under et minut hver: {0}", "I “{0}”", "Ikke brain rot", "Fortryd",
            "Streng tilstand er slået fra", "Lås dine regler et stykke tid, så du ikke kan lempe, sætte på pause eller fjerne dem.", "Slå streng tilstand til",
            "Dagens præstationer", "Rent indtil videre", "Under {0} min brain rot i dag", "Ingen binge", "Intet websted over {0} min",
            "Holdt fast", "Blokering ikke sat på pause i dag", "Låst inde", "Streng tilstand slået til i dag",
        },
        ["fi"] = new[]
        {
            "Tila", "Mihin selainaikasi menee tänään. Vain sivustokohtaiset summat, tallennettu tälle tietokoneelle ja poistetaan keskiyöllä.",
            "Brain rot tänään", "/ {0} selaimessa tänään",
            "Ei vielä mitään tänään. Aika, jolloin sivu on edessä Chromessa, Edgessä, Firefoxissa, Bravessa, Vivaldissa tai Operassa, näkyy tässä.",
            "Mihin aikasi meni", "Ryhmitelty sivustoittain ja jaettu tarkemmin sinne, minne eniten aikaa menee.",
            "Brain rot", "Varo", "Kunnossa", "Kunnossa – sinun valintasi",
            "Brain rot: yli {0} min tänään, tai avattu {1} kertaa tai useammin ja yli {2} min.",
            "Avattu {0}×", "Muut sivustot, kukin alle minuutin: {0}", "Säännössä ”{0}”", "Ei brain rotia", "Kumoa",
            "Tiukka tila on pois päältä", "Lukitse sääntösi joksikin aikaa, jotta et voi löysätä, keskeyttää tai poistaa niitä.", "Kytke tiukka tila päälle",
            "Tämän päivän saavutukset", "Puhdas tähän asti", "Alle {0} min brain rotia tänään", "Ei ahmimista", "Ei sivustoa yli {0} min",
            "Pysyi päällä", "Estoa ei keskeytetty tänään", "Lukittu", "Tiukka tila päällä tänään",
        },
        ["cs"] = new[]
        {
            "Stav", "Kam dnes jde váš čas v prohlížeči. Jen součty za web, uložené v tomto PC a o půlnoci smazané.",
            "Brain rot dnes", "z {0} v prohlížeči dnes",
            "Dnes zatím nic. Čas se stránkou v popředí v Chrome, Edge, Firefox, Brave, Vivaldi nebo Opera se zobrazí zde.",
            "Kam šel váš čas", "Seskupeno podle webu a dále rozděleno tam, kam jde nejvíc času.",
            "Brain rot", "Pozor", "V pořádku", "V pořádku – vaše volba",
            "Brain rot: dnes přes {0} min, nebo otevřeno {1}× či víckrát a přes {2} min.",
            "Otevřeno {0}×", "Další weby, každý pod minutu: {0}", "V „{0}“", "Není brain rot", "Zpět",
            "Přísný režim je vypnutý", "Zamkněte svá pravidla na čas, abyste je nemohli zmírnit, pozastavit ani odstranit.", "Zapnout přísný režim",
            "Dnešní úspěchy", "Zatím čisto", "Dnes méně než {0} min brain rotu", "Žádné maratony", "Žádný web přes {0} min",
            "Vydrželi jste", "Blokování dnes nepozastaveno", "Zamčeno", "Přísný režim dnes zapnutý",
        },
        ["sk"] = new[]
        {
            "Stav", "Kam dnes ide váš čas v prehliadači. Len súčty za web, uložené v tomto PC a o polnoci vymazané.",
            "Brain rot dnes", "z {0} v prehliadači dnes",
            "Dnes zatiaľ nič. Čas so stránkou v popredí v Chrome, Edge, Firefox, Brave, Vivaldi alebo Opera sa zobrazí tu.",
            "Kam išiel váš čas", "Zoskupené podľa webu a ďalej rozdelené tam, kam ide najviac času.",
            "Brain rot", "Pozor", "V poriadku", "V poriadku – vaša voľba",
            "Brain rot: dnes viac ako {0} min, alebo otvorené {1}× či viackrát a viac ako {2} min.",
            "Otvorené {0}×", "Ďalšie weby, každý pod minútu: {0}", "V „{0}“", "Nie je brain rot", "Späť",
            "Prísny režim je vypnutý", "Zamknite svoje pravidlá na čas, aby ste ich nemohli zmierniť, pozastaviť ani odstrániť.", "Zapnúť prísny režim",
            "Dnešné úspechy", "Zatiaľ čisto", "Dnes menej ako {0} min brain rotu", "Žiadne maratóny", "Žiadny web nad {0} min",
            "Vydržali ste", "Blokovanie dnes nepozastavené", "Zamknuté", "Prísny režim dnes zapnutý",
        },
        ["sl"] = new[]
        {
            "Stanje", "Kam gre danes vaš čas v brskalniku. Le seštevki po spletnih mestih, shranjeni na tem računalniku in izbrisani ob polnoči.",
            "Brain rot danes", "od {0} v brskalniku danes",
            "Danes še nič. Čas s stranjo v ospredju v Chromu, Edgeu, Firefoxu, Bravu, Vivaldiju ali Operi se prikaže tukaj.",
            "Kam je šel vaš čas", "Združeno po spletnih mestih in dodatno razdeljeno tam, kamor gre največ časa.",
            "Brain rot", "Pozor", "V redu", "V redu – vaša izbira",
            "Brain rot: danes več kot {0} min ali odprto {1}-krat ali večkrat in več kot {2} min.",
            "Odprto {0}×", "Druga spletna mesta, vsako manj kot minuto: {0}", "V »{0}«", "Ni brain rot", "Razveljavi",
            "Strogi način je izklopljen", "Za nekaj časa zaklenite pravila, da jih ne morete omiliti, začasno ustaviti ali odstraniti.", "Vklopi strogi način",
            "Današnji dosežki", "Zaenkrat čisto", "Danes manj kot {0} min brain rota", "Brez popivanja", "Nobeno mesto nad {0} min",
            "Vztrajali", "Blokiranje danes ni bilo ustavljeno", "Zaklenjeno", "Strogi način danes vklopljen",
        },
        ["hr"] = new[]
        {
            "Stanje", "Kamo danas ide vaše vrijeme u pregledniku. Samo zbrojevi po web-mjestu, spremljeni na ovom računalu i obrisani u ponoć.",
            "Brain rot danas", "od {0} u pregledniku danas",
            "Danas još ništa. Vrijeme sa stranicom u prvom planu u Chromeu, Edgeu, Firefoxu, Braveu, Vivaldiju ili Operi prikazuje se ovdje.",
            "Kamo je otišlo vaše vrijeme", "Grupirano po web-mjestu i dodatno podijeljeno tamo kamo ide najviše vremena.",
            "Brain rot", "Oprez", "U redu", "U redu – vaš izbor",
            "Brain rot: danas više od {0} min ili otvoreno {1} puta ili više i više od {2} min.",
            "Otvoreno {0}×", "Ostala web-mjesta, svako manje od minute: {0}", "U „{0}”", "Nije brain rot", "Poništi",
            "Strogi način je isključen", "Zaključajte pravila na neko vrijeme kako ih ne biste mogli ublažiti, pauzirati ili ukloniti.", "Uključi strogi način",
            "Današnja postignuća", "Zasad čisto", "Danas manje od {0} min brain rota", "Bez pretjerivanja", "Nijedno mjesto preko {0} min",
            "Izdržali ste", "Blokiranje danas nije pauzirano", "Zaključano", "Strogi način danas uključen",
        },
        ["ro"] = new[]
        {
            "Stare", "Unde se duce azi timpul tău în browser. Doar totaluri pe site, păstrate pe acest PC și șterse la miezul nopții.",
            "Brain rot azi", "din {0} în browser azi",
            "Nimic încă azi. Timpul cu o pagină în prim-plan în Chrome, Edge, Firefox, Brave, Vivaldi sau Opera apare aici.",
            "Unde s-a dus timpul tău", "Grupat pe site și împărțit mai departe acolo unde se duce cel mai mult timp.",
            "Brain rot", "Atenție", "În regulă", "În regulă — alegerea ta",
            "Brain rot: peste {0} min azi sau deschis de {1} ori sau mai mult și peste {2} min.",
            "Deschis de {0}×", "Alte site-uri, sub un minut fiecare: {0}", "În „{0}”", "Nu e brain rot", "Anulează",
            "Modul strict este oprit", "Blochează-ți regulile o vreme, ca să nu le poți relaxa, întrerupe sau șterge.", "Pornește modul strict",
            "Realizările de azi", "Curat până acum", "Sub {0} min de brain rot azi", "Fără exagerări", "Niciun site peste {0} min",
            "Ai rezistat", "Blocarea nu a fost întreruptă azi", "Sub cheie", "Modul strict activ azi",
        },
        ["hu"] = new[]
        {
            "Állapot", "Mire megy ma a böngészőben töltött időd. Csak oldalankénti összegek, ezen a gépen tárolva, éjfélkor törölve.",
            "Brain rot ma", "a mai {0} böngészésből",
            "Ma még semmi. Az idő, amikor egy oldal elöl van a Chrome-ban, Edge-ben, Firefoxban, Brave-ben, Vivaldiban vagy Operában, itt jelenik meg.",
            "Mire ment el az időd", "Oldalanként csoportosítva, és tovább bontva ott, ahová a legtöbb idő megy.",
            "Brain rot", "Vigyázat", "Rendben", "Rendben – a te döntésed",
            "Brain rot: ma több mint {0} perc, vagy legalább {1}-szer megnyitva és több mint {2} perc.",
            "{0}× megnyitva", "Egyéb oldalak, mindegyik egy perc alatt: {0}", "Ebben: „{0}”", "Nem brain rot", "Visszavonás",
            "A szigorú mód ki van kapcsolva", "Zárold a szabályaidat egy időre, hogy ne tudd lazítani, szüneteltetni vagy törölni őket.", "Szigorú mód bekapcsolása",
            "Mai eredmények", "Eddig tiszta", "Ma kevesebb mint {0} perc brain rot", "Nincs darálás", "Egy oldal sem több mint {0} perc",
            "Kitartottál", "A tiltás ma nem volt szüneteltetve", "Bezárva", "Szigorú mód ma bekapcsolva",
        },
        ["el"] = new[]
        {
            "Κατάσταση", "Πού πηγαίνει σήμερα ο χρόνος σας στον περιηγητή. Μόνο σύνολα ανά ιστότοπο, αποθηκευμένα σε αυτόν τον υπολογιστή και διαγράφονται τα μεσάνυχτα.",
            "Brain rot σήμερα", "από {0} στον περιηγητή σήμερα",
            "Τίποτα ακόμη σήμερα. Ο χρόνος με μια σελίδα μπροστά σε Chrome, Edge, Firefox, Brave, Vivaldi ή Opera εμφανίζεται εδώ.",
            "Πού πήγε ο χρόνος σας", "Ομαδοποιημένο ανά ιστότοπο και αναλυτικά εκεί όπου πηγαίνει ο περισσότερος χρόνος.",
            "Brain rot", "Προσοχή", "Εντάξει", "Εντάξει — δική σας επιλογή",
            "Brain rot: πάνω από {0} λεπ. σήμερα, ή άνοιξε {1} φορές ή περισσότερες και πάνω από {2} λεπ.",
            "Άνοιξε {0}×", "Άλλοι ιστότοποι, κάτω από ένα λεπτό ο καθένας: {0}", "Στον «{0}»", "Όχι brain rot", "Αναίρεση",
            "Η αυστηρή λειτουργία είναι ανενεργή", "Κλειδώστε τους κανόνες σας για λίγο, ώστε να μην μπορείτε να τους χαλαρώσετε, να τους παύσετε ή να τους αφαιρέσετε.", "Ενεργοποίηση αυστηρής λειτουργίας",
            "Σημερινά επιτεύγματα", "Καθαρά ως τώρα", "Λιγότερα από {0} λεπ. brain rot σήμερα", "Χωρίς υπερβολές", "Κανένας ιστότοπος πάνω από {0} λεπ.",
            "Άντεξες", "Ο αποκλεισμός δεν τέθηκε σε παύση σήμερα", "Κλειδωμένο", "Αυστηρή λειτουργία ενεργή σήμερα",
        },
        ["bg"] = new[]
        {
            "Състояние", "Къде отива днес времето ви в браузъра. Само суми по сайт, пазени на този компютър и изтривани в полунощ.",
            "Brain rot днес", "от {0} в браузъра днес",
            "Днес още нищо. Времето със страница отпред в Chrome, Edge, Firefox, Brave, Vivaldi или Opera се показва тук.",
            "Къде отиде времето ви", "Групирано по сайт и разделено още там, където отива най-много време.",
            "Brain rot", "Внимание", "Наред", "Наред — ваш избор",
            "Brain rot: над {0} мин днес или отворен {1} пъти или повече и над {2} мин.",
            "Отворен {0}×", "Други сайтове, под минута всеки: {0}", "В „{0}“", "Не е brain rot", "Отмяна",
            "Строгият режим е изключен", "Заключете правилата си за известно време, за да не можете да ги облекчите, спрете или премахнете.", "Включи строгия режим",
            "Днешни постижения", "Чисто засега", "Под {0} мин brain rot днес", "Без запои", "Нито един сайт над {0} мин",
            "Издържахте", "Блокирането не е спирано днес", "Заключено", "Строг режим включен днес",
        },
        ["lt"] = new[]
        {
            "Būsena", "Kur šiandien eina jūsų laikas naršyklėje. Tik sumos pagal svetainę, saugomos šiame kompiuteryje ir ištrinamos vidurnaktį.",
            "Brain rot šiandien", "iš {0} naršyklėje šiandien",
            "Šiandien dar nieko. Laikas, kai puslapis priekyje Chrome, Edge, Firefox, Brave, Vivaldi ar Opera, rodomas čia.",
            "Kur dingo jūsų laikas", "Sugrupuota pagal svetainę ir išskaidyta ten, kur eina daugiausia laiko.",
            "Brain rot", "Atsargiai", "Gerai", "Gerai – jūsų sprendimas",
            "Brain rot: šiandien daugiau nei {0} min arba atidaryta {1} k. ar daugiau ir daugiau nei {2} min.",
            "Atidaryta {0}×", "Kitos svetainės, kiekviena trumpiau nei minutę: {0}", "Taisyklėje „{0}“", "Ne brain rot", "Atšaukti",
            "Griežtasis režimas išjungtas", "Užrakinkite taisykles kuriam laikui, kad negalėtumėte jų sušvelninti, pristabdyti ar pašalinti.", "Įjungti griežtąjį režimą",
            "Šiandienos pasiekimai", "Kol kas švaru", "Šiandien mažiau nei {0} min brain rot", "Be persivalgymo", "Nė vienos svetainės virš {0} min",
            "Ištvėrėte", "Blokavimas šiandien nepristabdytas", "Užrakinta", "Griežtasis režimas šiandien įjungtas",
        },
        ["lv"] = new[]
        {
            "Statuss", "Kur šodien aiziet jūsu laiks pārlūkā. Tikai summas pa vietnēm, glabātas šajā datorā un dzēstas pusnaktī.",
            "Brain rot šodien", "no {0} pārlūkā šodien",
            "Šodien vēl nekā. Laiks, kad lapa ir priekšplānā Chrome, Edge, Firefox, Brave, Vivaldi vai Opera, tiek rādīts šeit.",
            "Kur aizgāja jūsu laiks", "Grupēts pa vietnēm un sīkāk sadalīts tur, kur aiziet visvairāk laika.",
            "Brain rot", "Uzmanību", "Kārtībā", "Kārtībā — jūsu izvēle",
            "Brain rot: šodien vairāk nekā {0} min vai atvērts {1} reizes vai vairāk un vairāk nekā {2} min.",
            "Atvērts {0}×", "Citas vietnes, katra mazāk par minūti: {0}", "Noteikumā “{0}”", "Nav brain rot", "Atsaukt",
            "Stingrais režīms ir izslēgts", "Uz laiku bloķējiet savus noteikumus, lai nevarētu tos atvieglot, apturēt vai noņemt.", "Ieslēgt stingro režīmu",
            "Šodienas sasniegumi", "Pagaidām tīrs", "Šodien mazāk nekā {0} min brain rot", "Bez pārmērībām", "Neviena vietne virs {0} min",
            "Izturējāt", "Bloķēšana šodien nav apturēta", "Bloķēts", "Stingrais režīms šodien ieslēgts",
        },
        ["et"] = new[]
        {
            "Olek", "Kuhu läheb täna sinu brauseriaeg. Ainult summad saidi kaupa, hoitud selles arvutis ja kustutatud keskööl.",
            "Brain rot täna", "{0}-st brauseris täna",
            "Täna veel midagi pole. Aeg, mil leht on Chrome'is, Edge'is, Firefoxis, Brave'is, Vivaldis või Operas ees, kuvatakse siin.",
            "Kuhu su aeg läks", "Rühmitatud saidi kaupa ja jagatud täpsemalt sinna, kuhu läheb kõige rohkem aega.",
            "Brain rot", "Ettevaatust", "Korras", "Korras – sinu otsus",
            "Brain rot: täna üle {0} min või avatud {1} korda või rohkem ja üle {2} min.",
            "Avatud {0}×", "Muud saidid, igaüks alla minuti: {0}", "Reeglis „{0}”", "Pole brain rot", "Võta tagasi",
            "Range režiim on väljas", "Lukusta oma reeglid mõneks ajaks, et sa ei saaks neid leevendada, peatada ega eemaldada.", "Lülita range režiim sisse",
            "Tänased saavutused", "Siiani puhas", "Täna alla {0} min brain roti", "Ilma liialdusteta", "Ükski sait üle {0} min",
            "Pidasid vastu", "Blokeerimist täna ei peatatud", "Lukus", "Range režiim täna sees",
        },
        ["ga"] = new[]
        {
            "Stádas", "Cá dtéann d'am brabhsálaí inniu. Iomláin in aghaidh an tsuímh amháin, coinnithe ar an ríomhaire seo agus glanta ag meán oíche.",
            "Brain rot inniu", "as {0} sa bhrabhsálaí inniu",
            "Faic fós inniu. Taispeántar anseo an t-am le leathanach chun tosaigh in Chrome, Edge, Firefox, Brave, Vivaldi nó Opera.",
            "Cá ndeachaigh d'am", "Grúpáilte de réir suímh agus roinnte níos mó san áit a dtéann an chuid is mó den am.",
            "Brain rot", "Seachain", "Ceart go leor", "Ceart go leor — do rogha",
            "Brain rot: os cionn {0} nóim. inniu, nó oscailte {1} uair nó níos mó agus os cionn {2} nóim.",
            "Oscailte {0}×", "Suíomhanna eile, faoi nóiméad an ceann: {0}", "In “{0}”", "Ní brain rot é", "Cealaigh",
            "Tá an mód docht múchta", "Cuir glas ar do rialacha ar feadh tamaill, ionas nach féidir leat iad a mhaolú, a chur ar sos ná a bhaint.", "Cas air an mód docht",
            "Éachtaí an lae inniu", "Glan go dtí seo", "Níos lú ná {0} nóim. de brain rot inniu", "Gan ragús", "Níl suíomh ar bith os cionn {0} nóim.",
            "D'fhan sé air", "Níor cuireadh an blocáil ar sos inniu", "Faoi ghlas", "Mód docht air inniu",
        },
        ["mt"] = new[]
        {
            "Status", "Fejn imur illum il-ħin tiegħek fil-browser. Totali għal kull sit biss, miżmuma fuq dan il-PC u mħassra f'nofsillejl.",
            "Brain rot illum", "minn {0} fil-browser illum",
            "Xejn s'issa llum. Il-ħin b'paġna quddiem f'Chrome, Edge, Firefox, Brave, Vivaldi jew Opera jidher hawn.",
            "Fejn mar il-ħin tiegħek", "Miġbur skont is-sit u maqsum aktar fejn imur l-aktar ħin.",
            "Brain rot", "Oqgħod attent", "Tajjeb", "Tajjeb — l-għażla tiegħek",
            "Brain rot: aktar minn {0} min illum, jew miftuħ {1} darbiet jew aktar u aktar minn {2} min.",
            "Miftuħ {0}×", "Siti oħra, inqas minn minuta kull wieħed: {0}", "Fi “{0}”", "Mhux brain rot", "Ħassar",
            "Il-modalità stretta hija mitfija", "Issakkar ir-regoli tiegħek għal ftit żmien, biex ma tkunx tista' ttaffihom, tissospendihom jew tneħħihom.", "Ixgħel il-modalità stretta",
            "Il-kisbiet tal-lum", "Nadif s'issa", "Inqas minn {0} min ta' brain rot illum", "Bla eċċessi", "Ebda sit 'il fuq minn {0} min",
            "Żammejt sod", "L-imblukkar ma ġiex sospiż illum", "Imsakkar", "Modalità stretta mixgħula llum",
        },
        ["ca"] = new[]
        {
            "Estat", "On va avui el teu temps al navegador. Només totals per lloc, desats en aquest PC i esborrats a mitjanit.",
            "Brain rot avui", "de {0} al navegador avui",
            "Encara res avui. El temps amb una pàgina al davant a Chrome, Edge, Firefox, Brave, Vivaldi o Opera apareix aquí.",
            "On ha anat el teu temps", "Agrupat per lloc i desglossat on va la major part del temps.",
            "Brain rot", "Compte", "Bé", "Bé: tu decideixes",
            "Brain rot: més de {0} min avui, o obert {1} vegades o més i més de {2} min.",
            "Obert {0}×", "Altres llocs, menys d'un minut cadascun: {0}", "A «{0}»", "No és brain rot", "Desfés",
            "El mode estricte està apagat", "Bloqueja les teves regles una estona perquè no puguis suavitzar-les, pausar-les ni treure-les.", "Activa el mode estricte",
            "Èxits d'avui", "Net fins ara", "Menys de {0} min de brain rot avui", "Sense excessos", "Cap lloc de més de {0} min",
            "Ferm", "El bloqueig no s'ha pausat avui", "Sota clau", "Mode estricte actiu avui",
        },
        ["nb"] = new[]
        {
            "Status", "Hvor nettlesertiden din går i dag. Bare summer per nettsted, lagret på denne PC-en og slettet ved midnatt.",
            "Brain rot i dag", "av {0} i nettleseren i dag",
            "Ingenting ennå i dag. Tid med en side i forgrunnen i Chrome, Edge, Firefox, Brave, Vivaldi eller Opera vises her.",
            "Hvor tiden din ble av", "Gruppert etter nettsted og delt videre der mesteparten av tiden går.",
            "Brain rot", "Pass på", "Greit", "Greit – ditt valg",
            "Brain rot: over {0} min i dag, eller åpnet {1} ganger eller mer og over {2} min.",
            "Åpnet {0}×", "Andre nettsteder, under ett minutt hver: {0}", "I «{0}»", "Ikke brain rot", "Angre",
            "Streng modus er av", "Lås reglene dine en stund, så du ikke kan løsne, pause eller fjerne dem.", "Slå på streng modus",
            "Dagens prestasjoner", "Rent så langt", "Under {0} min brain rot i dag", "Ingen binge", "Ingen nettsteder over {0} min",
            "Holdt ut", "Blokkering ikke satt på pause i dag", "Låst inne", "Streng modus på i dag",
        },
        ["tr"] = new[]
        {
            "Durum", "Bugün tarayıcı zamanın nereye gidiyor. Yalnızca site başına toplamlar; bu bilgisayarda tutulur ve gece yarısı silinir.",
            "Bugünkü brain rot", "bugün tarayıcıdaki {0} içinden",
            "Bugün henüz bir şey yok. Chrome, Edge, Firefox, Brave, Vivaldi veya Opera'da önde bir sayfayla geçen süre burada görünür.",
            "Zamanın nereye gitti", "Siteye göre gruplanır ve zamanın çoğunun gittiği yerde daha da bölünür.",
            "Brain rot", "Dikkat", "Sorun yok", "Sorun yok — senin kararın",
            "Brain rot: bugün {0} dk'dan fazla ya da {1} kez veya daha çok açılmış ve {2} dk'dan fazla.",
            "{0}× açıldı", "Diğer siteler, her biri bir dakikadan az: {0}", "“{0}” içinde", "Brain rot değil", "Geri al",
            "Katı mod kapalı", "Kurallarını bir süreliğine kilitle; böylece onları gevşetemez, duraklatamaz ya da kaldıramazsın.", "Katı modu aç",
            "Bugünün başarıları", "Şimdilik temiz", "Bugün {0} dk'dan az brain rot", "Aşırıya kaçma yok", "Hiçbir site {0} dk'yı geçmedi",
            "Dayandın", "Engelleme bugün duraklatılmadı", "Kilitli", "Katı mod bugün açık",
        },
        ["ru"] = new[]
        {
            "Статус", "Куда сегодня уходит ваше время в браузере. Только суммы по сайтам, хранятся на этом компьютере и удаляются в полночь.",
            "Brain rot сегодня", "из {0} в браузере сегодня",
            "Сегодня пока ничего. Время со страницей на переднем плане в Chrome, Edge, Firefox, Brave, Vivaldi или Opera появится здесь.",
            "Куда ушло ваше время", "Сгруппировано по сайтам и детализировано там, куда уходит больше всего времени.",
            "Brain rot", "Осторожно", "Нормально", "Нормально — ваш выбор",
            "Brain rot: сегодня больше {0} мин или открыт {1} раз и больше и больше {2} мин.",
            "Открыт {0}×", "Другие сайты, меньше минуты каждый: {0}", "В «{0}»", "Не brain rot", "Отменить",
            "Строгий режим выключен", "Заблокируйте правила на время, чтобы не смочь их ослабить, приостановить или удалить.", "Включить строгий режим",
            "Достижения дня", "Пока чисто", "Меньше {0} мин brain rot сегодня", "Без запоев", "Ни одного сайта дольше {0} мин",
            "Продержались", "Блокировка сегодня не приостанавливалась", "Под замком", "Строгий режим сегодня включён",
        },
        ["uk"] = new[]
        {
            "Стан", "Куди сьогодні йде ваш час у браузері. Лише суми за сайтами, зберігаються на цьому комп'ютері й видаляються опівночі.",
            "Brain rot сьогодні", "з {0} у браузері сьогодні",
            "Сьогодні поки нічого. Час зі сторінкою на передньому плані в Chrome, Edge, Firefox, Brave, Vivaldi чи Opera з'явиться тут.",
            "Куди пішов ваш час", "Згруповано за сайтами й деталізовано там, куди йде найбільше часу.",
            "Brain rot", "Обережно", "Нормально", "Нормально — ваш вибір",
            "Brain rot: сьогодні понад {0} хв або відкрито {1} разів і більше та понад {2} хв.",
            "Відкрито {0}×", "Інші сайти, менше хвилини кожен: {0}", "У «{0}»", "Не brain rot", "Скасувати",
            "Суворий режим вимкнено", "Заблокуйте правила на певний час, щоб не могти їх послабити, призупинити чи видалити.", "Увімкнути суворий режим",
            "Досягнення дня", "Поки чисто", "Менше {0} хв brain rot сьогодні", "Без запоїв", "Жодного сайту понад {0} хв",
            "Протрималися", "Блокування сьогодні не призупинялося", "Під замком", "Суворий режим сьогодні увімкнено",
        },
        ["bs"] = new[]
        {
            "Stanje", "Gdje danas ide vaše vrijeme u pregledniku. Samo zbirovi po stranici, sačuvani na ovom računaru i obrisani u ponoć.",
            "Brain rot danas", "od {0} u pregledniku danas",
            "Danas još ništa. Vrijeme sa stranicom u prvom planu u Chromeu, Edgeu, Firefoxu, Braveu, Vivaldiju ili Operi prikazuje se ovdje.",
            "Gdje je otišlo vaše vrijeme", "Grupisano po stranici i dodatno podijeljeno tamo gdje ide najviše vremena.",
            "Brain rot", "Oprez", "U redu", "U redu – vaš izbor",
            "Brain rot: danas više od {0} min ili otvoreno {1} puta ili više i više od {2} min.",
            "Otvoreno {0}×", "Ostale stranice, svaka manje od minute: {0}", "U „{0}“", "Nije brain rot", "Poništi",
            "Strogi način je isključen", "Zaključajte pravila na neko vrijeme da ih ne biste mogli ublažiti, pauzirati ili ukloniti.", "Uključi strogi način",
            "Današnja postignuća", "Zasad čisto", "Danas manje od {0} min brain rota", "Bez pretjerivanja", "Nijedna stranica preko {0} min",
            "Izdržali ste", "Blokiranje danas nije pauzirano", "Zaključano", "Strogi način danas uključen",
        },
        ["sr"] = new[]
        {
            "Стање", "Где данас иде ваше време у прегледачу. Само збирови по сајту, сачувани на овом рачунару и обрисани у поноћ.",
            "Brain rot данас", "од {0} у прегледачу данас",
            "Данас још ништа. Време са страницом у првом плану у Chrome-у, Edge-у, Firefox-у, Brave-у, Vivaldi-ју или Opera-и приказује се овде.",
            "Где је отишло ваше време", "Груписано по сајту и додатно подељено тамо где иде највише времена.",
            "Brain rot", "Опрез", "У реду", "У реду – ваш избор",
            "Brain rot: данас више од {0} мин или отворено {1} пута или више и више од {2} мин.",
            "Отворено {0}×", "Остали сајтови, сваки мање од минута: {0}", "У „{0}“", "Није brain rot", "Поништи",
            "Строги режим је искључен", "Закључајте правила на неко време да их не бисте могли ублажити, паузирати или уклонити.", "Укључи строги режим",
            "Данашња достигнућа", "Засад чисто", "Данас мање од {0} мин brain rot-а", "Без претеривања", "Ниједан сајт преко {0} мин",
            "Издржали сте", "Блокирање данас није паузирано", "Закључано", "Строги режим данас укључен",
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
