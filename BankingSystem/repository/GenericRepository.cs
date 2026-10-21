using BankingSystem.config;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.repository;

public class GenericRepository<TEntity, TId> : IRepository<TEntity, TId> where TEntity : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public GenericRepository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync(new object?[] { id }, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(TEntity entity)
    {
        var entry = Context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            DbSet.Update(entity);
        }
    }
    public virtual void Delete(TEntity entity) => DbSet.Remove(entity);
}
