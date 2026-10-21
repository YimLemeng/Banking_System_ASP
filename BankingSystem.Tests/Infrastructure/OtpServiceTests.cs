using BankingSystem.config;
using BankingSystem.entity;
using BankingSystem.exception;
using BankingSystem.security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BankingSystem.Tests;

public class OtpServiceTests
{
    private readonly AppDbContext _context;
    private readonly IOptions<OtpOptions> _options;
    private readonly Mock<ILogger<OtpService>> _loggerMock;

    public OtpServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(dbOptions);
        _loggerMock = new Mock<ILogger<OtpService>>();
        _options = Options.Create(new OtpOptions
        {
            ExpiryMinutes = 5,
            ResendCooldownSeconds = 60,
            MaxRequestsPerHour = 5,
            MaxFailedAttempts = 5,
            LockoutMinutes = 15
        });
    }

    [Fact]
    public async Task GenerateOtpAsync_ShouldReturn6DigitCode()
    {
        var service = new OtpService(_context, _options, _loggerMock.Object);
        var code = await service.GenerateOtpAsync("+85512345678", OtpPurpose.Login);

        code.Should().MatchRegex(@"^\d{6}$");
        var saved = await _context.OtpCodes.FirstOrDefaultAsync(o => o.PhoneNumber == "+85512345678");
        saved.Should().NotBeNull();
        saved!.CodeHash.Should().NotBe(code); // hashed
    }

    [Fact]
    public async Task GenerateOtpAsync_ShouldThrowConflict_WhenCooldownNotMet()
    {
        var service = new OtpService(_context, _options, _loggerMock.Object);
        await service.GenerateOtpAsync("+85512345678", OtpPurpose.Login);

        // Immediate second call
        var act = async () => await service.GenerateOtpAsync("+85512345678", OtpPurpose.Login);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Please wait*");
    }

    [Fact]
    public async Task VerifyOtpAsync_ShouldSucceed_WhenCodeMatches()
    {
        var service = new OtpService(_context, _options, _loggerMock.Object);
        var code = await service.GenerateOtpAsync("+85512345678", OtpPurpose.Login);

        var act = async () => await service.VerifyOtpAsync("+85512345678", code, OtpPurpose.Login);
        await act.Should().NotThrowAsync();

        var otp = await _context.OtpCodes.FirstAsync(o => o.PhoneNumber == "+85512345678");
        otp.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyOtpAsync_ShouldLock_After5FailedAttempts()
    {
        var service = new OtpService(_context, _options, _loggerMock.Object);
        await service.GenerateOtpAsync("+85512345678", OtpPurpose.Login);

        for (int i = 0; i < 4; i++)
        {
            var act = async () => await service.VerifyOtpAsync("+85512345678", "000000", OtpPurpose.Login);
            await act.Should().ThrowAsync<DomainException>();
        }

        // 5th attempt triggers lockout
        var finalAct = async () => await service.VerifyOtpAsync("+85512345678", "000000", OtpPurpose.Login);
        await finalAct.Should().ThrowAsync<DomainException>().WithMessage("*Maximum verification attempts*");

        var otp = await _context.OtpCodes.FirstAsync(o => o.PhoneNumber == "+85512345678");
        otp.Attempts.Should().Be(5);
        otp.IsUsed.Should().BeTrue();
        otp.LockoutEnd.Should().NotBeNull();
    }
}
