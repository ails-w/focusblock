using Microsoft.Extensions.Hosting;

namespace FocusBlock.Daemon;

public class Worker : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly TaskCompletionSource _started =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _started.Task;

    public Worker(TimeSpan? interval = null)
    {
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _started.TrySetResult();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown: StopAsync cancels the loop.
        }
    }
}