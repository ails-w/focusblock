using FluentAssertions;

using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class BlockEnforcerTests
{
    [Fact]
    public async Task BlockEnforcer_KillProcessAsync_SendsSigterm_First()
    {
        var sender = new FakeSignalSender();
        var source = new FakeProcessSource(); // The process is gone after the grace period.
        var enforcer = new BlockEnforcer(sender, source, TimeSpan.Zero);

        await enforcer.KillProcessAsync(1234);

        sender.Signals.Select(signal => signal.Signal).Should().Equal(Signal.Term);
    }

    [Fact]
    public async Task BlockEnforcer_KillProcessAsync_EscalatesToSigkill_WhenProcessStillAlive()
    {
        var sender = new FakeSignalSender();
        var source = new FakeProcessSource().WithAlive(1234);
        var enforcer = new BlockEnforcer(sender, source, TimeSpan.Zero);

        await enforcer.KillProcessAsync(1234);

        sender.Signals.Select(signal => signal.Signal).Should().Equal(Signal.Term, Signal.Kill);
    }

    [Fact]
    public async Task BlockEnforcer_KillProcessAsync_DoesNotEscalate_WhenProcessExited()
    {
        var sender = new FakeSignalSender();
        var source = new FakeProcessSource();
        var enforcer = new BlockEnforcer(sender, source, TimeSpan.Zero);

        await enforcer.KillProcessAsync(1234);

        sender.Signals.Should().NotContain(signal => signal.Signal == Signal.Kill);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockEnforcer_KillProcessAsync_KillsRealProcess()
    {
        System.Diagnostics.Process? process = System.Diagnostics.Process.Start("sleep", "60");
        process.Should().NotBeNull();

        var enforcer = new BlockEnforcer(
            new LibcSignalSender(),
            new ProcProcessSource(),
            TimeSpan.FromMilliseconds(200));

        try
        {
            await enforcer.KillProcessAsync(process!.Id);

            process.WaitForExit(2000).Should().BeTrue();
        }
        finally
        {
            if (!process!.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private sealed class FakeSignalSender : ISignalSender
    {
        public List<(int Pid, Signal Signal)> Signals { get; } = new();

        public bool Send(int pid, Signal signal)
        {
            Signals.Add((pid, signal));
            return true;
        }
    }

    private sealed class FakeProcessSource : IProcessSource
    {
        private readonly HashSet<int> _alive = new();

        public FakeProcessSource WithAlive(int pid)
        {
            _alive.Add(pid);
            return this;
        }

        public IEnumerable<int> GetProcessIds() => _alive;

        public string? ReadStatus(int pid) => null;

        public bool Exists(int pid) => _alive.Contains(pid);
    }
}
