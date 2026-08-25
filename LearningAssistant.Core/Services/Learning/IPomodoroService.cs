using LearningAssistant.Models.Pomodoro;

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
        PomodoroStatistics GetStatistics();
    }
}