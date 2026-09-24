using FluentAssertions;

using FocusBlock.Tui.Services;

namespace FocusBlock.Tests.Unit;

public class ChallengeSystemTests
{
    [Fact]
    public void ChallengeSystem_GenerateChallenge_ReturnsRandomText()
    {
        var system = new ChallengeSystem();

        string challenge = system.GenerateChallenge();

        challenge.Should().NotBeNullOrWhiteSpace();
        challenge.Split('-').Should().HaveCount(4);
    }

    [Fact]
    public void ChallengeSystem_GenerateChallenge_ReturnsDifferentText_AcrossCalls()
    {
        var system = new ChallengeSystem();

        string first = system.GenerateChallenge();
        string second = system.GenerateChallenge();

        // Probabilistic: the space is 12^4 (20736), so a collision is very unlikely.
        second.Should().NotBe(first);
    }

    [Fact]
    public void ChallengeSystem_Matches_ReturnsTrue_WhenInputMatches()
    {
        var system = new ChallengeSystem();

        bool result = system.Matches("cobalt-otter-lantern-drift", "cobalt-otter-lantern-drift");

        result.Should().BeTrue();
    }

    [Fact]
    public void ChallengeSystem_Matches_ReturnsFalse_WhenInputDiffers()
    {
        var system = new ChallengeSystem();

        bool result = system.Matches("cobalt-otter-lantern-drift", "maple-ivory-harbor-ember");

        result.Should().BeFalse();
    }

    [Fact]
    public void ChallengeSystem_Matches_IgnoresSurroundingWhitespace()
    {
        var system = new ChallengeSystem();

        bool result = system.Matches("  cobalt-otter-lantern-drift  ", "cobalt-otter-lantern-drift");

        result.Should().BeTrue();
    }
}