using LearningAssistant.Common;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    public class PomodoroService : IPomodoroService, IDisposable
    {
        private readonly IDataPersistenceService _persistenceService;
        private readonly ILogger<PomodoroService> _logger;
        private readonly string _statsDir;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();

        private PomodoroState _currentState = PomodoroState.Idle;
        private PomodoroState _previousState = PomodoroState.Idle;
        private TimeSpan _timeRemaining;
        private TimeSpan _totalDuration;
        private int _completedPomodoros;
        private int _todayCompletedPomodoros;
        private DateTime _sessionStartTime;
        private PomodoroSettings _settings = new PomodoroSettings();
        private DateTime _lastStatDate;

        public PomodoroState CurrentState => _currentState;
        public TimeSpan TimeRemaining => _timeRemaining;
        public TimeSpan TotalDuration => _totalDuration;
        public int CompletedPomodoros => _completedPomodoros;
        public int TodayCompletedPomodoros => _todayCompletedPomodoros;
        public PomodoroSettings Settings => _settings;

        public event EventHandler<PomodoroStateChangedEventArgs>? StateChanged;
        public event EventHandler<TimeSpan>? Tick;
        public event EventHandler? SessionCompleted;
        public event EventHandler? BreakCompleted;

        public PomodoroService(
            IDataPersistenceService persistenceService,
            ILogger<PomodoroService> logger)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statsDir = Path.Combine(AppPaths.UsersDir, "pomodoro_stats");
            EnsureDirectoryExists();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += Timer_Elapsed;

            LoadTodayStats();
            LoadSettings();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_statsDir))
            {
                Directory.CreateDirectory(_statsDir);
            }
        }

        private string GetTodayStatsPath()
        {
            return Path.Combine(_statsDir, $"{DateTime.Today:yyyyMMdd}_pomodoro.json");
        }

        private string GetSettingsPath()
        {
            return Path.Combine(_statsDir, "pomodoro_settings.json");
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_currentState == PomodoroState.Studying || _currentState == PomodoroState.ShortBreak ||
                    _currentState == PomodoroState.LongBreak)
                    return;

                ChangeState(PomodoroState.Studying);
                _timeRemaining = TimeSpan.FromMinutes(_settings.StudyMinutes);
                _totalDuration = _timeRemaining;
                _sessionStartTime = DateTime.Now;
                _timer.Start();
            }

            _logger.LogInformation("番茄钟开始学习");
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (_currentState == PomodoroState.Studying || _currentState == PomodoroState.ShortBreak ||
                    _currentState == PomodoroState.LongBreak)
                {
                    _previousState = _currentState;
                    ChangeState(PomodoroState.Paused);
                    _timer.Stop();
                }
            }

            _logger.LogInformation("番茄钟暂停");
        }

        public void Resume()
        {
            lock (_lock)
            {
                if (_currentState == PomodoroState.Paused)
                {
                    ChangeState(_previousState);
                    _timer.Start();
                }
            }

            _logger.LogInformation("番茄钟继续");
        }

        public void Stop()
        {
            lock (_lock)
            {
                _timer.Stop();
                ChangeState(PomodoroState.Idle);
                _timeRemaining = TimeSpan.Zero;
                _totalDuration = TimeSpan.Zero;
            }

            _logger.LogInformation("番茄钟停止");
        }

        public void Skip()
        {
            lock (_lock)
            {
                if (_currentState == PomodoroState.Studying)
                {
                    _timer.Stop();
                    HandleStudySessionEnd();
                }
                else if (_currentState == PomodoroState.ShortBreak || _currentState == PomodoroState.LongBreak)
                {
                    _timer.Stop();
                    HandleBreakEnd();
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _timer.Stop();
                _completedPomodoros = 0;
                ChangeState(PomodoroState.Idle);
                _timeRemaining = TimeSpan.Zero;
                _totalDuration = TimeSpan.Zero;
            }

            _logger.LogInformation("番茄钟重置");
        }

        public void UpdateSettings(PomodoroSettings settings)
        {
            _settings = new PomodoroSettings
            {
                StudyMinutes = settings.StudyMinutes,
                ShortBreakMinutes = settings.ShortBreakMinutes,
                LongBreakMinutes = settings.LongBreakMinutes,
                LongBreakInterval = settings.LongBreakInterval,
                AutoStartBreak = settings.AutoStartBreak,
                AutoStartStudy = settings.AutoStartStudy,
                PlaySound = settings.PlaySound,
                ShowNotification = settings.ShowNotification
            };

            SaveSettings();
            _logger.LogInformation("番茄钟设置已更新");
        }

        public PomodoroDailyStats GetTodayStats()
        {
            LoadTodayStats();
            return new PomodoroDailyStats
            {
                CompletedPomodoros = _todayCompletedPomodoros,
                TotalStudyMinutes = _todayCompletedPomodoros * _settings.StudyMinutes,
                TotalBreakMinutes = (int)(_todayCompletedPomodoros * _settings.ShortBreakMinutes * 0.75),
                Date = DateTime.Today
            };
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (_currentState != PomodoroState.Studying &&
                    _currentState != PomodoroState.ShortBreak &&
                    _currentState != PomodoroState.LongBreak)
                    return;

                _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
                Tick?.Invoke(this, _timeRemaining);

                if (_timeRemaining.TotalSeconds <= 0)
                {
                    _timer.Stop();

                    if (_currentState == PomodoroState.Studying)
                    {
                        HandleStudySessionEnd();
                    }
                    else
                    {
                        HandleBreakEnd();
                    }
                }
            }
        }

        private void HandleStudySessionEnd()
        {
            _completedPomodoros++;
            _todayCompletedPomodoros++;
            SaveTodayStats();

            SessionCompleted?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("番茄钟学习完成，累计 {Count} 个", _completedPomodoros);

            bool isLongBreak = _completedPomodoros % _settings.LongBreakInterval == 0;
            var nextState = isLongBreak ? PomodoroState.LongBreak : PomodoroState.ShortBreak;
            var breakDuration = isLongBreak
                ? TimeSpan.FromMinutes(_settings.LongBreakMinutes)
                : TimeSpan.FromMinutes(_settings.ShortBreakMinutes);

            if (_settings.AutoStartBreak)
            {
                ChangeState(nextState);
                _timeRemaining = breakDuration;
                _totalDuration = breakDuration;
                _timer.Start();
            }
            else
            {
                ChangeState(PomodoroState.Idle);
                _timeRemaining = breakDuration;
            }
        }

        private void HandleBreakEnd()
        {
            BreakCompleted?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("番茄钟休息结束");

            if (_settings.AutoStartStudy)
            {
                ChangeState(PomodoroState.Studying);
                _timeRemaining = TimeSpan.FromMinutes(_settings.StudyMinutes);
                _totalDuration = _timeRemaining;
                _sessionStartTime = DateTime.Now;
                _timer.Start();
            }
            else
            {
                ChangeState(PomodoroState.Idle);
                _timeRemaining = TimeSpan.Zero;
            }
        }

        private void ChangeState(PomodoroState newState)
        {
            var oldState = _currentState;
            _currentState = newState;

            StateChanged?.Invoke(this, new PomodoroStateChangedEventArgs
            {
                OldState = oldState,
                NewState = newState,
                Duration = _timeRemaining
            });
        }

        private void LoadTodayStats()
        {
            try
            {
                if (_lastStatDate == DateTime.Today)
                    return;

                var path = GetTodayStatsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var stats = JsonSerializer.Deserialize<PomodoroDailyStats>(json);
                    if (stats != null)
                    {
                        _todayCompletedPomodoros = stats.CompletedPomodoros;
                    }
                }
                else
                {
                    _todayCompletedPomodoros = 0;
                }

                _lastStatDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载番茄钟统计失败");
                _todayCompletedPomodoros = 0;
            }
        }

        private void SaveTodayStats()
        {
            try
            {
                var stats = new PomodoroDailyStats
                {
                    CompletedPomodoros = _todayCompletedPomodoros,
                    TotalStudyMinutes = _todayCompletedPomodoros * _settings.StudyMinutes,
                    Date = DateTime.Today
                };

                var path = GetTodayStatsPath();
                var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存番茄钟统计失败");
            }
        }

        private void LoadSettings()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<PomodoroSettings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载番茄钟设置失败，使用默认值");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var path = GetSettingsPath();
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存番茄钟设置失败");
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
