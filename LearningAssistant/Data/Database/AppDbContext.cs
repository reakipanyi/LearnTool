using LearningAssistant.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace LearningAssistant.Data.Database
{
    /// <summary>
    /// 应用程序数据库上下文
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>跨进程互斥锁名称（基于数据库文件路径哈希）。</summary>
        private static readonly Mutex _dbWriteMutex = CreateGlobalMutex();

        /// <summary>SQLite 忙/锁错误码（SqliteException.SqliteErrorCode）。</summary>
        private const int SQLITE_BUSY = 5;
        private const int SQLITE_LOCKED = 6;
        /// <summary>SQLite 写冲突错误码。</summary>
        private const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;
        private const int MAX_SAVE_RETRIES = 6;

        public DbSet<UserProfileEntity> UserProfiles { get; set; }
        public DbSet<CategoryProgressEntity> CategoryProgresses { get; set; }
        public DbSet<LearningRecordEntity> LearningRecords { get; set; }
        public DbSet<ReminderEntity> Reminders { get; set; }
        public DbSet<SpacedRepetitionItemEntity> SpacedRepetitionItems { get; set; }
        public DbSet<AppSessionEntity> AppSessions { get; set; }
        public DbSet<ReviewLogEntity> ReviewLogs { get; set; }
        public DbSet<LearningItemStateEntity> LearningItemStates { get; set; }
        public DbSet<ReminderRepeatDayEntity> ReminderRepeatDays { get; set; }
        public DbSet<StudyStatsEntity> StudyStats { get; set; }
        public DbSet<PomodoroSettingsEntity> PomodoroSettings { get; set; }
        public DbSet<PomodoroRecordEntity> PomodoroRecords { get; set; }
        public DbSet<WrongAnswerEntity> WrongAnswers { get; set; }
        public DbSet<LearningItemEntity> LearningItems { get; set; }
        public DbSet<NoteEntity> Notes { get; set; }
        public DbSet<LearningPathEntity> LearningPaths { get; set; }
        public DbSet<LearningPathItemEntity> LearningPathItems { get; set; }
        public DbSet<BadgeUnlockEntity> BadgeUnlocks { get; set; }
        public DbSet<DailyChallengeEntity> DailyChallenges { get; set; }
        public DbSet<ChallengeHistoryEntity> ChallengeHistory { get; set; }
        public DbSet<LearningGoalEntity> LearningGoals { get; set; }
        public DbSet<DailyGoalRecordEntity> DailyGoalRecords { get; set; }
        public DbSet<MigrationCheckpointEntity> MigrationCheckpoints { get; set; }

        private readonly string _dbPath;

        private static Mutex CreateGlobalMutex()
        {
            // 使用数据库路径生成稳定的全局互斥锁名称。
            // 跨进程/多实例运行时可把写操作串行化，避免 1555/5 错误。
            string mutexId = $"Global\\LearningAssistantDB_{AppPaths.DatabasePath.GetHashCode():X8}";
            return new Mutex(false, mutexId);
        }

        /// <summary>
        /// 默认构造函数，使用默认数据库路径
        /// </summary>
        public AppDbContext()
        {
            _dbPath = GetDefaultDbPath();
        }

        /// <summary>
        /// 使用指定配置的构造函数，用于测试
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
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
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

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
                System.Diagnostics.Trace.TraceError($"创建数据库目录失败: {ex.Message}");
            }

            // BusyTimeout=5000: SQLite 遇到锁时最多等待 5 秒，避免立刻抛 SQLITE_BUSY。
            var connectionString = $"Data Source={_dbPath};Cache=Shared;Pooling=True;BusyTimeout=5000;";
            optionsBuilder.UseSqlite(connectionString);
        }

        /// <summary>
        /// 打开连接时启用 WAL 模式（读写并发支持更好）。
        /// </summary>
        private void EnsureWALMode()
        {
            try
            {
                var connection = Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                var result = cmd.ExecuteScalar()?.ToString();
                if (string.Equals(result, "wal", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                // 若无法切换（例如 DB 已有连接），静默忽略，默认 DELETE 模式仍可用。
            }
            catch
            {
                // 启用 WAL 是非关键增强，失败不影响主流程。
            }
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

            // 学习统计配置
            modelBuilder.Entity<StudyStatsEntity>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<StudyStatsEntity>()
                .HasIndex(s => s.UserId)
                .IsUnique();

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

            // 迁移检查点：用于 B-008 中断后断点续传
            modelBuilder.Entity<MigrationCheckpointEntity>()
                .HasKey(m => m.StepId);

            modelBuilder.Entity<MigrationCheckpointEntity>()
                .HasIndex(m => m.Status);
        }

        /// <summary>
        /// 确保数据库已创建
        /// </summary>
        public void EnsureDatabaseCreated()
        {
            try
            {
                Database.EnsureCreated();
                EnsureWALMode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"数据库创建失败: {ex.Message}");
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
                RepairUserProfilesTable();
                RepairCategoryProgressesTable();
                RepairLearningRecordsTable();
                RepairRemindersTable();
                RepairReminderRepeatDaysTable();
                RepairSpacedRepetitionItemsTable();
                RepairAppSessionsTable();
                RepairStudyStatsTable();
                RepairLearningItemStatesTable();
                RepairWrongAnswersTable();
                RepairReviewLogsTable();
                RepairBadgeUnlocksTable();
                RepairDailyChallengesTable();
                RepairChallengeHistoryTable();
                RepairLearningGoalsTable();
                RepairDailyGoalRecordsTable();
                RepairNotesTable();
                RepairLearningPathsTable();
                RepairLearningPathItemsTable();
                RepairLearningItemsTable();
                RepairPomodoroSettingsTable();
                RepairPomodoroRecordsTable();
                RepairSpacedRepetitionItemsColumn();
                RepairPomodoroSettingsColumns();
                RepairRowVersionColumns();
                RepairMigrationCheckpointsTable();
                EnsureWALMode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Schema修复失败: {ex.Message}");
                throw;
            }
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

        private void RepairUserProfilesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS UserProfiles (
                UserId TEXT PRIMARY KEY,
                UserName TEXT NOT NULL,
                LastLoginTime TEXT NOT NULL,
                AvatarPath TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_UserProfiles_UserName ON UserProfiles(UserName);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairCategoryProgressesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS CategoryProgresses (
                UserId TEXT NOT NULL,
                CategoryName TEXT NOT NULL,
                KnownItemsJson TEXT NOT NULL DEFAULT '[]',
                UnknownItemsJson TEXT NOT NULL DEFAULT '[]',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (UserId, CategoryName)
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_CategoryProgresses_UserId ON CategoryProgresses(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_CategoryProgresses_CategoryName ON CategoryProgresses(CategoryName);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningRecordsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                ActivityType TEXT NOT NULL,
                SubCategory TEXT NOT NULL DEFAULT '',
                Content TEXT NOT NULL DEFAULT '',
                Duration INTEGER NOT NULL DEFAULT 0,
                RecordDate TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningRecords_UserId ON LearningRecords(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningRecords_RecordDate ON LearningRecords(RecordDate);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairRemindersTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS Reminders (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Type TEXT NOT NULL DEFAULT 'Study',
                Title TEXT NOT NULL,
                Message TEXT NOT NULL DEFAULT '',
                Time TEXT NOT NULL,
                Enabled INTEGER NOT NULL DEFAULT 1,
                RepeatMode TEXT NOT NULL DEFAULT 'Once',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_Reminders_UserId ON Reminders(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_Reminders_Enabled ON Reminders(Enabled);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairReminderRepeatDaysTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS ReminderRepeatDays (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ReminderId TEXT NOT NULL,
                DayOfWeek INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ReminderRepeatDays_ReminderId ON ReminderRepeatDays(ReminderId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ReminderRepeatDays_DayOfWeek ON ReminderRepeatDays(DayOfWeek);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairSpacedRepetitionItemsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS SpacedRepetitionItems (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Content TEXT NOT NULL,
                Answer TEXT NOT NULL DEFAULT '',
                Interval INTEGER NOT NULL DEFAULT 0,
                EaseFactor REAL NOT NULL DEFAULT 2.5,
                Repetition INTEGER NOT NULL DEFAULT 0,
                NextReviewDate TEXT NOT NULL,
                LastReviewDate TEXT,
                Category TEXT NOT NULL DEFAULT '',
                AlgorithmType TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_UserId ON SpacedRepetitionItems(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_NextReviewDate ON SpacedRepetitionItems(NextReviewDate);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_IsActive ON SpacedRepetitionItems(IsActive);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_LearningStage ON SpacedRepetitionItems(AlgorithmType);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairAppSessionsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS AppSessions (
                SessionKey TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                LastAccessTime TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_AppSessions_LastAccessTime ON AppSessions(LastAccessTime);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairBadgeUnlocksTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS BadgeUnlocks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                BadgeId TEXT NOT NULL,
                UnlockedAt TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_BadgeUnlocks_UserId ON BadgeUnlocks(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_BadgeUnlocks_BadgeId ON BadgeUnlocks(BadgeId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairDailyChallengesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS DailyChallenges (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                ChallengesJson TEXT NOT NULL DEFAULT '[]',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyChallenges_UserId ON DailyChallenges(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyChallenges_Date ON DailyChallenges(Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairChallengeHistoryTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS ChallengeHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                CompletedCount INTEGER NOT NULL DEFAULT 0,
                TotalCount INTEGER NOT NULL DEFAULT 0,
                ClaimedCount INTEGER NOT NULL DEFAULT 0,
                TotalXP INTEGER NOT NULL DEFAULT 0,
                ChallengesJson TEXT NOT NULL DEFAULT '[]',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ChallengeHistory_UserId ON ChallengeHistory(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_ChallengeHistory_Date ON ChallengeHistory(Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningGoalsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningGoals (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                GoalType TEXT NOT NULL,
                TargetValue INTEGER NOT NULL DEFAULT 0,
                Unit TEXT,
                Enabled INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningGoals_UserId ON LearningGoals(UserId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairDailyGoalRecordsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS DailyGoalRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                ProgressJson TEXT NOT NULL DEFAULT '{}',
                CompletedJson TEXT NOT NULL DEFAULT '{}',
                AllCompleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyGoalRecords_UserId ON DailyGoalRecords(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyGoalRecords_Date ON DailyGoalRecords(Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairNotesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS Notes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Category TEXT NOT NULL DEFAULT '',
                SubCategory TEXT NOT NULL DEFAULT '',
                Title TEXT NOT NULL DEFAULT '',
                Content TEXT NOT NULL DEFAULT '',
                ContentKey TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_Notes_UserId ON Notes(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_Notes_Category ON Notes(Category);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningPathsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningPaths (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                Category TEXT NOT NULL DEFAULT '',
                IsCustom INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningPaths_UserId ON LearningPaths(UserId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningPathItemsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningPathItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                PathId INTEGER NOT NULL,
                Content TEXT NOT NULL,
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                LearningStage INTEGER NOT NULL DEFAULT 0,
                MasteryLevel INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningPathItems_UserId ON LearningPathItems(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningPathItems_PathId ON LearningPathItems(PathId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningItemsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Category TEXT NOT NULL,
                SubCategory TEXT NOT NULL DEFAULT '',
                Content TEXT NOT NULL,
                Explanation TEXT NOT NULL DEFAULT '',
                ExamplesJson TEXT NOT NULL DEFAULT '[]',
                MasteryLevel INTEGER NOT NULL DEFAULT 0,
                ReviewCount INTEGER NOT NULL DEFAULT 0,
                LastReviewAt TEXT,
                NextReviewAt TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItems_UserId ON LearningItems(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItems_Category ON LearningItems(Category);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItems_SubCategory ON LearningItems(SubCategory);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairPomodoroSettingsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS PomodoroSettings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                StudyMinutes INTEGER NOT NULL DEFAULT 25,
                ShortBreakMinutes INTEGER NOT NULL DEFAULT 5,
                LongBreakMinutes INTEGER NOT NULL DEFAULT 15,
                LongBreakInterval INTEGER NOT NULL DEFAULT 4,
                AutoStartBreak INTEGER NOT NULL DEFAULT 0,
                AutoStartStudy INTEGER NOT NULL DEFAULT 0,
                PlaySound INTEGER NOT NULL DEFAULT 1,
                ShowNotification INTEGER NOT NULL DEFAULT 1,
                SoundEnabled INTEGER NOT NULL DEFAULT 1,
                Volume INTEGER NOT NULL DEFAULT 50,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_PomodoroSettings_UserId ON PomodoroSettings(UserId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairPomodoroRecordsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS PomodoroRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Type TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT,
                Duration INTEGER NOT NULL DEFAULT 0,
                Completed INTEGER NOT NULL DEFAULT 0,
                Category TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_PomodoroRecords_UserId ON PomodoroRecords(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_PomodoroRecords_StartTime ON PomodoroRecords(StartTime);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_PomodoroRecords_Type ON PomodoroRecords(Type);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairPomodoroSettingsColumns()
        {
            try
            {
                var sql = @"ALTER TABLE PomodoroSettings ADD COLUMN SoundEnabled INTEGER NOT NULL DEFAULT 1;";
                Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"添加SoundEnabled列失败（可能已存在）: {ex.Message}");
            }

            try
            {
                var sql = @"ALTER TABLE PomodoroSettings ADD COLUMN Volume INTEGER NOT NULL DEFAULT 50;";
                Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"添加Volume列失败（可能已存在）: {ex.Message}");
            }
        }

        private void RepairSpacedRepetitionItemsColumn()
        {
            try
            {
                var sql = @"ALTER TABLE SpacedRepetitionItems ADD COLUMN AlgorithmType TEXT;";
                Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"添加AlgorithmType列失败（可能已存在）: {ex.Message}");
            }
        }

        private static readonly string[] RowVersionTables = new[]
        {
            "UserProfiles", "CategoryProgresses", "LearningRecords", "Reminders",
            "ReminderRepeatDays", "SpacedRepetitionItems", "StudyStats",
            "LearningItemStates", "PomodoroSettings", "PomodoroRecords", "WrongAnswers",
            "ReviewLogs", "Notes", "LearningPaths", "LearningPathItems", "BadgeUnlocks",
            "DailyChallenges", "ChallengeHistory", "LearningGoals", "DailyGoalRecords",
            "LearningItems"
        };

        /// <summary>
        /// 为所有包含 AuditableEntityBase 派生表补齐 RowVersion 列。
        /// 老 DB 升级时需要从旧库补列。
        /// </summary>
        private void RepairRowVersionColumns()
        {
            foreach (var table in RowVersionTables)
            {
                try
                {
                    Database.ExecuteSqlRaw($"ALTER TABLE {table} ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"为表 {table} 添加 RowVersion 失败（可能已存在）: {ex.Message}");
                }
            }
        }

        private void RepairMigrationCheckpointsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS MigrationCheckpoints (
                StepId TEXT PRIMARY KEY,
                Status TEXT NOT NULL,
                DetailJson TEXT NOT NULL DEFAULT '{}',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);
            sql = @"CREATE INDEX IF NOT EXISTS IX_MigrationCheckpoints_Status ON MigrationCheckpoints(Status);";
            Database.ExecuteSqlRaw(sql);
        }

        #region SaveChanges：行版本号递增 + 写互斥 + 忙重试

        public override int SaveChanges()
        {
            return SaveChangesWithRetry(acceptAllChangesOnSuccess: true);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return SaveChangesWithRetry(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesWithRetryAsync(acceptAllChangesOnSuccess: true, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return SaveChangesWithRetryAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// 内部统一写入调度（同步）：自动递增 RowVersion + 写互斥 + SQLite 忙错误指数退避重试。
        /// </summary>
        private int SaveChangesWithRetry(bool acceptAllChangesOnSuccess)
        {
            PrepareAuditableEntities();
            bool mutexHeld = AcquireWriteMutex();
            try
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        return base.SaveChanges(acceptAllChangesOnSuccess);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (IsBusyOrLocked(ex) && attempt < MAX_SAVE_RETRIES)
                    {
                        attempt++;
                        int delayMs = 10 << (attempt - 1);
                        Thread.Sleep(delayMs);
                    }
                }
            }
            finally
            {
                ReleaseWriteMutex(mutexHeld);
            }
        }

        /// <summary>
        /// 内部统一写入调度（异步）。
        /// </summary>
        private async Task<int> SaveChangesWithRetryAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
        {
            PrepareAuditableEntities();
            bool mutexHeld = AcquireWriteMutex();
            try
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (IsBusyOrLocked(ex) && attempt < MAX_SAVE_RETRIES)
                    {
                        attempt++;
                        int delayMs = 10 << (attempt - 1);
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ReleaseWriteMutex(mutexHeld);
            }
        }

        private bool AcquireWriteMutex()
        {
            try
            {
                return _dbWriteMutex.WaitOne(TimeSpan.FromSeconds(8));
            }
            catch (AbandonedMutexException)
            {
                return true; // 前一个进程崩溃后遗留下的 mutex，我们拿到了所有权
            }
        }

        private static void ReleaseWriteMutex(bool mutexHeld)
        {
            if (mutexHeld)
            {
                try { _dbWriteMutex.ReleaseMutex(); }
                catch { /* 极端情况下释放失败，不影响业务 */ }
            }
        }

        private static bool IsBusyOrLocked(Exception ex)
        {
            if (ex is SqliteException sqliteEx)
            {
                return sqliteEx.SqliteErrorCode == SQLITE_BUSY
                    || sqliteEx.SqliteErrorCode == SQLITE_LOCKED
                    || sqliteEx.SqliteErrorCode == SQLITE_CONSTRAINT_PRIMARYKEY;
            }
            return ex.InnerException != null && IsBusyOrLocked(ex.InnerException);
        }

        /// <summary>
        /// 在写入前自动更新 UpdatedAt 并递增 RowVersion 模拟并发令牌。
        /// </summary>
        private void PrepareAuditableEntities()
        {
            var entries = ChangeTracker.Entries<AuditableEntityBase>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.RowVersion = 1;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.Now;
                    unchecked { entry.Entity.RowVersion += 1; }
                }
            }
        }

        #endregion
    }
}
