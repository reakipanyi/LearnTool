using Microsoft.EntityFrameworkCore;
using LearningAssistant.Models.User;

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

        private readonly string _dbPath;

        /// <summary>
        /// 默认构造函数，使用默认数据库路径
        /// </summary>
        public AppDbContext()
        {
            _dbPath = GetDefaultDbPath();
        }

        /// <summary>
        /// 自定义数据库路径构造函数
        /// </summary>
        /// <param name="dbPath">数据库文件路径</param>
        public AppDbContext(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>
        /// 获取默认数据库路径
        /// </summary>
        private string GetDefaultDbPath()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appFolder = Path.Combine(appDataPath, "LearningAssistant");
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);
                
                return Path.Combine(appFolder, "learning_assistant.db");
            }
            catch (Exception)
            {
                // 回退到应用程序目录
                var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "learning_assistant.db");
                return fallbackPath;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
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
            catch (Exception)
            {
                // 静默处理数据库创建错误
            }
        }
    }
}
