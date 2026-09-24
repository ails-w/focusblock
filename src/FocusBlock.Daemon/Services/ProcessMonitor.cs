namespace FocusBlock.Daemon.Services;

public class ProcessMonitor
{
    private readonly IProcessSource _source;

    public ProcessMonitor(IProcessSource source)
    {
        _source = source;
    }

    /// <summary>
    /// Enumerates running processes as (pid, name) pairs. Processes that die mid-scan or whose
    /// name cannot be parsed are skipped instead of aborting the scan.
    /// </summary>
    /// <remarks>
    /// The <c>Name:</c> field in <c>/proc/&lt;pid&gt;/status</c> is the kernel <c>comm</c> value, which
    /// the kernel truncates to <b>15 characters</b>. A configured app name longer than that can
    /// never match a process here, because the reported name is already truncated.
    /// </remarks>
    public IEnumerable<ProcessInfo> GetProcesses()
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
                yield return new ProcessInfo(pid, processName);
            }
        }
    }

    public IEnumerable<string> GetRunningProcesses() => GetProcesses().Select(p => p.Name);

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