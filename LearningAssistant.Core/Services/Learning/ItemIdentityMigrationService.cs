using LearningAssistant.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 启动期词库 Id 回填迁移：为旧 JSON 学习项补齐稳定 Id。
    /// 使用迁移检查点记录上次成功时间，后续启动只扫描被修改过的文件。
    /// </summary>
    public interface IItemIdentityMigrationService
    {
        /// <summary>执行回填，返回本次补写的条数。</summary>
        int Run();
    }

    public sealed class ItemIdentityMigrationService : IItemIdentityMigrationService
    {
        private const string StepId = "ItemIdentity-v1";

        private const string Pending = "Pending";
        private const string Running = "Running";
        private const string Completed = "Completed";
        private const string Failed = "Failed";

        private readonly IContentLoaderService _contentLoaderService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<ItemIdentityMigrationService>? _logger;

        public ItemIdentityMigrationService(
            IContentLoaderService contentLoaderService,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<ItemIdentityMigrationService>? logger = null)
        {
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger;
        }

        public int Run()
        {
            var baseline = TryGetCompletedBaseline();
            MarkStatus(Running, new { modifiedSince = baseline });

            try
            {
                var fixedCount = _contentLoaderService.EnsureAllItemsHaveIds(baseline);
                MarkStatus(Completed, new { fixedCount, finishedAt = DateTime.Now });
                _logger?.LogInformation("Item identity migration completed, {Count} items fixed", fixedCount);
                return fixedCount;
            }
            catch (Exception ex)
            {
                MarkStatus(Failed, new { error = ex.Message });
                _logger?.LogError(ex, "Item identity migration failed");
                return 0;
            }
        }

        /// <summary>
        /// 读取上次成功时间；从未成功迁移过则返回 null（全量扫描）。
        /// 回退 1 秒，避免文件时间精度导致边界文件被漏扫。
        /// </summary>
        private DateTime? TryGetCompletedBaseline()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var step = db.MigrationCheckpoints.AsNoTracking().FirstOrDefault(s => s.StepId == StepId);
                if (step != null && step.Status == Completed)
                {
                    return step.UpdatedAt.AddSeconds(-1);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to read item identity migration checkpoint");
            }

            return null;
        }

        private void MarkStatus(string status, object? detail)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var step = db.MigrationCheckpoints.FirstOrDefault(s => s.StepId == StepId);
                var detailJson = detail != null
                    ? System.Text.Json.JsonSerializer.Serialize(detail)
                    : (step?.DetailJson ?? "{}");

                if (step == null)
                {
                    db.MigrationCheckpoints.Add(new MigrationCheckpointEntity
                    {
                        StepId = StepId,
                        Status = status,
                        DetailJson = detailJson,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    step.Status = status;
                    step.DetailJson = detailJson;
                    step.UpdatedAt = DateTime.Now;
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update item identity migration checkpoint: {Status}", status);
            }
        }

        /// <summary>供测试或首次初始化使用：Pending 状态。</summary>
        public void EnsureCheckpointExists()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                if (db.MigrationCheckpoints.Any(s => s.StepId == StepId)) return;

                db.MigrationCheckpoints.Add(new MigrationCheckpointEntity
                {
                    StepId = StepId,
                    Status = Pending,
                    DetailJson = "{}",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create item identity migration checkpoint");
            }
        }
    }
}
