namespace FocusBlock.Daemon.Services;

public class ProcessMonitor
{
    private readonly IProcessSource _source;

    public ProcessMonitor(IProcessSource source)
    {
        _source = source;
    }

    public IEnumerable<string> GetRunningProcesses()
    {
        foreach (int pid in _source.GetProcessIds())
        {
            string? status = _source.ReadStatus(pid);
            if (status is null)
            {
                continue;
            }

            string? processName = ExtractProcessName(status);
            if (!string.IsNullOrEmpty(processName))
            {
                yield return processName;
            }
        }
    }

    public string? ExtractProcessName(string statusContent)
    {
        foreach (string line in statusContent.Split('\n'))
        {
            if (line.StartsWith("Name:", StringComparison.Ordinal))
            {
                return line["Name:".Length..].Trim();
            }
        }

        return null;
    }
}