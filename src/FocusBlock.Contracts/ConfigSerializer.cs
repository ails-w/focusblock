using System.Text.Json;

namespace FocusBlock.Contracts;

public static class ConfigSerializer
{
    // JsonSerializerOptions builds and caches metadata on first use, so it must be reused
    // instead of allocated per call.
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(AppConfig config) => JsonSerializer.Serialize(config, Options);

    public static AppConfig Deserialize(string json) =>
        JsonSerializer.Deserialize<AppConfig>(json)
        ?? throw new JsonException("Config JSON is empty or invalid.");
}