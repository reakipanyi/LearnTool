using System.Collections.Generic;

namespace LearningAssistant.Models.User
{
    public class EncouragementConfig
    {
        public List<string> CorrectMessages { get; set; } = new List<string>
        {
            "太棒了！继续保持！💪",
            "你做得很好！🌟",
            "学习使我快乐！📚",
            "坚持就是胜利！✨",
            "知识就是力量！💡",
            "每天进步一点点！🌱",
            "加油，你可以的！🚀",
            "聪明的选择！🎯",
            "继续努力！🔥",
            "你真了不起！👏",
            "再接再厉！🏃",
            "勇往直前！⚡",
            "信心十足！💯",
            "专注学习！🎧",
            "收获满满！📖",
            "步步为营！🚶",
            "厚积薄发！📈",
            "持之以恒！⏰",
            "自强不息！🌟",
            "志在必得！🎯"
        };

        public List<string> WrongMessages { get; set; } = new List<string>
        {
            "再想想！💭",
            "加油！💪",
            "别灰心！🌈",
            "继续尝试！🔥",
            "下次会更好！🌟",
            "再接再厉！💡",
            "相信自己！💪",
            "仔细想想！🤔",
            "别放弃！🚀",
            "坚持就是胜利！✨"
        };

        public List<string> KnownAudios { get; set; } = new List<string>
        {
            "well_done",
            "great",
            "好极了",
            "棒棒哒",
            "awesome",
            "excellent",
            "perfect"
        };

        public List<string> UnknownAudios { get; set; } = new List<string>
        {
            "加油",
            "可惜啦",
            "keep_trying",
            "dont_give_up",
            "come_on",
            "try_again"
        };

        public bool UseTTSAsFallback { get; set; } = true;

        public int Volume { get; set; } = 100;
    }
}
