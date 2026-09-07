using FocusBlock.Tui.Services;
using FluentAssertions;

namespace FocusBlock.Tests.Unit;

public class AuthServiceTests
{
    private readonly AuthService _service = new();

    [Fact]
    public void AuthService_HashPassword_ReturnsHashAndSalt()
    {
        var (hash, salt) = _service.HashPassword("mi-contrasena");

        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("mi-contrasena");
    }

    [Fact]
    public void AuthService_VerifyPassword_ReturnsTrue_WhenCorrect()
    {
        var (hash, salt) = _service.HashPassword("secreto");

        _service.VerifyPassword("secreto", hash, salt).Should().BeTrue();
    }

    [Fact]
    public void AuthService_VerifyPassword_ReturnsFalse_WhenWrong()
    {
        var (hash, salt) = _service.HashPassword("secreto");

        _service.VerifyPassword("incorrecta", hash, salt).Should().BeFalse();
    }
}