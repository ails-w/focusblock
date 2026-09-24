using FluentAssertions;

using FocusBlock.Tui.Views;

namespace FocusBlock.Tests.Unit;

public class PasswordDialogTests
{
    [Fact]
    public void PasswordDialog_HasSecretFieldAndButtons()
    {
        var dialog = new PasswordDialog();

        dialog.PasswordField.Secret.Should().BeTrue();
        dialog.OkButton.Should().NotBeNull();
        dialog.CancelButton.Should().NotBeNull();
    }

    [Fact]
    public void PasswordDialog_Password_ReturnsTypedText()
    {
        var dialog = new PasswordDialog();

        dialog.PasswordField.Text = "hunter2";

        dialog.Password.Should().Be("hunter2");
    }

    [Fact]
    public void PasswordDialog_Password_ReturnsNull_WhenEmpty()
    {
        var dialog = new PasswordDialog();

        dialog.PasswordField.Text = "";

        dialog.Password.Should().BeNull();
    }
}