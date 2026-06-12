namespace LearningAssistant.Models.User
{
    public class LearningProgress
    {
        public Dictionary<string, CategoryProgress> CategoryProgresses { get; set; } = new Dictionary<string, CategoryProgress>();
        public DateTime LastStudyTime { get; set; } = DateTime.MinValue;
        public int TotalStudyMinutes { get; set; } = 0;

        /// <summary>
        /// 兼容旧版本 JSON 的字段，保留 setter 但实际值通过计算属性获得
        /// </summary>
        [Obsolete("请使用 ComputedTotalItemsStudied 计算属性")]
        public int TotalItemsStudied { get; set; } = 0;

        /// <summary>
        /// 兼容旧版本 JSON 的字段，保留 setter 但实际值通过计算属性获得
        /// </summary>
        [Obsolete("请使用 ComputedTotalItemsMastered 计算属性")]
        public int TotalItemsMastered { get; set; } = 0;

        public int PerfectSessions { get; set; } = 0;

        /// <summary>
        /// 已学习项目总数 - 从所有分类进度动态计算得出
        /// 避免手动同步导致的数据不一致
        /// </summary>
        public int ComputedTotalItemsStudied =>
            CategoryProgresses?.Values.Sum(c => c.TotalTestCount) ?? 0;

        /// <summary>
        /// 已掌握项目总数 - 从所有分类进度动态计算得出
        /// </summary>
        public int ComputedTotalItemsMastered =>
            CategoryProgresses?.Values.Sum(c => c.CorrectCount) ?? 0;
    }

    public class CategoryProgress
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<string> KnownItems { get; set; } = new List<string>();
        public List<string> UnknownItems { get; set; } = new List<string>();
        public int TotalTestCount { get; set; } = 0;
        public int CorrectCount { get; set; } = 0;
        public DateTime LastTestDate { get; set; } = DateTime.MinValue;
        public int LastResumeIndex { get; set; } = 0;
        public int QuickTestResumeIndex { get; set; } = 0;
        public string LastStudyMode { get; set; } = string.Empty;

        /// <summary>
        /// 该分类的掌握率（百分比）- 计算属性
        /// </summary>
        public double AccuracyRate => TotalTestCount > 0 ? (double)CorrectCount / TotalTestCount * 100 : 0;
    }
}
