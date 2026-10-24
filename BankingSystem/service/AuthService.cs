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
    private readonly ITotpService _totpService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITotpService totpService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _totpService = totpService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<LoginPreCheckResponseDto> LoginPreCheckAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(username, ct);

        var now = DateTimeOffset.UtcNow;

        if (user == null)
            throw new UnauthorizedException("Invalid username or password.");

        if (user.IsLockedOut(now))
            throw new UnauthorizedException($"Account is temporarily locked. Try again in {Math.Ceiling((user.LockoutEnd!.Value - now).TotalMinutes)} minutes.");

        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(now);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid username or password.");
        }

        string secretKey = user.TotpSecretKey ?? _totpService.GenerateSecretKey();
        if (string.IsNullOrEmpty(user.TotpSecretKey))
        {
            user.TotpSecretKey = secretKey;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var qrUri = _totpService.GetQrCodeUri(secretKey, user.Username, "TrustBank");
        var qrBase64 = _totpService.GetQrCodeBase64Png(qrUri);

        return new LoginPreCheckResponseDto(
            RequiresTotp: true,
            TotpEnabled: user.TotpEnabled,
            TotpSetupQrBase64: qrBase64,
            TotpSecretKey: secretKey,
            TempToken: null
        );
    }

    public async Task<AuthResponseDto> LoginWithTotpAsync(LoginWithTotpRequestDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(username, ct);

        var now = DateTimeOffset.UtcNow;

        if (user == null)
            throw new UnauthorizedException("Invalid username or password.");

        if (user.IsLockedOut(now))
            throw new UnauthorizedException($"Account is temporarily locked. Try again in {Math.Ceiling((user.LockoutEnd!.Value - now).TotalMinutes)} minutes.");

        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(now);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (string.IsNullOrEmpty(user.TotpSecretKey))
            throw new AppException("2FA Authenticator has not been initialized. Please try logging in again.");

        var isValidCode = _totpService.ValidateCode(user.TotpSecretKey, dto.TotpCode);
        if (!isValidCode)
        {
            user.RecordLoginFailure(now);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid 6-digit Authenticator code. Please check your Authenticator app and try again.");
        }

        if (!user.TotpEnabled)
        {
            user.TotpEnabled = true;
        }

        user.RecordLoginSuccess();
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, now.AddDays(7), _currentUser.IpAddress);
        _userRepository.Update(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.LoginSuccess, nameof(User), $"Login success via TOTP Authenticator (Username: {user.Username})",
            userId: user.Id, maskedPhoneNumber: null, ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return new AuthResponseDto(accessToken, refreshToken, _jwtTokenService.AccessTokenExpirationMinutes * 60, "Bearer", _mapper.Map<UserDto>(user));
    }

    public async Task<TotpSetupResponseDto> SetupTotpAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null) throw new NotFoundException($"User {userId} was not found.");

        var secret = _totpService.GenerateSecretKey();
        user.TotpSecretKey = secret;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        var qrUri = _totpService.GetQrCodeUri(secret, user.Username, "TrustBank");
        var qrBase64 = _totpService.GetQrCodeBase64Png(qrUri);

        return new TotpSetupResponseDto(secret, qrUri, qrBase64);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(username, ct);

        var now = DateTimeOffset.UtcNow;

        if (user == null)
            throw new UnauthorizedException("Invalid username or password.");

        if (user.IsLockedOut(now))
            throw new UnauthorizedException($"Account is temporarily locked. Try again in {Math.Ceiling((user.LockoutEnd!.Value - now).TotalMinutes)} minutes.");

        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.RecordLoginFailure(now);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid username or password.");
        }

        user.RecordLoginSuccess();
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, now.AddDays(7), _currentUser.IpAddress);
        _userRepository.Update(user);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.LoginSuccess, nameof(User), $"Login success (Username: {user.Username})",
            userId: user.Id, maskedPhoneNumber: null, ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return new AuthResponseDto(accessToken, refreshToken, _jwtTokenService.AccessTokenExpirationMinutes * 60, "Bearer", _mapper.Map<UserDto>(user));
    }

    public async Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default)
    {
        var username = dto.Username.Trim().ToLowerInvariant();
        var existingUsername = await _userRepository.GetByUsernameAsync(username, ct);
        if (existingUsername != null)
            throw new ConflictException("A user with this username is already registered.");

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existingEmail = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);
            if (existingEmail != null)
                throw new ConflictException("A user with this email address is already registered.");
        }

        var user = User.Create(username, _passwordHasher.HashPassword(dto.Password), dto.FullName, UserRole.Teller, dto.Email);
        await _userRepository.AddAsync(user, ct);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.Register, nameof(User), $"User registered with username {username}",
            userId: user.Id, maskedPhoneNumber: null, entityId: user.Id.ToString()), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<UserDto>(user);
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
            userId: user.Id, maskedPhoneNumber: null, ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
