using FocusBlock.Contracts;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Seam between IPC transport and the domain logic that answers a request.
/// </summary>
/// <remarks>
/// Handling <c>status</c>/<c>add_block</c>/<c>remove_block</c> needs the <c>BlockEngine</c>, which
/// arrives in Phase 4. Keeping this seam separate lets the transport, framing and routing of
/// Feature 3.4 be tested with a fake handler, while the real handler is wired in Phase 4.
/// </remarks>
public interface IIpcRequestHandler
{
    /// <summary>Handles one decoded request and returns the message to write back.</summary>
    Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken ct = default);
}