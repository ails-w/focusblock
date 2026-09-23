using FocusBlock.Contracts;

namespace FocusBlock.Daemon.Services;

/// <summary>
/// Pure decision core that answers whether a block rule applies at a given instant.
/// It has no clock, disk, network, or state of its own: the caller owns the clock and
/// passes <c>now</c> in, which keeps the engine deterministic and trivially testable.
/// </summary>
public class BlockEngine
{
    /// <summary>
    /// Evaluates a single rule against <paramref name="now"/>.
    /// The active window is the half-open interval <c>[StartTime, EndTime)</c>: a time equal
    /// to <c>StartTime</c> is blocked, while a time equal to <c>EndTime</c> is not. When
    /// <c>StartTime</c> is later than <c>EndTime</c> the window wraps around midnight and is
    /// active on both sides of the day boundary. A rule is never active when it is disabled
    /// or when <c>StartTime == EndTime</c>, since that describes a degenerate (empty) window.
    /// </summary>
    public bool Evaluate(BlockRuleConfig rule, TimeOnly now)
    {
        if (!rule.Enabled || rule.StartTime == rule.EndTime)
        {
            return false;
        }

        return rule.StartTime < rule.EndTime
            ? rule.StartTime <= now && now < rule.EndTime     // normal window
            : now >= rule.StartTime || now < rule.EndTime;    // crosses midnight
    }

    /// <summary>
    /// Returns the names of every app whose rule is active at <paramref name="now"/>.
    /// Disabled, out-of-schedule, and degenerate rules contribute no entries.
    /// </summary>
    public IReadOnlyList<string> GetAppsToBlock(AppConfig config, TimeOnly now) =>
        config.BlockRules.Where(rule => Evaluate(rule, now)).Select(rule => rule.AppName).ToList();
}