namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 学习项基类
    /// 所有学习内容（单词、短语、句子等）的抽象基类
    /// </summary>
    public abstract class LearningItem
    {
        /*
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        */
        /// <summary>
        /// 获取主要内容（如单词、短语等）
        /// </summary>
        /// <returns>学习项的主要内容</returns>
        public abstract string GetMainContent();

        /// <summary>
        /// 获取显示文本（包含详细信息）
        /// </summary>
        /// <returns>格式化后的显示文本</returns>
        public abstract string GetDisplayText();

        /// <summary>
        /// 获取发音（音标或拼音）
        /// </summary>
        /// <returns>发音信息</returns>
        public abstract string GetPronunciation();

        /// <summary>
        /// 获取显示结构（DisplayContent 中冒号左侧的标签部分，例如：词性 | 音标 | 释义）
        /// </summary>
        /// <returns>结构标签文本</returns>
        public abstract string GetDisplayStruct();
    }
}