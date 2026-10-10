namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Works out which page one browser window is really showing when the only
/// source is its address bar. The bar holds whatever the user is typing, so
/// typed-but-unsent text must never count as the page: while the bar is being
/// edited, the window keeps the last page it really showed, until there is
/// evidence a new page loaded.
/// </summary>
internal sealed class AddressBarTracker
{
    private Uri? _page;
    private bool _editing;
    private string _titleWhenEditStarted = string.Empty;
    private bool _holdingText;
    private string? _heldText;

    /// <summary>The browser reported the page's address directly; trust it.</summary>
    public Uri? Confirm(Uri? page)
    {
        _page = page;
        EndEdit();
        return _page;
    }

    /// <summary>
    /// Resolves the page from one reading of the address bar.
    /// </summary>
    /// <param name="barText">The bar's text, or null when it could not be read.</param>
    /// <param name="barHasFocus">True while the bar has keyboard focus (being edited).</param>
    /// <param name="windowIsForeground">True when the window is the one in front.</param>
    /// <param name="windowTitle">The window title, which follows the loaded page.</param>
    public Uri? Resolve(string? barText, bool barHasFocus, bool windowIsForeground, string windowTitle)
    {
        if (barHasFocus)
        {
            if (!_editing)
            {
                _editing = true;
                _titleWhenEditStarted = windowTitle;
            }

            _holdingText = false;
            _heldText = null;
            return _page;
        }

        UrlNormalizer.TryNormalize(barText, out Uri? shown);

        if (_editing)
        {
            // Left mid-edit for another window: the bar may still hold unsent text.
            if (!windowIsForeground)
            {
                return _page;
            }

            bool pageLoaded = !string.Equals(windowTitle, _titleWhenEditStarted, StringComparison.Ordinal);
            bool editUndone = shown == _page;
            bool browserRewroteBar = _holdingText && !string.Equals(barText, _heldText, StringComparison.Ordinal);
            if (!pageLoaded && !editUndone && !browserRewroteBar)
            {
                // Focus left the bar but nothing new loaded: the text is unsent.
                if (!_holdingText)
                {
                    _holdingText = true;
                    _heldText = barText;
                }

                return _page;
            }

            EndEdit();
        }

        _page = shown;
        return _page;
    }

    private void EndEdit()
    {
        _editing = false;
        _holdingText = false;
        _heldText = null;
    }
}
