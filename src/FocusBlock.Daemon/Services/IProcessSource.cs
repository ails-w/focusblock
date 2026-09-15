namespace FocusBlock.Daemon.Services;

/// <summary>Seam over the OS process source (real: /proc).</summary>
public interface IProcessSource
{
    IEnumerable<int> GetProcessIds();
    string? ReadStatus(int pid);
    bool Exists(int pid);
}
