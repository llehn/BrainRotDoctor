using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// Private windows are recognized from the browser's own chrome, not from a page
/// that happens to mention Incognito. Titles below are the real strings from
/// Chrome, Edge and Firefox on this project (including German Chrome, which
/// keeps the marker out of GetWindowText).
/// </summary>
public sealed class PrivateBrowsingTests
{
    [Theory]
    [InlineData("BRDChromePriv - Google Chrome (anonym)")]
    [InlineData("Example Domain - Google Chrome (Incognito)")]
    [InlineData("Example Domain - Google Chrome (Inkognito)")]
    [InlineData("BRDEdgePriv - [InPrivate] – Microsoft Edge")]
    [InlineData("BRDEdgePriv - Microsoft Edge (InPrivate)")]
    [InlineData("Mozilla Firefox Private Browsing")]
    [InlineData("Example Domain — Mozilla Firefox Private Browsing")]
    [InlineData("Example Domain — Mozilla Firefox Privater Modus")]
    [InlineData("News - Brave (Private)")]
    public void Private_chrome_names_are_private(string text) =>
        Assert.True(PrivateBrowsing.LooksPrivate(text), text);

    [Theory]
    [InlineData("BRDChromePriv - Google Chrome")]
    [InlineData("BRDChromeNorm - Google Chrome")]
    [InlineData("Incognito - Google Chrome")]
    [InlineData("Private browsing - Google Chrome")]
    [InlineData("InPrivate - Google Chrome")]
    [InlineData("BRDEdgeNorm - Profil 1 – Microsoft Edge")]
    [InlineData("BRDEdgeNorm - Microsoft Edge")]
    [InlineData("Mozilla Firefox")]
    [InlineData("Example Domain — Mozilla Firefox")]
    [InlineData("Private Browsing — Mozilla Firefox")]
    [InlineData("")]
    [InlineData(null)]
    public void Ordinary_titles_are_not_private(string? text) =>
        Assert.False(PrivateBrowsing.LooksPrivate(text), text ?? "(null)");

    [Theory]
    [InlineData("Inkognito")]
    [InlineData("Incognito")]
    [InlineData("InPrivate-Browsen")]
    [InlineData("InPrivate browsing")]
    public void Toolbar_private_buttons_are_private(string name) =>
        Assert.True(PrivateBrowsing.LooksPrivateButton(name), name);

    [Theory]
    [InlineData("Ich")]
    [InlineData("Chrome")]
    [InlineData("Neuer Tab")]
    [InlineData("Schließen")]
    [InlineData("New Incognito window - pinned")]
    [InlineData("Neues Inkognitofenster – angepinnt")]
    [InlineData("Neues InPrivate-Fenster")]
    public void Ordinary_toolbar_buttons_are_not_private(string name) =>
        Assert.False(PrivateBrowsing.LooksPrivateButton(name), name);
}
