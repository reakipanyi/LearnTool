namespace LearningAssistant.Models.User
{
    public enum ChallengeType
    {
        LearnItems,      // 学习项数
        ReviewItems,     // 复习项数
        WrongItems,      // 错题复习数
        StudyTime,       // 学习时长
        Accuracy,        // 正确率
        Streak,          // 连续天数
        Notes,           // 笔记数
        Favorites,       // 收藏数
        PerfectDay,      // 完美一天
        Custom           // 自定义
    }

    /// <summary>
    /// 每日挑战任务
    /// </summary>
    public class Challenge
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Emoji { get; set; }
        public int Target { get; set; }
        public int Current { get; set; }
        public int Reward { get; set; }
        public bool Completed { get; set; }
        public bool Claimed { get; set; }
        public ChallengeType Type { get; set; }

        public Challenge() { }

        public Challenge(string id, string name, string description, string emoji, int target, int reward)
        {
            Id = id;
            Name = name;
            Description = description;
            Emoji = emoji;
            Target = target;
            Current = 0;
            Reward = reward;
            Completed = false;
            Claimed = false;
            Type = ChallengeType.Custom;
        }
    }
}
