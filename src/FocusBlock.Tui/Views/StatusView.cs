using FocusBlock.Tui.Models;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace FocusBlock.Tui.Views;

public class StatusView : View
{
    private readonly Label _statusLabel;

    public string StatusText => _statusLabel.Text;

    public StatusView()
    {
        _statusLabel = new Label
        {
            Text = "Daemon: unknown",
            X = 0,
            Y = 0,
        };
        Add(_statusLabel);
    }

    public void RefreshStatus(DaemonStatus status)
    {
        _statusLabel.Text =
            $"Daemon: {(status.IsRunning ? "running" : "stopped")} · " +
            $"Uptime: {status.Uptime} · " +
            $"Active blocks: {status.ActiveBlocks}";
    }
}