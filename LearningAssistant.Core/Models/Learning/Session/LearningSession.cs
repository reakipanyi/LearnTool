
namespace LearningAssistant.Models.User
{
    public class LearningSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
        
        public string Language { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        
        public int TotalItems { get; set; }
        public int KnownItems { get; set; }
        public int UnknownItems { get; set; }
        public double Accuracy => TotalItems > 0 ? (double)KnownItems / TotalItems : 0;
        
        public List<LearningItemRecord> ItemRecords { get; set; } = new List<LearningItemRecord>();
    }

    public class LearningItemRecord
    {
        public string ItemId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsKnown { get; set; }
        public DateTime TimeStamp { get; set; }
        public int AttemptCount { get; set; }
    }

    public class DailyGoal
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int TargetItems { get; set; }
        public int CompletedItems { get; set; }
        public bool IsCompleted => CompletedItems >= TargetItems;
    }
}
