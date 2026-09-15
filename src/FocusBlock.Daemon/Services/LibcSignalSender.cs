using System.Runtime.InteropServices;

namespace FocusBlock.Daemon.Services;

/// <summary>Real <see cref="ISignalSender"/> calling libc kill().</summary>
public class LibcSignalSender : ISignalSender
{
    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    public bool Send(int pid, Signal signal) => kill(pid, (int)signal) == 0;
}
