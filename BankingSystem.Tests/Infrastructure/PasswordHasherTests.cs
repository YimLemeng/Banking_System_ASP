using BankingSystem.security;
using FluentAssertions;
using Xunit;

namespace BankingSystem.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldProduceDifferentHash_ForSamePassword()
    {
        var hasher = new PasswordHasher();
        var password = "Secure@Password123!";

        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);

        hash1.Should().NotBeNullOrWhiteSpace();
        hash2.Should().NotBeNullOrWhiteSpace();
        hash1.Should().NotBe(password);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_ForMatchingPassword()
    {
        var hasher = new PasswordHasher();
        var password = "Secure@Password123!";
        var hash = hasher.HashPassword(password);

        var isValid = hasher.VerifyPassword(password, hash);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_ForWrongPassword()
    {
        var hasher = new PasswordHasher();
        var password = "Secure@Password123!";
        var hash = hasher.HashPassword(password);

        var isValid = hasher.VerifyPassword("WrongPassword999!", hash);
        isValid.Should().BeFalse();
    }
}
