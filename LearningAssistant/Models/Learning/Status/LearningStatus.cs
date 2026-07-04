namespace LearningAssistant.Models.Learning.Status
{
    public enum LearningStatus
    {
        New = 0,
        Learning = 1,
        Known = 2,
        Mastered = 3
    }

    public static class LearningStatusExtensions
    {
        public static LearningStatus Promote(this LearningStatus status)
        {
            return status switch
            {
                LearningStatus.New => LearningStatus.Learning,
                LearningStatus.Learning => LearningStatus.Known,
                LearningStatus.Known => LearningStatus.Mastered,
                LearningStatus.Mastered => LearningStatus.Mastered,
                _ => status
            };
        }

        public static LearningStatus Demote(this LearningStatus status)
        {
            return status switch
            {
                LearningStatus.New => LearningStatus.New,
                LearningStatus.Learning => LearningStatus.New,
                LearningStatus.Known => LearningStatus.Learning,
                LearningStatus.Mastered => LearningStatus.Known,
                _ => status
            };
        }
    }
}