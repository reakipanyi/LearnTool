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
        public DbSet<ReviewLogEntity> ReviewLogs { get; set; }
        public DbSet<LearningItemStateEntity> LearningItemStates { get; set; }
        public DbSet<ReminderRepeatDayEntity> ReminderRepeatDays { get; set; }
        public DbSet<BadgeUnlockEntity> BadgeUnlocks { get; set; }
        public DbSet<StudyStatsEntity> StudyStats { get; set; }
        public DbSet<DailyChallengeEntity> DailyChallenges { get; set; }
        public DbSet<ChallengeHistoryEntity> ChallengeHistories { get; set; }
        public DbSet<LearningGoalEntity> LearningGoals { get; set; }
        public DbSet<DailyGoalRecordEntity> DailyGoalRecords { get; set; }
        public DbSet<FavoriteFolderEntity> FavoriteFolders { get; set; }
        public DbSet<FavoriteItemEntity> FavoriteItems { get; set; }
        public DbSet<PomodoroSettingsEntity> PomodoroSettings { get; set; }
        public DbSet<PomodoroRecordEntity> PomodoroRecords { get; set; }
        public DbSet<WrongAnswerEntity> WrongAnswers { get; set; }

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
            try
            {
                var dbDir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建数据库目录失败: {ex.Message}");
            }

            var connectionString = $"Data Source={_dbPath};Cache=Shared;Pooling=True;";
            optionsBuilder.UseSqlite(connectionString);

#if DEBUG
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

            modelBuilder.Entity<SpacedRepetitionItemEntity>()
                .HasIndex(s => s.LearningStage);

            modelBuilder.Entity<SpacedRepetitionItemEntity>()
                .HasMany(s => s.ReviewLogs)
                .WithOne(r => r.SpacedRepetitionItem)
                .HasForeignKey(r => r.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            // 复习日志配置
            modelBuilder.Entity<ReviewLogEntity>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<ReviewLogEntity>()
                .HasIndex(r => r.UserId);

            modelBuilder.Entity<ReviewLogEntity>()
                .HasIndex(r => r.ContentId);

            modelBuilder.Entity<ReviewLogEntity>()
                .HasIndex(r => r.ReviewTime);

            modelBuilder.Entity<ReviewLogEntity>()
                .HasIndex(r => r.AlgorithmType);

            // 学习项状态配置
            modelBuilder.Entity<LearningItemStateEntity>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<LearningItemStateEntity>()
                .HasIndex(l => l.UserId);

            modelBuilder.Entity<LearningItemStateEntity>()
                .HasIndex(l => l.CategoryName);

            modelBuilder.Entity<LearningItemStateEntity>()
                .HasIndex(l => l.IsKnown);

            modelBuilder.Entity<LearningItemStateEntity>()
                .HasIndex(l => new { l.UserId, l.CategoryName, l.Content })
                .IsUnique();

            // 提醒重复日期配置
            modelBuilder.Entity<ReminderRepeatDayEntity>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<ReminderRepeatDayEntity>()
                .HasIndex(r => r.ReminderId);

            modelBuilder.Entity<ReminderRepeatDayEntity>()
                .HasIndex(r => r.DayOfWeek);

            modelBuilder.Entity<ReminderRepeatDayEntity>()
                .HasOne(r => r.Reminder)
                .WithMany()
                .HasForeignKey(r => r.ReminderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 会话状态配置
            modelBuilder.Entity<AppSessionEntity>()
                .HasKey(a => a.SessionKey);

            modelBuilder.Entity<AppSessionEntity>()
                .HasIndex(a => a.LastAccessTime);

            // 徽章解锁配置
            modelBuilder.Entity<BadgeUnlockEntity>()
                .HasKey(b => b.Id);

            modelBuilder.Entity<BadgeUnlockEntity>()
                .HasIndex(b => b.UserId);

            modelBuilder.Entity<BadgeUnlockEntity>()
                .HasIndex(b => new { b.UserId, b.BadgeId })
                .IsUnique();

            // 学习统计配置
            modelBuilder.Entity<StudyStatsEntity>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<StudyStatsEntity>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            // 每日挑战配置
            modelBuilder.Entity<DailyChallengeEntity>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<DailyChallengeEntity>()
                .HasIndex(d => new { d.UserId, d.Date })
                .IsUnique();

            // 挑战历史配置
            modelBuilder.Entity<ChallengeHistoryEntity>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<ChallengeHistoryEntity>()
                .HasIndex(c => c.UserId);

            modelBuilder.Entity<ChallengeHistoryEntity>()
                .HasIndex(c => c.Date);

            // 学习目标配置
            modelBuilder.Entity<LearningGoalEntity>()
                .HasKey(g => g.Id);

            modelBuilder.Entity<LearningGoalEntity>()
                .HasIndex(g => new { g.UserId, g.GoalType })
                .IsUnique();

            // 每日目标记录配置
            modelBuilder.Entity<DailyGoalRecordEntity>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<DailyGoalRecordEntity>()
                .HasIndex(r => new { r.UserId, r.Date })
                .IsUnique();

            // 收藏夹文件夹配置
            modelBuilder.Entity<FavoriteFolderEntity>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<FavoriteFolderEntity>()
                .HasIndex(f => f.UserId);

            modelBuilder.Entity<FavoriteFolderEntity>()
                .HasIndex(f => f.ParentId);

            // 收藏项配置
            modelBuilder.Entity<FavoriteItemEntity>()
                .HasKey(f => f.Id);

            modelBuilder.Entity<FavoriteItemEntity>()
                .HasIndex(f => f.UserId);

            modelBuilder.Entity<FavoriteItemEntity>()
                .HasIndex(f => f.FolderId);

            modelBuilder.Entity<FavoriteItemEntity>()
                .HasIndex(f => f.ItemType);

            modelBuilder.Entity<FavoriteItemEntity>()
                .HasIndex(f => f.IsPinned);

            // 番茄钟设置配置
            modelBuilder.Entity<PomodoroSettingsEntity>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<PomodoroSettingsEntity>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // 番茄钟记录配置
            modelBuilder.Entity<PomodoroRecordEntity>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<PomodoroRecordEntity>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<PomodoroRecordEntity>()
                .HasIndex(p => p.StartTime);

            modelBuilder.Entity<PomodoroRecordEntity>()
                .HasIndex(p => p.Type);

            // 错题本配置
            modelBuilder.Entity<WrongAnswerEntity>()
                .HasKey(w => w.Id);

            modelBuilder.Entity<WrongAnswerEntity>()
                .HasIndex(w => w.UserId);

            modelBuilder.Entity<WrongAnswerEntity>()
                .HasIndex(w => w.Category);

            modelBuilder.Entity<WrongAnswerEntity>()
                .HasIndex(w => w.IsActive);

            modelBuilder.Entity<WrongAnswerEntity>()
                .HasIndex(w => w.MasteryLevel);

            modelBuilder.Entity<WrongAnswerEntity>()
                .HasIndex(w => w.NextReviewAt);
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

        /// <summary>
        /// 修复数据库Schema，添加缺失的表和列
        /// </summary>
        public void RepairSchema()
        {
            try
            {
                RepairBadgeUnlocksTable();
                RepairStudyStatsTable();
                RepairDailyChallengesTable();
                RepairChallengeHistoriesTable();
                RepairLearningItemStatesTable();
                RepairWrongAnswersTable();
                RepairReviewLogsTable();
                RepairSpacedRepetitionItemsColumn();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Schema修复失败: {ex.Message}");
                throw;
            }
        }

        private void RepairBadgeUnlocksTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS BadgeUnlocks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                BadgeId TEXT NOT NULL,
                UnlockedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_BadgeUnlocks_UserId_BadgeId ON BadgeUnlocks(UserId, BadgeId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairStudyStatsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS StudyStats (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                TodayLearnedCount INTEGER NOT NULL DEFAULT 0,
                StreakDays INTEGER NOT NULL DEFAULT 0,
                TotalScore INTEGER NOT NULL DEFAULT 0,
                TotalLearnedCount INTEGER NOT NULL DEFAULT 0,
                XP INTEGER NOT NULL DEFAULT 0,
                LastStudyDate TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_StudyStats_UserId ON StudyStats(UserId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairDailyChallengesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS DailyChallenges (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                ChallengesJson TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_DailyChallenges_UserId_Date ON DailyChallenges(UserId, Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairChallengeHistoriesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS ChallengeHistories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                CompletedCount INTEGER NOT NULL DEFAULT 0,
                TotalCount INTEGER NOT NULL DEFAULT 0,
                ClaimedCount INTEGER NOT NULL DEFAULT 0,
                TotalXP INTEGER NOT NULL DEFAULT 0,
                ChallengesJson TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ChallengeHistories_UserId ON ChallengeHistories(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ChallengeHistories_Date ON ChallengeHistories(Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningItemStatesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningItemStates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                CategoryName TEXT NOT NULL,
                Content TEXT NOT NULL,
                IsKnown INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItemStates_UserId ON LearningItemStates(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItemStates_CategoryName ON LearningItemStates(CategoryName);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItemStates_IsKnown ON LearningItemStates(IsKnown);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_LearningItemStates_UserId_CategoryName_Content ON LearningItemStates(UserId, CategoryName, Content);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairWrongAnswersTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS WrongAnswers (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Subject TEXT NOT NULL,
                Category TEXT NOT NULL,
                Question TEXT NOT NULL,
                CorrectAnswer TEXT NOT NULL,
                UserAnswer TEXT NOT NULL,
                Explanation TEXT NOT NULL,
                AddedAt TEXT NOT NULL,
                LastReviewAt TEXT,
                ReviewCount INTEGER NOT NULL DEFAULT 0,
                WrongCount INTEGER NOT NULL DEFAULT 1,
                CorrectCount INTEGER NOT NULL DEFAULT 0,
                Difficulty REAL NOT NULL DEFAULT 0.5,
                MasteryLevel INTEGER NOT NULL DEFAULT 0,
                Tags TEXT,
                NextReviewAt TEXT,
                FirstWrongAt TEXT NOT NULL,
                LastWrongAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                Notes TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_WrongAnswers_UserId ON WrongAnswers(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_WrongAnswers_Category ON WrongAnswers(Category);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_WrongAnswers_IsActive ON WrongAnswers(IsActive);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_WrongAnswers_MasteryLevel ON WrongAnswers(MasteryLevel);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_WrongAnswers_NextReviewAt ON WrongAnswers(NextReviewAt);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairReviewLogsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS ReviewLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                ContentId TEXT NOT NULL,
                Rating INTEGER NOT NULL,
                Interval INTEGER NOT NULL DEFAULT 0,
                EaseFactor REAL,
                Stability REAL,
                Difficulty REAL,
                ReviewTime TEXT NOT NULL,
                Duration INTEGER NOT NULL DEFAULT 0,
                AlgorithmType TEXT,
                CreatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ReviewLogs_UserId ON ReviewLogs(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ReviewLogs_ContentId ON ReviewLogs(ContentId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ReviewLogs_ReviewTime ON ReviewLogs(ReviewTime);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ReviewLogs_AlgorithmType ON ReviewLogs(AlgorithmType);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairSpacedRepetitionItemsColumn()
        {
            try
            {
                var sql = @"ALTER TABLE SpacedRepetitionItems ADD COLUMN AlgorithmType TEXT;";
                Database.ExecuteSqlRaw(sql);
            }
            catch (Exception)
            {
            }
        }
    }
}
