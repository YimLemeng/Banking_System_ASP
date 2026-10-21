using BankingSystem.entity;

namespace BankingSystem.repository;

public interface ICustomerRepository : IRepository<Customer, Guid>
{
    Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdWithAccountsAsync(Guid id, CancellationToken cancellationToken = default);
}
