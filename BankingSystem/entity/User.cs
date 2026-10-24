using BankingSystem.exception;

namespace BankingSystem.entity;

public class User
{
    private readonly List<RefreshToken> _refreshTokens = new();

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Teller;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    public string? TotpSecretKey { get; set; }
    public bool TotpEnabled { get; set; } = false;

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static User Create(
        string username,
        string passwordHash,
        string fullName,
        UserRole role = UserRole.Teller,
        string? email = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            Status = UserStatus.Active,
            Email = email?.Trim().ToLowerInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void EnableTotp(string secretKey)
    {
        TotpSecretKey = secretKey;
        TotpEnabled = true;
    }

    public void DisableTotp()
    {
        TotpSecretKey = null;
        TotpEnabled = false;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");
        PasswordHash = newPasswordHash;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd.HasValue && LockoutEnd > now;

    public void RecordLoginSuccess()
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        if (Status == UserStatus.LockedOut)
            Status = UserStatus.Active;
    }

    public void RecordLoginFailure(DateTimeOffset now, int maxAttempts = 5, int lockoutMinutes = 15)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
        {
            LockoutEnd = now.AddMinutes(lockoutMinutes);
            Status = UserStatus.LockedOut;
        }
    }

    public void Unlock()
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        if (Status == UserStatus.LockedOut)
            Status = UserStatus.Active;
    }

    public RefreshToken AddRefreshToken(string token, DateTimeOffset expiresAt, string? createdByIp)
    {
        var rt = new RefreshToken
        {
            Id = Guid.Empty,
            UserId = Id,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = createdByIp
        };
        _refreshTokens.Add(rt);
        return rt;
    }

    public void RevokeRefreshToken(string token, string? revokedByIp, string? replacedByToken = null)
    {
        var existing = _refreshTokens.SingleOrDefault(r => r.Token == token);
        existing?.Revoke(revokedByIp, replacedByToken);
    }

    public void RevokeAllRefreshTokens(string? revokedByIp)
    {
        foreach (var rt in _refreshTokens.Where(r => r.IsActive))
            rt.Revoke(revokedByIp);
    }
}
