using FocusBlock.Tui.Views;
using FluentAssertions;

namespace FocusBlock.Tests.Unit;

public class BlockListViewTests
{
    [Fact]
    public void BlockListView_DisplaysListOfApps()
    {
        var view = new BlockListView();

        view.ShowApps(new[] { "firefox", "spotify" });

        view.Apps.Should().Contain(new[] { "firefox", "spotify" });
        view.Apps.Should().HaveCount(2);
    }
}