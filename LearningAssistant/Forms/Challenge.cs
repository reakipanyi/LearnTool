namespace LearningAssistant.Forms
{
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
        }
    }
}
