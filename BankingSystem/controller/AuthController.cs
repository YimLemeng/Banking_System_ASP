using BankingSystem.dto;
using BankingSystem.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(dto, ct);
        return Ok(ApiResponse<UserDto>.Ok(result, "Customer registered successfully. Verification OTP dispatched via SMS."));
    }

    [HttpPost("otp/request")]
    public async Task<ActionResult<ApiResponse<string>>> RequestOtp([FromBody] RequestOtpDto dto, CancellationToken ct)
    {
        var result = await _authService.RequestOtpAsync(dto, ct);
        return Ok(ApiResponse<string>.Ok(result));
    }

    [HttpPost("otp/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyOtp([FromBody] VerifyOtpDto dto, CancellationToken ct)
    {
        var result = await _authService.VerifyOtpAsync(dto, ct);
        return Ok(ApiResponse<bool>.Ok(result, "OTP verified successfully."));
    }

    [HttpPost("login/phone")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginWithPhone([FromBody] LoginPhoneRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginWithPhoneAsync(dto, ct);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Authentication successful."));
    }

    [HttpPost("login/phone-otp")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginWithPhoneOtp([FromBody] LoginPhoneOtpRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginWithPhoneOtpAsync(dto, ct);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Authentication via OTP successful."));
    }

    [HttpPost("login/email")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginWithEmail([FromBody] LoginEmailRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginWithEmailAsync(dto, ct);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Authentication successful."));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(dto, ct);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed successfully."));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        await _authService.RevokeTokenAsync(dto.RefreshToken, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Token revoked successfully."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequestDto dto, CancellationToken ct)
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        await _authService.ChangePasswordAsync(userId, dto, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Password changed successfully."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequestDto dto, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(dto, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Password reset successfully."));
    }
}
