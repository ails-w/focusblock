using FocusBlock.Tui.Models;
using FocusBlock.Tui.Views;
using FluentAssertions;

namespace FocusBlock.Tests.Unit;

public class StatusViewTests
{
    [Fact]
    public void StatusView_DisplaysDaemonStatus()
    {
        var view = new StatusView();

        view.RefreshStatus(new DaemonStatus(IsRunning: true, Uptime: TimeSpan.FromMinutes(5), ActiveBlocks: 2));

        view.StatusText.Should().Contain("running");
        view.StatusText.Should().Contain("Active blocks: 2");
    }
}