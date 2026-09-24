using FluentAssertions;

using FocusBlock.Tui.Views;

namespace FocusBlock.Tests.Unit;

public class ChallengeDialogTests
{
    [Fact]
    public void ChallengeDialog_ShowsChallenge_AndValidatesInput()
    {
        const string challenge = "cobalt-otter-lantern-drift";
        var dialog = new ChallengeDialog(challenge);

        dialog.ChallengeLabel.Text.Should().Contain(challenge);

        dialog.InputField.Text = challenge;

        dialog.ValidateInput().Should().BeTrue();
    }

    [Fact]
    public void ChallengeDialog_ValidateInput_ReturnsFalse_WhenInputWrong()
    {
        const string challenge = "cobalt-otter-lantern-drift";
        var dialog = new ChallengeDialog(challenge);

        dialog.InputField.Text = "maple-ivory-harbor-ember";

        dialog.ValidateInput().Should().BeFalse();
    }

    [Fact]
    public void ChallengeDialog_HasOkAndCancelButtons()
    {
        var dialog = new ChallengeDialog("cobalt-otter-lantern-drift");

        dialog.OkButton.Should().NotBeNull();
        dialog.CancelButton.Should().NotBeNull();
    }
}