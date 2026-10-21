using BankingSystem.config;
using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class TransactionRepository : GenericRepository<Transaction, Guid>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context) { }

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(t => t.IdempotencyKey == key, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetPagedTransactionsAsync(
        Guid accountId, int pageIndex, int pageSize, DateTimeOffset? from, DateTimeOffset? to, TransactionType? type, CancellationToken ct = default)
    {
        var q = DbSet.Where(t => t.AccountId == accountId || t.TargetAccountId == accountId);
        if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);
        if (type.HasValue) q = q.Where(t => t.Type == type.Value);

        return await q.OrderByDescending(t => t.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<int> GetTransactionsCountAsync(
        Guid accountId, DateTimeOffset? from, DateTimeOffset? to, TransactionType? type, CancellationToken ct = default)
    {
        var q = DbSet.Where(t => t.AccountId == accountId || t.TargetAccountId == accountId);
        if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);
        if (type.HasValue) q = q.Where(t => t.Type == type.Value);

        return await q.CountAsync(ct);
    }
}
