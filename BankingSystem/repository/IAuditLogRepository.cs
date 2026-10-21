using BankingSystem.entity;

namespace BankingSystem.repository;

public interface IAuditLogRepository : IRepository<AuditLog, Guid>
{
    Task<IReadOnlyList<AuditLog>> GetPagedLogsAsync(
        int pageIndex, int pageSize, Guid? userId, AuditAction? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
    Task<int> GetLogsCountAsync(
        Guid? userId, AuditAction? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
}
