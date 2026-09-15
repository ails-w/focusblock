using FluentAssertions;

using FocusBlock.Daemon;

namespace FocusBlock.Tests.Unit;

public class WorkerTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task Worker_StartsAndRunsUntilCancelled()
    {
        var worker = new Worker(Interval);

        await worker.StartAsync(CancellationToken.None);
        await worker.Started.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        worker.Started.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Worker_InvokesTick_WhileRunning()
    {
        int tickCount = 0;
        var firstTick =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Worker(Interval, _ =>
        {
            if (Interlocked.Increment(ref tickCount) == 1)
            {
                firstTick.TrySetResult();
            }

            return Task.CompletedTask;
        });

        await worker.StartAsync(CancellationToken.None);
        await firstTick.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        tickCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task Worker_StopsInvokingTick_AfterStopAsync()
    {
        int tickCount = 0;
        var worker = new Worker(Interval, _ =>
        {
            Interlocked.Increment(ref tickCount);
            return Task.CompletedTask;
        });

        await worker.StartAsync(CancellationToken.None);
        await worker.Started.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        int countAfterStop = Volatile.Read(ref tickCount);
        await Task.Delay(50);

        Volatile.Read(ref tickCount).Should().Be(countAfterStop);
    }
}