using BankingSystem.dto;

namespace BankingSystem.service;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
    Task<LoginPreCheckResponseDto> LoginPreCheckAsync(LoginRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginWithTotpAsync(LoginWithTotpRequestDto dto, CancellationToken ct = default);
    Task<TotpSetupResponseDto> SetupTotpAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginWithEmailAsync(LoginEmailRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken ct = default);
    Task RevokeTokenAsync(string token, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto, CancellationToken ct = default);
}
