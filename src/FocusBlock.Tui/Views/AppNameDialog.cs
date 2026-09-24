using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

/// <summary>
/// Modal dialog that asks for the application name whose block should be lifted early.
/// </summary>
/// <remarks>
/// This dialog only collects the name typed by the user; the daemon decides whether that name
/// matches a blocked application, so no validation happens here.
/// </remarks>
public class AppNameDialog : Dialog
{
    /// <summary>Gets the field where the user types the application name.</summary>
    public TextField AppNameField { get; }

    /// <summary>Gets the button that confirms the application name.</summary>
    public Button OkButton { get; }

    /// <summary>Gets the button that cancels the early stop.</summary>
    public Button CancelButton { get; }

    /// <summary>Creates the dialog.</summary>
    public AppNameDialog()
    {
        Title = "Early stop";
        var label = new Label { Text = "App name:", X = 1, Y = 1 };
        AppNameField = new TextField { X = 1, Y = 3, Width = Dim.Fill(2) };
        OkButton = new Button { Text = "OK" };
        CancelButton = new Button { Text = "Cancel" };

        Add(label, AppNameField);
        AddButton(OkButton);
        AddButton(CancelButton);
    }

    /// <summary>Gets the trimmed application name, or null when the field is blank.</summary>
    public string? AppName =>
        string.IsNullOrWhiteSpace(AppNameField.Text) ? null : AppNameField.Text.Trim();
}