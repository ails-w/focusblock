using System.Text.Json;

namespace FocusBlock.Contracts;

public static class ConfigSerializer
{
    public static string Serialize(AppConfig config) =>
        JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

    public static AppConfig Deserialize(string json) =>
        JsonSerializer.Deserialize<AppConfig>(json)
        ?? throw new JsonException("Config JSON is empty or invalid.");
}