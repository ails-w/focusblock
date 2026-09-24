using FocusBlock.Contracts;

namespace FocusBlock.Tui.Services;

/// <summary>Client seam for the TUI↔daemon IPC channel, so the early-stop flow can be faked in tests.</summary>
public interface IIpcClient
{
    /// <summary>Sends one <paramref name="request"/> and returns the daemon's response.</summary>
    Task<IpcMessage> SendAsync(IpcMessage request, CancellationToken ct = default);
}