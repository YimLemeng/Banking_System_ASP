using BankingSystem.entity;

namespace BankingSystem.security;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task VerifyOtpAsync(string phoneNumber, string plainOtpCode, OtpPurpose purpose, CancellationToken cancellationToken = default);
    string MaskPhoneNumber(string phoneNumber);
}
