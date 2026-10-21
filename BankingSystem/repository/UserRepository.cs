using BankingSystem.config;
using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class UserRepository : GenericRepository<User, Guid>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.TryCreate(phoneNumber, out var phoneObj) || phoneObj is null)
            return null;

        return await DbSet.Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneObj, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await DbSet.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => await DbSet.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.RefreshTokens.Any(r => r.Token == refreshToken), cancellationToken);
}
