using FluentAssertions;

using FocusBlock.Tui.Views;

using Terminal.Gui.Views;

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

    [Fact]
    public void MainWindow_EarlyStopMenu_InvokesCallback()
    {
        bool called = false;
        var window = new MainWindow(() =>
        {
            called = true;
            return Task.CompletedTask;
        });

        MenuItem? item = window.MenuBar
            .GetMenuItemsWith(menuItem => menuItem.Title.Contains("Early Stop"))
            .SingleOrDefault();

        item.Should().NotBeNull();
        item!.Action.Should().NotBeNull();
        item.Action!.Invoke();

        called.Should().BeTrue();
    }
}