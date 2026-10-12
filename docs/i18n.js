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
    },
  };

  function apply(code) {
    var dict = T[code] || T.en;
    document.documentElement.lang = code;
    if (dict["meta.title"]) document.title = dict["meta.title"];
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
