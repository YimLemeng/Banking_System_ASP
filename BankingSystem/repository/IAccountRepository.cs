using BankingSystem.entity;

namespace BankingSystem.repository;

public interface IAccountRepository : IRepository<Account, Guid>
{
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
