using BankingSystem.config;
using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class CustomerRepository : GenericRepository<Customer, Guid>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.Include(c => c.Accounts).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Customer?> GetByIdWithAccountsAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.Include(c => c.Accounts).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
