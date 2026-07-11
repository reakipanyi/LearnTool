using LearningAssistant.Common;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Data
{
    public interface ILearningItemMigrationService
    {
        Task MigrateFromFilesToDatabaseAsync(Guid userId);
        Task<bool> HasDatabaseDataAsync(Guid userId);
        Task<List<LearningItem>> LoadItemsFromDatabaseAsync(LearningContext context);
        Task SaveItemsToDatabaseAsync(Guid userId, List<LearningItem> items);
    }

    public class LearningItemMigrationService : ILearningItemMigrationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly ILogger<LearningItemMigrationService> _logger;

        public LearningItemMigrationService(AppDbContext dbContext, IContentLoaderService contentLoaderService, ILogger<LearningItemMigrationService> logger)
        {
            _dbContext = dbContext;
            _contentLoaderService = contentLoaderService;
            _logger = logger;
        }

        public async Task MigrateFromFilesToDatabaseAsync(Guid userId)
        {
            try
            {
                var subjects = _contentLoaderService.GetAllSubjects();
                
                foreach (var subjectName in subjects)
                {
                    if (!Enum.TryParse<SubjectType>(subjectName, true, out var subject))
                    {
                        continue;
                    }

                    var subCategories = _contentLoaderService.GetSubCategories(subject);
                    
                    foreach (var subCategory in subCategories)
                    {
                        var files = _contentLoaderService.GetWordBankFiles(subCategory);
                        
                        foreach (var file in files)
                        {
                            var context = new LearningContext(userId.ToString(), subject, subCategory, file);
                            var items = _contentLoaderService.LoadItems(context);
                            
                            if (items.Count == 0) continue;

                            foreach (var item in items)
                            {
                                var existingEntity = await _dbContext.LearningItems
                                    .FirstOrDefaultAsync(e => e.Id == item.Id);

                                if (existingEntity == null)
                                {
                                    var entity = item.ToEntity();
                                    await _dbContext.LearningItems.AddAsync(entity);
                                }
                            }
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Learning item data migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate learning item data from files to database");
                throw;
            }
        }

        public async Task<bool> HasDatabaseDataAsync(Guid userId)
        {
            return await _dbContext.LearningItems.AnyAsync();
        }

        public async Task<List<LearningItem>> LoadItemsFromDatabaseAsync(LearningContext context)
        {
            try
            {
                var items = await _dbContext.LearningItems
                    .Where(e => e.Subject == context.Subject.ToString() && 
                                e.SubCategory == context.SubCategory.ToString())
                    .ToListAsync();

                return items.Select(e => e.ToModel()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load learning items from database");
                return new List<LearningItem>();
            }
        }

        public async Task SaveItemsToDatabaseAsync(Guid userId, List<LearningItem> items)
        {
            try
            {
                foreach (var item in items)
                {
                    var existingEntity = await _dbContext.LearningItems
                        .FirstOrDefaultAsync(e => e.Id == item.Id);

                    if (existingEntity != null)
                    {
                        var updatedEntity = item.ToEntity();
                        _dbContext.Entry(existingEntity).CurrentValues.SetValues(updatedEntity);
                    }
                    else
                    {
                        var entity = item.ToEntity();
                        await _dbContext.LearningItems.AddAsync(entity);
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Saved {Count} learning items to database", items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save learning items to database");
                throw;
            }
        }
    }
}