using System.Net.Sockets;

using FocusBlock.Contracts;

namespace FocusBlock.Tui.Services;

/// <summary>
/// Orchestrates the early-stop flow: show a challenge, collect the password and ask the daemon to
/// stop blocking an app early. Degrades to <see langword="false"/> when the daemon is unreachable.
/// </summary>
public class EarlyStopService
{
    private readonly IIpcClient _client;
    private readonly ChallengeSystem _challenges;
    private readonly IEarlyStopPrompt _prompt;

    /// <summary>Creates the service from its IPC, challenge and UI seams.</summary>
    /// <param name="client">IPC client used to reach the daemon.</param>
    /// <param name="challenges">Generator of the human-typed challenge shown as friction.</param>
    /// <param name="prompt">UI seam that asks the user for the early-stop password.</param>
    public EarlyStopService(IIpcClient client, ChallengeSystem challenges, IEarlyStopPrompt prompt)
    {
        _client = client;
        _challenges = challenges;
        _prompt = prompt;
    }

    /// <summary>Runs the early-stop flow for an app. Returns true when the daemon granted it.</summary>
    /// <param name="appName">Application whose block should be lifted early.</param>
    /// <param name="ct">Cancellation token for the IPC round-trip.</param>
    public async Task<bool> RequestEarlyStopAsync(string appName, CancellationToken ct = default)
    {
        string challenge = _challenges.GenerateChallenge();
        string? password = _prompt.AskForPassword(challenge);
        if (password is null)
        {
            return false;
        }

        try
        {
            IpcMessage response = await _client.SendAsync(
                new IpcMessage(MessageType.ForceStop, AppName: appName, Password: password), ct);
            return response.Type == MessageType.Ok;
        }
        catch (IOException)
        {
            return false;   // daemon down or connection dropped: degrade gracefully
        }
        catch (SocketException)
        {
            return false;
        }
    }
}