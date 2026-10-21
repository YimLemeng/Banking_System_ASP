using BankingSystem.entity;
using BankingSystem.exception;
using FluentAssertions;
using Xunit;

namespace BankingSystem.Tests;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("012345678", "+85512345678")]
    [InlineData("098765432", "+85598765432")]
    [InlineData("0085512345678", "+85512345678")]
    [InlineData("+85512345678", "+85512345678")]
    [InlineData("12345678", "+85512345678")]
    [InlineData("012-345-678", "+85512345678")]
    [InlineData("(012) 345 678", "+85512345678")]
    public void Normalize_ShouldFormatCambodianNumbersToE164(string input, string expected)
    {
        var phone = PhoneNumber.Create(input);
        phone.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_WithInternationalNumber_ShouldPreserveCountryCode()
    {
        var usPhone = PhoneNumber.Create("+14155552671");
        var vietnamPhone = PhoneNumber.Create("+84901234567");

        usPhone.Value.Should().Be("+14155552671");
        vietnamPhone.Value.Should().Be("+84901234567");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-phone")]
    [InlineData("abc")]
    public void Create_WithInvalidNumber_ShouldThrowInvalidPhoneNumberException(string invalidNumber)
    {
        var act = () => PhoneNumber.Create(invalidNumber);
        act.Should().Throw<InvalidPhoneNumberException>();
    }

    [Fact]
    public void Mask_ShouldMaskMiddleDigits()
    {
        var phone = PhoneNumber.Create("+85512345678");
        var masked = phone.Mask();

        masked.Should().StartWith("+855");
        masked.Should().EndWith("5678");
        masked.Should().Contain("*");
        masked.Should().NotContain("1234");
    }
}
