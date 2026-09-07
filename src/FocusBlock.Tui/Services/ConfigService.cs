using FocusBlock.Contracts;

namespace FocusBlock.Tui.Services;

public class ConfigService
{
    private readonly string _path;

    public ConfigService(string path)
    {
        _path = path;
    }

    public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            return new AppConfig();
        }

        string json = await File.ReadAllTextAsync(_path, ct);
        return ConfigSerializer.Deserialize(json);
    }

    public async Task SaveAsync(AppConfig config, CancellationToken ct = default)
    {
        string json = ConfigSerializer.Serialize(config);
        await File.WriteAllTextAsync(_path, json, ct);
    }
}