using FocusBlock.Contracts;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Real <see cref="IIpcRequestHandler"/>: routes decoded requests to the block engine and the
/// cooldown manager. The daemon runs as root and is the trusted side of the channel, so password
/// verification for an early stop happens here and the secret never leaves this process.
/// </summary>
/// <remarks>
/// Responses never echo the password back: the caller only learns whether the request succeeded.
/// </remarks>
public class DaemonRequestHandler : IIpcRequestHandler
{
    private readonly BlockEngine _engine;
    private readonly CooldownManager _cooldowns;
    private readonly AppConfig _config;
    private readonly TimeProvider _time;

    public DaemonRequestHandler(
        BlockEngine engine,
        CooldownManager cooldowns,
        AppConfig config,
        TimeProvider time)
    {
        _engine = engine;
        _cooldowns = cooldowns;
        _config = config;
        _time = time;
    }

    /// <inheritdoc/>
    public Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken ct = default)
    {
        IpcMessage response = request.Type switch
        {
            MessageType.Status => new IpcMessage(
                MessageType.StatusResponse, Detail: "daemon running"),
            MessageType.ForceStop => HandleForceStop(request),
            _ => new IpcMessage(
                MessageType.Error, Detail: $"Unsupported message type: {request.Type}"),
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// Handles an early-stop request. The password is verified on the trusted daemon side and is
    /// never logged or echoed back. A successful verification starts the per-app cooldown so the
    /// app is not immediately re-blocked; a failed one leaves the cooldown untouched.
    /// </summary>
    private IpcMessage HandleForceStop(IpcMessage request)
    {
        if (string.IsNullOrWhiteSpace(request.AppName) || request.Password is null)
        {
            return new IpcMessage(
                MessageType.Error, Detail: "force_stop requires app_name and password");
        }

        if (!_engine.TryEarlyStop(request.Password, _config.Security))
        {
            return new IpcMessage(MessageType.Error, Detail: "invalid password");
        }

        _cooldowns.StartCooldown(
            request.AppName,
            _time.GetUtcNow(),
            TimeSpan.FromMinutes(_config.Security.CooldownMinutes));

        return new IpcMessage(MessageType.Ok);
    }
}