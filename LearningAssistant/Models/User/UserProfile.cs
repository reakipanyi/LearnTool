using LearningAssistant.Services.Learning;

namespace LearningAssistant.Models.User
{
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastLoginTime { get; set; } = DateTime.Now;
        public string AvatarPath { get; set; } = string.Empty;
        public LearningProgress LearningProgress { get; set; } = new LearningProgress();
        public List<UnlockedAchievement> UnlockedAchievements { get; set; } = new List<UnlockedAchievement>();
        public int ConsecutiveStudyDays { get; set; }
        public DateTime? LastStudyDate { get; set; }
        public int TotalStudyTimeMinutes { get; set; }
        public List<string> BadgeIds { get; set; } = new List<string>();
        public int TodayStudyTimeMinutes { get; set; }
        public int TodayItemsStudied { get; set; }

        public int XP { get; set; }
        public int TotalXP { get; set; }
        public int Level { get; set; } = 1;
        public int Coins { get; set; }
        public int TotalStudyMinutes
        {
            get => TotalStudyTimeMinutes;
            set => TotalStudyTimeMinutes = value;
        }
        public int TotalItemsStudied { get; set; }
        public int TodayStudyMinutes
        {
            get => TodayStudyTimeMinutes;
            set => TodayStudyTimeMinutes = value;
        }
        public int StudyDays { get; set; }
        public int LongestStreak { get; set; }
        public DateTime LastLoginDate
        {
            get => LastLoginTime;
            set => LastLoginTime = value;
        }
        public int CurrentStreak
        {
            get => ConsecutiveStudyDays;
            set => ConsecutiveStudyDays = value;
        }

        public void UpdateStudyRecord()
        {
            var today = DateTime.Today;

            if (LastStudyDate == null)
            {
                ConsecutiveStudyDays = 1;
                LastStudyDate = today;
            }
            else
            {
                var lastDate = LastStudyDate.Value.Date;
                var daysDiff = (today - lastDate).Days;

                if (daysDiff == 0)
                {
                }
                else if (daysDiff == 1)
                {
                    ConsecutiveStudyDays++;
                    LastStudyDate = today;
                }
                else
                {
                    ConsecutiveStudyDays = 1;
                    LastStudyDate = today;
                }
            }
        }

        public void AddStudyTime(int minutes)
        {
            TotalStudyTimeMinutes += minutes;
            TodayStudyTimeMinutes += minutes;
        }

        public void IncrementTodayItems()
        {
            TodayItemsStudied++;
        }

        public void ResetDailyStats()
        {
            var today = DateTime.Today;
            if (LastStudyDate?.Date != today)
            {
                TodayStudyTimeMinutes = 0;
                TodayItemsStudied = 0;
            }
        }

        public string GetStudyStatsSummary()
        {
            var totalHours = TotalStudyTimeMinutes / 60;
            var totalMinutes = TotalStudyTimeMinutes % 60;
            return $"连续 {ConsecutiveStudyDays} 天 | 累计 {totalHours} 小时 {totalMinutes} 分钟 | 今日 {TodayStudyTimeMinutes} 分钟";
        }
    }
}