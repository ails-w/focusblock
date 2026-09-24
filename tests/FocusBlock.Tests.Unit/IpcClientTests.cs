using System.Net.Sockets;

using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Daemon.Services;
using FocusBlock.Tui.Services;

namespace FocusBlock.Tests.Unit;

public class IpcClientTests
{
    [Fact]
    public async Task IpcClient_SendAsync_ReturnsResponse_FromServer()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(_ => new IpcMessage(MessageType.StatusResponse));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        var client = new IpcClient(path);

        IpcMessage response = await client.SendAsync(new IpcMessage(MessageType.Status));

        response.Type.Should().Be(MessageType.StatusResponse);
    }

    [Fact]
    public async Task IpcClient_SendAsync_RoundTripsAppNameAndPassword_ForForceStop()
    {
        string path = NewSocketPath();
        var handler = new FakeRequestHandler(_ => new IpcMessage(MessageType.Ok));
        await using var server = new IpcServer(path, handler);
        await server.StartAsync();

        var client = new IpcClient(path);

        IpcMessage response = await client.SendAsync(
            new IpcMessage(MessageType.ForceStop, AppName: "firefox", Password: "hunter2"));

        response.Type.Should().Be(MessageType.Ok);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Type.Should().Be(MessageType.ForceStop);
        handler.LastRequest.AppName.Should().Be("firefox");
        handler.LastRequest.Password.Should().Be("hunter2");
    }

    [Fact]
    public async Task IpcClient_SendAsync_Throws_WhenDaemonNotRunning()
    {
        // No server is listening on this path, so connect() fails at the OS level.
        var client = new IpcClient(NewSocketPath());

        Func<Task> act = () => client.SendAsync(new IpcMessage(MessageType.Status));

        await act.Should().ThrowAsync<SocketException>();
    }

    private static string NewSocketPath() =>
        Path.Combine(Path.GetTempPath(), $"focusblock-client-test-{Guid.NewGuid():N}.sock");

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
}