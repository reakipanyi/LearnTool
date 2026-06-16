namespace LearningAssistant.Forms
{
    /// <summary>
    /// 学习统计数据（用于持久化）
    /// </summary>
    public class StudyStats
    {
        public int TodayLearnedCount { get; set; }
        public int StreakDays { get; set; }
        public int TotalScore { get; set; }
        public DateTime LastStudyDate { get; set; }
        public int TotalLearnedCount { get; set; }
        public int QuizCorrectCount { get; set; }
        public int FavoriteCount { get; set; }
        public int NoteCount { get; set; }
        public int XP { get; set; }
        public int CurrentLevel { get; set; }
    }
}
