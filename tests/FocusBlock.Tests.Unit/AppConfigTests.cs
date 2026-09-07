using FluentAssertions;

using FocusBlock.Contracts;

namespace FocusBlock.Tests.Unit;

public class AppConfigTests
{
    [Fact]
    public void AppConfig_DefaultValues_AreCorrect()
    {
        var config = new AppConfig();

        config.BlockRules.Should().BeEmpty();
        config.Security.Should().NotBeNull();
        config.Security.CooldownMinutes.Should().Be(10);
    }

    [Fact]
    public void AppConfig_SerializeDeserialize_RoundTrips()
    {
        var original = new AppConfig
        {
            BlockRules =
            [
                new BlockRuleConfig { AppName = "firefox", StartTime = new(9, 30), EndTime = new(17, 0) },
            ],
            Security = new SecurityConfig { CooldownMinutes = 25 },
        };

        string json = ConfigSerializer.Serialize(original);
        AppConfig result = ConfigSerializer.Deserialize(json);

        result.BlockRules.Should().HaveCount(1);
        result.BlockRules[0].AppName.Should().Be("firefox");
        result.BlockRules[0].StartTime.Should().Be(new TimeOnly(9, 30));
        result.Security.CooldownMinutes.Should().Be(25);
    }
}