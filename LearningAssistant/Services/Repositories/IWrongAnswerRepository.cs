using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Services.Repositories
{
    public interface IWrongAnswerRepository : IRepository<WrongAnswerEntity>
    {
        Task<IEnumerable<WrongAnswerEntity>> GetByUserIdAsync(string userId);
        Task<IEnumerable<WrongAnswerEntity>> GetByUserIdAndCategoryAsync(string userId, string category);
    }

    public class WrongAnswerRepository : RepositoryBase<WrongAnswerEntity>, IWrongAnswerRepository
    {
        public WrongAnswerRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<WrongAnswerEntity>> GetByUserIdAsync(string userId)
        {
            return await DbSet.Where(item => item.UserId == userId).ToListAsync();
        }

        public async Task<IEnumerable<WrongAnswerEntity>> GetByUserIdAndCategoryAsync(string userId, string category)
        {
            return await DbSet.Where(item => item.UserId == userId && item.Category == category).ToListAsync();
        }
    }
}