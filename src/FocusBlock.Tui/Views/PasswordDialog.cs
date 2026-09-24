using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

/// <summary>
/// Modal dialog that asks for the early-stop password in a masked field.
/// </summary>
/// <remarks>
/// This is the password step after <see cref="ChallengeDialog"/>'s friction step. The dialog only
/// collects the typed value; the daemon verifies it against the stored Argon2id hash, so no
/// validation happens here.
/// </remarks>
public class PasswordDialog : Dialog
{
    /// <summary>Gets the masked field where the user types the password.</summary>
    public TextField PasswordField { get; }

    /// <summary>Gets the button that confirms the password.</summary>
    public Button OkButton { get; }

    /// <summary>Gets the button that cancels the early stop.</summary>
    public Button CancelButton { get; }

    /// <summary>Creates the dialog.</summary>
    public PasswordDialog()
    {
        Title = "Password";
        var label = new Label { Text = "Password:", X = 1, Y = 1 };
        PasswordField = new TextField { X = 1, Y = 3, Width = Dim.Fill(2), Secret = true };
        OkButton = new Button { Text = "OK" };
        CancelButton = new Button { Text = "Cancel" };

        Add(label, PasswordField);
        AddButton(OkButton);
        AddButton(CancelButton);
    }

    /// <summary>Gets the typed password, or null when the field is empty.</summary>
    public string? Password =>
        string.IsNullOrEmpty(PasswordField.Text) ? null : PasswordField.Text;
}