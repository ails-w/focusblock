using FluentAssertions;

using FocusBlock.Contracts;
using FocusBlock.Core;
using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class BlockEngineTests
{
    private static BlockEngine NewEngine() => new(new AuthService());

    private static BlockRuleConfig DayRule() => new()
    {
        AppName = "firefox",
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(17, 0),
        Enabled = true,
    };

    private static BlockRuleConfig MidnightRule() => new()
    {
        AppName = "steam",
        StartTime = new TimeOnly(22, 0),
        EndTime = new TimeOnly(6, 0),
        Enabled = true,
    };

    [Fact]
    public void BlockEngine_Evaluate_ReturnsBlock_WhenInSchedule()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(DayRule(), new TimeOnly(12, 0));

        blocked.Should().BeTrue();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsNoBlock_WhenOutsideSchedule()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(DayRule(), new TimeOnly(18, 0));

        blocked.Should().BeFalse();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsNoBlock_WhenRuleDisabled()
    {
        var rule = DayRule();
        rule.Enabled = false;
        var engine = NewEngine();

        var blocked = engine.Evaluate(rule, new TimeOnly(12, 0));

        blocked.Should().BeFalse();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsBlock_OnStartBoundary()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(DayRule(), new TimeOnly(9, 0));

        blocked.Should().BeTrue();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsNoBlock_OnEndBoundary()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(DayRule(), new TimeOnly(17, 0));

        blocked.Should().BeFalse();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsBlock_WhenScheduleCrossesMidnight()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(MidnightRule(), new TimeOnly(23, 0));

        blocked.Should().BeTrue();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsBlock_EarlyMorning_WhenScheduleCrossesMidnight()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(MidnightRule(), new TimeOnly(3, 0));

        blocked.Should().BeTrue();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsNoBlock_WhenOutsideMidnightSchedule()
    {
        var engine = NewEngine();

        var blocked = engine.Evaluate(MidnightRule(), new TimeOnly(12, 0));

        blocked.Should().BeFalse();
    }

    [Fact]
    public void BlockEngine_Evaluate_ReturnsNoBlock_WhenWindowIsDegenerate()
    {
        var rule = DayRule();
        rule.StartTime = new TimeOnly(10, 0);
        rule.EndTime = new TimeOnly(10, 0);
        var engine = NewEngine();

        var blocked = engine.Evaluate(rule, new TimeOnly(10, 0));

        blocked.Should().BeFalse();
    }

    [Fact]
    public void BlockEngine_GetAppsToBlock_ReturnsOnlyRulesInSchedule()
    {
        var config = new AppConfig
        {
            BlockRules =
            [
                new BlockRuleConfig
                {
                    AppName = "firefox",
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Enabled = true,
                },
                new BlockRuleConfig
                {
                    AppName = "steam",
                    StartTime = new TimeOnly(18, 0),
                    EndTime = new TimeOnly(20, 0),
                    Enabled = true,
                },
            ],
        };
        var engine = NewEngine();

        var apps = engine.GetAppsToBlock(config, new TimeOnly(12, 0));

        apps.Should().Equal("firefox");
    }

    [Fact]
    public void BlockEngine_TryEarlyStop_ReturnsTrue_WhenPasswordCorrect()
    {
        var auth = new AuthService();
        var (hash, salt) = auth.HashPassword("secreto");
        var security = new SecurityConfig { PasswordHash = hash, PasswordSalt = salt };

        var authorized = new BlockEngine(auth).TryEarlyStop("secreto", security);

        authorized.Should().BeTrue();
    }

    [Fact]
    public void BlockEngine_TryEarlyStop_ReturnsFalse_WhenPasswordWrong()
    {
        var auth = new AuthService();
        var (hash, salt) = auth.HashPassword("secreto");
        var security = new SecurityConfig { PasswordHash = hash, PasswordSalt = salt };

        var authorized = new BlockEngine(auth).TryEarlyStop("incorrecta", security);

        authorized.Should().BeFalse();
    }

    [Fact]
    public void BlockEngine_TryEarlyStop_ReturnsFalse_WhenNoPasswordConfigured()
    {
        var auth = new AuthService();
        var security = new SecurityConfig();

        var authorized = new BlockEngine(auth).TryEarlyStop("cualquiera", security);

        authorized.Should().BeFalse();
    }
}