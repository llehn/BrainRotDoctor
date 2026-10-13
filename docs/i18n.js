/* BrainRotDoctor site localization.
   English is canonical; missing keys fall back to English. Values may contain
   inline HTML (set via innerHTML — all content is static, no user input). */
(function () {
  "use strict";

  var LANGS = [
    ["en", "English"],
    ["de", "Deutsch"],
    ["fr", "Français"],
    ["es", "Español"],
    ["it", "Italiano"],
    ["pt", "Português"],
    ["pl", "Polski"],
    ["ro", "Română"],
    ["tr", "Türkçe"],
    ["ru", "Русский"],
  ];

  var GH = `<a href="https://github.com/llehn/BrainRotDoctor">GitHub</a>`;

  var T = {
    en: {
      "meta.title": `BrainRotDoctor — stop the scroll, keep your messages`,
      "hero.recognise": `You open Instagram to answer one message. Forty minutes later you're still scrolling.`,
      "hero.h1": `Stop the scroll. Keep your messages.`,
      "hero.promise": `BrainRotDoctor gives Reels, Shorts and feeds a few minutes an hour, then closes them. <b>Messages and everything else you need stay open.</b>`,
      "download": `Download for Windows`,
      "free": `Free · Windows 10 and 11`,
      "holds.title": `You can't <span>switch it off</span> on a whim.`,
      "holds.why": `Extensions and screen-time limits give way the moment you want to scroll, because turning them off takes three clicks.`,
      "holds.strict": `Turn on <b>strict mode</b> for an evening or a week. Until it ends, your rules can't be changed and the app can't be closed or uninstalled. There is no early exit.`,
      "closer.title": `Take your evenings back.`,
      "footer": `BrainRotDoctor · Free and open source · ` + GH,
      "dl.meta.title": `Download BrainRotDoctor`,
      "dl.h1": `Your download is starting.`,
      "dl.ifnot": `If nothing happened, use the button below.`,
      "dl.why.title": `Windows may stop you. That's normal.`,
      "dl.why": `BrainRotDoctor isn't digitally signed, so Windows doesn't recognize the publisher. It shows a protection screen. That's Windows being cautious, not a verdict that the file is unsafe.`,
      "dl.browser": `Your browser may ask to keep the file first. Choose Keep.`,
      "dl.steps.title": `When you open it`,
      "dl.step1": `If you see <b>Windows protected your PC</b>, click <b>More info</b>.`,
      "dl.step2": `Then click <b>Run anyway</b>.`,
      "dl.win10": `On Windows 10 it looks a bit older. Same two clicks.`,
      "dl.trust": `Only continue if you downloaded it from llehn.github.io/BrainRotDoctor.`,
      "ss.title": `Windows protected your PC`,
      "ss.body": `Microsoft Defender SmartScreen prevented an unrecognized app from starting. Running this app might put your PC at risk.`,
      "ss.app": `App`,
      "ss.publisher": `Publisher`,
      "ss.unknown": `Unknown publisher`,
      "ss.more": `More info`,
      "ss.dont": `Don't run`,
      "ss.run": `Run anyway`,
    },

    de: {
      "meta.title": `BrainRotDoctor — Schluss mit dem Scrollen, Nachrichten bleiben`,
      "hero.recognise": `Du öffnest Instagram, um auf eine Nachricht zu antworten. Vierzig Minuten später scrollst du immer noch.`,
      "hero.h1": `Schluss mit dem Scrollen. Deine Nachrichten bleiben.`,
      "hero.promise": `BrainRotDoctor gibt Reels, Shorts und Feeds ein paar Minuten pro Stunde und schließt sie dann. <b>Nachrichten und alles andere, was du brauchst, bleiben offen.</b>`,
      "download": `Für Windows herunterladen`,
      "free": `Kostenlos · Windows 10 und 11`,
      "holds.title": `Du kannst es nicht <span>einfach abschalten</span>.`,
      "holds.why": `Erweiterungen und Bildschirmzeit-Limits geben nach, sobald du scrollen willst, denn sie sind mit drei Klicks aus.`,
      "holds.strict": `Schalte den <b>strengen Modus</b> für einen Abend oder eine Woche ein. Bis er endet, lassen sich deine Regeln nicht ändern und die App lässt sich weder schließen noch deinstallieren. Einen vorzeitigen Ausweg gibt es nicht.`,
      "closer.title": `Hol dir deine Abende zurück.`,
      "footer": `BrainRotDoctor · Kostenlos und quelloffen · ` + GH,
      "dl.meta.title": `BrainRotDoctor herunterladen`,
      "dl.h1": `Der Download startet.`,
      "dl.ifnot": `Falls nichts passiert, nutze den Button darunter.`,
      "dl.why.title": `Windows kann dich stoppen. Das ist normal.`,
      "dl.why": `BrainRotDoctor ist nicht digital signiert, deshalb kennt Windows den Herausgeber nicht. Es zeigt einen Schutzhinweis. Das ist Vorsicht, kein Urteil, dass die Datei unsicher ist.`,
      "dl.browser": `Dein Browser fragt vielleicht zuerst, ob die Datei behalten werden soll. Wähle Behalten.`,
      "dl.steps.title": `Wenn du die Datei öffnest`,
      "dl.step1": `Steht da <b>Windows hat den PC geschützt</b>, klicke auf <b>Weitere Informationen</b>.`,
      "dl.step2": `Dann auf <b>Trotzdem ausführen</b>.`,
      "dl.win10": `Unter Windows 10 sieht es etwas älter aus. Dieselben zwei Klicks.`,
      "dl.trust": `Mach das nur, wenn du die Datei von llehn.github.io/BrainRotDoctor hast.`,
      "ss.title": `Windows hat Ihren PC geschützt`,
      "ss.body": `Microsoft Defender SmartScreen hat verhindert, dass eine nicht erkannte App gestartet wird. Das Ausführen dieser App stellt möglicherweise ein Risiko für Ihren PC dar.`,
      "ss.app": `App`,
      "ss.publisher": `Herausgeber`,
      "ss.unknown": `Unbekannter Herausgeber`,
      "ss.more": `Weitere Informationen`,
      "ss.dont": `Nicht ausführen`,
      "ss.run": `Trotzdem ausführen`,
    },

    fr: {
      "meta.title": `BrainRotDoctor — arrêtez de scroller, gardez vos messages`,
      "hero.recognise": `Vous ouvrez Instagram pour répondre à un message. Quarante minutes plus tard, vous scrollez encore.`,
      "hero.h1": `Arrêtez de scroller. Gardez vos messages.`,
      "hero.promise": `BrainRotDoctor accorde aux Reels, aux Shorts et aux fils quelques minutes par heure, puis les ferme. <b>Les messages et tout ce dont vous avez besoin restent ouverts.</b>`,
      "download": `Télécharger pour Windows`,
      "free": `Gratuit · Windows 10 et 11`,
      "holds.title": `Impossible de <span>le couper</span> sur un coup de tête.`,
      "holds.why": `Les extensions et les limites de temps d'écran cèdent dès que vous voulez scroller, car il suffit de trois clics pour les désactiver.`,
      "holds.strict": `Activez le <b>mode strict</b> pour une soirée ou une semaine. Jusqu'à la fin, vos règles ne peuvent pas être modifiées et l'application ne peut être ni fermée ni désinstallée. Il n'y a pas de sortie anticipée.`,
      "closer.title": `Reprenez vos soirées.`,
      "footer": `BrainRotDoctor · Gratuit et open source · ` + GH,
      "dl.meta.title": `Télécharger BrainRotDoctor`,
      "dl.h1": `Le téléchargement commence.`,
      "dl.ifnot": `S'il ne démarre pas, utilisez le bouton ci-dessous.`,
      "dl.why.title": `Windows peut vous arrêter. C'est normal.`,
      "dl.why": `BrainRotDoctor n'est pas signé numériquement, donc Windows ne reconnaît pas l'éditeur. Il affiche un écran de protection. C'est de la prudence, pas un verdict que le fichier est dangereux.`,
      "dl.browser": `Votre navigateur peut d'abord demander de conserver le fichier. Choisissez Conserver.`,
      "dl.steps.title": `Quand vous l'ouvrez`,
      "dl.step1": `Si vous voyez <b>Windows a protégé votre PC</b>, cliquez sur <b>Plus d'infos</b>.`,
      "dl.step2": `Puis sur <b>Exécuter quand même</b>.`,
      "dl.win10": `Sous Windows 10, ça a l'air un peu plus ancien. Les deux mêmes clics.`,
      "dl.trust": `Ne continuez que si vous l'avez téléchargé depuis llehn.github.io/BrainRotDoctor.`,
      "ss.title": `Windows a protégé votre PC`,
      "ss.body": `Microsoft Defender SmartScreen a empêché le démarrage d'une application non reconnue. L'exécution de cette application pourrait mettre votre PC en danger.`,
      "ss.app": `Application`,
      "ss.publisher": `Éditeur`,
      "ss.unknown": `Éditeur inconnu`,
      "ss.more": `Plus d'infos`,
      "ss.dont": `Ne pas exécuter`,
      "ss.run": `Exécuter quand même`,
    },

    es: {
      "meta.title": `BrainRotDoctor — deja de hacer scroll, conserva tus mensajes`,
      "hero.recognise": `Abres Instagram para contestar un mensaje. Cuarenta minutos después sigues haciendo scroll.`,
      "hero.h1": `Deja de hacer scroll. Conserva tus mensajes.`,
      "hero.promise": `BrainRotDoctor da a los Reels, los Shorts y los feeds unos minutos por hora y luego los cierra. <b>Los mensajes y todo lo demás que necesitas siguen abiertos.</b>`,
      "download": `Descargar para Windows`,
      "free": `Gratis · Windows 10 y 11`,
      "holds.title": `No puedes <span>apagarlo</span> por un impulso.`,
      "holds.why": `Las extensiones y los límites de tiempo de pantalla ceden en cuanto quieres hacer scroll, porque se desactivan en tres clics.`,
      "holds.strict": `Activa el <b>modo estricto</b> para una tarde o una semana. Hasta que termine, tus reglas no se pueden cambiar y la app no se puede cerrar ni desinstalar. No hay salida anticipada.`,
      "closer.title": `Recupera tus tardes.`,
      "footer": `BrainRotDoctor · Gratis y de código abierto · ` + GH,
      "dl.meta.title": `Descargar BrainRotDoctor`,
      "dl.h1": `La descarga está empezando.`,
      "dl.ifnot": `Si no empieza, usa el botón de abajo.`,
      "dl.why.title": `Windows puede detenerte. Es normal.`,
      "dl.why": `BrainRotDoctor no está firmado digitalmente, así que Windows no reconoce al editor. Muestra una pantalla de protección. Es precaución, no un veredicto de que el archivo sea peligroso.`,
      "dl.browser": `El navegador puede preguntar primero si quieres conservar el archivo. Elige Conservar.`,
      "dl.steps.title": `Cuando lo abras`,
      "dl.step1": `Si ves <b>Windows ha protegido el PC</b>, pulsa <b>Más información</b>.`,
      "dl.step2": `Luego pulsa <b>Ejecutar de todas formas</b>.`,
      "dl.win10": `En Windows 10 se ve un poco más antiguo. Los mismos dos clics.`,
      "dl.trust": `Hazlo solo si lo has descargado desde llehn.github.io/BrainRotDoctor.`,
      "ss.title": `Windows ha protegido el PC`,
      "ss.body": `Microsoft Defender SmartScreen impidió que se iniciara una aplicación no reconocida. La ejecución de esta aplicación podría poner en riesgo el PC.`,
      "ss.app": `Aplicación`,
      "ss.publisher": `Editor`,
      "ss.unknown": `Editor desconocido`,
      "ss.more": `Más información`,
      "ss.dont": `No ejecutar`,
      "ss.run": `Ejecutar de todas formas`,
    },

    it: {
      "meta.title": `BrainRotDoctor — basta scrollare, i messaggi restano`,
      "hero.recognise": `Apri Instagram per rispondere a un messaggio. Quaranta minuti dopo stai ancora scrollando.`,
      "hero.h1": `Basta scrollare. I tuoi messaggi restano.`,
      "hero.promise": `BrainRotDoctor concede a Reels, Shorts e feed pochi minuti all'ora, poi li chiude. <b>I messaggi e tutto il resto che ti serve restano aperti.</b>`,
      "download": `Scarica per Windows`,
      "free": `Gratis · Windows 10 e 11`,
      "holds.title": `Non puoi <span>spegnerlo</span> d'impulso.`,
      "holds.why": `Le estensioni e i limiti di tempo di utilizzo cedono appena vuoi scrollare, perché si disattivano in tre clic.`,
      "holds.strict": `Attiva la <b>modalità rigida</b> per una serata o una settimana. Finché non finisce, le tue regole non si possono cambiare e l'app non si può chiudere né disinstallare. Non c'è un'uscita anticipata.`,
      "closer.title": `Riprenditi le tue serate.`,
      "footer": `BrainRotDoctor · Gratis e open source · ` + GH,
      "dl.meta.title": `Scarica BrainRotDoctor`,
      "dl.h1": `Il download sta partendo.`,
      "dl.ifnot": `Se non parte, usa il pulsante qui sotto.`,
      "dl.why.title": `Windows può fermarti. È normale.`,
      "dl.why": `BrainRotDoctor non è firmato digitalmente, quindi Windows non riconosce l'editore. Mostra una schermata di protezione. È prudenza, non un verdetto che il file sia pericoloso.`,
      "dl.browser": `Il browser potrebbe chiedere prima di tenere il file. Scegli Conserva.`,
      "dl.steps.title": `Quando lo apri`,
      "dl.step1": `Se vedi <b>Windows ha protetto il PC</b>, clicca <b>Altre info</b>.`,
      "dl.step2": `Poi clicca <b>Esegui comunque</b>.`,
      "dl.win10": `Su Windows 10 sembra un po' più vecchio. Stessi due clic.`,
      "dl.trust": `Fallo solo se l'hai scaricato da llehn.github.io/BrainRotDoctor.`,
      "ss.title": `Windows ha protetto il PC`,
      "ss.body": `Microsoft Defender SmartScreen ha impedito l'avvio di un'app non riconosciuta. L'esecuzione di questa app potrebbe mettere a rischio il PC.`,
      "ss.app": `App`,
      "ss.publisher": `Editore`,
      "ss.unknown": `Editore sconosciuto`,
      "ss.more": `Ulteriori informazioni`,
      "ss.dont": `Non eseguire`,
      "ss.run": `Esegui comunque`,
    },

    pt: {
      "meta.title": `BrainRotDoctor — pára de fazer scroll, mantém as tuas mensagens`,
      "hero.recognise": `Abres o Instagram para responder a uma mensagem. Quarenta minutos depois ainda estás a fazer scroll.`,
      "hero.h1": `Pára o scroll. Mantém as tuas mensagens.`,
      "hero.promise": `O BrainRotDoctor dá aos Reels, aos Shorts e aos feeds alguns minutos por hora e depois fecha-os. <b>As mensagens e tudo o resto de que precisas continuam abertos.</b>`,
      "download": `Transferir para Windows`,
      "free": `Grátis · Windows 10 e 11`,
      "holds.title": `Não o consegues <span>desligar</span> por impulso.`,
      "holds.why": `As extensões e os limites de tempo de ecrã cedem no momento em que queres fazer scroll, porque se desligam em três cliques.`,
      "holds.strict": `Ativa o <b>modo rigoroso</b> para uma noite ou uma semana. Até terminar, as tuas regras não podem ser alteradas e a app não pode ser fechada nem desinstalada. Não há saída antecipada.`,
      "closer.title": `Recupera as tuas noites.`,
      "footer": `BrainRotDoctor · Grátis e de código aberto · ` + GH,
      "dl.meta.title": `Transferir o BrainRotDoctor`,
      "dl.h1": `A transferência está a começar.`,
      "dl.ifnot": `Se não começar, usa o botão abaixo.`,
      "dl.why.title": `O Windows pode impedir-te. É normal.`,
      "dl.why": `O BrainRotDoctor não está assinado digitalmente, por isso o Windows não reconhece o editor. Mostra um ecrã de proteção. É prudência, não um veredicto de que o ficheiro é perigoso.`,
      "dl.browser": `O browser pode perguntar primeiro se queres manter o ficheiro. Escolhe Manter.`,
      "dl.steps.title": `Quando o abrires`,
      "dl.step1": `Se vires <b>O Windows protegeu o PC</b>, clica em <b>Mais informações</b>.`,
      "dl.step2": `Depois clica em <b>Executar mesmo assim</b>.`,
      "dl.win10": `No Windows 10 parece um pouco mais antigo. Os mesmos dois cliques.`,
      "dl.trust": `Só continua se o descarregaste de llehn.github.io/BrainRotDoctor.`,
      "ss.title": `O Windows protegeu o PC`,
      "ss.body": `O Microsoft Defender SmartScreen impediu o início de uma aplicação não reconhecida. A execução desta aplicação poderá constituir um risco para o PC.`,
      "ss.app": `Aplicação`,
      "ss.publisher": `Editor`,
      "ss.unknown": `Editor desconhecido`,
      "ss.more": `Mais informações`,
      "ss.dont": `Não executar`,
      "ss.run": `Executar mesmo assim`,
    },

    pl: {
      "meta.title": `BrainRotDoctor — koniec scrollowania, wiadomości zostają`,
      "hero.recognise": `Otwierasz Instagram, żeby odpisać na jedną wiadomość. Czterdzieści minut później wciąż scrollujesz.`,
      "hero.h1": `Koniec scrollowania. Wiadomości zostają.`,
      "hero.promise": `BrainRotDoctor daje rolkom, Shorts i feedom kilka minut na godzinę, a potem je zamyka. <b>Wiadomości i wszystko inne, czego potrzebujesz, zostają otwarte.</b>`,
      "download": `Pobierz dla Windows`,
      "free": `Za darmo · Windows 10 i 11`,
      "holds.title": `Nie <span>wyłączysz go</span> pod wpływem impulsu.`,
      "holds.why": `Rozszerzenia i limity czasu przed ekranem puszczają w chwili, gdy chcesz scrollować, bo wyłącza się je trzema kliknięciami.`,
      "holds.strict": `Włącz <b>tryb ścisły</b> na wieczór albo na tydzień. Dopóki trwa, reguł nie da się zmienić, a aplikacji nie da się zamknąć ani odinstalować. Nie ma wcześniejszego wyjścia.`,
      "closer.title": `Odzyskaj swoje wieczory.`,
      "footer": `BrainRotDoctor · Za darmo i open source · ` + GH,
      "dl.meta.title": `Pobierz BrainRotDoctor`,
      "dl.h1": `Pobieranie się zaczyna.`,
      "dl.ifnot": `Jeśli nic się nie dzieje, użyj przycisku poniżej.`,
      "dl.why.title": `Windows może cię zatrzymać. To normalne.`,
      "dl.why": `BrainRotDoctor nie jest cyfrowo podpisany, więc Windows nie rozpoznaje wydawcy. Pokazuje ekran ochrony. To ostrożność, nie wyrok, że plik jest niebezpieczny.`,
      "dl.browser": `Przeglądarka może najpierw zapytać, czy zachować plik. Wybierz Zachowaj.`,
      "dl.steps.title": `Gdy go otworzysz`,
      "dl.step1": `Jeśli zobaczysz <b>System Windows ochronił komputer</b>, kliknij <b>Więcej informacji</b>.`,
      "dl.step2": `Potem kliknij <b>Uruchom mimo to</b>.`,
      "dl.win10": `Na Windows 10 wygląda trochę starzej. Te same dwa kliknięcia.`,
      "dl.trust": `Rób to tylko, jeśli pobrałeś go z llehn.github.io/BrainRotDoctor.`,
      "ss.title": `System Windows ochronił komputer`,
      "ss.body": `Microsoft Defender SmartScreen uniemożliwił uruchomienie nierozpoznanej aplikacji. Uruchomienie tej aplikacji może narazić komputer na ryzyko.`,
      "ss.app": `Aplikacja`,
      "ss.publisher": `Wydawca`,
      "ss.unknown": `Nieznany wydawca`,
      "ss.more": `Więcej informacji`,
      "ss.dont": `Nie uruchamiaj`,
      "ss.run": `Uruchom mimo to`,
    },

    ro: {
      "meta.title": `BrainRotDoctor — gata cu scrollul, mesajele rămân`,
      "hero.recognise": `Deschizi Instagram ca să răspunzi la un mesaj. Patruzeci de minute mai târziu încă dai scroll.`,
      "hero.h1": `Gata cu scrollul. Mesajele rămân.`,
      "hero.promise": `BrainRotDoctor le dă Reels, Shorts și feedurilor câteva minute pe oră, apoi le închide. <b>Mesajele și tot ce-ți mai trebuie rămân deschise.</b>`,
      "download": `Descarcă pentru Windows`,
      "free": `Gratuit · Windows 10 și 11`,
      "holds.title": `Nu-l poți <span>opri</span> dintr-un impuls.`,
      "holds.why": `Extensiile și limitele de timp petrecut pe ecran cedează exact când vrei să dai scroll, pentru că se opresc din trei clicuri.`,
      "holds.strict": `Pornește <b>modul strict</b> pentru o seară sau o săptămână. Până se termină, regulile nu pot fi schimbate, iar aplicația nu poate fi închisă sau dezinstalată. Nu există ieșire înainte de termen.`,
      "closer.title": `Ia-ți serile înapoi.`,
      "footer": `BrainRotDoctor · Gratuit și open source · ` + GH,
      "dl.meta.title": `Descarcă BrainRotDoctor`,
      "dl.h1": `Descărcarea începe.`,
      "dl.ifnot": `Dacă nu pornește, folosește butonul de mai jos.`,
      "dl.why.title": `Windows te poate opri. E normal.`,
      "dl.why": `BrainRotDoctor nu e semnat digital, deci Windows nu recunoaște editorul. Arată un ecran de protecție. E prudență, nu un verdict că fișierul e periculos.`,
      "dl.browser": `Browserul poate întreba mai întâi dacă păstrezi fișierul. Alege Păstrează.`,
      "dl.steps.title": `Când îl deschizi`,
      "dl.step1": `Dacă vezi <b>Windows ți-a protejat PC-ul</b>, apasă <b>Mai multe informații</b>.`,
      "dl.step2": `Apoi apasă <b>Rulează oricum</b>.`,
      "dl.win10": `Pe Windows 10 arată un pic mai vechi. Aceleași două clicuri.`,
      "dl.trust": `Continuă doar dacă l-ai descărcat de pe llehn.github.io/BrainRotDoctor.`,
      "ss.title": `Windows ți-a protejat PC-ul`,
      "ss.body": `Microsoft Defender SmartScreen a împiedicat pornirea unei aplicații nerecunoscute. Rularea acestei aplicații ar putea pune PC-ul în pericol.`,
      "ss.app": `Aplicație`,
      "ss.publisher": `Editor`,
      "ss.unknown": `Editor necunoscut`,
      "ss.more": `Mai multe informații`,
      "ss.dont": `Nu rula`,
      "ss.run": `Rulează oricum`,
    },

    tr: {
      "meta.title": `BrainRotDoctor — kaydırmayı bırak, mesajların kalsın`,
      "hero.recognise": `Tek bir mesaja cevap vermek için Instagram'ı açıyorsun. Kırk dakika sonra hâlâ kaydırıyorsun.`,
      "hero.h1": `Kaydırmayı bırak. Mesajların kalsın.`,
      "hero.promise": `BrainRotDoctor Reels, Shorts ve akışlara saatte birkaç dakika verir, sonra onları kapatır. <b>Mesajlar ve ihtiyacın olan her şey açık kalır.</b>`,
      "download": `Windows için indir`,
      "free": `Ücretsiz · Windows 10 ve 11`,
      "holds.title": `Bir anlık hevesle <span>kapatamazsın</span>.`,
      "holds.why": `Eklentiler ve ekran süresi sınırları, kaydırmak istediğin an pes eder, çünkü üç tıkla kapanırlar.`,
      "holds.strict": `<b>Katı modu</b> bir akşamlığına ya da bir haftalığına aç. Bitene kadar kuralların değiştirilemez, uygulama kapatılamaz ve kaldırılamaz. Erken çıkış yok.`,
      "closer.title": `Akşamlarını geri al.`,
      "footer": `BrainRotDoctor · Ücretsiz ve açık kaynak · ` + GH,
      "dl.meta.title": `BrainRotDoctor'ı indir`,
      "dl.h1": `İndirme başlıyor.`,
      "dl.ifnot": `Başlamazsa aşağıdaki düğmeyi kullan.`,
      "dl.why.title": `Windows seni durdurabilir. Bu normal.`,
      "dl.why": `BrainRotDoctor dijital olarak imzalanmadığı için Windows yayıncıyı tanımaz. Bir koruma ekranı gösterir. Bu tedbirdir, dosyanın tehlikeli olduğu anlamına gelmez.`,
      "dl.browser": `Tarayıcın önce dosyayı tutmak isteyip istemediğini sorabilir. Tut'u seç.`,
      "dl.steps.title": `Dosyayı açtığında`,
      "dl.step1": `<b>Windows PC'nizi korudu</b> yazısını görürsen <b>Daha fazla bilgi</b>ye tıkla.`,
      "dl.step2": `Sonra <b>Yine de çalıştır</b>a tıkla.`,
      "dl.win10": `Windows 10'da biraz daha eski durur. Aynı iki tık.`,
      "dl.trust": `Bunu yalnızca llehn.github.io/BrainRotDoctor adresinden indirdiysen yap.`,
      "ss.title": `Windows PC'nizi korudu`,
      "ss.body": `Microsoft Defender SmartScreen tanınmayan bir uygulamanın başlatılmasını engelledi. Bu uygulamayı çalıştırmak PC'niz için risk oluşturabilir.`,
      "ss.app": `Uygulama`,
      "ss.publisher": `Yayımcı`,
      "ss.unknown": `Bilinmeyen yayımcı`,
      "ss.more": `Daha fazla bilgi`,
      "ss.dont": `Çalıştırma`,
      "ss.run": `Yine de çalıştır`,
    },

    ru: {
      "meta.title": `BrainRotDoctor — хватит листать, сообщения остаются`,
      "hero.recognise": `Ты открываешь Instagram, чтобы ответить на одно сообщение. Через сорок минут ты всё ещё листаешь.`,
      "hero.h1": `Хватит листать. Сообщения остаются.`,
      "hero.promise": `BrainRotDoctor даёт Reels, Shorts и лентам несколько минут в час, а потом закрывает их. <b>Сообщения и всё остальное, что тебе нужно, остаются открытыми.</b>`,
      "download": `Скачать для Windows`,
      "free": `Бесплатно · Windows 10 и 11`,
      "holds.title": `Его не <span>выключить</span> под настроение.`,
      "holds.why": `Расширения и лимиты экранного времени сдаются, как только хочется полистать, потому что отключаются в три клика.`,
      "holds.strict": `Включи <b>строгий режим</b> на вечер или на неделю. Пока он не закончится, правила нельзя изменить, а приложение нельзя закрыть или удалить. Досрочного выхода нет.`,
      "closer.title": `Верни себе свои вечера.`,
      "footer": `BrainRotDoctor · Бесплатно и с открытым кодом · ` + GH,
      "dl.meta.title": `Скачать BrainRotDoctor`,
      "dl.h1": `Загрузка начинается.`,
      "dl.ifnot": `Если ничего не произошло, нажми кнопку ниже.`,
      "dl.why.title": `Windows может тебя остановить. Это нормально.`,
      "dl.why": `BrainRotDoctor не подписан цифровой подписью, поэтому Windows не узнаёт издателя. Показывается экран защиты. Это осторожность, а не приговор, что файл опасен.`,
      "dl.browser": `Браузер может сначала спросить, сохранять ли файл. Выбери Сохранить.`,
      "dl.steps.title": `Когда откроешь файл`,
      "dl.step1": `Если видишь <b>Windows защитил компьютер</b>, нажми <b>Дополнительные сведения</b>.`,
      "dl.step2": `Затем нажми <b>Выполнить в любом случае</b>.`,
      "dl.win10": `В Windows 10 окно выглядит чуть старше. Те же два нажатия.`,
      "dl.trust": `Делай так, только если скачал его с llehn.github.io/BrainRotDoctor.`,
      "ss.title": `Windows защитил компьютер`,
      "ss.body": `Microsoft Defender SmartScreen предотвратил запуск нераспознанного приложения. Запуск этого приложения может подвергнуть компьютер риску.`,
      "ss.app": `Приложение`,
      "ss.publisher": `Издатель`,
      "ss.unknown": `Неизвестный издатель`,
      "ss.more": `Дополнительные сведения`,
      "ss.dont": `Не выполнять`,
      "ss.run": `Выполнить в любом случае`,
    },
  };

  function apply(code) {
    var dict = T[code] || T.en;
    document.documentElement.lang = code;
    var titleEl = document.querySelector("title[data-i18n]");
    var titleKey = titleEl ? titleEl.getAttribute("data-i18n") : "meta.title";
    var title = dict[titleKey] != null ? dict[titleKey] : T.en[titleKey];
    if (title) document.title = title;
    document.querySelectorAll("[data-i18n]").forEach(function (el) {
      var k = el.getAttribute("data-i18n");
      var v = dict[k] != null ? dict[k] : T.en[k];
      if (v != null) el.innerHTML = v;
    });
    try { localStorage.setItem("brd.lang", code); } catch (e) {}
    var sel = document.getElementById("langsel");
    if (sel) sel.value = code;
  }

  function detect() {
    var q = new URLSearchParams(location.search).get("lang");
    if (q && T[q]) return q;
    try {
      var saved = localStorage.getItem("brd.lang");
      if (saved && T[saved]) return saved;
    } catch (e) {}
    var prefs = navigator.languages || [navigator.language || "en"];
    for (var i = 0; i < prefs.length; i++) {
      var c = (prefs[i] || "").slice(0, 2).toLowerCase();
      if (T[c]) return c;
    }
    return "en";
  }

  function init() {
    var sel = document.getElementById("langsel");
    if (sel) {
      LANGS.forEach(function (l) {
        var o = document.createElement("option");
        o.value = l[0];
        o.textContent = l[1];
        sel.appendChild(o);
      });
      sel.addEventListener("change", function () { apply(sel.value); });
    }
    apply(detect());
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
