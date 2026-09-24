namespace FocusBlock.Core;

/// <summary>Port for verifying a password against a stored hash and salt.</summary>
public interface IPasswordVerifier
{
    bool VerifyPassword(string password, string hash, string salt);
}