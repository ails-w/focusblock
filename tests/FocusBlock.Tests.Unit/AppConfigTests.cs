using FocusBlock.Contracts;
using FluentAssertions;

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
}