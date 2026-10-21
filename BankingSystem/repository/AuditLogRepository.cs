using BankingSystem.config;
using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class AuditLogRepository : GenericRepository<AuditLog, Guid>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AuditLog>> GetPagedLogsAsync(
        int pageIndex, int pageSize, Guid? userId, AuditAction? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var q = DbSet.AsQueryable();
        if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
        if (action.HasValue) q = q.Where(a => a.Action == action.Value);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.CreatedAt <= to.Value);

        return await q.OrderByDescending(a => a.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<int> GetLogsCountAsync(
        Guid? userId, AuditAction? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var q = DbSet.AsQueryable();
        if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
        if (action.HasValue) q = q.Where(a => a.Action == action.Value);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.CreatedAt <= to.Value);

        return await q.CountAsync(ct);
    }
}
