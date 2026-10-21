using BankingSystem.entity;

namespace BankingSystem.security;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int AccessTokenExpirationMinutes { get; }
}
