using FocusBlock.Tui.Views;

using Terminal.Gui.App;

namespace FocusBlock.Tui.Services;

/// <summary>
/// Terminal.Gui-backed implementation of <see cref="IEarlyStopPrompt"/>: runs the friction dialog
/// and the password dialog modally and returns the typed password.
/// </summary>
/// <remarks>
/// <para>
/// Acceptance is detected through <see cref="Dialog.Result"/>, the zero-based index of the
/// button the user pressed, or <see langword="null"/> when the dialog was dismissed with Escape.
/// Both dialogs add their OK button first, so an accepted dialog has <c>Result == 0</c>; any other
/// value (including <see langword="null"/>) means cancel.
/// </para>
/// <para>
/// This class is UI glue: it drives the real modal loop, so it is exercised by hand rather than in
/// unit tests, which fake <see cref="IEarlyStopPrompt"/> instead.
/// </para>
/// </remarks>
public class TerminalEarlyStopPrompt : IEarlyStopPrompt
{
    private const int OkButtonIndex = 0;

    private readonly IApplication _app;

    /// <summary>Creates the prompt from the application used to run the modal dialogs.</summary>
    /// <param name="app">Application instance that owns the modal loop.</param>
    public TerminalEarlyStopPrompt(IApplication app) => _app = app;

    /// <inheritdoc />
    public string? AskForPassword(string challenge)
    {
        var challengeDialog = new ChallengeDialog(challenge);
        _app.Run(challengeDialog);

        if (challengeDialog.Result != OkButtonIndex || !challengeDialog.ValidateInput())
        {
            return null;
        }

        var passwordDialog = new PasswordDialog();
        _app.Run(passwordDialog);

        return passwordDialog.Result == OkButtonIndex ? passwordDialog.Password : null;
    }
}