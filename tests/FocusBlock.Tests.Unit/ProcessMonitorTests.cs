using FluentAssertions;

using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class ProcessMonitorTests
{
    [Fact]
    public void ProcessMonitor_GetRunningProcesses_ReturnsList()
    {
        var monitor = new ProcessMonitor();

        List<string> processes = monitor.GetRunningProcesses().ToList();

        processes.Should().NotBeEmpty();
    }

    [Fact]
    public void ProcessMonitor_ExtractProcessName_ParsesStatus()
    {
        const string status = "Name:\tfirefox\nUmask:\t0022\nState:\tS (sleeping)\n";

        new ProcessMonitor().ExtractProcessName(status).Should().Be("firefox");
    }
}