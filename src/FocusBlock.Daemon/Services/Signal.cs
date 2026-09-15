namespace FocusBlock.Daemon.Services;

/// <summary>POSIX signal numbers used to terminate a blocked process.</summary>
public enum Signal
{
    Term = 15,
    Kill = 9,
}