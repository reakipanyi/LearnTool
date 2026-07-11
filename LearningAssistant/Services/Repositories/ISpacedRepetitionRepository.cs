using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Services.Repositories
{
    public interface ISpacedRepetitionRepository : IRepository<SpacedRepetitionItemEntity>
    {
        Task<IEnumerable<SpacedRepetitionItemEntity>> GetItemsDueForReviewAsync(string userId, DateTime? date = null);
        Task<IEnumerable<SpacedRepetitionItemEntity>> GetAllItemsByUserIdAsync(string userId);
        Task<int> GetTodayReviewCountAsync(string userId);
    }

    public class SpacedRepetitionRepository : RepositoryBase<SpacedRepetitionItemEntity>, ISpacedRepetitionRepository
    {
        public SpacedRepetitionRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<SpacedRepetitionItemEntity>> GetItemsDueForReviewAsync(string userId, DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Now;
            return await DbSet
                .Where(item => item.UserId == userId && 
                               item.NextReviewDate <= targetDate && 
                               item.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<SpacedRepetitionItemEntity>> GetAllItemsByUserIdAsync(string userId)
        {
            return await DbSet.Where(item => item.UserId == userId).ToListAsync();
        }

        public async Task<int> GetTodayReviewCountAsync(string userId)
        {
            var today = DateTime.Now.Date;
            return await DbSet
                .CountAsync(item => item.UserId == userId && 
                                    item.LastReviewDate.HasValue && 
                                    item.LastReviewDate.Value.Date == today);
        }
    }
}