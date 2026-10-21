using BankingSystem.entity;

namespace BankingSystem.repository;

public interface ITransactionRepository : IRepository<Transaction, Guid>
{
    Task<Transaction?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetPagedTransactionsAsync(
        Guid accountId, int pageIndex, int pageSize, DateTimeOffset? from, DateTimeOffset? to, TransactionType? type, CancellationToken ct = default);
    Task<int> GetTransactionsCountAsync(
        Guid accountId, DateTimeOffset? from, DateTimeOffset? to, TransactionType? type, CancellationToken ct = default);
}
