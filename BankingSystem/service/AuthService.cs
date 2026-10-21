using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.exception;
using BankingSystem.repository;
using BankingSystem.security;

namespace BankingSystem.service;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOtpService otpService,
        ISmsSender smsSender,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _smsSender = smsSender;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        var phone = PhoneNumber.Create(dto.PhoneNumber);
        var existing = await _userRepository.GetByPhoneNumberAsync(phone.Value, ct);
        if (existing != null)
            throw new ConflictException("A user with this phone number is already registered.");

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existingEmail = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);
            if (existingEmail != null)
                throw new ConflictException("A user with this email address is already registered.");
        }

        var user = User.Create(phone, _passwordHasher.HashPassword(dto.Password), dto.FullName, UserRole.Customer, dto.Email);
        await _userRepository.AddAsync(user, ct);

        var code = await _otpService.GenerateOtpAsync(phone.Value, OtpPurpose.Register, ct);
        await _smsSender.SendSmsAsync(phone.Value, $"Your Banking verification code is: {code}. Valid for 5 minutes.", ct);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.Register, nameof(User), $"Customer registered with phone {phone.Mask()}",
            userId: user.Id, maskedPhoneNumber: phone.Mask(), entityId: user.Id.ToString()), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<string> RequestOtpAsync(RequestOtpDto dto, CancellationToken ct = default)
    {
        var normalized = PhoneNumber.Normalize(dto.PhoneNumber);
        var masked = _otpService.MaskPhoneNumber(normalized);

        var code = await _otpService.GenerateOtpAsync(normalized, dto.Purpose, ct);
        await _smsSender.SendSmsAsync(normalized, $"Your verification code is: {code}. Valid for 5 minutes.", ct);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.OtpRequested, "OtpCode", $"OTP requested for {dto.Purpose}",
            userId: _currentUser.UserId, maskedPhoneNumber: masked, ipAddress: _currentUser.IpAddress, userAgent: _currentUser.UserAgent), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return $"Verification code sent to {masked}.";
    }

    public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto, CancellationToken ct = default)
    {
        var normalized = PhoneNumber.Normalize(dto.PhoneNumber);
        await _otpService.VerifyOtpAsync(normalized, dto.Code, dto.Purpose, ct);

        if (dto.Purpose == OtpPurpose.Register)
        {
            var user = await _userRepository.GetByPhoneNumberAsync(normalized, ct);
            if (user != null)
            {
                user.ConfirmPhoneNumber();
                _userRepository.Update(user);
            }
        }

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.OtpVerified, "OtpCode", $"OTP verified for {dto.Purpose}",
            userId: _currentUser.UserId, maskedPhoneNumber: _otpService.MaskPhoneNumber(normalized), ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AuthResponseDto> LoginWithPhoneAsync(LoginPhoneRequestDto dto, CancellationToken ct = default)
    {
        var normalized = PhoneNumber.Normalize(dto.PhoneNumber);
        var user = await _userRepository.GetByPhoneNumberAsync(normalized, ct);
        var now = DateTimeOffset.UtcNow;

        if (user == null)
            throw new UnauthorizedException("Invalid phone number or password.");

        if (user.IsLockedOut(now))
            throw new UnauthorizedException($"Account is temporarily locked. Try again in {Math.Ceiling((user.LockoutEnd!.Value - now).TotalMinutes)} minutes.");

        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(now);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid phone number or password.");
        }

        user.RecordLoginSuccess();
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, now.AddDays(7), _currentUser.IpAddress);
        _userRepository.Update(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.LoginSuccess, nameof(User), "Login success (Phone + Password)",
            userId: user.Id, maskedPhoneNumber: user.PhoneNumber.Mask(), ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return new AuthResponseDto(accessToken, refreshToken, _jwtTokenService.AccessTokenExpirationMinutes * 60, "Bearer", _mapper.Map<UserDto>(user));
    }

    public async Task<AuthResponseDto> LoginWithPhoneOtpAsync(LoginPhoneOtpRequestDto dto, CancellationToken ct = default)
    {
        var normalized = PhoneNumber.Normalize(dto.PhoneNumber);
        await _otpService.VerifyOtpAsync(normalized, dto.Code, OtpPurpose.Login, ct);

        var user = await _userRepository.GetByPhoneNumberAsync(normalized, ct);
        if (user == null)
            throw new NotFoundException("No account found with this phone number.");

        var now = DateTimeOffset.UtcNow;
        if (user.IsLockedOut(now))
            throw new UnauthorizedException("Account is currently locked out.");

        user.RecordLoginSuccess();
        user.ConfirmPhoneNumber();

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, now.AddDays(7), _currentUser.IpAddress);
        _userRepository.Update(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.LoginSuccess, nameof(User), "Passwordless Login success (Phone + OTP)",
            userId: user.Id, maskedPhoneNumber: user.PhoneNumber.Mask(), ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return new AuthResponseDto(accessToken, refreshToken, _jwtTokenService.AccessTokenExpirationMinutes * 60, "Bearer", _mapper.Map<UserDto>(user));
    }

    public async Task<AuthResponseDto> LoginWithEmailAsync(LoginEmailRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);
        var now = DateTimeOffset.UtcNow;

        if (user == null)
            throw new UnauthorizedException("Invalid email or password.");

        if (user.IsLockedOut(now))
            throw new UnauthorizedException("Account is temporarily locked out.");

        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(now);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.RecordLoginSuccess();
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, now.AddDays(7), _currentUser.IpAddress);
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(ct);
        return new AuthResponseDto(accessToken, refreshToken, _jwtTokenService.AccessTokenExpirationMinutes * 60, "Bearer", _mapper.Map<UserDto>(user));
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(dto.RefreshToken, ct);
        if (user == null)
            throw new UnauthorizedException("Invalid refresh token.");

        var existing = user.RefreshTokens.SingleOrDefault(r => r.Token == dto.RefreshToken);
        if (existing == null || !existing.IsActive)
            throw new UnauthorizedException("Refresh token is expired or revoked.");

        var now = DateTimeOffset.UtcNow;
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        existing.Revoke(_currentUser.IpAddress, newRefreshToken);
        user.AddRefreshToken(newRefreshToken, now.AddDays(7), _currentUser.IpAddress);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        return new AuthResponseDto(accessToken, newRefreshToken, _jwtTokenService.AccessTokenExpirationMinutes * 60, "Bearer", _mapper.Map<UserDto>(user));
    }

    public async Task RevokeTokenAsync(string token, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(token, ct);
        if (user == null) throw new NotFoundException("Token not found.");

        user.RevokeRefreshToken(token, _currentUser.IpAddress);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null) throw new NotFoundException("User not found.");

        if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Incorrect current password.");

        user.ChangePassword(_passwordHasher.HashPassword(dto.NewPassword));
        user.RevokeAllRefreshTokens(_currentUser.IpAddress);
        _userRepository.Update(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.PasswordChanged, nameof(User), "Password changed successfully",
            userId: user.Id, maskedPhoneNumber: user.PhoneNumber.Mask(), ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken ct = default)
    {
        var normalized = PhoneNumber.Normalize(dto.PhoneNumber);
        await _otpService.VerifyOtpAsync(normalized, dto.OtpCode, OtpPurpose.ResetPassword, ct);

        var user = await _userRepository.GetByPhoneNumberAsync(normalized, ct);
        if (user == null) throw new NotFoundException("User not found.");

        user.ChangePassword(_passwordHasher.HashPassword(dto.NewPassword));
        user.Unlock();
        user.RevokeAllRefreshTokens(_currentUser.IpAddress);
        _userRepository.Update(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.PasswordReset, nameof(User), "Password reset via OTP",
            userId: user.Id, maskedPhoneNumber: user.PhoneNumber.Mask(), ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
