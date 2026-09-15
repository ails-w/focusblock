namespace FocusBlock.Daemon.Services;

/// <summary>Real <see cref="IProcessSource"/> reading the Linux /proc filesystem.</summary>
public class ProcProcessSource : IProcessSource
{
    private const string ProcPath = "/proc";

    public IEnumerable<int> GetProcessIds()
    {
        foreach (string entry in Directory.GetDirectories(ProcPath))
        {
            string name = Path.GetFileName(entry);
            if (int.TryParse(name, out int pid))
            {
                yield return pid;
            }
        }
    }

    public string? ReadStatus(int pid)
    {
        try
        {
            return File.ReadAllText(Path.Combine(ProcPath, pid.ToString(), "status"));
        }
        catch (IOException)
        {
            // The process died mid-scan: report as absent instead of crashing the scan.
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            // The process is not readable by this user: report as absent.
            return null;
        }
    }

    public bool Exists(int pid) => Directory.Exists(Path.Combine(ProcPath, pid.ToString()));
}
