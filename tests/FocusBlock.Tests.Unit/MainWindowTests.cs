using FocusBlock.Tui.Views;
using FluentAssertions;

namespace FocusBlock.Tests.Unit;

public class MainWindowTests
{
    [Fact]
    public void MainWindow_HasMenuBarAndStatusBar()
    {
        var window = new MainWindow();

        window.MenuBar.Should().NotBeNull();
        window.StatusBar.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_MenuNavigatesToViews()
    {
        var window = new MainWindow();

        window.ShowView(window.BlockListView);
        window.Content.Should().BeSameAs(window.BlockListView);

        window.ShowView(window.AddBlockView);
        window.Content.Should().BeSameAs(window.AddBlockView);

        window.ShowView(window.StatusView);
        window.Content.Should().BeSameAs(window.StatusView);
    }
}