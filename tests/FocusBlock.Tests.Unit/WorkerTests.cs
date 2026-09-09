using FluentAssertions;

using FocusBlock.Daemon;

namespace FocusBlock.Tests.Unit;

public class WorkerTests
{
    [Fact]
    public async Task Worker_StartsAndRunsUntilCancelled()
    {
        var worker = new Worker(TimeSpan.FromMilliseconds(10));
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await worker.Started.WaitAsync(TimeSpan.FromSeconds(2));

        cts.Cancel();
        await worker.StopAsync(cts.Token);
    }
}