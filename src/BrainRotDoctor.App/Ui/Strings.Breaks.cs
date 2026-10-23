namespace BrainRotDoctor.App.Ui;

/// <summary>
/// Strings for the pause dialog (short breaks, a longer one a minute at a time)
/// and the strict-mode card that leans long. Merged into the tables by
/// <see cref="Strings.BuildTables"/>.
/// </summary>
internal static partial class Strings
{
    private static readonly string[] BreakKeys =
    {
        "take_break", "break_help", "paused_today_total", "len_n_min", "longer_break", "break_one_minute",
        "break_hold_note", "break_worth_it", "hold_to_pause", "keep_blocking", "strict_pitch", "strict_pitch_help",
        "len_until_tomorrow", "len_work_day", "recommended", "another_length", "change_mind",
    };

    // Per language, in the order of BreakKeys.
    private static readonly Dictionary<string, string[]> Breaks = new()
    {
        ["en"] = new[]
        {
            "Take a short break", "Nothing is blocked while you're paused. When it ends, blocking comes back by itself.", "You've paused {0} today", "{0} min", "Longer…", "Longer pauses go up one minute at a time.",
            "Over {0} minutes you have to hold the button to start.", "{0} minutes of feeds. Is it worth it?", "Hold to pause until {0}", "Keep blocking", "Lock in. Longer works better.", "While it's on, nothing can be paused or switched off until it ends. That's the point.",
            "Until tomorrow", "Rest of the work day", "Recommended", "Another length…", "You get {0} seconds to change your mind after you click.",
        },
        ["de"] = new[]
        {
            "Kurze Pause machen", "Während der Pause wird nichts blockiert. Danach blockiert die App von selbst wieder.", "Heute schon {0} pausiert", "{0} Min.", "Länger …", "Längere Pausen gehen nur Minute für Minute.",
            "Über {0} Minuten musst du die Taste gedrückt halten.", "{0} Minuten Feeds. Ist es das wert?", "Halten, um bis {0} zu pausieren", "Weiter blockieren", "Zieh es durch. Länger wirkt besser.", "Solange er läuft, lässt sich nichts pausieren oder abschalten. Genau darum geht es.",
            "Bis morgen", "Rest des Arbeitstags", "Empfohlen", "Andere Dauer …", "Nach dem Klick hast du {0} Sekunden, um es dir anders zu überlegen.",
        },
        ["fr"] = new[]
        {
            "Faire une courte pause", "Rien n'est bloqué pendant la pause. À la fin, le blocage reprend tout seul.", "Déjà {0} de pause aujourd'hui", "{0} min", "Plus long…", "Les pauses plus longues avancent minute par minute.",
            "Au-delà de {0} minutes, il faut maintenir le bouton pour commencer.", "{0} minutes de fils d'actualité. Ça en vaut la peine ?", "Maintenir pour une pause jusqu'à {0}", "Continuer à bloquer", "Engagez-vous. Plus c'est long, mieux c'est.", "Tant qu'il est actif, rien ne peut être mis en pause ni désactivé avant la fin. C'est tout l'intérêt.",
            "Jusqu'à demain", "Le reste de la journée de travail", "Recommandé", "Une autre durée…", "Après le clic, vous avez {0} secondes pour changer d'avis.",
        },
        ["es"] = new[]
        {
            "Tomar un descanso corto", "Durante la pausa no se bloquea nada. Al terminar, el bloqueo vuelve solo.", "Hoy ya llevas {0} en pausa", "{0} min", "Más tiempo…", "Las pausas más largas suben de minuto en minuto.",
            "Con más de {0} minutos tienes que mantener pulsado el botón para empezar.", "{0} minutos de feeds. ¿Vale la pena?", "Mantén pulsado para pausar hasta las {0}", "Seguir bloqueando", "Comprométete. Cuanto más largo, mejor.", "Mientras está activo, nada se puede pausar ni desactivar hasta que termine. De eso se trata.",
            "Hasta mañana", "El resto de la jornada", "Recomendado", "Otra duración…", "Tras hacer clic tienes {0} segundos para cambiar de opinión.",
        },
        ["it"] = new[]
        {
            "Fai una breve pausa", "In pausa non viene bloccato nulla. Alla fine, il blocco riprende da solo.", "Oggi già {0} di pausa", "{0} min", "Più a lungo…", "Le pause più lunghe salgono un minuto alla volta.",
            "Oltre {0} minuti devi tenere premuto il pulsante per iniziare.", "{0} minuti di feed. Ne vale la pena?", "Tieni premuto per la pausa fino alle {0}", "Continua a bloccare", "Impegnati. Più a lungo funziona meglio.", "Finché è attiva, niente si può mettere in pausa o spegnere fino alla fine. È proprio questo il punto.",
            "Fino a domani", "Il resto della giornata lavorativa", "Consigliato", "Un'altra durata…", "Dopo il clic hai {0} secondi per cambiare idea.",
        },
        ["pt"] = new[]
        {
            "Fazer uma pausa curta", "Durante a pausa nada é bloqueado. Quando acaba, o bloqueio volta sozinho.", "Já fez {0} de pausa hoje", "{0} min", "Mais tempo…", "Pausas mais longas sobem um minuto de cada vez.",
            "Acima de {0} minutos tem de manter o botão premido para começar.", "{0} minutos de feeds. Vale a pena?", "Mantenha premido para pausar até às {0}", "Continuar a bloquear", "Comprometa-se. Quanto mais tempo, melhor.", "Enquanto está ligado, nada pode ser pausado nem desligado até terminar. É esse o objetivo.",
            "Até amanhã", "O resto do dia de trabalho", "Recomendado", "Outra duração…", "Depois de clicar tem {0} segundos para mudar de ideias.",
        },
        ["nl"] = new[]
        {
            "Even kort pauzeren", "Tijdens de pauze wordt niets geblokkeerd. Daarna blokkeert de app vanzelf weer.", "Vandaag al {0} gepauzeerd", "{0} min", "Langer…", "Langere pauzes gaan minuut voor minuut omhoog.",
            "Boven {0} minuten moet je de knop ingedrukt houden om te starten.", "{0} minuten feeds. Is het dat waard?", "Houd ingedrukt om te pauzeren tot {0}", "Blijven blokkeren", "Zet door. Langer werkt beter.", "Zolang hij aan staat, kan niets worden gepauzeerd of uitgezet tot hij afloopt. Daar gaat het om.",
            "Tot morgen", "De rest van de werkdag", "Aanbevolen", "Andere duur…", "Na het klikken heb je {0} seconden om je te bedenken.",
        },
        ["pl"] = new[]
        {
            "Zrób krótką przerwę", "W czasie pauzy nic nie jest blokowane. Gdy się skończy, blokowanie wraca samo.", "Dziś już {0} pauzy", "{0} min", "Dłużej…", "Dłuższe pauzy rosną o minutę naraz.",
            "Powyżej {0} minut trzeba przytrzymać przycisk, żeby zacząć.", "{0} min przewijania. Warto?", "Przytrzymaj, aby wstrzymać do {0}", "Blokuj dalej", "Zaangażuj się. Dłużej działa lepiej.", "Gdy jest włączony, niczego nie da się wstrzymać ani wyłączyć do końca. O to właśnie chodzi.",
            "Do jutra", "Reszta dnia pracy", "Polecane", "Inny czas…", "Po kliknięciu masz {0} s, żeby zmienić zdanie.",
        },
        ["sv"] = new[]
        {
            "Ta en kort paus", "Inget blockeras medan du pausar. När pausen tar slut blockeras det igen av sig självt.", "Pausat {0} i dag", "{0} min", "Längre…", "Längre pauser ökar en minut i taget.",
            "Över {0} minuter måste du hålla in knappen för att starta.", "{0} minuter flöden. Är det värt det?", "Håll in för att pausa till {0}", "Fortsätt blockera", "Kör hårt. Längre fungerar bättre.", "Medan det är på kan inget pausas eller stängas av förrän det tar slut. Det är hela poängen.",
            "Till i morgon", "Resten av arbetsdagen", "Rekommenderas", "En annan längd…", "Efter klicket har du {0} sekunder på dig att ångra dig.",
        },
        ["da"] = new[]
        {
            "Hold en kort pause", "Intet blokeres, mens du holder pause. Når den slutter, blokeres der igen af sig selv.", "Holdt pause {0} i dag", "{0} min", "Længere…", "Længere pauser stiger ét minut ad gangen.",
            "Over {0} minutter skal du holde knappen nede for at starte.", "{0} minutters feeds. Er det det værd?", "Hold nede for pause til {0}", "Bliv ved med at blokere", "Bind dig. Længere virker bedre.", "Mens den er slået til, kan intet sættes på pause eller slås fra, før den slutter. Det er hele pointen.",
            "Til i morgen", "Resten af arbejdsdagen", "Anbefalet", "En anden længde…", "Efter klikket har du {0} sekunder til at fortryde.",
        },
        ["fi"] = new[]
        {
            "Pidä lyhyt tauko", "Tauon aikana mitään ei estetä. Kun se päättyy, esto palaa itsestään.", "Taukoja tänään {0}", "{0} min", "Pidempi…", "Pidemmät tauot kasvavat minuutti kerrallaan.",
            "Yli {0} minuutin tauossa painiketta on pidettävä pohjassa.", "{0} minuuttia syötteitä. Onko se sen arvoista?", "Pidä pohjassa: tauko klo {0} asti", "Jatka estämistä", "Sitoudu. Pidempi toimii paremmin.", "Kun se on päällä, mitään ei voi keskeyttää tai sammuttaa ennen kuin se päättyy. Siinä koko idea.",
            "Huomiseen", "Loppu työpäivä", "Suositeltu", "Muu kesto…", "Napsautuksen jälkeen sinulla on {0} sekuntia aikaa muuttaa mielesi.",
        },
        ["cs"] = new[]
        {
            "Dát si krátkou pauzu", "Během pauzy se nic neblokuje. Až skončí, blokování se samo vrátí.", "Dnes už pauza {0}", "{0} min", "Déle…", "Delší pauzy přibývají po minutě.",
            "Nad {0} minut musíte tlačítko podržet, aby pauza začala.", "{0} min feedů. Stojí to za to?", "Podržte pro pauzu do {0}", "Dál blokovat", "Vydržte. Déle funguje lépe.", "Dokud běží, nic nejde pozastavit ani vypnout, dokud neskončí. O to právě jde.",
            "Do zítřka", "Zbytek pracovního dne", "Doporučeno", "Jiná délka…", "Po kliknutí máte {0} s na rozmyšlenou.",
        },
        ["sk"] = new[]
        {
            "Dať si krátku pauzu", "Počas pauzy sa nič neblokuje. Keď skončí, blokovanie sa samo vráti.", "Dnes už pauza {0}", "{0} min", "Dlhšie…", "Dlhšie pauzy pribúdajú po minúte.",
            "Nad {0} minút musíte tlačidlo podržať, aby pauza začala.", "{0} min feedov. Stojí to za to?", "Podržte pre pauzu do {0}", "Ďalej blokovať", "Vydržte. Dlhšie funguje lepšie.", "Kým beží, nič sa nedá pozastaviť ani vypnúť, kým neskončí. O to práve ide.",
            "Do zajtra", "Zvyšok pracovného dňa", "Odporúčané", "Iná dĺžka…", "Po kliknutí máte {0} s na rozmyslenie.",
        },
        ["sl"] = new[]
        {
            "Vzemi si kratek premor", "Med premorom ni nič blokirano. Ko se konča, se blokiranje samo vrne.", "Danes že {0} premora", "{0} min", "Dlje…", "Daljši premori rastejo po minuto.",
            "Nad {0} minut moraš gumb držati, da se premor začne.", "{0} min objav. Je vredno?", "Drži za premor do {0}", "Še naprej blokiraj", "Vztrajaj. Dlje deluje bolje.", "Ko je vklopljen, ničesar ni mogoče zaustaviti ali izklopiti, dokler se ne konča. Prav to je bistvo.",
            "Do jutri", "Preostanek delovnega dne", "Priporočeno", "Drugačno trajanje…", "Po kliku imaš {0} s, da si premisliš.",
        },
        ["hr"] = new[]
        {
            "Uzmi kratku pauzu", "Tijekom pauze ništa nije blokirano. Kad završi, blokiranje se samo vraća.", "Danas već {0} pauze", "{0} min", "Duže…", "Duže pauze rastu minutu po minutu.",
            "Iznad {0} minuta moraš držati gumb da pauza počne.", "{0} min feedova. Isplati li se?", "Drži za pauzu do {0}", "Nastavi blokirati", "Drži se. Duže djeluje bolje.", "Dok je uključen, ništa se ne može pauzirati ni isključiti dok ne završi. Upravo je to poanta.",
            "Do sutra", "Ostatak radnog dana", "Preporučeno", "Drugo trajanje…", "Nakon klika imaš {0} s da se predomisliš.",
        },
        ["ro"] = new[]
        {
            "Ia o pauză scurtă", "În pauză nu se blochează nimic. Când se termină, blocarea revine singură.", "Azi ai luat deja {0} de pauză", "{0} min", "Mai mult…", "Pauzele mai lungi cresc câte un minut.",
            "Peste {0} minute trebuie să ții apăsat butonul ca să înceapă.", "{0} minute de feeduri. Merită?", "Ține apăsat pentru pauză până la {0}", "Continuă blocarea", "Angajează-te. Mai lung funcționează mai bine.", "Cât timp e pornit, nimic nu poate fi pus pe pauză sau oprit până se termină. Asta e ideea.",
            "Până mâine", "Restul zilei de lucru", "Recomandat", "Altă durată…", "După clic ai {0} secunde să te răzgândești.",
        },
        ["hu"] = new[]
        {
            "Tarts egy rövid szünetet", "Szünet alatt semmi sincs letiltva. Ha véget ér, a tiltás magától visszaáll.", "Ma már {0} szünet", "{0} perc", "Hosszabb…", "A hosszabb szünet percenként nő.",
            "{0} perc fölött nyomva kell tartanod a gombot az indításhoz.", "{0} perc hírfolyam. Megéri?", "Tartsd nyomva: szünet eddig: {0}", "Tiltás folytatása", "Tarts ki. A hosszabb jobban működik.", "Amíg be van kapcsolva, semmit nem lehet szüneteltetni vagy kikapcsolni, amíg le nem jár. Pont ez a lényeg.",
            "Holnapig", "A munkanap hátralévő része", "Ajánlott", "Más időtartam…", "Kattintás után {0} másodperced van meggondolni magad.",
        },
        ["el"] = new[]
        {
            "Κάνε ένα σύντομο διάλειμμα", "Κατά την παύση δεν μπλοκάρεται τίποτα. Όταν τελειώσει, το μπλοκάρισμα επιστρέφει μόνο του.", "Σήμερα ήδη {0} σε παύση", "{0} λεπ.", "Περισσότερο…", "Οι μεγαλύτερες παύσεις ανεβαίνουν ένα λεπτό τη φορά.",
            "Πάνω από {0} λεπτά πρέπει να κρατήσεις πατημένο το κουμπί για να ξεκινήσει.", "{0} λεπτά ροών. Αξίζει;", "Κράτα πατημένο για παύση έως {0}", "Συνέχισε το μπλοκάρισμα", "Δέσμευσου. Το μεγαλύτερο λειτουργεί καλύτερα.", "Όσο είναι ενεργή, τίποτα δεν μπορεί να μπει σε παύση ή να απενεργοποιηθεί μέχρι να τελειώσει. Αυτό είναι το νόημα.",
            "Μέχρι αύριο", "Το υπόλοιπο της εργάσιμης", "Προτείνεται", "Άλλη διάρκεια…", "Μετά το κλικ έχεις {0} δευτερόλεπτα να το ξανασκεφτείς.",
        },
        ["bg"] = new[]
        {
            "Направи кратка почивка", "По време на паузата нищо не се блокира. Когато свърши, блокирането се връща само.", "Днес вече {0} пауза", "{0} мин", "По-дълго…", "По-дългите паузи растат с по една минута.",
            "Над {0} минути трябва да задържиш бутона, за да започне.", "{0} минути фийдове. Струва ли си?", "Задръж за пауза до {0}", "Продължи да блокираш", "Ангажирай се. По-дългото работи по-добре.", "Докато е включен, нищо не може да се спре на пауза или изключи, докато не изтече. Точно това е смисълът.",
            "До утре", "Остатъкът от работния ден", "Препоръчано", "Друга продължителност…", "След клика имаш {0} секунди да се откажеш.",
        },
        ["lt"] = new[]
        {
            "Padaryk trumpą pertrauką", "Pauzės metu niekas neblokuojama. Kai ji baigiasi, blokavimas grįžta pats.", "Šiandien jau {0} pauzės", "{0} min", "Ilgiau…", "Ilgesnės pauzės didėja po minutę.",
            "Ilgesnei nei {0} min pauzei mygtuką reikia palaikyti.", "{0} min srautų. Ar verta?", "Palaikyk, kad pristabdytum iki {0}", "Toliau blokuoti", "Įsipareigok. Ilgiau veikia geriau.", "Kol jis įjungtas, nieko negalima pristabdyti ar išjungti, kol nesibaigs. Tai ir yra esmė.",
            "Iki rytojaus", "Likusi darbo dienos dalis", "Rekomenduojama", "Kita trukmė…", "Paspaudęs turi {0} s apsigalvoti.",
        },
        ["lv"] = new[]
        {
            "Paņem īsu pauzi", "Pauzes laikā nekas netiek bloķēts. Kad tā beidzas, bloķēšana atsākas pati.", "Šodien jau {0} pauzē", "{0} min", "Ilgāk…", "Garākas pauzes pieaug pa minūtei.",
            "Virs {0} minūtēm poga jātur nospiesta, lai sāktu.", "{0} min plūsmu. Vai tas ir tā vērts?", "Turi, lai pauzētu līdz {0}", "Turpināt bloķēt", "Apņemies. Ilgāk darbojas labāk.", "Kamēr tas ir ieslēgts, neko nevar pauzēt vai izslēgt, līdz tas beidzas. Tieši tā ir jēga.",
            "Līdz rītdienai", "Atlikusī darba diena", "Ieteicams", "Cits ilgums…", "Pēc klikšķa tev ir {0} s, lai pārdomātu.",
        },
        ["et"] = new[]
        {
            "Tee lühike paus", "Pausi ajal ei blokeerita midagi. Kui see lõpeb, tuleb blokeerimine ise tagasi.", "Täna juba {0} pausil", "{0} min", "Pikem…", "Pikemad pausid kasvavad minuti kaupa.",
            "Üle {0} minuti pead alustamiseks nuppu all hoidma.", "{0} minutit vooge. Kas see on seda väärt?", "Hoia all: paus kuni {0}", "Jätka blokeerimist", "Pühendu. Pikem toimib paremini.", "Kui see on sees, ei saa midagi peatada ega välja lülitada enne, kui see lõpeb. Selles ongi mõte.",
            "Homseni", "Ülejäänud tööpäev", "Soovitatud", "Muu kestus…", "Pärast klõpsu on sul {0} sekundit meelt muuta.",
        },
        ["ga"] = new[]
        {
            "Glac sos gearr", "Ní bhlocáiltear aon rud le linn sosa. Nuair a chríochnaíonn sé, tosaíonn an blocáil arís leis féin.", "{0} de shos inniu cheana", "{0} nóim.", "Níos faide…", "Méadaíonn sosanna níos faide nóiméad ar nóiméad.",
            "Os cionn {0} nóiméad, caithfidh tú an cnaipe a choinneáil síos chun tosú.", "{0} nóiméad d'fhothaí. An fiú é?", "Coinnigh síos chun sos a ghlacadh go dtí {0}", "Lean ar aghaidh ag blocáil", "Tiomnaigh. Is fearr níos faide.", "Fad is atá sé ar siúl, ní féidir aon rud a chur ar sos ná a mhúchadh go dtí go gcríochnaíonn sé. Sin an pointe.",
            "Go dtí amárach", "An chuid eile den lá oibre", "Molta", "Fad eile…", "Tar éis duit cliceáil, tá {0} soicind agat chun d'intinn a athrú.",
        },
        ["mt"] = new[]
        {
            "Ħu waqfa qasira", "Xejn ma jiġi mblukkat waqt il-waqfa. Meta tispiċċa, l-imblukkar jerġa' lura waħdu.", "Diġà {0} waqfa llum", "{0} min", "Itwal…", "Waqfiet itwal jiżdiedu minuta minuta.",
            "Aktar minn {0} minuta trid iżżomm il-buttuna magħfusa biex tibda.", "{0} minuta ta' feeds. Tiswa?", "Żomm biex tieqaf sa {0}", "Kompli mblokka", "Impenja ruħek. Itwal jaħdem aħjar.", "Sakemm ikun mixgħul, xejn ma jista' jitwaqqaf jew jintefa sakemm jispiċċa. Dak hu l-punt.",
            "Sa għada", "Il-bqija tal-jum tax-xogħol", "Rakkomandat", "Tul ieħor…", "Wara li tikklikkja għandek {0} sekondi biex tbiddel fehmtek.",
        },
        ["ca"] = new[]
        {
            "Fes una pausa curta", "Durant la pausa no es bloqueja res. Quan s'acaba, el bloqueig torna tot sol.", "Avui ja portes {0} en pausa", "{0} min", "Més temps…", "Les pauses més llargues pugen minut a minut.",
            "Amb més de {0} minuts has de mantenir premut el botó per començar.", "{0} minuts de feeds. Val la pena?", "Mantén premut per pausar fins a les {0}", "Continua bloquejant", "Compromet-t'hi. Com més llarg, millor.", "Mentre està activat, res no es pot pausar ni desactivar fins que acabi. D'això es tracta.",
            "Fins demà", "La resta de la jornada", "Recomanat", "Una altra durada…", "Després de fer clic tens {0} segons per canviar d'opinió.",
        },
        ["nb"] = new[]
        {
            "Ta en kort pause", "Ingenting blokkeres mens du har pause. Når den er over, blokkeres det igjen av seg selv.", "Pause i {0} i dag", "{0} min", "Lengre…", "Lengre pauser øker ett minutt om gangen.",
            "Over {0} minutter må du holde knappen inne for å starte.", "{0} minutter med feeder. Er det verdt det?", "Hold inne for pause til {0}", "Fortsett å blokkere", "Bind deg. Lengre virker bedre.", "Mens den er på, kan ingenting settes på pause eller slås av før den er over. Det er hele poenget.",
            "Til i morgen", "Resten av arbeidsdagen", "Anbefalt", "En annen lengde…", "Etter klikket har du {0} sekunder på å angre.",
        },
        ["tr"] = new[]
        {
            "Kısa bir mola ver", "Duraklatma sırasında hiçbir şey engellenmez. Bitince engelleme kendiliğinden geri gelir.", "Bugün zaten {0} duraklattın", "{0} dk", "Daha uzun…", "Daha uzun molalar dakika dakika artar.",
            "{0} dakikanın üzerinde başlatmak için düğmeyi basılı tutmalısın.", "{0} dakika akış. Buna değer mi?", "{0} saatine kadar duraklatmak için basılı tut", "Engellemeye devam et", "Kararlı ol. Uzun olan daha iyi işler.", "Açıkken bitene kadar hiçbir şey duraklatılamaz veya kapatılamaz. Asıl mesele de bu.",
            "Yarına kadar", "İş gününün geri kalanı", "Önerilen", "Başka bir süre…", "Tıkladıktan sonra fikrini değiştirmek için {0} saniyen var.",
        },
        ["ru"] = new[]
        {
            "Сделать короткий перерыв", "Во время паузы ничего не блокируется. Когда она закончится, блокировка вернётся сама.", "Сегодня уже {0} на паузе", "{0} мин", "Дольше…", "Более длинные паузы растут по минуте.",
            "Больше {0} мин — нужно удерживать кнопку, чтобы начать.", "{0} мин лент. Оно того стоит?", "Удерживайте для паузы до {0}", "Продолжать блокировать", "Держитесь. Дольше — лучше.", "Пока он включён, ничего нельзя поставить на паузу или выключить до конца. В этом и смысл.",
            "До завтра", "Остаток рабочего дня", "Рекомендуем", "Другая длительность…", "После нажатия у вас {0} с, чтобы передумать.",
        },
        ["uk"] = new[]
        {
            "Зробити коротку перерву", "Під час паузи нічого не блокується. Коли вона скінчиться, блокування повернеться саме.", "Сьогодні вже {0} на паузі", "{0} хв", "Довше…", "Довші паузи ростуть по хвилині.",
            "Понад {0} хв — треба утримувати кнопку, щоб почати.", "{0} хв стрічок. Чи варто?", "Утримуйте для паузи до {0}", "Блокувати далі", "Тримайтеся. Довше — краще.", "Поки він увімкнений, нічого не можна поставити на паузу чи вимкнути до кінця. У цьому й суть.",
            "До завтра", "Решта робочого дня", "Рекомендовано", "Інша тривалість…", "Після натискання у вас {0} с, щоб передумати.",
        },
        ["bs"] = new[]
        {
            "Uzmi kratku pauzu", "Tokom pauze ništa nije blokirano. Kad završi, blokiranje se samo vraća.", "Danas već {0} pauze", "{0} min", "Duže…", "Duže pauze rastu minutu po minutu.",
            "Iznad {0} minuta moraš držati dugme da pauza počne.", "{0} min feedova. Isplati li se?", "Drži za pauzu do {0}", "Nastavi blokirati", "Drži se. Duže djeluje bolje.", "Dok je uključen, ništa se ne može pauzirati ni isključiti dok ne završi. Upravo je to poenta.",
            "Do sutra", "Ostatak radnog dana", "Preporučeno", "Drugo trajanje…", "Nakon klika imaš {0} s da se predomisliš.",
        },
        ["sr"] = new[]
        {
            "Узми кратку паузу", "Током паузе ништа није блокирано. Кад се заврши, блокирање се само враћа.", "Данас већ {0} паузе", "{0} мин", "Дуже…", "Дуже паузе расту минут по минут.",
            "Изнад {0} минута мораш да држиш дугме да би пауза почела.", "{0} мин објава. Да ли се исплати?", "Држи за паузу до {0}", "Настави да блокираш", "Издржи. Дуже делује боље.", "Док је укључен, ништа не може да се паузира ни искључи док се не заврши. Управо у томе је поента.",
            "До сутра", "Остатак радног дана", "Препоручено", "Друго трајање…", "После клика имаш {0} с да се предомислиш.",
        },
    };

    private static void MergeBreaks(Dictionary<string, Dictionary<string, string>> tables)
    {
        foreach ((string code, string[] text) in Breaks)
        {
            if (tables.TryGetValue(code, out Dictionary<string, string>? table))
            {
                for (int i = 0; i < BreakKeys.Length; i++)
                {
                    table[BreakKeys[i]] = text[i];
                }
            }
        }
    }
}
