namespace BankingSystem.security;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string passwordHash) =>
        !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(passwordHash) &&
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
