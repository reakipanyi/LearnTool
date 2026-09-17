using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Services.Repositories
{
    public interface IReminderRepository : IRepository<ReminderEntity>
    {
        Task<IEnumerable<ReminderEntity>> GetByUserIdAsync(string userId);
        Task<IEnumerable<ReminderEntity>> GetActiveRemindersAsync(string userId);
        Task<IEnumerable<ReminderEntity>> GetRemindersForTodayAsync(string userId);
    }

    public class ReminderRepository : RepositoryBase<ReminderEntity>, IReminderRepository
    {
        public ReminderRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<ReminderEntity>> GetByUserIdAsync(string userId)
        {
            return await DbSet.Where(r => r.UserId == userId).ToListAsync();
        }

        public async Task<IEnumerable<ReminderEntity>> GetActiveRemindersAsync(string userId)
        {
            return await DbSet.Where(r => r.UserId == userId && r.Enabled).ToListAsync();
        }

        public async Task<IEnumerable<ReminderEntity>> GetRemindersForTodayAsync(string userId)
        {
            return await DbSet.Where(r => r.UserId == userId && r.Enabled).ToListAsync();
        }
    }
}