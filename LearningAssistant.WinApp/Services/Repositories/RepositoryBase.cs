using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Services.Repositories
{
    public abstract class RepositoryBase<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext DbContext;
        protected readonly DbSet<T> DbSet;

        protected RepositoryBase(AppDbContext dbContext)
        {
            DbContext = dbContext;
            DbSet = dbContext.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(object id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await DbSet.AddAsync(entity);
            await DbContext.SaveChangesAsync();
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await DbSet.AddRangeAsync(entities);
            await DbContext.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            DbSet.Update(entity);
            await DbContext.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            DbSet.Remove(entity);
            await DbContext.SaveChangesAsync();
        }

        public virtual async Task DeleteByIdAsync(object id)
        {
            var entity = await DbSet.FindAsync(id);
            if (entity != null)
            {
                DbSet.Remove(entity);
                await DbContext.SaveChangesAsync();
            }
        }

        public virtual async Task<bool> ExistsAsync(object id)
        {
            return await DbSet.FindAsync(id) != null;
        }

        public virtual async Task<int> CountAsync()
        {
            return await DbSet.CountAsync();
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await DbContext.SaveChangesAsync();
        }

        public virtual Task BeginTransactionAsync()
        {
            return DbContext.Database.BeginTransactionAsync();
        }

        public virtual Task CommitTransactionAsync()
        {
            return DbContext.Database.CommitTransactionAsync();
        }

        public virtual Task RollbackTransactionAsync()
        {
            return DbContext.Database.RollbackTransactionAsync();
        }
    }
}