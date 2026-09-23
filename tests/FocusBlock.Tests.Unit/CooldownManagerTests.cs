using FluentAssertions;

using FocusBlock.Daemon.Services;

namespace FocusBlock.Tests.Unit;

public class CooldownManagerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(10);

    [Fact]
    public void CooldownManager_StartCooldown_SetsExpiry()
    {
        var manager = new CooldownManager();

        manager.StartCooldown("firefox", T0, Duration);

        manager.IsOnCooldown("firefox", T0).Should().BeTrue();
        manager.IsOnCooldown("firefox", T0 + Duration + TimeSpan.FromMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void CooldownManager_IsOnCooldown_ReturnsTrue_WhenActive()
    {
        var manager = new CooldownManager();
        manager.StartCooldown("firefox", T0, Duration);

        manager.IsOnCooldown("firefox", T0 + TimeSpan.FromMinutes(5)).Should().BeTrue();
    }

    [Fact]
    public void CooldownManager_IsOnCooldown_ReturnsFalse_WhenExpired()
    {
        var manager = new CooldownManager();
        manager.StartCooldown("firefox", T0, Duration);

        manager.IsOnCooldown("firefox", T0 + TimeSpan.FromMinutes(11)).Should().BeFalse();
    }

    [Fact]
    public void CooldownManager_IsOnCooldown_ReturnsFalse_WhenNeverStarted()
    {
        var manager = new CooldownManager();

        manager.IsOnCooldown("never-started", T0).Should().BeFalse();
    }

    [Fact]
    public void CooldownManager_IsOnCooldown_ReturnsFalse_OnExactExpiry()
    {
        var manager = new CooldownManager();
        manager.StartCooldown("firefox", T0, Duration);

        manager.IsOnCooldown("firefox", T0 + Duration).Should().BeFalse();
    }

    [Fact]
    public void CooldownManager_StartCooldown_IsPerApp()
    {
        var manager = new CooldownManager();

        manager.StartCooldown("firefox", T0, Duration);

        manager.IsOnCooldown("steam", T0).Should().BeFalse();
    }

    [Fact]
    public void CooldownManager_StartCooldown_ExtendsExpiry_WhenCalledAgain()
    {
        var manager = new CooldownManager();
        manager.StartCooldown("firefox", T0, Duration);

        manager.StartCooldown("firefox", T0 + TimeSpan.FromMinutes(5), Duration);

        manager.IsOnCooldown("firefox", T0 + TimeSpan.FromMinutes(14)).Should().BeTrue();
        manager.IsOnCooldown("firefox", T0 + TimeSpan.FromMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void CooldownManager_IsThreadSafe_UnderConcurrentAccess()
    {
        var manager = new CooldownManager();

        Action concurrentAccess = () => Parallel.For(0, 200, i =>
        {
            manager.StartCooldown("shared", T0, Duration);
            manager.IsOnCooldown("shared", T0).Should().BeTrue();

            string appName = $"app-{i}";
            manager.StartCooldown(appName, T0, Duration);
            manager.IsOnCooldown(appName, T0).Should().BeTrue();
        });

        concurrentAccess.Should().NotThrow();
        manager.IsOnCooldown("shared", T0 + TimeSpan.FromMinutes(5)).Should().BeTrue();
        manager.IsOnCooldown("app-42", T0 + TimeSpan.FromMinutes(5)).Should().BeTrue();
        manager.IsOnCooldown("never-started", T0).Should().BeFalse();
    }
}