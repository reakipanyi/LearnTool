using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Pomodoro;
using LearningAssistant.Services.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Services.Learning
{
    public class PomodoroService : IPomodoroService, IDisposable
    {
        private readonly IDataPersistenceService _persistenceService;
        private readonly ILogger<PomodoroService> _logger;
        private readonly IEventBus? _eventBus;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();
        private readonly SynchronizationContext? _syncContext;

        private PomodoroState _currentState = PomodoroState.Idle;
        private PomodoroState _previousState = PomodoroState.Idle;
        private TimeSpan _timeRemaining;
        private TimeSpan _totalDuration;
        private int _completedPomodoros;
        private int _todayCompletedPomodoros;
        private DateTime _sessionStartTime;
        private PomodoroSettings _settings = new PomodoroSettings();
        private DateTime _lastStatDate;

        private List<PomodoroRecord> _records = new();
        private string? _currentTask;
        private int _interruptionCount;
        private DateTime _phaseStartTime;

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

        public event EventHandler<int>? PomodoroCompleted;
        public event EventHandler<PomodoroState>? PhaseCompleted;

        public PomodoroService(
            IDataPersistenceService persistenceService,
            ILogger<PomodoroService> logger,
            IEventBus? eventBus = null)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus;
            _syncContext = SynchronizationContext.Current;

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += Timer_Elapsed;

            LoadRecords();
            LoadTodayStats();
            LoadSettings();
        }

        private string StatsDir => Path.Combine(AppPaths.UsersDir, "pomodoro_stats");

        private string DataFilePath => Path.Combine(StatsDir, "pomodoro_records.json");
        private string SettingsFilePath => Path.Combine(StatsDir, "pomodoro_settings.json");

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(StatsDir))
            {
                Directory.CreateDirectory(StatsDir);
            }
        }

        public void Start()
        {
            StartWork(null);
        }

        public void StartWork(string? task = null)
        {
            lock (_lock)
            {
                if (_currentState == PomodoroState.Studying || _currentState == PomodoroState.ShortBreak ||
                    _currentState == PomodoroState.LongBreak)
                    return;

                _currentTask = task;
                _interruptionCount = 0;
                _phaseStartTime = DateTime.Now;

                ChangeState(PomodoroState.Studying);
                _timeRemaining = TimeSpan.FromMinutes(_settings.StudyMinutes);
                _totalDuration = _timeRemaining;
                _sessionStartTime = DateTime.Now;
                _timer.Start();
            }

            _logger.LogInformation("番茄钟开始学习: {Task}", task ?? "未命名任务");
        }

        public void StartShortBreak()
        {
            lock (_lock)
            {
                _phaseStartTime = DateTime.Now;
                ChangeState(PomodoroState.ShortBreak);
                _timeRemaining = TimeSpan.FromMinutes(_settings.ShortBreakMinutes);
                _totalDuration = _timeRemaining;
                _timer.Start();
            }

            _logger.LogInformation("番茄钟开始短休息");
        }

        public void StartLongBreak()
        {
            lock (_lock)
            {
                _phaseStartTime = DateTime.Now;
                ChangeState(PomodoroState.LongBreak);
                _timeRemaining = TimeSpan.FromMinutes(_settings.LongBreakMinutes);
                _totalDuration = _timeRemaining;
                _timer.Start();
            }

            _logger.LogInformation("番茄钟开始长休息");
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
                    _interruptionCount++;
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
                    _phaseStartTime = DateTime.Now;
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

                if (_currentState == PomodoroState.Studying || _currentState == PomodoroState.ShortBreak ||
                    _currentState == PomodoroState.LongBreak || _currentState == PomodoroState.Paused)
                {
                    SaveCurrentRecord(false);
                }

                ChangeState(PomodoroState.Idle);
                _timeRemaining = TimeSpan.Zero;
                _totalDuration = TimeSpan.Zero;
                _currentTask = null;
                _interruptionCount = 0;
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

        public PomodoroStatistics GetStatistics()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayRecords = _records.Where(r => r.StartTime.Date == today && r.Type == Models.Pomodoro.PomodoroState.Studying && r.Completed).ToList();
            var weekRecords = _records.Where(r => r.StartTime >= weekStart && r.Type == Models.Pomodoro.PomodoroState.Studying && r.Completed).ToList();
            var monthRecords = _records.Where(r => r.StartTime >= monthStart && r.Type == Models.Pomodoro.PomodoroState.Studying && r.Completed).ToList();
            var allRecords = _records.Where(r => r.Type == Models.Pomodoro.PomodoroState.Studying && r.Completed).ToList();

            var dailyData = new List<DailyPomodoroData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayRecords = _records.Where(r => r.StartTime.Date == date && r.Type == Models.Pomodoro.PomodoroState.Studying && r.Completed).ToList();
                dailyData.Add(new DailyPomodoroData
                {
                    Date = date,
                    Count = dayRecords.Count,
                    FocusMinutes = dayRecords.Sum(r => r.DurationSeconds) / 60
                });
            }

            var streakDays = CalculateStreak();

            return new PomodoroStatistics
            {
                TodayCount = todayRecords.Count,
                TodayFocusMinutes = todayRecords.Sum(r => r.DurationSeconds) / 60,
                WeekCount = weekRecords.Count,
                WeekFocusMinutes = weekRecords.Sum(r => r.DurationSeconds) / 60,
                MonthCount = monthRecords.Count,
                MonthFocusMinutes = monthRecords.Sum(r => r.DurationSeconds) / 60,
                TotalCount = allRecords.Count,
                TotalFocusMinutes = allRecords.Sum(r => r.DurationSeconds) / 60,
                StreakDays = streakDays,
                DailyData = dailyData,
                TodayCompletionRate = 8 > 0 ? (double)todayRecords.Count / 8 : 0
            };
        }

        public List<PomodoroRecord> GetRecords(DateTime startDate, DateTime endDate)
        {
            return _records
                .Where(r => r.StartTime >= startDate && r.StartTime <= endDate)
                .OrderByDescending(r => r.StartTime)
                .ToList();
        }

        public List<PomodoroRecord> GetTodayRecords()
        {
            var today = DateTime.Today;
            return GetRecords(today, today.AddDays(1).AddSeconds(-1));
        }

        public void AddManualRecord(DateTime date, int minutes, string? task = null)
        {
            var record = new PomodoroRecord
            {
                StartTime = date,
                EndTime = date.AddMinutes(minutes),
                Type = Models.Pomodoro.PomodoroState.Studying,
                DurationSeconds = minutes * 60,
                PlannedDurationSeconds = _settings.StudyMinutes * 60,
                Completed = true,
                Task = task,
                InterruptionCount = 0
            };

            _records.Add(record);
            UpdateDailyCount();
            SaveRecords();
        }

        public bool DeleteRecord(string recordId)
        {
            var record = _records.FirstOrDefault(r => r.Id == recordId);
            if (record == null) return false;

            _records.Remove(record);
            UpdateDailyCount();
            SaveRecords();
            return true;
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
            SaveCurrentRecord(true);
            SaveTodayStats();

            SessionCompleted?.Invoke(this, EventArgs.Empty);
            PomodoroCompleted?.Invoke(this, _completedPomodoros);
            _logger.LogInformation("番茄钟学习完成，累计 {Count} 个", _completedPomodoros);

            if (_eventBus != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _eventBus.PublishAsync(new PomodoroCompletedEvent
                        {
                            UserId = "default",
                            DurationMinutes = _settings.StudyMinutes,
                            TaskName = _currentTask,
                            CompletedCount = _completedPomodoros,
                            CompletedAt = DateTime.Now
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "发布番茄钟完成事件失败");
                    }
                });
            }

            bool isLongBreak = _completedPomodoros % _settings.LongBreakInterval == 0;
            var nextState = isLongBreak ? PomodoroState.LongBreak : PomodoroState.ShortBreak;
            var breakDuration = isLongBreak
                ? TimeSpan.FromMinutes(_settings.LongBreakMinutes)
                : TimeSpan.FromMinutes(_settings.ShortBreakMinutes);

            PhaseCompleted?.Invoke(this, nextState);

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
            SaveCurrentRecord(true);
            BreakCompleted?.Invoke(this, EventArgs.Empty);
            PhaseCompleted?.Invoke(this, PomodoroState.Idle);
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

        private void SaveCurrentRecord(bool completed)
        {
            var record = new PomodoroRecord
            {
                StartTime = _phaseStartTime,
                EndTime = DateTime.Now,
                Type = _currentState == PomodoroState.Paused ? _previousState : _currentState switch
                {
                    PomodoroState.Studying => Models.Pomodoro.PomodoroState.Studying,
                    PomodoroState.ShortBreak => PomodoroState.ShortBreak,
                    PomodoroState.LongBreak => PomodoroState.LongBreak,
                    _ => PomodoroState.Idle
                },
                DurationSeconds = (int)(DateTime.Now - _phaseStartTime).TotalSeconds,
                PlannedDurationSeconds = (int)_totalDuration.TotalSeconds,
                Completed = completed,
                Task = _currentTask,
                InterruptionCount = _interruptionCount
            };

            _records.Add(record);
            SaveRecords();
        }

        private int CalculateStreak()
        {
            var streak = 0;
            var date = DateTime.Today;

            while (true)
            {
                var hasCompleted = _records.Any(r =>
                    r.StartTime.Date == date &&
                    r.Type == Models.Pomodoro.PomodoroState.Studying &&
                    r.Completed);

                if (hasCompleted)
                {
                    streak++;
                    date = date.AddDays(-1);
                }
                else
                {
                    if (streak == 0 && date == DateTime.Today)
                    {
                        date = date.AddDays(-1);
                        continue;
                    }
                    break;
                }
            }

            return streak;
        }

        private void UpdateDailyCount()
        {
            var today = DateTime.Today;
            _todayCompletedPomodoros = _records.Count(r =>
                r.StartTime.Date == today &&
                r.Type == Models.Pomodoro.PomodoroState.Studying &&
                r.Completed);
        }

        private void LoadTodayStats()
        {
            try
            {
                if (_lastStatDate == DateTime.Today)
                    return;

                _todayCompletedPomodoros = _records.Count(r =>
                    r.StartTime.Date == DateTime.Today &&
                    r.Type == Models.Pomodoro.PomodoroState.Studying &&
                    r.Completed);

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
            UpdateDailyCount();
        }

        private void LoadSettings()
        {
            try
            {
                var path = SettingsFilePath;
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
                EnsureDirectoryExists();
                var path = SettingsFilePath;
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存番茄钟设置失败");
            }
        }

        private void LoadRecords()
        {
            try
            {
                var path = DataFilePath;
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _records = JsonSerializer.Deserialize<List<PomodoroRecord>>(json) ?? new List<PomodoroRecord>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载番茄钟记录失败");
                _records = new List<PomodoroRecord>();
            }
        }

        private void SaveRecords()
        {
            try
            {
                EnsureDirectoryExists();
                var path = DataFilePath;
                var json = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存番茄钟记录失败");
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}