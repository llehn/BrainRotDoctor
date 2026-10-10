using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class AddressBarTrackerTests
{
    private const string NewTab = "New Tab - Google Chrome";
    private const string Instagram = "Instagram - Google Chrome";

    [Fact]
    public void Shows_the_address_bar_page_when_nothing_is_being_typed()
    {
        var tracker = new AddressBarTracker();

        Uri? page = tracker.Resolve("https://www.instagram.com/", false, true, Instagram);

        Assert.Equal("www.instagram.com", page?.Host);
    }

    [Fact]
    public void Typing_in_a_new_tab_never_counts_as_visiting_the_typed_site()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve(null, false, true, NewTab);

        Assert.Null(tracker.Resolve("instagram.com", true, true, NewTab));
        Assert.Null(tracker.Resolve("instagram.com/explore", true, true, NewTab));
    }

    [Fact]
    public void The_typed_site_counts_once_its_page_loads()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve(null, false, true, NewTab);
        tracker.Resolve("instagram.com", true, true, NewTab);

        // Enter: focus moves to the page, which is still loading.
        Assert.Null(tracker.Resolve("instagram.com", false, true, NewTab));

        Uri? page = tracker.Resolve("https://www.instagram.com/", false, true, Instagram);
        Assert.Equal("www.instagram.com", page?.Host);
    }

    [Fact]
    public void A_new_page_counts_when_the_browser_rewrites_the_bar_even_if_the_title_stays()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve("https://www.instagram.com/", false, true, Instagram);
        tracker.Resolve("instagram.com/reels", true, true, Instagram);
        tracker.Resolve("instagram.com/reels", false, true, Instagram);

        Uri? page = tracker.Resolve("https://www.instagram.com/reels/", false, true, Instagram);

        Assert.Equal("/reels/", page?.AbsolutePath);
    }

    [Fact]
    public void Unsent_text_left_in_the_bar_is_ignored_after_clicking_into_the_page()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve(null, false, true, NewTab);
        tracker.Resolve("instagram.com", true, true, NewTab);

        Assert.Null(tracker.Resolve("instagram.com", false, true, NewTab));
        Assert.Null(tracker.Resolve("instagram.com", false, true, NewTab));
    }

    [Fact]
    public void Unsent_text_is_ignored_while_the_user_is_in_another_window()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve(null, false, true, NewTab);
        tracker.Resolve("instagram.com", true, true, NewTab);

        Assert.Null(tracker.Resolve("instagram.com", false, false, NewTab));
    }

    [Fact]
    public void Clicking_into_the_bar_does_not_pause_the_page_being_watched()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve("https://www.instagram.com/", false, true, Instagram);

        Uri? page = tracker.Resolve("https://www.instagram.com/", true, true, Instagram);

        Assert.Equal("www.instagram.com", page?.Host);
    }

    [Fact]
    public void Undoing_an_edit_returns_to_following_the_bar()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve("https://example.com/", false, true, "Example");
        tracker.Resolve("instagram.com", true, true, "Example");
        tracker.Resolve("https://example.com/", false, true, "Example");

        Uri? page = tracker.Resolve("https://example.com/other", false, true, "Example");

        Assert.Equal("/other", page?.AbsolutePath);
    }

    [Fact]
    public void A_page_address_reported_by_the_browser_is_trusted()
    {
        var tracker = new AddressBarTracker();
        tracker.Resolve("instagram.com", true, true, NewTab);

        Uri? page = tracker.Confirm(new Uri("https://example.com/"));

        Assert.Equal("example.com", page?.Host);
        Assert.Equal("example.com", tracker.Resolve("https://example.com/", false, true, NewTab)?.Host);
    }
}
