using System.Security.Cryptography;
using System.Text;
using BankingSystem.config;
using BankingSystem.entity;
using BankingSystem.exception;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BankingSystem.security;

public class OtpService : IOtpService
{
    private readonly AppDbContext _context;
    private readonly OtpOptions _options;
    private readonly ILogger<OtpService> _logger;

    public OtpService(AppDbContext context, IOptions<OtpOptions> options, ILogger<OtpService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var latestOtp = await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestOtp != null && (now - latestOtp.CreatedAt).TotalSeconds < _options.ResendCooldownSeconds)
        {
            var remainingCooldown = _options.ResendCooldownSeconds - (int)(now - latestOtp.CreatedAt).TotalSeconds;
            throw new ConflictException($"Please wait {remainingCooldown} seconds before requesting a new code.");
        }

        var oneHourAgo = now.AddHours(-1);
        var requestsInPastHour = await _context.OtpCodes
            .CountAsync(o => o.PhoneNumber == phoneNumber && o.CreatedAt >= oneHourAgo, cancellationToken);

        if (requestsInPastHour >= _options.MaxRequestsPerHour)
        {
            throw new ConflictException("Maximum verification requests reached for this phone number. Please try again later.");
        }

        var previousPending = await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var p in previousPending)
            p.MarkAsUsed();

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var codeHash = HashCode(code);

        var otpEntity = OtpCode.Create(phoneNumber, codeHash, purpose, _options.ExpiryMinutes);
        await _context.OtpCodes.AddAsync(otpEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated OTP for phone {MaskedPhone} purpose {Purpose}", MaskPhoneNumber(phoneNumber), purpose);
        return code;
    }

    public async Task VerifyOtpAsync(string phoneNumber, string plainOtpCode, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var otp = await _context.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp == null)
            throw new DomainException("Invalid or expired verification code.");

        if (otp.IsLocked(now))
        {
            var remaining = Math.Ceiling((otp.LockoutEnd!.Value - now).TotalMinutes);
            throw new DomainException($"Too many failed attempts. Code locked. Please try again in {remaining} minutes.");
        }

        var incomingHash = HashCode(plainOtpCode.Trim());
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(incomingHash), Encoding.UTF8.GetBytes(otp.CodeHash)))
        {
            otp.IncrementAttempts(now, _options.MaxFailedAttempts, _options.LockoutMinutes);
            await _context.SaveChangesAsync(cancellationToken);

            var remaining = Math.Max(0, _options.MaxFailedAttempts - otp.Attempts);
            throw new DomainException(remaining > 0
                ? $"Invalid verification code. {remaining} attempts remaining."
                : "Maximum verification attempts exceeded. Code has been invalidated.");
        }

        otp.MarkAsUsed();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return "***";
        if (PhoneNumber.TryCreate(phoneNumber, out var parsed))
            return parsed!.Mask();

        if (phoneNumber.Length <= 4) return "***";
        return phoneNumber[..2] + new string('*', phoneNumber.Length - 4) + phoneNumber[^2..];
    }

    private static string HashCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
