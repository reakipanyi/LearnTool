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
        public DbSet<DailyRollupEntity> DailyRollups { get; set; }
        public DbSet<MigrationCheckpointEntity> MigrationCheckpoints { get; set; }

        private readonly string _dbPath;

        /// <summary>
        /// 默认构造函数，使用默认数据库路径
        /// </summary>
        public AppDbContext()
        {
            _dbPath = GetDefaultDbPath();
        }

        /// <summary>
        /// 供测试或需要自定义连接字符串的场景使用（如 SQLite 内存库）。
        /// 传入的 options 已配置 provider，此时 OnConfiguring 不再覆盖连接串。
        /// </summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // 即使由 DbContextFactory 以 options 构造，也保留真实数据库路径，
            // 便于调试和 OnConfiguring 未配置时回退使用。
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
                return;

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

            var connectionString = $"Data Source={_dbPath};Cache=Shared;Pooling=True;";
            optionsBuilder.UseSqlite(connectionString);
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

            // 每日统计快照配置
            modelBuilder.Entity<DailyRollupEntity>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<DailyRollupEntity>()
                .HasIndex(d => new { d.UserId, d.Date })
                .IsUnique();

            modelBuilder.Entity<DailyRollupEntity>()
                .HasIndex(d => d.Date);

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
                RepairDailyRollupsTable();
                RepairMigrationCheckpointsTable();
                RepairNotesTable();
                RepairLearningPathsTable();
                RepairLearningPathItemsTable();
                RepairLearningItemsTable();
                RepairPomodoroSettingsTable();
                RepairPomodoroRecordsTable();
                RepairSpacedRepetitionItemsColumn();
                RepairPomodoroSettingsColumns();
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
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                Notes TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                EaseFactor REAL NULL,
                Stability REAL NULL,
                Difficulty REAL NULL,
                ReviewTime TEXT NOT NULL,
                Duration INTEGER NOT NULL DEFAULT 0,
                AlgorithmType TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                UserId TEXT CONSTRAINT PK_UserProfiles PRIMARY KEY NOT NULL,
                UserName TEXT NOT NULL,
                LastLoginTime TEXT NOT NULL,
                AvatarPath TEXT NOT NULL,
                ConsecutiveStudyDays INTEGER NOT NULL,
                LastStudyDate TEXT NULL,
                TotalStudyTimeMinutes INTEGER NOT NULL,
                TodayStudyTimeMinutes INTEGER NOT NULL,
                TodayItemsStudied INTEGER NOT NULL,
                XP INTEGER NOT NULL,
                TotalXP INTEGER NOT NULL,
                Level INTEGER NOT NULL,
                Coins INTEGER NOT NULL,
                TotalItemsStudied INTEGER NOT NULL,
                StudyDays INTEGER NOT NULL,
                LongestStreak INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                RowVersion INTEGER NOT NULL
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
                TotalTestCount INTEGER NOT NULL DEFAULT 0,
                CorrectCount INTEGER NOT NULL DEFAULT 0,
                LastTestDate TEXT NOT NULL,
                LastResumeIndex INTEGER NOT NULL DEFAULT 0,
                QuickTestResumeIndex INTEGER NOT NULL DEFAULT 0,
                LastStudyMode TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0,
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
                Count INTEGER NOT NULL DEFAULT 1,
                RecordDate TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                Description TEXT NOT NULL DEFAULT '',
                Time TEXT NOT NULL,
                RepeatType TEXT NOT NULL DEFAULT '',
                RepeatDaysJson TEXT NOT NULL DEFAULT '[]',
                Enabled INTEGER NOT NULL DEFAULT 1,
                LastTriggered TEXT NULL,
                TriggerCount INTEGER NOT NULL DEFAULT 0,
                OpenCount INTEGER NOT NULL DEFAULT 0,
                SnoozeCount INTEGER NOT NULL DEFAULT 0,
                DismissCount INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                Repetitions INTEGER NOT NULL DEFAULT 0,
                EFactor REAL NOT NULL DEFAULT 2.5,
                NextReviewDate TEXT NOT NULL,
                WrongCount INTEGER NOT NULL DEFAULT 0,
                CorrectCount INTEGER NOT NULL DEFAULT 0,
                Stability REAL NOT NULL DEFAULT 0,
                Difficulty REAL NOT NULL DEFAULT 5,
                Retrievability REAL NOT NULL DEFAULT 1,
                LearningStage INTEGER NOT NULL DEFAULT 0,
                LastReviewDate TEXT NULL,
                ReviewCount INTEGER NOT NULL DEFAULT 0,
                CorrectStreak INTEGER NOT NULL DEFAULT 0,
                AlgorithmType TEXT NULL,
                Category TEXT NULL,
                Subject TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_UserId ON SpacedRepetitionItems(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_NextReviewDate ON SpacedRepetitionItems(NextReviewDate);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_IsActive ON SpacedRepetitionItems(IsActive);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_SpacedRepetitionItems_LearningStage ON SpacedRepetitionItems(LearningStage);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairAppSessionsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS AppSessions (
                SessionKey TEXT PRIMARY KEY,
                SessionDataJson TEXT NOT NULL DEFAULT '',
                LastAccessTime TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                ProgressJson TEXT NOT NULL DEFAULT '{{}}',
                CompletedJson TEXT NOT NULL DEFAULT '{{}}',
                AllCompleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyGoalRecords_UserId ON DailyGoalRecords(UserId);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyGoalRecords_Date ON DailyGoalRecords(Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairDailyRollupsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS DailyRollups (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                TimeSpentMinutes INTEGER NOT NULL DEFAULT 0,
                ItemsStudied INTEGER NOT NULL DEFAULT 0,
                CorrectCount INTEGER NOT NULL DEFAULT 0,
                WrongCount INTEGER NOT NULL DEFAULT 0,
                Accuracy REAL NOT NULL DEFAULT 0,
                StreakDays INTEGER NOT NULL DEFAULT 0,
                XP INTEGER NOT NULL DEFAULT 0,
                Level INTEGER NOT NULL DEFAULT 1,
                GoalCompleted INTEGER NOT NULL DEFAULT 0,
                TopCategory TEXT NOT NULL DEFAULT '',
                WeakCategory TEXT NOT NULL DEFAULT '',
                Version INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_DailyRollups_UserId_Date ON DailyRollups(UserId, Date);";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_DailyRollups_Date ON DailyRollups(Date);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairMigrationCheckpointsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS MigrationCheckpoints (
                StepId TEXT PRIMARY KEY,
                Status TEXT NOT NULL DEFAULT 'Pending',
                DetailJson TEXT NOT NULL DEFAULT '{{}}',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairNotesTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS Notes (
                Id TEXT CONSTRAINT PK_Notes PRIMARY KEY NOT NULL,
                UserId TEXT NOT NULL,
                Title TEXT NOT NULL DEFAULT '',
                Content TEXT NOT NULL DEFAULT '',
                Category TEXT NOT NULL DEFAULT '',
                Tags TEXT NOT NULL DEFAULT '',
                RelatedType TEXT NOT NULL DEFAULT '',
                RelatedItemId TEXT NOT NULL DEFAULT '',
                RelatedItemTitle TEXT NOT NULL DEFAULT '',
                Importance INTEGER NOT NULL DEFAULT 3,
                IsFavorite INTEGER NOT NULL DEFAULT 0,
                LastReviewedAt TEXT,
                ReviewCount INTEGER NOT NULL DEFAULT 0,
                Color TEXT NOT NULL DEFAULT '',
                Source TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
                Id TEXT CONSTRAINT PK_LearningPaths PRIMARY KEY NOT NULL,
                UserId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                Goal TEXT NOT NULL DEFAULT '',
                PathType TEXT NOT NULL DEFAULT 'custom',
                Domain TEXT NOT NULL DEFAULT '',
                Level TEXT NOT NULL DEFAULT '初级',
                TotalEstimatedMinutes INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                StartDate TEXT,
                TargetDate TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningPaths_UserId ON LearningPaths(UserId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningPathItemsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningPathItems (
                Id TEXT CONSTRAINT PK_LearningPathItems PRIMARY KEY NOT NULL,
                PathId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL DEFAULT '',
                ContentType TEXT NOT NULL DEFAULT '',
                ContentIds TEXT NOT NULL DEFAULT '[]',
                EstimatedMinutes INTEGER NOT NULL DEFAULT 0,
                DifficultyLevel INTEGER NOT NULL DEFAULT 1,
                Prerequisites TEXT NOT NULL DEFAULT '[]',
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CompletedAt TEXT,
                Progress INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningPathItems_PathId ON LearningPathItems(PathId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairLearningItemsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS LearningItems (
                Id TEXT CONSTRAINT PK_LearningItems PRIMARY KEY NOT NULL,
                Subject TEXT NOT NULL,
                SubCategory TEXT NOT NULL DEFAULT '',
                MainContent TEXT NOT NULL,
                MeaningJson TEXT,
                ExampleJson TEXT,
                PronunciationJson TEXT,
                CharacterFeaturesJson TEXT,
                WordFeaturesJson TEXT,
                ExtendedProperties TEXT NOT NULL DEFAULT '{{}}',
                Status TEXT NOT NULL DEFAULT 'New',
                ReviewCount INTEGER NOT NULL DEFAULT 0,
                LastReviewedAt TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE INDEX IF NOT EXISTS IX_LearningItems_Subject ON LearningItems(Subject);";
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
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
            );";
            Database.ExecuteSqlRaw(sql);

            sql = @"CREATE UNIQUE INDEX IF NOT EXISTS IX_PomodoroSettings_UserId ON PomodoroSettings(UserId);";
            Database.ExecuteSqlRaw(sql);
        }

        private void RepairPomodoroRecordsTable()
        {
            var sql = @"CREATE TABLE IF NOT EXISTS PomodoroRecords (
                Id TEXT CONSTRAINT PK_PomodoroRecords PRIMARY KEY NOT NULL,
                UserId TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Type TEXT NOT NULL DEFAULT '',
                DurationSeconds INTEGER NOT NULL DEFAULT 0,
                PlannedDurationSeconds INTEGER NOT NULL DEFAULT 0,
                Completed INTEGER NOT NULL DEFAULT 0,
                Task TEXT,
                InterruptionCount INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                RowVersion INTEGER NOT NULL DEFAULT 0
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
    }
}
