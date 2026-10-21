namespace BankingSystem.config;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "BankingSystemApi";
    public string Audience { get; set; } = "BankingSystemClients";
    public string SecretKey { get; set; } = "EnterpriseBankingSystemUltraSecureSecretKeyForJwt2026!LongEnoughForSha256";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class OtpOptions
{
    public const string SectionName = "Otp";
    public int ExpiryMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxRequestsPerHour { get; set; } = 5;
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
