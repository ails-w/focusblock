namespace FocusBlock.Daemon.Services;

public class ProcessMonitor
{
    public IEnumerable<string> GetRunningProcesses()
    {
        foreach (string entry in Directory.GetDirectories("/proc"))
        {
            string pid = Path.GetFileName(entry);
            if (!int.TryParse(pid, out _))
            {
                continue;
            }

            string statusPath = Path.Combine(entry, "status");
            if (!File.Exists(statusPath))
            {
                continue;
            }

            string? processName = ExtractProcessName(File.ReadAllText(statusPath));
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