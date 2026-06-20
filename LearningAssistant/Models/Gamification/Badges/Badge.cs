namespace LearningAssistant.Models.User
{
    public class Badge
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public BadgeCategory Category { get; set; }
        public BadgeRequirement Requirement { get; set; } = new BadgeRequirement();
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }

    public class BadgeRequirement
    {
        public BadgeType Type { get; set; }
        public int TargetValue { get; set; }
    }

    public enum BadgeCategory
    {
        Learning,
        Consistency,
        Mastery,
        Special
    }

    public enum BadgeType
    {
        ConsecutiveDays,
        TotalStudyTime,
        TotalItemsLearned,
        PerfectSession
    }

    public static class BadgeHelper
    {
        public static List<Badge> GetAllBadges()
        {
            return new List<Badge>
            {
                new Badge
                {
                    Id = "study_legend",
                    Name = "学习达人",
                    Description = "连续学习 7 天以上",
                    Icon = "🏆",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 7 }
                },
                new Badge
                {
                    Id = "perseverance",
                    Name = "坚持不懈",
                    Description = "连续学习 30 天以上",
                    Icon = "💪",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 30 }
                },
                new Badge
                {
                    Id = "study_master",
                    Name = "学习大师",
                    Description = "累计学习超过 100 小时",
                    Icon = "🎓",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalStudyTime, TargetValue = 6000 }
                },
                new Badge
                {
                    Id = "time_investor",
                    Name = "时间投资者",
                    Description = "累计学习超过 10 小时",
                    Icon = "⏰",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalStudyTime, TargetValue = 600 }
                },
                new Badge
                {
                    Id = "perfect_student",
                    Name = "完美学生",
                    Description = "完成一次完美的学习（全部答对）",
                    Icon = "🌟",
                    Category = BadgeCategory.Special,
                    Requirement = new BadgeRequirement { Type = BadgeType.PerfectSession, TargetValue = 1 }
                },
                new Badge
                {
                    Id = "knowledge_seeker",
                    Name = "知识探索者",
                    Description = "累计学习超过 500 个项目",
                    Icon = "📚",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 500 }
                },
                new Badge
                {
                    Id = "daily_learner",
                    Name = "每日学习者",
                    Description = "连续学习 3 天以上",
                    Icon = "📖",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 3 }
                },
                new Badge
                {
                    Id = "week_warrior",
                    Name = "周冠军",
                    Description = "连续学习 7 天",
                    Icon = "🏅",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 7 }
                }
            };
        }

        public static bool CheckBadgeRequirement(Badge badge, UserProfile profile)
        {
            return badge.Requirement.Type switch
            {
                BadgeType.ConsecutiveDays => profile.ConsecutiveStudyDays >= badge.Requirement.TargetValue,
                BadgeType.TotalStudyTime => profile.TotalStudyTimeMinutes >= badge.Requirement.TargetValue,
                BadgeType.TotalItemsLearned => profile.LearningProgress.ComputedTotalItemsStudied >= badge.Requirement.TargetValue,
                BadgeType.PerfectSession => profile.LearningProgress.PerfectSessions >= badge.Requirement.TargetValue,
                _ => false
            };
        }
    }
}
