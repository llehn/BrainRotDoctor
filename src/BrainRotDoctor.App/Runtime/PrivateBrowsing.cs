namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Whether a browser window is a private session (Chrome Incognito, Edge
/// InPrivate, Firefox Private Browsing, and the same idea in other languages).
/// The window title is enough for Edge and Firefox; Chrome often keeps the
/// private marker only in the accessibility name of the window chrome.
/// </summary>
internal static class PrivateBrowsing
{
    // Product-suffix phrases (Firefox and friends). Matched only in the last
    // dash-separated segment so a page titled "Private browsing" is not treated
    // as a private window.
    private static readonly string[] ProductPhrases =
    [
        "private browsing",
        "privater modus",
        "privates surfen",
        "navigation privée",
        "navegación privada",
        "navegacao privada",
        "navegação privada",
        "navigazione anonima",
        "navigazione in incognito",
        "privé browsen",
        "privénavigatie",
        "tryb prywatny",
        "privat surfing",
        "privat surfning",
        "privat browsing",
        "privat nettlesing",
        "yksityinen selaus",
        "anonymní prohlížení",
        "súkromné prehliadanie",
        "zasebno brskanje",
        "privatno pregledavanje",
        "navigare privată",
        "privát böngészés",
        "ιδιωτική περιήγηση",
        "поверително сърфиране",
        "privatusis naršymas",
        "privātā pārlūkošana",
        "privaatne veebilehitsemine",
        "brabhsáil phríobháideach",
        "navegació privada",
        "gizli gezinme",
        "приватный просмотр",
        "приватний перегляд",
        "приватно прегледање",
    ];

    // Inner text of (parentheses) or [brackets] that browsers append for a
    // private or guest session. "anonym" is Chrome's German accessible name.
    private static readonly string[] ModeWords =
    [
        "incognito",
        "inkognito",
        "incógnito",
        "inprivate",
        "anonym",
        "guest",
        "gast",
        "secret",
        "gizli",
        "privé",
        "privada",
        "privato",
        "private",
    ];

    /// <summary>
    /// True when <paramref name="text"/> is a window title or chrome name that
    /// marks a private session — not when it is merely a page title.
    /// </summary>
    public static bool LooksPrivate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Edge inserts "[InPrivate]" or "(InPrivate)" as its own marker, not as a page title.
        if (ContainsIgnoreCase(text, "[inprivate]") || ContainsIgnoreCase(text, "(inprivate)"))
        {
            return true;
        }

        string product = LastSegment(text);
        return HasModeParenthetical(product) || HasProductPhrase(product);
    }

    /// <summary>
    /// True when a toolbar button is the browser's own private-session control
    /// (Chrome's spy-hat, Edge's InPrivate badge). Page buttons are ignored by
    /// the caller; this only looks at the name.
    /// </summary>
    public static bool LooksPrivateButton(string? name)
    {
        if (LooksPrivate(name))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 48)
        {
            return false;
        }

        string trimmed = name.Trim();
        return StartsModeWord(trimmed);
    }

    private static bool HasProductPhrase(string product)
    {
        foreach (string phrase in ProductPhrases)
        {
            if (ContainsIgnoreCase(product, phrase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasModeParenthetical(string text)
    {
        return HasModeDelimiters(text, '(', ')') || HasModeDelimiters(text, '[', ']');
    }

    private static bool HasModeDelimiters(string text, char open, char close)
    {
        int from = 0;
        while (from < text.Length)
        {
            int start = text.IndexOf(open, from);
            if (start < 0)
            {
                return false;
            }

            int end = text.IndexOf(close, start + 1);
            if (end < 0)
            {
                return false;
            }

            if (IsModeWord(text.AsSpan(start + 1, end - start - 1)))
            {
                return true;
            }

            from = end + 1;
        }

        return false;
    }

    private static bool IsModeWord(ReadOnlySpan<char> inner)
    {
        string word = inner.Trim().ToString();
        if (word.Length == 0 || word.Length > 32)
        {
            return false;
        }

        foreach (string mode in ModeWords)
        {
            if (ContainsIgnoreCase(word, mode))
            {
                return true;
            }
        }

        return false;
    }

    // Only a button named for the mode itself ("Incognito", "InPrivate-Browsen").
    // "New Incognito window", pinned in an ordinary window, is not a marker.
    private static bool StartsModeWord(string name)
    {
        foreach (string mode in ModeWords)
        {
            if (name.StartsWith(mode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The browser's own name in a title such as "Example - Google Chrome (Incognito)"
    /// or "Example — Mozilla Firefox Private Browsing". Page titles sit before the
    /// last dash, so they cannot make a normal window look private.
    /// </summary>
    private static string LastSegment(string text)
    {
        int last = -1;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is '-' or '\u2013' or '\u2014')
            {
                last = i;
            }
        }

        return last < 0 ? text.Trim() : text[(last + 1)..].Trim();
    }

    private static bool ContainsIgnoreCase(string text, string value) =>
        text.Contains(value, StringComparison.OrdinalIgnoreCase);
}
