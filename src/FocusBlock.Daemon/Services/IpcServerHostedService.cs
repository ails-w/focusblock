using Microsoft.Extensions.Hosting;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Hosts the <see cref="IpcServer"/> for the lifetime of the application: starts the listener on
/// startup and stops it on shutdown so the socket file is removed cleanly.
/// </summary>
public sealed class IpcServerHostedService : BackgroundService
{
    private readonly IpcServer _server;

    public IpcServerHostedService(IpcServer server) => _server = server;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _server.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown: the host cancels the token to stop this service.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _server.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}