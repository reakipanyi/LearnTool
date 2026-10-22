using LearningAssistant.Common;

namespace LearningAssistant.Models.Practice
{
    /// <summary>练习类型（P2）：单词跟读与听写先行，其余类型预留。</summary>
    public enum PracticeType
    {
        Shadowing = 0,
        Dictation = 1,
        VoiceRecall = 2,
        Choice = 3,
        Reading = 4
    }

    /// <summary>
    /// 一次练习的结果记录。跟读按“参考分”保存，不默认写入错题本；
    /// 听写未通过时才由练习服务发布 ItemWrongEvent。
    /// </summary>
    public class PracticeRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = string.Empty;
        public string? LearningItemId { get; set; }
        public string ItemContent { get; set; } = string.Empty;
        public PracticeType Type { get; set; }
        public StudyContentSource SourceType { get; set; } = StudyContentSource.Manual;
        public double Score { get; set; }
        public double MaxScore { get; set; } = 100;
        public int DurationMs { get; set; }
        public string? RecordingPath { get; set; }
        public string MetaJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool Passed => Score >= MaxScore * 0.8;
    }

    /// <summary>练习汇总，供统计面板下钻。</summary>
    public class PracticeSummary
    {
        public int TotalCount { get; set; }
        public int ShadowingCount { get; set; }
        public int DictationCount { get; set; }
        public double AverageScore { get; set; }
        public DateTime? LastPracticedAt { get; set; }
    }
}
