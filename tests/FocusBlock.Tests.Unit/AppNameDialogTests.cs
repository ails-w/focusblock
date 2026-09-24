using FluentAssertions;

using FocusBlock.Tui.Views;

namespace FocusBlock.Tests.Unit;

public class AppNameDialogTests
{
    [Fact]
    public void AppNameDialog_HasFieldAndButtons()
    {
        var dialog = new AppNameDialog();

        dialog.AppNameField.Should().NotBeNull();
        dialog.OkButton.Should().NotBeNull();
        dialog.CancelButton.Should().NotBeNull();
    }

    [Fact]
    public void AppNameDialog_AppName_ReturnsTypedText()
    {
        var dialog = new AppNameDialog();

        dialog.AppNameField.Text = "firefox";

        dialog.AppName.Should().Be("firefox");
    }

    [Fact]
    public void AppNameDialog_AppName_ReturnsNull_WhenBlank()
    {
        var dialog = new AppNameDialog();

        dialog.AppNameField.Text = "";

        dialog.AppName.Should().BeNull();
    }
}