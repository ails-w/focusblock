namespace FocusBlock.Tui.Models;

public record DaemonStatus(bool IsRunning, TimeSpan Uptime, int ActiveBlocks);