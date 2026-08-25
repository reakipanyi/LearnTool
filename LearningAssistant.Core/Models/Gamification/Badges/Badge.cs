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
        /// <summary>
        /// 是否为隐藏成就（未解锁时不显示具体信息）
        /// </summary>
        public bool IsHidden { get; set; }

        public BadgeRarity Rarity
        {
            get
            {
                int target = Requirement.TargetValue;
                return target switch
                {
                    <= 10 => BadgeRarity.Common,
                    <= 50 => BadgeRarity.Uncommon,
                    <= 200 => BadgeRarity.Rare,
                    <= 1000 => BadgeRarity.Epic,
                    _ => BadgeRarity.Legendary
                };
            }
        }
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
        PerfectSession,
        QuizCorrect,
        FavoriteCount,
        NoteCount,
        SpeedLearning,
        /// <summary>
        /// 深夜时段学习（00:00-05:00）
        /// </summary>
        NightStudy,
        /// <summary>
        /// 隐藏徽章解锁数量
        /// </summary>
        HiddenBadgeCount
    }

    public enum BadgeRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class BadgeHelper
    {
        public static List<Badge> GetAllBadges()
        {
            return new List<Badge>
            {
                new Badge
                {
                    Id = "first_blood",
                    Name = "首战告捷",
                    Description = "完成第一次学习",
                    Icon = "🏆",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 1 }
                },
                new Badge
                {
                    Id = "streak_3",
                    Name = "三日坚持",
                    Description = "连续学习3天",
                    Icon = "🔥",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 3 }
                },
                new Badge
                {
                    Id = "streak_7",
                    Name = "一周达人",
                    Description = "连续学习7天",
                    Icon = "⭐",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 7 }
                },
                new Badge
                {
                    Id = "streak_30",
                    Name = "月度冠军",
                    Description = "连续学习30天",
                    Icon = "👑",
                    Category = BadgeCategory.Consistency,
                    Requirement = new BadgeRequirement { Type = BadgeType.ConsecutiveDays, TargetValue = 30 }
                },
                new Badge
                {
                    Id = "learn_100",
                    Name = "百题斩",
                    Description = "累计学习100项",
                    Icon = "💯",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 100 }
                },
                new Badge
                {
                    Id = "learn_500",
                    Name = "五百勇士",
                    Description = "累计学习500项",
                    Icon = "⚔️",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 500 }
                },
                new Badge
                {
                    Id = "learn_1000",
                    Name = "千题大师",
                    Description = "累计学习1000项",
                    Icon = "🏅",
                    Category = BadgeCategory.Mastery,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalItemsLearned, TargetValue = 1000 }
                },
                new Badge
                {
                    Id = "perfect_day",
                    Name = "完美一天",
                    Description = "单日学习50项",
                    Icon = "🌟",
                    Category = BadgeCategory.Special,
                    Requirement = new BadgeRequirement { Type = BadgeType.PerfectSession, TargetValue = 50 }
                },
                new Badge
                {
                    Id = "quiz_master",
                    Name = "答题高手",
                    Description = "答题模式答对20题",
                    Icon = "🎯",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.QuizCorrect, TargetValue = 20 }
                },
                new Badge
                {
                    Id = "favorite_collector",
                    Name = "收藏达人",
                    Description = "收藏20个内容",
                    Icon = "❤️",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.FavoriteCount, TargetValue = 20 }
                },
                new Badge
                {
                    Id = "note_taker",
                    Name = "笔记达人",
                    Description = "记录10条笔记",
                    Icon = "📝",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.NoteCount, TargetValue = 10 }
                },
                new Badge
                {
                    Id = "speed_learner",
                    Name = "神速学习",
                    Description = "5分钟内完成10项",
                    Icon = "⚡",
                    Category = BadgeCategory.Special,
                    Requirement = new BadgeRequirement { Type = BadgeType.SpeedLearning, TargetValue = 10 }
                },
                new Badge
                {
                    Id = "time_investor",
                    Name = "时间投资者",
                    Description = "累计学习超过10小时",
                    Icon = "⏰",
                    Category = BadgeCategory.Learning,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalStudyTime, TargetValue = 600 }
                },
                new Badge
                {
                    Id = "time_master",
                    Name = "时间大师",
                    Description = "累计学习超过100小时",
                    Icon = "🎓",
                    Category = BadgeCategory.Mastery,
                    Requirement = new BadgeRequirement { Type = BadgeType.TotalStudyTime, TargetValue = 6000 }
                },
                new Badge
                {
                    Id = "night_owl",
                    Name = "夜猫子",
                    Description = "在深夜时段（00:00-05:00）完成学习",
                    Icon = "🌙",
                    Category = BadgeCategory.Special,
                    IsHidden = true,
                    Requirement = new BadgeRequirement { Type = BadgeType.NightStudy, TargetValue = 1 }
                },
                new Badge
                {
                    Id = "easter_egg_master",
                    Name = "彩蛋猎人",
                    Description = "发现并解锁一个隐藏成就",
                    Icon = "🥚",
                    Category = BadgeCategory.Special,
                    IsHidden = true,
                    Requirement = new BadgeRequirement { Type = BadgeType.HiddenBadgeCount, TargetValue = 1 }
                }
            };
        }
    }
}
