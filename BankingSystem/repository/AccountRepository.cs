using BankingSystem.config;
using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class AccountRepository : GenericRepository<Account, Guid>, IAccountRepository
{
    public AccountRepository(AppDbContext context) : base(context) { }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        => await DbSet.Include(a => a.Customer).FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);

    public async Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await DbSet.Where(a => a.CustomerId == customerId).ToListAsync(cancellationToken);
}
