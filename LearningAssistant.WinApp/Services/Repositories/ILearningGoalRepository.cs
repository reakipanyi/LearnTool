using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Services.Repositories
{
    public interface ILearningGoalRepository : IRepository<LearningGoalEntity>
    {
        Task<IEnumerable<LearningGoalEntity>> GetByUserIdAsync(string userId);
        Task<IEnumerable<LearningGoalEntity>> GetEnabledGoalsAsync(string userId);
        Task<LearningGoalEntity?> GetByUserIdAndTypeAsync(string userId, string goalType);
    }

    public class LearningGoalRepository : RepositoryBase<LearningGoalEntity>, ILearningGoalRepository
    {
        public LearningGoalRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<LearningGoalEntity>> GetByUserIdAsync(string userId)
        {
            return await DbSet.Where(g => g.UserId == userId).ToListAsync();
        }

        public async Task<IEnumerable<LearningGoalEntity>> GetEnabledGoalsAsync(string userId)
        {
            return await DbSet.Where(g => g.UserId == userId && g.Enabled).ToListAsync();
        }

        public async Task<LearningGoalEntity?> GetByUserIdAndTypeAsync(string userId, string goalType)
        {
            return await DbSet.FirstOrDefaultAsync(g => g.UserId == userId && g.GoalType == goalType);
        }
    }
}