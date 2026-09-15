using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using FocusBlock.Contracts;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Unix domain socket server for the TUI↔daemon IPC channel. Frames messages as newline-delimited
/// JSON and routes each decoded <see cref="IpcMessage"/> to an <see cref="IIpcRequestHandler"/>.
/// </summary>
/// <remarks>
/// Feature 3.4 covers transport, framing and routing only. Deciding how to answer
/// <c>status</c>/<c>add_block</c>/<c>remove_block</c> belongs to the BlockEngine (Phase 4), which is
/// why the handler is injected through the <see cref="IIpcRequestHandler"/> seam.
/// </remarks>
public sealed class IpcServer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private readonly string _socketPath;
    private readonly IIpcRequestHandler _handler;

    private Socket? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private bool _disposed;

    public IpcServer(string socketPath, IIpcRequestHandler handler)
    {
        _socketPath = socketPath;
        _handler = handler;
    }

    /// <summary>
    /// Creates and binds the socket, starts listening and launches the accept loop. Returns once the
    /// socket is listening, so a client can connect immediately after this call completes.
    /// </summary>
    public Task StartAsync(CancellationToken ct = default)
    {
        DeleteSocketFile();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
        _listener.Listen(1);

        _acceptLoop = AcceptLoopAsync(_listener, _cts.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cancels the accept loop, closes and disposes the listener, and deletes the socket file.
    /// Safe to call more than once.
    /// </summary>
    public async Task StopAsync()
    {
        CancellationTokenSource? cts = _cts;
        _cts = null;
        cts?.Cancel();

        Socket? listener = _listener;
        _listener = null;
        listener?.Dispose();

        Task? acceptLoop = _acceptLoop;
        _acceptLoop = null;
        if (acceptLoop is not null)
        {
            try
            {
                await acceptLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the accept loop is cancelled during shutdown.
            }
        }

        cts?.Dispose();
        DeleteSocketFile();
    }

    /// <summary>Best-effort cleanup: any failure while stopping must not escape.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown raced with cancellation: nothing left to clean up.
        }
    }

    private async Task AcceptLoopAsync(Socket listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Socket connection;
            try
            {
                connection = await listener.AcceptAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            // Each connection runs independently so one bad client cannot stop the accept loop.
            _ = HandleConnectionAsync(connection, ct);
        }
    }

    private async Task HandleConnectionAsync(Socket connection, CancellationToken ct)
    {
        try
        {
            await using NetworkStream stream = new(connection, ownsSocket: true);
            using var reader = new StreamReader(
                stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
            {
                AutoFlush = true,
            };

            while (!ct.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                {
                    break; // The client closed the connection.
                }

                IpcMessage response = await ProcessLineAsync(line, ct).ConfigureAwait(false);
                await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions))
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Server shutdown or client disconnect.
        }
        catch (IOException)
        {
            // Connection dropped mid-read or mid-write.
        }
        catch (SocketException)
        {
            // Connection dropped.
        }
        catch (ObjectDisposedException)
        {
            // Stream or listener was disposed during shutdown.
        }
    }

    private async Task<IpcMessage> ProcessLineAsync(string line, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new IpcMessage(MessageType.Error, Detail: "Empty request line.");
        }

        IpcMessage? request;
        try
        {
            request = JsonSerializer.Deserialize<IpcMessage>(line, JsonOptions);
        }
        catch (JsonException)
        {
            return new IpcMessage(MessageType.Error, Detail: "Request is not valid JSON.");
        }

        if (request is null)
        {
            return new IpcMessage(MessageType.Error, Detail: "Request is not valid JSON.");
        }

        try
        {
            return await _handler.HandleAsync(request, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A failing handler must not tear down the connection: report and keep serving.
            return new IpcMessage(MessageType.Error, Detail: ex.Message);
        }
    }

    private void DeleteSocketFile()
    {
        // File.Delete is a no-op when the path does not exist and unlinks a stale socket file otherwise.
        File.Delete(_socketPath);
    }
}