using BankingSystem.dto;

namespace BankingSystem.service;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);
    Task<string> RequestOtpAsync(RequestOtpDto dto, CancellationToken ct = default);
    Task<bool> VerifyOtpAsync(VerifyOtpDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginWithPhoneAsync(LoginPhoneRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginWithPhoneOtpAsync(LoginPhoneOtpRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginWithEmailAsync(LoginEmailRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken ct = default);
    Task RevokeTokenAsync(string token, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken ct = default);
}
