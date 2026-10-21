namespace BankingSystem.security;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? PhoneNumber { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
