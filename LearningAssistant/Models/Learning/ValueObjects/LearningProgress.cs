namespace LearningAssistant.Models.Learning.ValueObjects
{
    public sealed class LearningProgress : ValueObject
    {
        public int TotalReviewCount { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate => TotalReviewCount > 0 ? (double)CorrectCount / TotalReviewCount : 0;
        public DateTime? LastReviewDate { get; set; }
        public int Streak { get; set; }

        public LearningProgress() { }

        public LearningProgress(int totalReviewCount, int correctCount, DateTime? lastReviewDate, int streak)
        {
            TotalReviewCount = totalReviewCount;
            CorrectCount = correctCount;
            LastReviewDate = lastReviewDate;
            Streak = streak;
        }

        public static LearningProgress Create()
            => new(0, 0, null, 0);

        public LearningProgress Update(bool isCorrect)
            => new(
                TotalReviewCount + 1,
                CorrectCount + (isCorrect ? 1 : 0),
                DateTime.Now,
                isCorrect ? Streak + 1 : 0
            );

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return TotalReviewCount;
            yield return CorrectCount;
            yield return LastReviewDate ?? DateTime.MinValue;
            yield return Streak;
        }
    }
}