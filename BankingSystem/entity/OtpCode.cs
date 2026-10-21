namespace BankingSystem.entity;

public class OtpCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LockoutEnd { get; set; }

    public static OtpCode Create(string phoneNumber, string codeHash, OtpPurpose purpose, int expiryMinutes = 5)
    {
        return new OtpCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            CodeHash = codeHash,
            Purpose = purpose,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes),
            Attempts = 0,
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsLocked(DateTimeOffset now) => LockoutEnd.HasValue && LockoutEnd > now;

    public void IncrementAttempts(DateTimeOffset now, int maxAttempts = 5, int lockoutMinutes = 15)
    {
        Attempts++;
        if (Attempts >= maxAttempts)
        {
            LockoutEnd = now.AddMinutes(lockoutMinutes);
            IsUsed = true;
        }
    }

    public void MarkAsUsed() => IsUsed = true;
}
