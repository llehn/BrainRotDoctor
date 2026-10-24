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
        "take_break", "break_help", "paused_today_total", "len_n_min", "longer_break", "break_worth_it",
        "keep_blocking", "strict_pitch", "strict_pitch_help", "len_until_tomorrow", "len_work_day", "recommended",
        "another_length", "strict_starts_in",
    };

    // Per language, in the order of BreakKeys.
    private static readonly Dictionary<string, string[]> Breaks = new()
    {
        ["en"] = new[]
        {
            "Take a short break", "Nothing is blocked while you're paused. When it ends, blocking comes back by itself.", "You've paused {0} today", "{0} min", "Longer…", "{0} minutes of feeds. Is it worth it?",
            "Keep blocking", "Lock in. Longer works better.", "While it's on, nothing can be paused or switched off until it ends. That's the point.", "Until tomorrow", "Rest of the work day", "Recommended",
            "Another length…", "Strict mode starts in {0} s",
        },
        ["de"] = new[]
        {
            "Kurze Pause machen", "Während der Pause wird nichts blockiert. Danach blockiert die App von selbst wieder.", "Heute schon {0} pausiert", "{0} Min.", "Länger …", "{0} Minuten Feeds. Ist es das wert?",
            "Weiter blockieren", "Zieh es durch. Länger wirkt besser.", "Solange er läuft, lässt sich nichts pausieren oder abschalten. Genau darum geht es.", "Bis morgen", "Rest des Arbeitstags", "Empfohlen",
            "Andere Dauer …", "Strikter Modus startet in {0} s",
        },
        ["fr"] = new[]
        {
            "Faire une courte pause", "Rien n'est bloqué pendant la pause. À la fin, le blocage reprend tout seul.", "Déjà {0} de pause aujourd'hui", "{0} min", "Plus long…", "{0} minutes de fils d'actualité. Ça en vaut la peine ?",
            "Continuer à bloquer", "Engagez-vous. Plus c'est long, mieux c'est.", "Tant qu'il est actif, rien ne peut être mis en pause ni désactivé avant la fin. C'est tout l'intérêt.", "Jusqu'à demain", "Le reste de la journée de travail", "Recommandé",
            "Une autre durée…", "Le mode strict démarre dans {0} s",
        },
        ["es"] = new[]
        {
            "Tomar un descanso corto", "Durante la pausa no se bloquea nada. Al terminar, el bloqueo vuelve solo.", "Hoy ya llevas {0} en pausa", "{0} min", "Más tiempo…", "{0} minutos de feeds. ¿Vale la pena?",
            "Seguir bloqueando", "Comprométete. Cuanto más largo, mejor.", "Mientras está activo, nada se puede pausar ni desactivar hasta que termine. De eso se trata.", "Hasta mañana", "El resto de la jornada", "Recomendado",
            "Otra duración…", "El modo estricto empieza en {0} s",
        },
        ["it"] = new[]
        {
            "Fai una breve pausa", "In pausa non viene bloccato nulla. Alla fine, il blocco riprende da solo.", "Oggi già {0} di pausa", "{0} min", "Più a lungo…", "{0} minuti di feed. Ne vale la pena?",
            "Continua a bloccare", "Impegnati. Più a lungo funziona meglio.", "Finché è attiva, niente si può mettere in pausa o spegnere fino alla fine. È proprio questo il punto.", "Fino a domani", "Il resto della giornata lavorativa", "Consigliato",
            "Un'altra durata…", "La modalità rigida parte tra {0} s",
        },
        ["pt"] = new[]
        {
            "Fazer uma pausa curta", "Durante a pausa nada é bloqueado. Quando acaba, o bloqueio volta sozinho.", "Já fez {0} de pausa hoje", "{0} min", "Mais tempo…", "{0} minutos de feeds. Vale a pena?",
            "Continuar a bloquear", "Comprometa-se. Quanto mais tempo, melhor.", "Enquanto está ligado, nada pode ser pausado nem desligado até terminar. É esse o objetivo.", "Até amanhã", "O resto do dia de trabalho", "Recomendado",
            "Outra duração…", "O modo rigoroso começa em {0} s",
        },
        ["nl"] = new[]
        {
            "Even kort pauzeren", "Tijdens de pauze wordt niets geblokkeerd. Daarna blokkeert de app vanzelf weer.", "Vandaag al {0} gepauzeerd", "{0} min", "Langer…", "{0} minuten feeds. Is het dat waard?",
            "Blijven blokkeren", "Zet door. Langer werkt beter.", "Zolang hij aan staat, kan niets worden gepauzeerd of uitgezet tot hij afloopt. Daar gaat het om.", "Tot morgen", "De rest van de werkdag", "Aanbevolen",
            "Andere duur…", "Strikte modus start over {0} s",
        },
        ["pl"] = new[]
        {
            "Zrób krótką przerwę", "W czasie pauzy nic nie jest blokowane. Gdy się skończy, blokowanie wraca samo.", "Dziś już {0} pauzy", "{0} min", "Dłużej…", "{0} min przewijania. Warto?",
            "Blokuj dalej", "Zaangażuj się. Dłużej działa lepiej.", "Gdy jest włączony, niczego nie da się wstrzymać ani wyłączyć do końca. O to właśnie chodzi.", "Do jutra", "Reszta dnia pracy", "Polecane",
            "Inny czas…", "Tryb rygorystyczny włączy się za {0} s",
        },
        ["sv"] = new[]
        {
            "Ta en kort paus", "Inget blockeras medan du pausar. När pausen tar slut blockeras det igen av sig självt.", "Pausat {0} i dag", "{0} min", "Längre…", "{0} minuter flöden. Är det värt det?",
            "Fortsätt blockera", "Kör hårt. Längre fungerar bättre.", "Medan det är på kan inget pausas eller stängas av förrän det tar slut. Det är hela poängen.", "Till i morgon", "Resten av arbetsdagen", "Rekommenderas",
            "En annan längd…", "Strikt läge startar om {0} s",
        },
        ["da"] = new[]
        {
            "Hold en kort pause", "Intet blokeres, mens du holder pause. Når den slutter, blokeres der igen af sig selv.", "Holdt pause {0} i dag", "{0} min", "Længere…", "{0} minutters feeds. Er det det værd?",
            "Bliv ved med at blokere", "Bind dig. Længere virker bedre.", "Mens den er slået til, kan intet sættes på pause eller slås fra, før den slutter. Det er hele pointen.", "Til i morgen", "Resten af arbejdsdagen", "Anbefalet",
            "En anden længde…", "Streng tilstand starter om {0} s",
        },
        ["fi"] = new[]
        {
            "Pidä lyhyt tauko", "Tauon aikana mitään ei estetä. Kun se päättyy, esto palaa itsestään.", "Taukoja tänään {0}", "{0} min", "Pidempi…", "{0} minuuttia syötteitä. Onko se sen arvoista?",
            "Jatka estämistä", "Sitoudu. Pidempi toimii paremmin.", "Kun se on päällä, mitään ei voi keskeyttää tai sammuttaa ennen kuin se päättyy. Siinä koko idea.", "Huomiseen", "Loppu työpäivä", "Suositeltu",
            "Muu kesto…", "Tiukka tila alkaa {0} s kuluttua",
        },
        ["cs"] = new[]
        {
            "Dát si krátkou pauzu", "Během pauzy se nic neblokuje. Až skončí, blokování se samo vrátí.", "Dnes už pauza {0}", "{0} min", "Déle…", "{0} min feedů. Stojí to za to?",
            "Dál blokovat", "Vydržte. Déle funguje lépe.", "Dokud běží, nic nejde pozastavit ani vypnout, dokud neskončí. O to právě jde.", "Do zítřka", "Zbytek pracovního dne", "Doporučeno",
            "Jiná délka…", "Přísný režim začne za {0} s",
        },
        ["sk"] = new[]
        {
            "Dať si krátku pauzu", "Počas pauzy sa nič neblokuje. Keď skončí, blokovanie sa samo vráti.", "Dnes už pauza {0}", "{0} min", "Dlhšie…", "{0} min feedov. Stojí to za to?",
            "Ďalej blokovať", "Vydržte. Dlhšie funguje lepšie.", "Kým beží, nič sa nedá pozastaviť ani vypnúť, kým neskončí. O to práve ide.", "Do zajtra", "Zvyšok pracovného dňa", "Odporúčané",
            "Iná dĺžka…", "Prísny režim sa spustí o {0} s",
        },
        ["sl"] = new[]
        {
            "Vzemi si kratek premor", "Med premorom ni nič blokirano. Ko se konča, se blokiranje samo vrne.", "Danes že {0} premora", "{0} min", "Dlje…", "{0} min objav. Je vredno?",
            "Še naprej blokiraj", "Vztrajaj. Dlje deluje bolje.", "Ko je vklopljen, ničesar ni mogoče zaustaviti ali izklopiti, dokler se ne konča. Prav to je bistvo.", "Do jutri", "Preostanek delovnega dne", "Priporočeno",
            "Drugačno trajanje…", "Strogi način se začne čez {0} s",
        },
        ["hr"] = new[]
        {
            "Uzmi kratku pauzu", "Tijekom pauze ništa nije blokirano. Kad završi, blokiranje se samo vraća.", "Danas već {0} pauze", "{0} min", "Duže…", "{0} min feedova. Isplati li se?",
            "Nastavi blokirati", "Drži se. Duže djeluje bolje.", "Dok je uključen, ništa se ne može pauzirati ni isključiti dok ne završi. Upravo je to poanta.", "Do sutra", "Ostatak radnog dana", "Preporučeno",
            "Drugo trajanje…", "Strogi način počinje za {0} s",
        },
        ["ro"] = new[]
        {
            "Ia o pauză scurtă", "În pauză nu se blochează nimic. Când se termină, blocarea revine singură.", "Azi ai luat deja {0} de pauză", "{0} min", "Mai mult…", "{0} minute de feeduri. Merită?",
            "Continuă blocarea", "Angajează-te. Mai lung funcționează mai bine.", "Cât timp e pornit, nimic nu poate fi pus pe pauză sau oprit până se termină. Asta e ideea.", "Până mâine", "Restul zilei de lucru", "Recomandat",
            "Altă durată…", "Modul strict pornește în {0} s",
        },
        ["hu"] = new[]
        {
            "Tarts egy rövid szünetet", "Szünet alatt semmi sincs letiltva. Ha véget ér, a tiltás magától visszaáll.", "Ma már {0} szünet", "{0} perc", "Hosszabb…", "{0} perc hírfolyam. Megéri?",
            "Tiltás folytatása", "Tarts ki. A hosszabb jobban működik.", "Amíg be van kapcsolva, semmit nem lehet szüneteltetni vagy kikapcsolni, amíg le nem jár. Pont ez a lényeg.", "Holnapig", "A munkanap hátralévő része", "Ajánlott",
            "Más időtartam…", "A szigorú mód {0} mp múlva indul",
        },
        ["el"] = new[]
        {
            "Κάνε ένα σύντομο διάλειμμα", "Κατά την παύση δεν μπλοκάρεται τίποτα. Όταν τελειώσει, το μπλοκάρισμα επιστρέφει μόνο του.", "Σήμερα ήδη {0} σε παύση", "{0} λεπ.", "Περισσότερο…", "{0} λεπτά ροών. Αξίζει;",
            "Συνέχισε το μπλοκάρισμα", "Δέσμευσου. Το μεγαλύτερο λειτουργεί καλύτερα.", "Όσο είναι ενεργή, τίποτα δεν μπορεί να μπει σε παύση ή να απενεργοποιηθεί μέχρι να τελειώσει. Αυτό είναι το νόημα.", "Μέχρι αύριο", "Το υπόλοιπο της εργάσιμης", "Προτείνεται",
            "Άλλη διάρκεια…", "Η αυστηρή λειτουργία ξεκινά σε {0} δ",
        },
        ["bg"] = new[]
        {
            "Направи кратка почивка", "По време на паузата нищо не се блокира. Когато свърши, блокирането се връща само.", "Днес вече {0} пауза", "{0} мин", "По-дълго…", "{0} минути фийдове. Струва ли си?",
            "Продължи да блокираш", "Ангажирай се. По-дългото работи по-добре.", "Докато е включен, нищо не може да се спре на пауза или изключи, докато не изтече. Точно това е смисълът.", "До утре", "Остатъкът от работния ден", "Препоръчано",
            "Друга продължителност…", "Строгият режим започва след {0} с",
        },
        ["lt"] = new[]
        {
            "Padaryk trumpą pertrauką", "Pauzės metu niekas neblokuojama. Kai ji baigiasi, blokavimas grįžta pats.", "Šiandien jau {0} pauzės", "{0} min", "Ilgiau…", "{0} min srautų. Ar verta?",
            "Toliau blokuoti", "Įsipareigok. Ilgiau veikia geriau.", "Kol jis įjungtas, nieko negalima pristabdyti ar išjungti, kol nesibaigs. Tai ir yra esmė.", "Iki rytojaus", "Likusi darbo dienos dalis", "Rekomenduojama",
            "Kita trukmė…", "Griežtas režimas įsijungs po {0} s",
        },
        ["lv"] = new[]
        {
            "Paņem īsu pauzi", "Pauzes laikā nekas netiek bloķēts. Kad tā beidzas, bloķēšana atsākas pati.", "Šodien jau {0} pauzē", "{0} min", "Ilgāk…", "{0} min plūsmu. Vai tas ir tā vērts?",
            "Turpināt bloķēt", "Apņemies. Ilgāk darbojas labāk.", "Kamēr tas ir ieslēgts, neko nevar pauzēt vai izslēgt, līdz tas beidzas. Tieši tā ir jēga.", "Līdz rītdienai", "Atlikusī darba diena", "Ieteicams",
            "Cits ilgums…", "Stingrais režīms sāksies pēc {0} s",
        },
        ["et"] = new[]
        {
            "Tee lühike paus", "Pausi ajal ei blokeerita midagi. Kui see lõpeb, tuleb blokeerimine ise tagasi.", "Täna juba {0} pausil", "{0} min", "Pikem…", "{0} minutit vooge. Kas see on seda väärt?",
            "Jätka blokeerimist", "Pühendu. Pikem toimib paremini.", "Kui see on sees, ei saa midagi peatada ega välja lülitada enne, kui see lõpeb. Selles ongi mõte.", "Homseni", "Ülejäänud tööpäev", "Soovitatud",
            "Muu kestus…", "Range režiim algab {0} s pärast",
        },
        ["ga"] = new[]
        {
            "Glac sos gearr", "Ní bhlocáiltear aon rud le linn sosa. Nuair a chríochnaíonn sé, tosaíonn an blocáil arís leis féin.", "{0} de shos inniu cheana", "{0} nóim.", "Níos faide…", "{0} nóiméad d'fhothaí. An fiú é?",
            "Lean ar aghaidh ag blocáil", "Tiomnaigh. Is fearr níos faide.", "Fad is atá sé ar siúl, ní féidir aon rud a chur ar sos ná a mhúchadh go dtí go gcríochnaíonn sé. Sin an pointe.", "Go dtí amárach", "An chuid eile den lá oibre", "Molta",
            "Fad eile…", "Tosaíonn an mód dian i gceann {0} s",
        },
        ["mt"] = new[]
        {
            "Ħu waqfa qasira", "Xejn ma jiġi mblukkat waqt il-waqfa. Meta tispiċċa, l-imblukkar jerġa' lura waħdu.", "Diġà {0} waqfa llum", "{0} min", "Itwal…", "{0} minuta ta' feeds. Tiswa?",
            "Kompli mblokka", "Impenja ruħek. Itwal jaħdem aħjar.", "Sakemm ikun mixgħul, xejn ma jista' jitwaqqaf jew jintefa sakemm jispiċċa. Dak hu l-punt.", "Sa għada", "Il-bqija tal-jum tax-xogħol", "Rakkomandat",
            "Tul ieħor…", "Il-modalità stretta tibda f'{0} s",
        },
        ["ca"] = new[]
        {
            "Fes una pausa curta", "Durant la pausa no es bloqueja res. Quan s'acaba, el bloqueig torna tot sol.", "Avui ja portes {0} en pausa", "{0} min", "Més temps…", "{0} minuts de feeds. Val la pena?",
            "Continua bloquejant", "Compromet-t'hi. Com més llarg, millor.", "Mentre està activat, res no es pot pausar ni desactivar fins que acabi. D'això es tracta.", "Fins demà", "La resta de la jornada", "Recomanat",
            "Una altra durada…", "El mode estricte comença d'aquí a {0} s",
        },
        ["nb"] = new[]
        {
            "Ta en kort pause", "Ingenting blokkeres mens du har pause. Når den er over, blokkeres det igjen av seg selv.", "Pause i {0} i dag", "{0} min", "Lengre…", "{0} minutter med feeder. Er det verdt det?",
            "Fortsett å blokkere", "Bind deg. Lengre virker bedre.", "Mens den er på, kan ingenting settes på pause eller slås av før den er over. Det er hele poenget.", "Til i morgen", "Resten av arbeidsdagen", "Anbefalt",
            "En annen lengde…", "Streng modus starter om {0} s",
        },
        ["tr"] = new[]
        {
            "Kısa bir mola ver", "Duraklatma sırasında hiçbir şey engellenmez. Bitince engelleme kendiliğinden geri gelir.", "Bugün zaten {0} duraklattın", "{0} dk", "Daha uzun…", "{0} dakika akış. Buna değer mi?",
            "Engellemeye devam et", "Kararlı ol. Uzun olan daha iyi işler.", "Açıkken bitene kadar hiçbir şey duraklatılamaz veya kapatılamaz. Asıl mesele de bu.", "Yarına kadar", "İş gününün geri kalanı", "Önerilen",
            "Başka bir süre…", "Katı mod {0} sn içinde başlıyor",
        },
        ["ru"] = new[]
        {
            "Сделать короткий перерыв", "Во время паузы ничего не блокируется. Когда она закончится, блокировка вернётся сама.", "Сегодня уже {0} на паузе", "{0} мин", "Дольше…", "{0} мин лент. Оно того стоит?",
            "Продолжать блокировать", "Держитесь. Дольше — лучше.", "Пока он включён, ничего нельзя поставить на паузу или выключить до конца. В этом и смысл.", "До завтра", "Остаток рабочего дня", "Рекомендуем",
            "Другая длительность…", "Строгий режим включится через {0} с",
        },
        ["uk"] = new[]
        {
            "Зробити коротку перерву", "Під час паузи нічого не блокується. Коли вона скінчиться, блокування повернеться саме.", "Сьогодні вже {0} на паузі", "{0} хв", "Довше…", "{0} хв стрічок. Чи варто?",
            "Блокувати далі", "Тримайтеся. Довше — краще.", "Поки він увімкнений, нічого не можна поставити на паузу чи вимкнути до кінця. У цьому й суть.", "До завтра", "Решта робочого дня", "Рекомендовано",
            "Інша тривалість…", "Суворий режим увімкнеться через {0} с",
        },
        ["bs"] = new[]
        {
            "Uzmi kratku pauzu", "Tokom pauze ništa nije blokirano. Kad završi, blokiranje se samo vraća.", "Danas već {0} pauze", "{0} min", "Duže…", "{0} min feedova. Isplati li se?",
            "Nastavi blokirati", "Drži se. Duže djeluje bolje.", "Dok je uključen, ništa se ne može pauzirati ni isključiti dok ne završi. Upravo je to poenta.", "Do sutra", "Ostatak radnog dana", "Preporučeno",
            "Drugo trajanje…", "Strogi režim počinje za {0} s",
        },
        ["sr"] = new[]
        {
            "Узми кратку паузу", "Током паузе ништа није блокирано. Кад се заврши, блокирање се само враћа.", "Данас већ {0} паузе", "{0} мин", "Дуже…", "{0} мин објава. Да ли се исплати?",
            "Настави да блокираш", "Издржи. Дуже делује боље.", "Док је укључен, ништа не може да се паузира ни искључи док се не заврши. Управо у томе је поента.", "До сутра", "Остатак радног дана", "Препоручено",
            "Друго трајање…", "Строги режим почиње за {0} с",
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
