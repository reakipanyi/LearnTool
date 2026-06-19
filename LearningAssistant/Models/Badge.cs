namespace LearningAssistant.Models
{
    /// <summary>
    /// 学习徽章定义
    /// </summary>
    public class Badge
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Emoji { get; }
        public int RequiredCount { get; }
        public bool Unlocked { get; set; }

        public Badge(string id, string name, string description, string emoji, int requiredCount)
        {
            Id = id;
            Name = name;
            Description = description;
            Emoji = emoji;
            RequiredCount = requiredCount;
            Unlocked = false;
        }
    }
}
