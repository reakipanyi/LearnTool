using System;
using System.Collections.Generic;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// FSRS (Free Spaced Repetition Scheduler) 算法实现
    /// 基于机器学习的现代间隔重复算法，能够更准确地预测记忆状态
    /// 参考: https://github.com/open-spaced-repetition/fsrs4anki
    /// </summary>
    public class FSRSAlgorithm : ISpacedRepetitionAlgorithm
    {
        public string Name => "Free Spaced Repetition Scheduler";
        public string AlgorithmType => "FSRS";
        public double RecommendedRetention => 0.9;
        public double StabilityWeight => 1.5;
        public double AccuracyScore { get; set; } = 0.9;

        private const double InitialStability = 4.0;
        private const double InitialDifficulty = 5.0;
        private const double MinStability = 0.5;
        private const double MinDifficulty = 1.0;
        private const double MaxDifficulty = 10.0;
        private const double MeanDifficulty = 5.0;

        private static readonly double[] DifficultyDecay = { -0.0146, -0.0044, 0.0034, 0.0099, 0.0151, 0.0211, 0.0278, 0.0324, 0.0389, 0.0425 };
        private static readonly double[] StabilityDecay = { 0.0241, 0.0543, 0.0763, 0.1051, 0.1237, 0.1404, 0.1679, 0.1836, 0.1995, 0.2129 };

        public AlgorithmResult Calculate(ReviewItem item, int rating)
        {
            var result = new AlgorithmResult();

            if (rating < 1 || rating > 4)
            {
                result.ShouldReview = true;
                result.Message = "FSRS 评分范围应为 1-4";
                return result;
            }

            double currentStability = item.Stability > 0 ? item.Stability : InitialStability;
            double currentDifficulty = item.Difficulty > 0 ? item.Difficulty : InitialDifficulty;
            int repetitions = item.Repetitions;
            DateTime lastReview = item.LastReviewDate ?? DateTime.Now;

            double deltaT = (DateTime.Now - lastReview).TotalDays;

            double currentRetrievability = PredictRetrievability(currentStability, deltaT);

            double newStability;
            double newDifficulty;

            if (rating == 1)
            {
                newStability = currentStability * 0.32;
                newDifficulty = Math.Min(MaxDifficulty, currentDifficulty + 0.94);
                result.ShouldReview = true;
                result.Message = "忘记了，需要重新学习";
            }
            else
            {
                double hardPenalty = rating == 2 ? 0.80 : 1.0;
                double easyBonus = rating == 4 ? 1.3 : 1.0;

                newStability = UpdateStability(currentStability, currentRetrievability, rating, hardPenalty);
                newDifficulty = UpdateDifficulty(currentDifficulty, rating);
                newStability *= easyBonus;

                result.ShouldReview = false;
                result.Message = rating switch
                {
                    4 => "太轻松了！",
                    3 => "掌握良好",
                    2 => "有些困难，需要巩固",
                    _ => ""
                };
            }

            newStability = Math.Max(MinStability, newStability);
            newDifficulty = Math.Max(MinDifficulty, Math.Min(MaxDifficulty, newDifficulty));

            double newInterval = CalculateInterval(newStability, newDifficulty, 0.9);

            result.NewInterval = Math.Max(1, (int)Math.Round(newInterval));
            result.NewRepetitions = repetitions + 1;
            result.NewEFactor = DifficultyToEFactor(newDifficulty);
            result.NewStability = newStability;
            result.NewDifficulty = newDifficulty;
            result.PredictedRetention = PredictRetrievability(newStability, newInterval);
            result.Message = string.Format(result.Message + " 下次复习约 {0} 天后", result.NewInterval);

            return result;
        }

        public double PredictRetention(double stability, double difficulty, int days)
        {
            if (stability <= 0) return 0;

            double retrievability = PredictRetrievability(stability, days);

            double difficultyModifier = 1 - (difficulty - MeanDifficulty) / (MaxDifficulty * 2);
            difficultyModifier = Math.Max(0.5, Math.Min(1.5, difficultyModifier));

            return Math.Max(0, Math.Min(1, retrievability * difficultyModifier));
        }

        public int GetOptimalInterval(double stability, double targetRetention = 0.9)
        {
            if (stability <= 0) return 1;

            int interval = (int)Math.Round(-stability * Math.Log(targetRetention));
            return Math.Max(1, Math.Min(365, interval));
        }

        private double PredictRetrievability(double stability, double days)
        {
            if (stability <= 0) return 0;
            double retrievability = Math.Exp(-days / stability);
            return Math.Max(0, Math.Min(1, retrievability));
        }

        private double UpdateStability(double currentStability, double currentRetrievability, int rating, double hardPenalty)
        {
            int ratingIndex = Math.Clamp(rating - 1, 0, StabilityDecay.Length - 1);
            double stabilityDelta = StabilityDecay[ratingIndex];

            double retrievabilityError = currentRetrievability - (rating / 4.0);
            double stabilityDeltaR = retrievabilityError * stabilityDelta * 0.1;

            double newStability = currentStability * (1 + stabilityDelta + stabilityDeltaR) * hardPenalty;

            return Math.Max(MinStability, newStability);
        }

        private double UpdateDifficulty(double currentDifficulty, int rating)
        {
            int ratingIndex = Math.Clamp(rating - 1, 0, DifficultyDecay.Length - 1);
            double difficultyDelta = DifficultyDecay[ratingIndex];

            double meanReversion = (MeanDifficulty - currentDifficulty) * 0.1;
            double newDifficulty = currentDifficulty + difficultyDelta + meanReversion;

            return Math.Max(MinDifficulty, Math.Min(MaxDifficulty, newDifficulty));
        }

        private double CalculateInterval(double stability, double difficulty, double targetRetention)
        {
            double retrievability = targetRetention;
            double difficultyModifier = 1 - (difficulty - MeanDifficulty) / (MaxDifficulty * 2);
            difficultyModifier = Math.Max(0.5, Math.Min(1.5, difficultyModifier));

            retrievability = Math.Max(0.1, Math.Min(0.99, retrievability / difficultyModifier));

            double interval = -stability * Math.Log(retrievability);

            double maxInterval = 365.0;
            interval = Math.Min(interval, maxInterval);

            return interval;
        }

        private double DifficultyToEFactor(double difficulty)
        {
            if (difficulty >= 8) return 1.3;
            if (difficulty >= 6) return 1.7;
            if (difficulty >= 4) return 2.1;
            return 2.5;
        }
    }
}
