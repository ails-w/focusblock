namespace FocusBlock.Tui.Services;

/// <summary>Asks the user for the early-stop password after showing the challenge.</summary>
public interface IEarlyStopPrompt
{
    /// <summary>Returns the typed password, or null when the user cancels or fails the challenge.</summary>
    string? AskForPassword(string challenge);
}