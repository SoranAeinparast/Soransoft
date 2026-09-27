using Microsoft.EntityFrameworkCore;
using Soransoft.Application.Interfaces;
using System.Linq.Expressions;

namespace Soransoft.Infrastructure.Persistence
{
    /// <summary>پیاده‌سازی عمومی Repository</summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SoransoftDbContext Context;
        public Repository(SoransoftDbContext context) => Context = context;

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
            await Context.Set<T>().FindAsync(new object[] { id }, ct);

        public virtual async Task<List<T>> ListAsync(CancellationToken ct = default) =>
            await Context.Set<T>().AsNoTracking().ToListAsync(ct);

        public virtual async Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await Context.Set<T>().AsNoTracking().Where(predicate).ToListAsync(ct);

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await Context.Set<T>().AsNoTracking().FirstOrDefaultAsync(predicate, ct);

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            await Context.Set<T>().AnyAsync(predicate, ct);

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
            predicate is null ? await Context.Set<T>().CountAsync(ct) : await Context.Set<T>().CountAsync(predicate, ct);

        public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
            await Context.Set<T>().AddAsync(entity, ct);

        public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            Context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            Context.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }

        public virtual async Task SaveAsync(CancellationToken ct = default) =>
            await Context.SaveChangesAsync(ct);
    }
}
