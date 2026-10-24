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
        return Ok(ApiResponse<UserDto>.Ok(result, "Account registered successfully."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Authentication successful."));
    }

    [HttpPost("login/pre-check")]
    public async Task<ActionResult<ApiResponse<LoginPreCheckResponseDto>>> LoginPreCheck([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginPreCheckAsync(dto, ct);
        return Ok(ApiResponse<LoginPreCheckResponseDto>.Ok(result, "Pre-check completed."));
    }

    [HttpPost("login/totp")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginWithTotp([FromBody] LoginWithTotpRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginWithTotpAsync(dto, ct);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "TOTP authentication successful."));
    }

    [HttpPost("totp/setup")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TotpSetupResponseDto>>> SetupTotp(CancellationToken ct)
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        var result = await _authService.SetupTotpAsync(userId, ct);
        return Ok(ApiResponse<TotpSetupResponseDto>.Ok(result, "TOTP secret configured successfully."));
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
}
