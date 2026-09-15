namespace FocusBlock.Contracts;

/// <summary>
/// Identifies the kind of <see cref="IpcMessage"/> exchanged between the TUI (client) and the
/// daemon (server) over the Unix domain socket.
/// </summary>
public enum MessageType
{
    /// <summary>Request: ask the daemon for its current blocking status.</summary>
    Status,

    /// <summary>Response to <see cref="Status"/>.</summary>
    StatusResponse,

    /// <summary>Request: add a block rule for an application.</summary>
    AddBlock,

    /// <summary>Request: remove an existing block rule.</summary>
    RemoveBlock,

    /// <summary>Response: the request succeeded.</summary>
    Ok,

    /// <summary>Response: the request failed or could not be parsed.</summary>
    Error,
}

/// <summary>
/// Shared TUI↔daemon wire contract. One message is serialized as a single JSON line using
/// <c>snake_case</c> naming, e.g. <c>{"type":"add_block","app_name":"firefox"}</c>.
/// </summary>
/// <param name="Type">The message kind; drives how the receiver routes it.</param>
/// <param name="AppName">Application name for <see cref="MessageType.AddBlock"/>/<see cref="MessageType.RemoveBlock"/>.</param>
/// <param name="Schedule">Block window (e.g. <c>09:00-17:00</c>) for <see cref="MessageType.AddBlock"/>.</param>
/// <param name="Detail">Human-readable detail, typically set on <see cref="MessageType.Error"/>.</param>
public record IpcMessage(
    MessageType Type,
    string? AppName = null,
    string? Schedule = null,
    string? Detail = null);