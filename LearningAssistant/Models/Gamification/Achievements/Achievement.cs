namespace LearningAssistant.Models.User
{
    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public AchievementCategory Category { get; set; }
        public AchievementRequirement Requirement { get; set; } = new AchievementRequirement();
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class AchievementRequirement
    {
        public AchievementType Type { get; set; }
        public int TargetValue { get; set; }
        public string? SubCategory { get; set; }
    }

    public enum AchievementCategory
    {
        Learning,
        Consistency,
        Mastery,
        Exploration,
        Special
    }

    public enum AchievementType
    {
        TotalItemsStudied,
        ConsecutiveDays,
        MasteredItems,
        CategoriesCompleted,
        PerfectSession,
        StudyDuration
    }

    public static class AchievementHelper
    {
        public static List<Achievement> GetAllAchievements()
        {
            return new List<Achievement>
            {
                // 学习类成就
                new Achievement
                {
                    Id = "first_steps",
                    Name = "第一步",
                    Description = "完成你的第一个学习项目",
                    Icon = "👶",
                    Category = AchievementCategory.Learning,
                    Requirement = new AchievementRequirement { Type = AchievementType.TotalItemsStudied, TargetValue = 1 },
                    DisplayOrder = 1
                },
                new Achievement
                {
                    Id = "learner_10",
                    Name = "初学者",
                    Description = "学习了 10 个项目",
                    Icon = "📚",
                    Category = AchievementCategory.Learning,
                    Requirement = new AchievementRequirement { Type = AchievementType.TotalItemsStudied, TargetValue = 10 },
                    DisplayOrder = 2
                },
                new Achievement
                {
                    Id = "learner_100",
                    Name = "勤奋学者",
                    Description = "学习了 100 个项目",
                    Icon = "🎓",
                    Category = AchievementCategory.Learning,
                    Requirement = new AchievementRequirement { Type = AchievementType.TotalItemsStudied, TargetValue = 100 },
                    DisplayOrder = 3
                },
                new Achievement
                {
                    Id = "learner_500",
                    Name = "知识大师",
                    Description = "学习了 500 个项目",
                    Icon = "🏆",
                    Category = AchievementCategory.Learning,
                    Requirement = new AchievementRequirement { Type = AchievementType.TotalItemsStudied, TargetValue = 500 },
                    DisplayOrder = 4
                },

                // 坚持类成就
                new Achievement
                {
                    Id = "streak_1",
                    Name = "良好开端",
                    Description = "连续学习 1 天",
                    Icon = "🌟",
                    Category = AchievementCategory.Consistency,
                    Requirement = new AchievementRequirement { Type = AchievementType.ConsecutiveDays, TargetValue = 1 },
                    DisplayOrder = 5
                },
                new Achievement
                {
                    Id = "streak_7",
                    Name = "周冠军",
                    Description = "连续学习 7 天",
                    Icon = "📅",
                    Category = AchievementCategory.Consistency,
                    Requirement = new AchievementRequirement { Type = AchievementType.ConsecutiveDays, TargetValue = 7 },
                    DisplayOrder = 6
                },
                new Achievement
                {
                    Id = "streak_30",
                    Name = "月度之星",
                    Description = "连续学习 30 天",
                    Icon = "🌙",
                    Category = AchievementCategory.Consistency,
                    Requirement = new AchievementRequirement { Type = AchievementType.ConsecutiveDays, TargetValue = 30 },
                    DisplayOrder = 7
                },

                // 掌握类成就
                new Achievement
                {
                    Id = "master_10",
                    Name = "小有成就",
                    Description = "掌握了 10 个项目",
                    Icon = "✅",
                    Category = AchievementCategory.Mastery,
                    Requirement = new AchievementRequirement { Type = AchievementType.MasteredItems, TargetValue = 10 },
                    DisplayOrder = 8
                },
                new Achievement
                {
                    Id = "master_50",
                    Name = "精通者",
                    Description = "掌握了 50 个项目",
                    Icon = "💪",
                    Category = AchievementCategory.Mastery,
                    Requirement = new AchievementRequirement { Type = AchievementType.MasteredItems, TargetValue = 50 },
                    DisplayOrder = 9
                },

                // 完美会话成就
                new Achievement
                {
                    Id = "perfect_session",
                    Name = "完美表现",
                    Description = "在一次学习中全部答对",
                    Icon = "💯",
                    Category = AchievementCategory.Special,
                    Requirement = new AchievementRequirement { Type = AchievementType.PerfectSession, TargetValue = 1 },
                    DisplayOrder = 10
                }
            };
        }
    }
}
