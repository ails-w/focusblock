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
}