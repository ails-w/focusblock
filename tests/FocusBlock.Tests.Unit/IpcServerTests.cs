using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class IpcServerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    [Fact]
    public async Task IpcServer_HandlesStatusRequest()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(_ => new IpcMessage(MessageType.StatusResponse));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        await using var client = await TestClient.ConnectAsync(path);

        IpcMessage response = await client.SendAndReceiveAsync("{\"type\":\"status\"}");

        response.Type.Should().Be(MessageType.StatusResponse);
    }

    [Fact]
    public async Task IpcServer_HandlesAddBlockRequest()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(_ => new IpcMessage(MessageType.Ok));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        await using var client = await TestClient.ConnectAsync(path);

        IpcMessage response = await client.SendAndReceiveAsync(
            "{\"type\":\"add_block\",\"app_name\":\"firefox\",\"schedule\":\"09:00-17:00\"}");

        response.Type.Should().Be(MessageType.Ok);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Type.Should().Be(MessageType.AddBlock);
        handler.LastRequest.AppName.Should().Be("firefox");
        handler.LastRequest.Schedule.Should().Be("09:00-17:00");
    }

    [Fact]
    public async Task IpcServer_ReturnsError_WhenRequestIsMalformed()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(_ => new IpcMessage(MessageType.StatusResponse));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        await using (var client = await TestClient.ConnectAsync(path))
        {
            IpcMessage response = await client.SendAndReceiveAsync("not json");

            response.Type.Should().Be(MessageType.Error);
        }

        // The malformed line must not have killed the server: a new connection still works.
        await using (var client = await TestClient.ConnectAsync(path))
        {
            IpcMessage response = await client.SendAndReceiveAsync("{\"type\":\"status\"}");

            response.Type.Should().Be(MessageType.StatusResponse);
        }
    }

    [Fact]
    public async Task IpcServer_HandlesMultipleRequests_OnSameConnection()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(request => request.Type == MessageType.Status
            ? new IpcMessage(MessageType.StatusResponse)
            : new IpcMessage(MessageType.Ok));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        await using var client = await TestClient.ConnectAsync(path);

        IpcMessage first = await client.SendAndReceiveAsync("{\"type\":\"status\"}");
        IpcMessage second = await client.SendAndReceiveAsync("{\"type\":\"remove_block\"}");

        first.Type.Should().Be(MessageType.StatusResponse);
        second.Type.Should().Be(MessageType.Ok);
    }

    [Fact]
    public async Task IpcServer_DeletesSocketFile_AfterStop()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(_ => new IpcMessage(MessageType.Ok));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        File.Exists(path).Should().BeTrue();

        await server.StopAsync();

        File.Exists(path).Should().BeFalse();
    }

    private static string NewSocketPath() =>
        Path.Combine(Path.GetTempPath(), $"focusblock-test-{Guid.NewGuid():N}.sock");

    private static IpcMessage Parse(string json) =>
        JsonSerializer.Deserialize<IpcMessage>(json, JsonOptions)!;

    private sealed class FakeRequestHandler : IIpcRequestHandler
    {
        private readonly Func<IpcMessage, IpcMessage> _respond;

        public FakeRequestHandler(Func<IpcMessage, IpcMessage> respond) => _respond = respond;

        public IpcMessage? LastRequest { get; private set; }

        public Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class TestClient : IAsyncDisposable
    {
        private readonly NetworkStream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private TestClient(NetworkStream stream, StreamReader reader, StreamWriter writer)
        {
            _stream = stream;
            _reader = reader;
            _writer = writer;
        }

        public static async Task<TestClient> ConnectAsync(string socketPath)
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));

            var stream = new NetworkStream(socket, ownsSocket: true);
            var reader = new StreamReader(
                stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
            {
                AutoFlush = true,
            };

            return new TestClient(stream, reader, writer);
        }

        public async Task<IpcMessage> SendAndReceiveAsync(string line)
        {
            await _writer.WriteLineAsync(line);

            string? response = await _reader.ReadLineAsync();
            response.Should().NotBeNull();

            return Parse(response!);
        }

        public async ValueTask DisposeAsync()
        {
            _reader.Dispose();
            _writer.Dispose();
            await _stream.DisposeAsync();
        }
    }
}