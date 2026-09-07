using FluentAssertions;

using FocusBlock.Tui.Views;

namespace FocusBlock.Tests.Unit;

public class AddBlockViewTests
{
    [Fact]
    public void AddBlockView_ShowsFormFields()
    {
        var view = new AddBlockView();

        view.AppNameField.Should().NotBeNull();
        view.ScheduleField.Should().NotBeNull();
        view.AddButton.Should().NotBeNull();
    }
}