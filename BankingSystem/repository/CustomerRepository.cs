using BankingSystem.config;
using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class CustomerRepository : GenericRepository<Customer, Guid>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.Include(c => c.User).Include(c => c.Accounts).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Customer?> GetByIdWithAccountsAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.Include(c => c.User).Include(c => c.Accounts).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllWithAccountsAsync(CancellationToken cancellationToken = default)
        => await DbSet.Include(c => c.User).Include(c => c.Accounts).ToListAsync(cancellationToken);

    public async Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumber.Normalize(phoneNumber);
        return await DbSet.Include(c => c.User).Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.PhoneNumber == normalized || c.PhoneNumber == phoneNumber, cancellationToken);
    }
}
