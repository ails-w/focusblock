using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using FocusBlock.Contracts;

namespace FocusBlock.Tui.Services;

/// <summary>
/// Unix domain socket client for the TUI↔daemon IPC channel. Opens a fresh connection per
/// request (connect-per-request), writes one newline-delimited JSON line and reads one response
/// line, then closes the socket.
/// </summary>
/// <remarks>
/// If the daemon is not running, the connect fails at the OS level and the resulting
/// <see cref="SocketException"/> propagates to the caller, which decides how to degrade.
/// </remarks>
public sealed class IpcClient : IIpcClient
{
    private readonly string _socketPath;

    /// <summary>Creates a client that connects to the Unix socket at <paramref name="socketPath"/>.</summary>
    /// <param name="socketPath">Filesystem path of the daemon's Unix domain socket.</param>
    public IpcClient(string socketPath) => _socketPath = socketPath;

    /// <inheritdoc />
    public async Task<IpcMessage> SendAsync(IpcMessage request, CancellationToken ct = default)
    {
        using var socket = new Socket(
            AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(_socketPath), ct).ConfigureAwait(false);

        using var stream = new NetworkStream(socket, ownsSocket: false);
        using var reader = new StreamReader(
            stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
        };

        string json = JsonSerializer.Serialize(request, IpcJson.Options);
        await writer.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);

        string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (line is null)
        {
            throw new IOException("The daemon closed the connection without sending a response.");
        }

        return JsonSerializer.Deserialize<IpcMessage>(line, IpcJson.Options)
            ?? throw new IOException("The daemon sent an invalid IPC response.");
    }
}