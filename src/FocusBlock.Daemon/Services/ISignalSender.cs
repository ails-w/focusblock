namespace FocusBlock.Daemon.Services;

/// <summary>Seam over the OS signal syscall (real: libc kill()).</summary>
public interface ISignalSender
{
    /// <summary>Sends the signal to the pid. Returns true when the syscall succeeded.</summary>
    bool Send(int pid, Signal signal);
}