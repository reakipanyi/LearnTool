using LearningAssistant.Models;
using LearningAssistant.Models.User;

namespace LearningAssistant.Services.Gamification
{
    public interface IGamificationService
    {
        #region Stats
        int TodayLearnedCount { get; }
        int StreakDays { get; }
        int TotalScore { get; }
        int TotalLearnedCount { get; }
        int XP { get; }
        int CurrentLevel { get; }
        int XPToNextLevel { get; }
        string LevelTitle { get; }
        TimeSpan StudyDuration { get; }

        void AddScore(int points);
        void AddXP(int xp);
        void IncrementTodayLearned();
        void UpdateStudyDuration(TimeSpan duration);
        void CheckStreak();
        void RecordQuizCorrect();
        void RecordFavorite();
        void RecordNote();
        #endregion

        #region Badges
        IEnumerable<Badge> GetAllBadges();
        int UnlockedBadgeCount { get; }
        void CheckBadgeUnlock(string type, int value);
        Dictionary<string, int> GetBadgeProgress();
        #endregion

        #region Challenges
        IEnumerable<Challenge> GetDailyChallenges();
        int CompletedChallengeCount { get; }
        void UpdateChallengeProgress(string type, int value);
        bool ClaimChallengeReward(string challengeId);
        #endregion

        #region Events
        event EventHandler<ScoreChangedEventArgs>? ScoreChanged;
        event EventHandler<XPChangedEventArgs>? XPChanged;
        event EventHandler<LevelUpEventArgs>? LevelUp;
        event EventHandler<BadgesUnlockedEventArgs>? BadgesUnlocked;
        event EventHandler<ChallengeCompletedEventArgs>? ChallengeCompleted;
        #endregion

        #region Persistence
        void Load(string userId);
        void Save();
        #endregion

        #region UI Integration (Backward Compatibility)
        [Obsolete("Use events instead of SetUI pattern for better separation of concerns")]
        void SetStatsUI(Label studyTime, Label score, Label todayCount, Label streak,
            Label? level, Label? xp, ProgressBar? progressXp);

        [Obsolete("Use events instead of SetUI pattern for better separation of concerns")]
        void SetBadgeUI(FlowLayoutPanel panel, ToolTip toolTip);

        [Obsolete("Use events instead of SetUI pattern for better separation of concerns")]
        void SetChallengeUI(FlowLayoutPanel panel, object? soundService = null);

        void UpdateAllDisplays();
        #endregion
    }

    public class ScoreChangedEventArgs : EventArgs
    {
        public int NewScore { get; set; }
        public int Added { get; set; }
    }

    public class XPChangedEventArgs : EventArgs
    {
        public int NewXP { get; set; }
        public int Added { get; set; }
    }

    public class LevelUpEventArgs : EventArgs
    {
        public int NewLevel { get; set; }
        public string LevelTitle { get; set; } = string.Empty;
    }

    public class BadgesUnlockedEventArgs : EventArgs
    {
        public List<string> BadgeIds { get; set; } = new();
    }

    public class ChallengeCompletedEventArgs : EventArgs
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string ChallengeName { get; set; } = string.Empty;
        public int RewardXP { get; set; }
        public int RewardScore { get; set; }
    }
}
