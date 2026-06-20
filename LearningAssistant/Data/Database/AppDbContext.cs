using LearningAssistant.Common;
using Microsoft.EntityFrameworkCore;

namespace LearningAssistant.Data.Database
{
    /// <summary>
    /// 应用程序数据库上下文
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<UserProfileEntity> UserProfiles { get; set; }
        public DbSet<CategoryProgressEntity> CategoryProgresses { get; set; }
        public DbSet<LearningRecordEntity> LearningRecords { get; set; }
        public DbSet<ReminderEntity> Reminders { get; set; }
        public DbSet<SpacedRepetitionItemEntity> SpacedRepetitionItems { get; set; }
        public DbSet<AppSessionEntity> AppSessions { get; set; }

        private readonly string _dbPath;

        /// <summary>
        /// 默认构造函数，使用默认数据库路径
        /// </summary>
        public AppDbContext()
        {
            _dbPath = GetDefaultDbPath();
        }

        /// <summary>
        /// 获取默认数据库路径
        /// </summary>
        private string GetDefaultDbPath()
        {
            return AppPaths.DatabasePath;

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite 默认就启用了连接池，这里显式启用以确保配置生效
            // Cache=Shared 用于多进程访问同一数据库文件时的兼容性
            // Pooling=True 启用连接池，减少频繁创建连接的开销
            var connectionString = $"Data Source={_dbPath};Cache=Shared;Pooling=True;";
            optionsBuilder.UseSqlite(connectionString);

#if DEBUG
            // 仅在调试模式下输出 SQL 日志，便于排查问题 
            optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Debug);
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 用户配置
            modelBuilder.Entity<UserProfileEntity>()
                .HasKey(u => u.UserId);

            // 添加索引
            modelBuilder.Entity<UserProfileEntity>()
                .HasIndex(u => u.UserName);

            modelBuilder.Entity<UserProfileEntity>()
                .HasMany(u => u.CategoryProgresses)
                .WithOne(c => c.UserProfile)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 分类进度配置
            modelBuilder.Entity<CategoryProgressEntity>()
                .HasKey(c => new { c.UserId, c.CategoryName });

            // 添加索引
            modelBuilder.Entity<CategoryProgressEntity>()
                .HasIndex(c => c.UserId);
            modelBuilder.Entity<CategoryProgressEntity>()
                .HasIndex(c => c.CategoryName);

            // 学习记录配置
            modelBuilder.Entity<LearningRecordEntity>()
                .HasKey(l => l.Id);

            // 添加索引
            modelBuilder.Entity<LearningRecordEntity>()
                .HasIndex(l => l.UserId);
            modelBuilder.Entity<LearningRecordEntity>()
                .HasIndex(l => l.RecordDate);

            modelBuilder.Entity<LearningRecordEntity>()
                .HasOne(l => l.UserProfile)
                .WithMany(u => u.LearningRecords)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 提醒配置
            modelBuilder.Entity<ReminderEntity>()
                .HasKey(r => r.Id);

            // 添加索引
            modelBuilder.Entity<ReminderEntity>()
                .HasIndex(r => r.UserId);
            modelBuilder.Entity<ReminderEntity>()
                .HasIndex(r => r.Enabled);

            modelBuilder.Entity<ReminderEntity>()
                .HasOne(r => r.UserProfile)
                .WithMany(u => u.Reminders)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 间隔重复配置
            modelBuilder.Entity<SpacedRepetitionItemEntity>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<SpacedRepetitionItemEntity>()
                .HasIndex(s => s.UserId);

            modelBuilder.Entity<SpacedRepetitionItemEntity>()
                .HasIndex(s => s.NextReviewDate);

            modelBuilder.Entity<SpacedRepetitionItemEntity>()
                .HasIndex(s => s.IsActive);

            // 会话状态配置
            modelBuilder.Entity<AppSessionEntity>()
                .HasKey(a => a.SessionKey);

            modelBuilder.Entity<AppSessionEntity>()
                .HasIndex(a => a.LastAccessTime);
        }

        /// <summary>
        /// 确保数据库已创建
        /// </summary>
        public void EnsureDatabaseCreated()
        {
            try
            {
                Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库创建失败: {ex.Message}");
                throw;
            }
        }
    }
}
