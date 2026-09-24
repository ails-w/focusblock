namespace FocusBlock.Daemon.Services;

/// <summary>A running process: its pid and its name as reported by /proc/&lt;pid&gt;/status.</summary>
public readonly record struct ProcessInfo(int Pid, string Name);