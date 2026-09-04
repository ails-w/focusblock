using FocusBlock.Tui;
using FocusBlock.Tui.Views;
using FluentAssertions;
using Moq;
using Terminal.Gui.App;

namespace FocusBlock.Tests.Unit;

public class FocusBlockAppTests
{
    [Fact]
    public void FocusBlockApp_CreatesMainWindow()
    {
        var app = new FocusBlockApp(Mock.Of<IApplication>());

        app.MainWindow.Should().NotBeNull();
        app.MainWindow.Should().BeOfType<MainWindow>();
    }
}