using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Services.Repositories
{
    public interface IUserProfileRepository : IRepository<UserProfileEntity>
    {
        Task<UserProfileEntity?> GetByUserNameAsync(string userName);
        Task<UserProfileEntity?> GetByUserIdAsync(string userId);
        Task<IEnumerable<UserProfileEntity>> GetAllActiveUsersAsync();
    }

    public class UserProfileRepository : RepositoryBase<UserProfileEntity>, IUserProfileRepository
    {
        public UserProfileRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<UserProfileEntity?> GetByUserNameAsync(string userName)
        {
            return await DbSet.FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<UserProfileEntity?> GetByUserIdAsync(string userId)
        {
            return await DbSet.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<IEnumerable<UserProfileEntity>> GetAllActiveUsersAsync()
        {
            return await DbSet.Where(u => u.IsActive).ToListAsync();
        }
    }
}