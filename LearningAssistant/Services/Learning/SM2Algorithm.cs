using System;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// SM-2 间隔重复算法实现
    /// 基于 SuperMemo 2 算法，经典且稳定的间隔学习方法
    /// </summary>
    public class SM2Algorithm : ISpacedRepetitionAlgorithm
    {
        public string Name => "SuperMemo 2";
        public string AlgorithmType => "SM-2";
        public double RecommendedRetention => 0.9;
        public double StabilityWeight => 1.0;
        public double AccuracyScore { get; set; } = 0.85;

        private const double MinEFactor = 1.3;
        private const double InitialEFactor = 2.5;

        public AlgorithmResult Calculate(ReviewItem item, int rating)
        {
            var result = new AlgorithmResult();

            if (rating < 0 || rating > 5)
            {
                result.ShouldReview = true;
                result.Message = "质量评分无效，需要重新学习";
                return result;
            }

            double newEFactor = CalculateNewEFactor(item.EFactor, rating);
            int newInterval;
            int newRepetitions;

            if (rating < 3)
            {
                newRepetitions = 0;
                newInterval = 1;
                result.ShouldReview = true;
                result.Message = "需要重新学习";
            }
            else
            {
                newInterval = CalculateInterval(item.Repetitions, item.Interval, newEFactor);
                newRepetitions = item.Repetitions + 1;
                result.ShouldReview = false;

                result.Message = rating switch
                {
                    5 => "完美！下次复习将在 {0} 天后",
                    4 => "很好！下次复习将在 {0} 天后",
                    3 => "继续加油！下次复习将在 {0} 天后",
                    _ => "已掌握，下次复习将在 {0} 天后"
                };
            }

            result.NewInterval = newInterval;
            result.NewRepetitions = newRepetitions;
            result.NewEFactor = newEFactor;
            result.NewStability = newInterval;
            result.NewDifficulty = EFactorToDifficulty(newEFactor);
            result.PredictedRetention = CalculatePredictedRetention(newInterval, newEFactor);

            return result;
        }

        public double PredictRetention(double stability, double difficulty, int days)
        {
            if (stability <= 0) return 0;

            double retrievability = Math.Exp(-days / stability);
            retrievability = Math.Max(0, Math.Min(1, retrievability));

            double difficultyModifier = 1 - (difficulty - 5) / 15;
            difficultyModifier = Math.Max(0.5, Math.Min(1.5, difficultyModifier));

            return retrievability * difficultyModifier;
        }

        public int GetOptimalInterval(double stability, double targetRetention = 0.9)
        {
            if (stability <= 0) return 1;

            int interval = (int)Math.Round(-stability * Math.Log(targetRetention));
            return Math.Max(1, Math.Min(365, interval));
        }

        private double CalculateNewEFactor(double currentEFactor, int rating)
        {
            double delta = 0.1 - (5 - rating) * (0.08 + (5 - rating) * 0.02);
            double newEFactor = currentEFactor + delta;
            return Math.Max(MinEFactor, newEFactor);
        }

        private int CalculateInterval(int repetitions, int currentInterval, double newEFactor)
        {
            if (repetitions == 0)
            {
                return 1;
            }
            else if (repetitions == 1)
            {
                return 6;
            }
            else
            {
                return (int)Math.Round(currentInterval * newEFactor);
            }
        }

        private double EFactorToDifficulty(double eFactor)
        {
            if (eFactor >= 2.5) return 3;
            if (eFactor >= 2.3) return 5;
            if (eFactor >= 2.0) return 7;
            return 9;
        }

        private double CalculatePredictedRetention(int interval, double eFactor)
        {
            if (interval <= 0) return 0.5;
            double baseRetention = 1.0 / (1 + interval / (eFactor * 10));
            return Math.Max(0.1, Math.Min(0.99, baseRetention));
        }
    }
}
