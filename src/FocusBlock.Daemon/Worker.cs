using Microsoft.Extensions.Hosting;

namespace FocusBlock.Daemon;

public class Worker : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly Func<CancellationToken, Task> _tick;
    private readonly TaskCompletionSource _started =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _started.Task;

    public Worker(TimeSpan? interval = null, Func<CancellationToken, Task>? tick = null)
    {
        _interval = interval ?? TimeSpan.FromSeconds(5);
        _tick = tick ?? (_ => Task.CompletedTask);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _started.TrySetResult();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _tick(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown: StopAsync cancels the loop.
        }
    }
}