using System.Collections.Concurrent;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Tracks per-app cooldown (grace) windows. After an early stop an app enters a cooldown during
/// which the daemon must not re-block it. Instants are supplied by the caller, so the manager
/// stays deterministic and free of a hidden clock.
/// </summary>
public class CooldownManager
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _expiries = new(StringComparer.Ordinal);

    /// <summary>Starts (or extends) the cooldown for <paramref name="appName"/>.</summary>
    public void StartCooldown(string appName, DateTimeOffset now, TimeSpan duration) =>
        _expiries[appName] = now + duration;

    /// <summary>True while <paramref name="now"/> is strictly before the app's expiry.</summary>
    public bool IsOnCooldown(string appName, DateTimeOffset now) =>
        _expiries.TryGetValue(appName, out DateTimeOffset expiry) && now < expiry;
}