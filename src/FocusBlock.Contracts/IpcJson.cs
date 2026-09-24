using System.Text.Json;
using System.Text.Json.Serialization;

namespace FocusBlock.Contracts;

/// <summary>Shared JSON options for the IPC wire format (snake_case properties, string enums).</summary>
public static class IpcJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}