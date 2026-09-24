using FluentAssertions;

using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class ProcessMonitorTests
{
    [Fact]
    public void ProcessMonitor_ExtractProcessName_ParsesStatus()
    {
        const string status = "Name:\tfirefox\nUmask:\t0022\nState:\tS (sleeping)\n";

        new ProcessMonitor(new FakeProcessSource())
            .ExtractProcessName(status)
            .Should()
            .Be("firefox");
    }

    [Fact]
    public void ProcessMonitor_ExtractProcessName_ReturnsNull_WhenNameLineMissing()
    {
        const string status = "Umask:\t0022\nState:\tS (sleeping)\n";

        new ProcessMonitor(new FakeProcessSource()).ExtractProcessName(status).Should().BeNull();
    }

    [Fact]
    public void ProcessMonitor_GetRunningProcesses_ReturnsList()
    {
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\nState:\tS (sleeping)\n")
            .WithProcess(202, "Name:\tbash\nState:\tS (sleeping)\n");
        var monitor = new ProcessMonitor(source);

        List<string> processes = monitor.GetRunningProcesses().ToList();

        processes.Should().Equal("firefox", "bash");
    }

    [Fact]
    public void ProcessMonitor_GetRunningProcesses_SkipsProcesses_WithoutStatus()
    {
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\n")
            .WithMissingStatus(202)
            .WithProcess(303, "Name:\tbash\n");
        var monitor = new ProcessMonitor(source);

        List<string> processes = monitor.GetRunningProcesses().ToList();

        processes.Should().Equal("firefox", "bash");
    }

    [Fact]
    public void ProcessMonitor_GetRunningProcesses_SkipsProcesses_WithoutName()
    {
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\n")
            .WithProcess(202, "Umask:\t0022\n")
            .WithProcess(303, "Name:\tbash\n");
        var monitor = new ProcessMonitor(source);

        List<string> processes = monitor.GetRunningProcesses().ToList();

        processes.Should().Equal("firefox", "bash");
    }

    [Fact]
    public void ProcessMonitor_GetProcesses_ReturnsPidAndName()
    {
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\nState:\tS (sleeping)\n")
            .WithProcess(202, "Name:\tbash\nState:\tS (sleeping)\n");
        var monitor = new ProcessMonitor(source);

        List<ProcessInfo> processes = monitor.GetProcesses().ToList();

        processes.Should().Equal(new ProcessInfo(101, "firefox"), new ProcessInfo(202, "bash"));
    }

    [Fact]
    public void ProcessMonitor_GetProcesses_SkipsProcesses_WithoutStatus()
    {
        var source = new FakeProcessSource()
            .WithProcess(101, "Name:\tfirefox\n")
            .WithMissingStatus(202)
            .WithProcess(303, "Name:\tbash\n");
        var monitor = new ProcessMonitor(source);

        List<ProcessInfo> processes = monitor.GetProcesses().ToList();

        processes.Should().Equal(new ProcessInfo(101, "firefox"), new ProcessInfo(303, "bash"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ProcProcessSource_GetProcessIds_ContainsCurrentProcess()
    {
        var source = new ProcProcessSource();

        source.GetProcessIds().Should().Contain(Environment.ProcessId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ProcProcessSource_Exists_ReturnsTrue_ForCurrentProcess()
    {
        var source = new ProcProcessSource();

        source.Exists(Environment.ProcessId).Should().BeTrue();
    }

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

        public FakeProcessSource WithMissingStatus(int pid)
        {
            _pids.Add(pid);
            return this;
        }

        public IEnumerable<int> GetProcessIds() => _pids;

        public string? ReadStatus(int pid) =>
            _statuses.TryGetValue(pid, out string? status) ? status : null;

        public bool Exists(int pid) => _pids.Contains(pid);
    }
}