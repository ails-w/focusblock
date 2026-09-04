using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

public class AddBlockView : View
{
    public Label AppNameLabel { get; }
    public TextField AppNameField { get; }
    public Label ScheduleLabel { get; }
    public TextField ScheduleField { get; }
    public Button AddButton { get; }

    public AddBlockView()
    {
        AppNameLabel = new Label { Text = "App name:", X = 0, Y = 0 };
        AppNameField = new TextField { X = Pos.Right(AppNameLabel) + 1, Y = 0, Width = 20 };
        ScheduleLabel = new Label { Text = "Schedule:", X = 0, Y = 2 };
        ScheduleField = new TextField { X = Pos.Right(ScheduleLabel) + 1, Y = 2, Width = 20 };
        AddButton = new Button { Text = "Add Block", X = 0, Y = 4 };

        Add(AppNameLabel, AppNameField, ScheduleLabel, ScheduleField, AddButton);
    }
}