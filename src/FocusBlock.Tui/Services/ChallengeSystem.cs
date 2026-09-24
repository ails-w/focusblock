using System.Security.Cryptography;

namespace FocusBlock.Tui.Services;

/// <summary>
/// Generates and validates the human-typed challenge used as friction before an early stop.
/// </summary>
/// <remarks>
/// <para>
/// This is a speed bump, not a security boundary. The password hashed in
/// <c>SecurityConfig</c> is the real gate; the challenge only adds a few seconds of manual
/// friction so that stopping a block early is a deliberate act.
/// </para>
/// <para>
/// Because the challenge is displayed on screen, it is not a secret and no constant-time
/// comparison is used: <see cref="Matches"/> is a plain ordinal comparison.
/// </para>
/// </remarks>
public class ChallengeSystem
{
    private static readonly string[] Words =
    [
        "otter", "lantern", "cobalt", "drift", "ember", "garnet",
        "harbor", "ivory", "jasmine", "kettle", "lumen", "maple",
    ];

    private const int WordCount = 4;

    /// <summary>Builds a new challenge made of <see cref="WordCount"/> random words joined by '-'.</summary>
    /// <returns>A challenge such as <c>cobalt-otter-lantern-drift</c>.</returns>
    public string GenerateChallenge()
    {
        var words = new string[WordCount];
        for (int i = 0; i < WordCount; i++)
        {
            words[i] = Words[RandomNumberGenerator.GetInt32(Words.Length)];
        }

        return string.Join('-', words);
    }

    /// <summary>Checks whether the typed <paramref name="input"/> matches <paramref name="challenge"/>.</summary>
    /// <param name="input">The text typed by the user; surrounding whitespace is ignored.</param>
    /// <param name="challenge">The challenge that was displayed to the user.</param>
    /// <returns><see langword="true"/> when both strings are equal (ordinal, case-sensitive).</returns>
    public bool Matches(string input, string challenge) =>
        string.Equals(input.Trim(), challenge, StringComparison.Ordinal);
}