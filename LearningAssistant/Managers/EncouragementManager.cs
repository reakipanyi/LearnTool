namespace LearningAssistant.Managers
{
    /// <summary>
    /// 鼓励语管理器 - 负责生成学习过程中的鼓励语
    /// </summary>
    public class EncouragementManager
    {
        private readonly Random _random = new Random();

        // 鼓励语数组
        private readonly string[] _encouragements = {
            "太棒了！继续保持！💪", "你做得很好！🌟", "学习使我快乐！📚",
            "坚持就是胜利！✨", "知识就是力量！💡", "每天进步一点点！🌱",
            "加油，你可以的！🚀", "聪明的选择！🎯", "继续努力！🔥",
            "你真了不起！👏", "再接再厉！🏃", "勇往直前！⚡",
            "信心十足！💯", "专注学习！🎧", "收获满满！📖",
            "步步为营！🚶", "厚积薄发！📈", "持之以恒！⏰",
            "自强不息！🌟", "志在必得！🎯"
        };

        // 正确答案消息
        private readonly string[] _correctMessages = {
            "回答正确！🎉", "完美！🌟", "太棒了！👏", "正确！✅",
            "你真聪明！💡", "非常棒！⭐", "答对了！🎊", "真厉害！💪",
            "满分！💯", "超棒！🔥"
        };

        // 错误答案消息
        private readonly string[] _wrongMessages = {
            "再想想！💭", "加油！💪", "别灰心！🌈", "继续尝试！🔥",
            "下次会更好！🌟", "再接再厉！💡", "相信自己！💪",
            "仔细想想！🤔", "别放弃！🚀", "坚持就是胜利！✨"
        };

        /// <summary>
        /// 获取随机鼓励语
        /// </summary>
        public string GetRandomEncouragement() =>
            _encouragements[_random.Next(_encouragements.Length)];

        /// <summary>
        /// 获取随机正确答案消息
        /// </summary>
        public string GetRandomCorrectMessage() =>
            _correctMessages[_random.Next(_correctMessages.Length)];

        /// <summary>
        /// 获取随机错误答案消息
        /// </summary>
        public string GetRandomWrongMessage() =>
            _wrongMessages[_random.Next(_wrongMessages.Length)];

        /// <summary>
        /// 获取所有鼓励语
        /// </summary>
        public IEnumerable<string> GetAllEncouragements() => _encouragements;

        /// <summary>
        /// 获取鼓励语数量
        /// </summary>
        public int EncouragementCount => _encouragements.Length;

        /// <summary>
        /// 获取正确答案消息数量
        /// </summary>
        public int CorrectMessageCount => _correctMessages.Length;

        /// <summary>
        /// 获取错误答案消息数量
        /// </summary>
        public int WrongMessageCount => _wrongMessages.Length;
    }
}
