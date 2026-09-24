using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Core;
using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class DaemonRequestHandlerTests
{
    private const string Password = "correct horse battery staple";

    private static (DaemonRequestHandler Handler, CooldownManager Cooldowns) CreateHandler()
    {
        var auth = new AuthService();
        (string hash, string salt) = auth.HashPassword(Password);
        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                PasswordHash = hash,
                PasswordSalt = salt,
                CooldownMinutes = 10,
            },
        };

        var cooldowns = new CooldownManager();
        var engine = new BlockEngine(auth);
        var handler = new DaemonRequestHandler(engine, cooldowns, config, TimeProvider.System);
        return (handler, cooldowns);
    }

    [Fact]
    public async Task DaemonRequestHandler_HandleAsync_ReturnsStatusResponse_ForStatusRequest()
    {
        var (handler, _) = CreateHandler();

        IpcMessage response = await handler.HandleAsync(new IpcMessage(MessageType.Status));

        response.Type.Should().Be(MessageType.StatusResponse);
        response.Detail.Should().Be("daemon running");
    }

    [Fact]
    public async Task DaemonRequestHandler_HandleAsync_ReturnsOk_WhenForceStopPasswordCorrect()
    {
        var (handler, cooldowns) = CreateHandler();

        IpcMessage response = await handler.HandleAsync(
            new IpcMessage(MessageType.ForceStop, AppName: "firefox", Password: Password));

        response.Type.Should().Be(MessageType.Ok);
        cooldowns.IsOnCooldown("firefox", TimeProvider.System.GetUtcNow()).Should().BeTrue();
    }

    [Fact]
    public async Task DaemonRequestHandler_HandleAsync_ReturnsError_WhenForceStopPasswordWrong()
    {
        var (handler, cooldowns) = CreateHandler();

        IpcMessage response = await handler.HandleAsync(
            new IpcMessage(MessageType.ForceStop, AppName: "firefox", Password: "not-the-password"));

        response.Type.Should().Be(MessageType.Error);
        cooldowns.IsOnCooldown("firefox", TimeProvider.System.GetUtcNow()).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, Password)]
    [InlineData("firefox", null)]
    public async Task DaemonRequestHandler_HandleAsync_ReturnsError_WhenForceStopMissingData(
        string? appName, string? password)
    {
        var (handler, cooldowns) = CreateHandler();

        IpcMessage response = await handler.HandleAsync(
            new IpcMessage(MessageType.ForceStop, AppName: appName, Password: password));

        response.Type.Should().Be(MessageType.Error);
        cooldowns.IsOnCooldown("firefox", TimeProvider.System.GetUtcNow()).Should().BeFalse();
    }

    [Fact]
    public async Task DaemonRequestHandler_HandleAsync_ReturnsError_ForUnsupportedType()
    {
        var (handler, _) = CreateHandler();

        IpcMessage response = await handler.HandleAsync(new IpcMessage(MessageType.AddBlock));

        response.Type.Should().Be(MessageType.Error);
    }
}