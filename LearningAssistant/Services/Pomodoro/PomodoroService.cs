using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Pomodoro;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace LearningAssistant.Services.Pomodoro
{
    /// <summary>
    /// 番茄钟服务实现
    /// </summary>
    public class PomodoroService : IPomodoroService, IDisposable
    {
        private readonly ILogger<PomodoroService>? _logger;
        private readonly Timer _timer;
        private readonly SynchronizationContext? _syncContext;
        private readonly IEventBus? _eventBus;
        private readonly string _userId = "default";
        private PomodoroConfig _config;
        private List<PomodoroRecord> _records = new();
        private PomodoroState _currentState = PomodoroState.Idle;
        private PomodoroState _previousState = PomodoroState.Idle;
        private int _remainingSeconds;
        private int _totalSeconds;
        private int _elapsedSeconds;
        private int _completedCount;
        private DateTime _phaseStartTime;
        private string? _currentTask;
        private int _interruptionCount;
        private bool _disposed;

        public PomodoroState CurrentState => _currentState;
        public int RemainingSeconds => _remainingSeconds;
        public int ElapsedSeconds => _elapsedSeconds;
        public int TotalSeconds => _totalSeconds;
        public int CompletedCount => _completedCount;
        public PomodoroConfig Config => _config;
        public bool IsRunning => _currentState == PomodoroState.Working ||
                                  _currentState == PomodoroState.ShortBreak ||
                                  _currentState == PomodoroState.LongBreak;

        public event EventHandler<int>? Tick;
        public event EventHandler<PomodoroState>? StateChanged;
        public event EventHandler<int>? PomodoroCompleted;
        public event EventHandler<PomodoroState>? PhaseCompleted;

        private string DataFilePath => Path.Combine(AppPaths.GetUserDir(), "pomodoro_records.json");
        private string ConfigFilePath => Path.Combine(AppPaths.ConfigDir, "PomodoroSettings.json");

        public PomodoroService(ILogger<PomodoroService>? logger = null, IEventBus? eventBus = null, string userId = "default")
        {
            _logger = logger;
            _eventBus = eventBus;
            _userId = userId;
            _syncContext = SynchronizationContext.Current;
            _config = LoadConfig();
            _records = LoadRecords();

            _timer = new Timer(1000);
            _timer.Elapsed += OnTimerTick;
            _timer.AutoReset = true;

            UpdateDailyCount();
        }

        #region 控制方法

        public void StartWork(string? task = null)
        {
            _currentTask = task;
            _interruptionCount = 0;
            StartPhase(PomodoroState.Working, _config.WorkDuration * 60);
            _logger?.LogInformation("番茄钟开始工作: {Task}", task ?? "未命名任务");
        }

        public void StartShortBreak()
        {
            StartPhase(PomodoroState.ShortBreak, _config.ShortBreakDuration * 60);
            _logger?.LogInformation("番茄钟开始短休息");
        }

        public void StartLongBreak()
        {
            StartPhase(PomodoroState.LongBreak, _config.LongBreakDuration * 60);
            _logger?.LogInformation("番茄钟开始长休息");
        }

        public void Pause()
        {
            if (!IsRunning) return;

            _previousState = _currentState;
            _currentState = PomodoroState.Paused;
            _timer.Stop();
            RaiseStateChanged(_currentState);
            _interruptionCount++;
            _logger?.LogDebug("番茄钟暂停");
        }

        public void Resume()
        {
            if (_currentState != PomodoroState.Paused) return;

            _currentState = _previousState;
            _timer.Start();
            RaiseStateChanged(_currentState);
            _logger?.LogDebug("番茄钟继续");
        }

        public void Stop()
        {
            _timer.Stop();

            if (IsRunning || _currentState == PomodoroState.Paused)
            {
                SaveCurrentRecord(false);
            }

            _currentState = PomodoroState.Idle;
            _remainingSeconds = 0;
            _elapsedSeconds = 0;
            _totalSeconds = 0;
            _currentTask = null;
            _interruptionCount = 0;

            RaiseStateChanged(_currentState);
            _logger?.LogInformation("番茄钟停止");
        }

        public void Skip()
        {
            if (!IsRunning) return;

            SaveCurrentRecord(false);
            _timer.Stop();
            OnPhaseCompleted();
        }

        public void ResetDailyCount()
        {
            var today = DateTime.Today;
            _records.RemoveAll(r => r.StartTime.Date == today && r.Type == PomodoroState.Working && r.Completed);
            _completedCount = 0;
            SaveChanges();
        }

        public void UpdateConfig(Action<PomodoroConfig> updateAction)
        {
            updateAction(_config);
            SaveConfig();
        }

        #endregion

        #region 统计和记录

        public PomodoroStatistics GetStatistics()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayRecords = _records.Where(r => r.StartTime.Date == today && r.Type == PomodoroState.Working && r.Completed).ToList();
            var weekRecords = _records.Where(r => r.StartTime >= weekStart && r.Type == PomodoroState.Working && r.Completed).ToList();
            var monthRecords = _records.Where(r => r.StartTime >= monthStart && r.Type == PomodoroState.Working && r.Completed).ToList();
            var allRecords = _records.Where(r => r.Type == PomodoroState.Working && r.Completed).ToList();

            var dailyData = new List<DailyPomodoroData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayRecords = _records.Where(r => r.StartTime.Date == date && r.Type == PomodoroState.Working && r.Completed).ToList();
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
                TodayCompletionRate = _config.DailyTarget > 0 ? (double)todayRecords.Count / _config.DailyTarget : 0
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
                Type = PomodoroState.Working,
                DurationSeconds = minutes * 60,
                PlannedDurationSeconds = _config.WorkDuration * 60,
                Completed = true,
                Task = task,
                InterruptionCount = 0
            };

            _records.Add(record);
            UpdateDailyCount();
            SaveChanges();
        }

        public bool DeleteRecord(string recordId)
        {
            var record = _records.FirstOrDefault(r => r.Id == recordId);
            if (record == null) return false;

            _records.Remove(record);
            UpdateDailyCount();
            SaveChanges();
            return true;
        }

        public void SaveChanges()
        {
            SaveRecords();
        }

        #endregion

        #region 私有方法

        private void StartPhase(PomodoroState state, int seconds)
        {
            _currentState = state;
            _totalSeconds = seconds;
            _remainingSeconds = seconds;
            _elapsedSeconds = 0;
            _phaseStartTime = DateTime.Now;

            _timer.Start();
            RaiseStateChanged(_currentState);
        }

        private void OnTimerTick(object? sender, ElapsedEventArgs e)
        {
            _remainingSeconds--;
            _elapsedSeconds++;

            RaiseEvent(Tick, _remainingSeconds);

            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                OnPhaseCompleted();
            }
        }

        private void RaiseEvent<T>(EventHandler<T>? handler, T args)
        {
            if (handler == null) return;

            if (_syncContext != null)
            {
                _syncContext.Post(_ => handler(this, args), null);
            }
            else
            {
                handler(this, args);
            }
        }

        private void RaiseStateChanged(PomodoroState state)
        {
            if (_syncContext != null)
            {
                _syncContext.Post(_ => StateChanged?.Invoke(this, state), null);
            }
            else
            {
                StateChanged?.Invoke(this, state);
            }
        }

        private void OnPhaseCompleted()
        {
            var completedState = _currentState;
            SaveCurrentRecord(true);

            RaiseEvent(PhaseCompleted, completedState);

            if (completedState == PomodoroState.Working)
            {
                _completedCount++;
                RaiseEvent(PomodoroCompleted, _completedCount);
                _logger?.LogInformation("番茄钟完成: 第 {Count} 个", _completedCount);

                // 发布番茄钟完成事件
                if (_eventBus != null)
                {
                    _ = Task.Run(async () =>
                    {
                        await _eventBus.PublishAsync(new PomodoroCompletedEvent
                        {
                            UserId = _userId,
                            DurationMinutes = _config.WorkDuration,
                            TaskName = _currentTask,
                            CompletedCount = _completedCount,
                            CompletedAt = DateTime.Now
                        });
                    });
                }

                if (_config.AutoStartNext)
                {
                    if (_completedCount % _config.LongBreakInterval == 0)
                    {
                        StartLongBreak();
                    }
                    else
                    {
                        StartShortBreak();
                    }
                }
                else
                {
                    _currentState = PomodoroState.Idle;
                    RaiseStateChanged(_currentState);
                }
            }
            else
            {
                _logger?.LogInformation("番茄钟休息结束");

                if (_config.AutoStartNext)
                {
                    StartWork(_currentTask);
                }
                else
                {
                    _currentState = PomodoroState.Idle;
                    RaiseStateChanged(_currentState);
                }
            }
        }

        private void SaveCurrentRecord(bool completed)
        {
            var record = new PomodoroRecord
            {
                StartTime = _phaseStartTime,
                EndTime = DateTime.Now,
                Type = _currentState == PomodoroState.Paused ? _previousState : _currentState,
                DurationSeconds = _elapsedSeconds,
                PlannedDurationSeconds = _totalSeconds,
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
                    r.Type == PomodoroState.Working &&
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
            _completedCount = _records.Count(r =>
                r.StartTime.Date == today &&
                r.Type == PomodoroState.Working &&
                r.Completed);
        }

        private PomodoroConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<PomodoroConfig>(json);
                    if (config != null)
                        return config;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载番茄钟配置失败，使用默认配置");
            }

            return new PomodoroConfig();
        }

        private void SaveConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(_config, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存番茄钟配置失败");
            }
        }

        private List<PomodoroRecord> LoadRecords()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    var json = File.ReadAllText(DataFilePath);
                    return JsonSerializer.Deserialize<List<PomodoroRecord>>(json) ?? new List<PomodoroRecord>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载番茄钟记录失败");
            }

            return new List<PomodoroRecord>();
        }

        private void SaveRecords()
        {
            try
            {
                var directory = Path.GetDirectoryName(DataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(DataFilePath, JsonSerializer.Serialize(_records, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存番茄钟记录失败");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
        }

        #endregion
    }
}
