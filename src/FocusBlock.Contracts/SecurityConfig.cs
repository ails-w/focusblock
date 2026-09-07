namespace FocusBlock.Contracts;

public class SecurityConfig
{
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int CooldownMinutes { get; set; } = 10;
}