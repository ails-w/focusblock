using FocusBlock.Contracts;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Runs one blocking pass: asks the engine which apps are scheduled to be blocked right now,
/// skips the ones on cooldown, and asks the enforcer to terminate every matching process.
/// </summary>
/// <remarks>
/// Thin coordinator only: it holds no blocking policy of its own. The decision lives in
/// <see cref="BlockEngine"/>, the termination in <see cref="BlockEnforcer"/>, the process view in
/// <see cref="ProcessMonitor"/>, and the grace windows in <see cref="CooldownManager"/>. The clock
/// is read once per pass so every app in that pass is evaluated against the same instant.
/// </remarks>
public class BlockCoordinator
{
    private readonly ProcessMonitor _monitor;
    private readonly BlockEngine _engine;
    private readonly BlockEnforcer _enforcer;
    private readonly CooldownManager _cooldowns;
    private readonly AppConfig _config;
    private readonly TimeProvider _time;

    public BlockCoordinator(
        ProcessMonitor monitor,
        BlockEngine engine,
        BlockEnforcer enforcer,
        CooldownManager cooldowns,
        AppConfig config,
        TimeProvider time)
    {
        _monitor = monitor;
        _engine = engine;
        _enforcer = enforcer;
        _cooldowns = cooldowns;
        _config = config;
        _time = time;
    }

    /// <summary>Runs one blocking pass: evaluate rules, skip cooldowns, kill matching processes.</summary>
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        DateTimeOffset nowUtc = _time.GetUtcNow();
        TimeOnly nowLocal = TimeOnly.FromDateTime(_time.GetLocalNow().DateTime);

        IReadOnlyList<string> appsToBlock = _engine.GetAppsToBlock(_config, nowLocal);
        if (appsToBlock.Count == 0)
        {
            return;
        }

        List<ProcessInfo> processes = _monitor.GetProcesses().ToList();
        var killedPids = new HashSet<int>();

        foreach (string app in appsToBlock)
        {
            if (_cooldowns.IsOnCooldown(app, nowUtc))
            {
                continue;
            }

            foreach (ProcessInfo process in processes)
            {
                if (!string.Equals(process.Name, app, StringComparison.Ordinal)
                    || !killedPids.Add(process.Pid))
                {
                    continue;
                }

                await _enforcer.KillProcessAsync(process.Pid, ct);
            }
        }
    }
}