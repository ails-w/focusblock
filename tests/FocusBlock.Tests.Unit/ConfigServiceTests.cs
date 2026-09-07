using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Tui.Services;

namespace FocusBlock.Tests.Unit;

public class ConfigServiceTests
{
    [Fact]
    public async Task ConfigService_LoadAsync_ReturnsDefaults_WhenFileMissing()
    {
        var service = new ConfigService("/nonexistent/path/config.json");

        AppConfig config = await service.LoadAsync();

        config.BlockRules.Should().BeEmpty();
        config.Security.CooldownMinutes.Should().Be(10);
    }

    [Fact]
    public async Task ConfigService_SaveAsync_PersistsToFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"focusblock-test-{Guid.NewGuid():N}.json");
        try
        {
            var service = new ConfigService(path);
            var config = new AppConfig { Security = new SecurityConfig { CooldownMinutes = 25 } };

            await service.SaveAsync(config);

            File.Exists(path).Should().BeTrue();
            AppConfig loaded = await service.LoadAsync();
            loaded.Security.CooldownMinutes.Should().Be(25);
        }
        finally
        {
            File.Delete(path);
        }
    }
}