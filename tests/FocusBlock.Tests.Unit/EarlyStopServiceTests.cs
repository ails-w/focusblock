using System.Net.Sockets;

using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Tui.Services;

namespace FocusBlock.Tests.Unit;

public class EarlyStopServiceTests
{
    [Fact]
    public async Task EarlyStopService_RequestEarlyStop_ReturnsTrue_WhenDaemonGrants()
    {
        var client = new FakeIpcClient(new IpcMessage(MessageType.Ok));
        var prompt = new FakePrompt("hunter2");
        var service = new EarlyStopService(client, new ChallengeSystem(), prompt);

        bool result = await service.RequestEarlyStopAsync("firefox");

        result.Should().BeTrue();
        client.LastRequest.Should().NotBeNull();
        client.LastRequest!.Type.Should().Be(MessageType.ForceStop);
        client.LastRequest.AppName.Should().Be("firefox");
        client.LastRequest.Password.Should().Be("hunter2");
    }

    [Fact]
    public async Task EarlyStopService_RequestEarlyStop_ReturnsFalse_WhenPromptCancelled()
    {
        var client = new FakeIpcClient(new IpcMessage(MessageType.Ok));
        var prompt = new FakePrompt(null);
        var service = new EarlyStopService(client, new ChallengeSystem(), prompt);

        bool result = await service.RequestEarlyStopAsync("firefox");

        result.Should().BeFalse();
        client.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task EarlyStopService_RequestEarlyStop_ReturnsFalse_WhenDaemonRejects()
    {
        var client = new FakeIpcClient(new IpcMessage(MessageType.Error, Detail: "wrong password"));
        var prompt = new FakePrompt("wrong");
        var service = new EarlyStopService(client, new ChallengeSystem(), prompt);

        bool result = await service.RequestEarlyStopAsync("firefox");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EarlyStopService_RequestEarlyStop_ReturnsFalse_WhenDaemonIsDown()
    {
        var client = new FakeIpcClient(new SocketException((int)SocketError.ConnectionRefused));
        var prompt = new FakePrompt("hunter2");
        var service = new EarlyStopService(client, new ChallengeSystem(), prompt);

        bool result = await service.RequestEarlyStopAsync("firefox");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EarlyStopService_RequestEarlyStop_GeneratesAChallenge_ForThePrompt()
    {
        var client = new FakeIpcClient(new IpcMessage(MessageType.Ok));
        var prompt = new FakePrompt("hunter2");
        var service = new EarlyStopService(client, new ChallengeSystem(), prompt);

        await service.RequestEarlyStopAsync("firefox");

        prompt.LastChallenge.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class FakeIpcClient : IIpcClient
    {
        private readonly IpcMessage? _response;
        private readonly Exception? _toThrow;

        public FakeIpcClient(IpcMessage response) => _response = response;

        public FakeIpcClient(Exception toThrow) => _toThrow = toThrow;

        public IpcMessage? LastRequest { get; private set; }

        public Task<IpcMessage> SendAsync(IpcMessage request, CancellationToken ct = default)
        {
            LastRequest = request;
            if (_toThrow is not null)
            {
                throw _toThrow;
            }

            return Task.FromResult(_response!);
        }
    }

    private sealed class FakePrompt : IEarlyStopPrompt
    {
        private readonly string? _password;

        public FakePrompt(string? password) => _password = password;

        public string? LastChallenge { get; private set; }

        public string? AskForPassword(string challenge)
        {
            LastChallenge = challenge;
            return _password;
        }
    }
}