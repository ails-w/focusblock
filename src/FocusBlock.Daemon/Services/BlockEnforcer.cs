namespace FocusBlock.Daemon.Services;

/// <summary>
/// Terminates a blocked process given its PID: sends SIGTERM, waits a grace period, and
/// escalates to SIGKILL when the process is still alive.
/// </summary>
/// <remarks>
/// The name-to-PID mapping (deciding <em>which</em> processes to kill) belongs to Phase 4; this
/// class only enforces termination once a PID is known. Known limitation: the PID may be reused
/// between the SIGTERM and the liveness check, so an unrelated process could be signalled. A full
/// mitigation reads the process start time from <c>/proc/&lt;pid&gt;/stat</c> and compares it before
/// escalating.
/// </remarks>
public class BlockEnforcer
{
    private static readonly TimeSpan DefaultGracePeriod = TimeSpan.FromSeconds(2);

    private readonly ISignalSender _sender;
    private readonly IProcessSource _processSource;
    private readonly TimeSpan _gracePeriod;

    public BlockEnforcer(ISignalSender sender, IProcessSource processSource, TimeSpan? gracePeriod = null)
    {
        _sender = sender;
        _processSource = processSource;
        _gracePeriod = gracePeriod ?? DefaultGracePeriod;
    }

    /// <summary>SIGTERM → wait grace period → SIGKILL if the process still exists.</summary>
    public async Task KillProcessAsync(int pid, CancellationToken ct = default)
    {
        _sender.Send(pid, Signal.Term);

        await Task.Delay(_gracePeriod, ct);

        if (_processSource.Exists(pid))
        {
            _sender.Send(pid, Signal.Kill);
        }
    }
}
