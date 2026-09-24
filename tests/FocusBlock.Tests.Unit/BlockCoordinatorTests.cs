using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Core;
using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class BlockCoordinatorTests
{
    /// <summary>
    /// Fixed instant used by every test: 12:00 local. Time is injected through a
    /// <see cref="FixedTimeProvider"/> so rule windows are deterministic (no wall-clock flakiness).
    /// In-schedule rules use 09:00-17:00; the outside-schedule rule uses 20:00-21:00.
    /// </summary>
    private static readonly DateTimeOffset NoonUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BlockCoordinator_RunOnce_DoesNothing_WhenOutsideSchedule()
    {
        var config = Config(Rule("firefox", new TimeOnly(20, 0), new TimeOnly(21, 0)));
        var source = new FakeProcessSource().WithProcess(101, "Name:\tfirefox\n");
        var sender = new FakeSignalSender();

        await NewCoordinator(config, source, sender).RunOnceAsync();

        sender.Signals.Should().BeEmpty();
    }

    [Fact]
    public async Task BlockCoordinator_RunOnce_KillsMatchingProcess_WhenInSchedule()
    {
        var config = Config(Rule("firefox", new TimeOnly(9, 0), new TimeOnly(17, 0)));
        var source = new FakeProcessSource().WithProcess(101, "Name:\tfirefox\n");
        var sender = new FakeSignalSender();

        await NewCoordinator(config, source, sender).RunOnceAsync();

        sender.Signals.Should().Equal((101, Signal.Term));
    }

    [Fact]
    public async Task BlockCoordinator_RunOnce_KillsAllMatchingPids()
    {
        var config = Config(Rule("firefox", new TimeOnly(9, 0), new TimeOnly(17, 0)));
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\n")
            .WithProcess(202, "Name:\tfirefox\n");
        var sender = new FakeSignalSender();

        await NewCoordinator(config, source, sender).RunOnceAsync();

        sender.Signals.Should().Equal((101, Signal.Term), (202, Signal.Term));
    }

    [Fact]
    public async Task BlockCoordinator_RunOnce_DoesNotKillUnrelatedProcesses()
    {
        var config = Config(Rule("firefox", new TimeOnly(9, 0), new TimeOnly(17, 0)));
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\n")
            .WithProcess(202, "Name:\tbash\n");
        var sender = new FakeSignalSender();

        await NewCoordinator(config, source, sender).RunOnceAsync();

        sender.Signals.Should().Equal((101, Signal.Term));
    }

    [Fact]
    public async Task BlockCoordinator_RunOnce_SkipsApp_WhenOnCooldown()
    {
        var config = Config(Rule("firefox", new TimeOnly(9, 0), new TimeOnly(17, 0)));
        var source = new FakeProcessSource().WithProcess(101, "Name:\tfirefox\n");
        var sender = new FakeSignalSender();
        var cooldowns = new CooldownManager();
        cooldowns.StartCooldown("firefox", NoonUtc, TimeSpan.FromHours(1));

        await NewCoordinator(config, source, sender, cooldowns).RunOnceAsync();

        sender.Signals.Should().BeEmpty();
    }

    [Fact]
    public async Task BlockCoordinator_RunOnce_KillsPidOnce_WhenDuplicateRulesMatchSameApp()
    {
        var config = Config(
            Rule("firefox", new TimeOnly(9, 0), new TimeOnly(17, 0)),
            Rule("firefox", new TimeOnly(8, 0), new TimeOnly(18, 0)));
        var source = new FakeProcessSource().WithProcess(101, "Name:\tfirefox\n");
        var sender = new FakeSignalSender();

        await NewCoordinator(config, source, sender).RunOnceAsync();

        sender.Signals.Should().Equal((101, Signal.Term));
    }

    [Fact]
    public async Task BlockCoordinator_RunOnce_DoesNotScanProcesses_WhenNothingToBlock()
    {
        // No rules are active, so the pass must return before touching the process source.
        var sender = new FakeSignalSender();

        Func<Task> act = () => NewCoordinator(Config(), new ThrowingProcessSource(), sender)
            .RunOnceAsync();

        await act.Should().NotThrowAsync();
        sender.Signals.Should().BeEmpty();
    }

    private static AppConfig Config(params BlockRuleConfig[] rules) => new() { BlockRules = [.. rules] };

    private static BlockRuleConfig Rule(string appName, TimeOnly start, TimeOnly end) => new()
    {
        AppName = appName,
        StartTime = start,
        EndTime = end,
        Enabled = true,
    };

    private static BlockCoordinator NewCoordinator(
        AppConfig config,
        IProcessSource source,
        FakeSignalSender sender,
        CooldownManager? cooldowns = null)
    {
        var monitor = new ProcessMonitor(source);
        var engine = new BlockEngine(new AuthService());
        var enforcer = new BlockEnforcer(sender, source, TimeSpan.Zero);
        return new BlockCoordinator(
            monitor,
            engine,
            enforcer,
            cooldowns ?? new CooldownManager(),
            config,
            new FixedTimeProvider(NoonUtc));
    }

    /// <summary>Deterministic clock: always the same UTC instant, with UTC as the local zone.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
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

    /// <summary>
    /// In-memory process source. <see cref="Exists"/> always reports false so the enforcer never
    /// escalates: each kill produces exactly one SIGTERM, which keeps the assertions focused on
    /// the coordinator's selection logic.
    /// </summary>
    private sealed class FakeProcessSource : IProcessSource
    {
        private readonly List<int> _pids = new();
        private readonly Dictionary<int, string> _statuses = new();

        public FakeProcessSource WithProcess(int pid, string status)
        {
            _pids.Add(pid);
            _statuses[pid] = status;
            return this;
        }

        public IEnumerable<int> GetProcessIds() => _pids;

        public string? ReadStatus(int pid) =>
            _statuses.TryGetValue(pid, out string? status) ? status : null;

        public bool Exists(int pid) => false;
    }

    /// <summary>Source that fails the test if the coordinator enumerates it at all.</summary>
    private sealed class ThrowingProcessSource : IProcessSource
    {
        public IEnumerable<int> GetProcessIds() =>
            throw new InvalidOperationException("The process source must not be scanned.");

        public string? ReadStatus(int pid) => null;

        public bool Exists(int pid) => false;
    }
}