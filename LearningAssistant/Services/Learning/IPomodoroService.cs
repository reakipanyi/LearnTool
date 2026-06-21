namespace LearningAssistant.Services.Learning
{
    public interface IPomodoroService
    {
        PomodoroState CurrentState { get; }
        TimeSpan TimeRemaining { get; }
        TimeSpan TotalDuration { get; }
        int CompletedPomodoros { get; }
        int TodayCompletedPomodoros { get; }

        PomodoroSettings Settings { get; }

        event EventHandler<PomodoroStateChangedEventArgs>? StateChanged;
        event EventHandler<TimeSpan>? Tick;
        event EventHandler? SessionCompleted;
        event EventHandler? BreakCompleted;

        void Start();
        void Pause();
        void Resume();
        void Stop();
        void Skip();
        void Reset();

        void UpdateSettings(PomodoroSettings settings);

        PomodoroDailyStats GetTodayStats();
    }

    public enum PomodoroState
    {
        Idle,
        Studying,
        ShortBreak,
        LongBreak,
        Paused
    }

    public class PomodoroSettings
    {
        public int StudyMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int LongBreakInterval { get; set; } = 4;
        public bool AutoStartBreak { get; set; } = true;
        public bool AutoStartStudy { get; set; } = false;
        public bool PlaySound { get; set; } = true;
        public bool ShowNotification { get; set; } = true;
    }

    public class PomodoroStateChangedEventArgs : EventArgs
    {
        public PomodoroState OldState { get; set; }
        public PomodoroState NewState { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class PomodoroDailyStats
    {
        public int CompletedPomodoros { get; set; }
        public int TotalStudyMinutes { get; set; }
        public int TotalBreakMinutes { get; set; }
        public DateTime Date { get; set; }
    }
}
