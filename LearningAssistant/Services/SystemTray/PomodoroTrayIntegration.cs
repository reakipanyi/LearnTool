using LearningAssistant.Models.Hotkeys;
using LearningAssistant.Models.Pomodoro;
using LearningAssistant.Services.Hotkeys;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace LearningAssistant.Services.SystemTray
{
    /// <summary>
    /// 番茄钟托盘集成服务
    /// 将 PomodoroService 与 TrayIconService 集成，实现托盘状态显示、通知和快捷键控制
    /// </summary>
    public class PomodoroTrayIntegration : IDisposable
    {
        private readonly IPomodoroService _pomodoroService;
        private readonly ITrayIconService _trayIconService;
        private readonly IHotkeyService _hotkeyService;
        private readonly ILogger<PomodoroTrayIntegration>? _logger;
        private readonly System.Windows.Forms.Timer _updateTimer;
        private readonly SynchronizationContext? _syncContext;

        private bool _isInitialized;
        private bool _hotkeysRegistered;
        private bool _disposed;

        // 图标缓存：避免每秒重复创建导致 GDI 句柄泄漏
        private readonly Dictionary<PomodoroState, Icon> _iconCache = new();
        private PomodoroState? _lastIconState;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // 托盘菜单项文本（用于状态更新）
        private const string MenuStartPause = "🍅 开始番茄钟";
        private const string MenuPause = "⏸️ 暂停番茄钟";
        private const string MenuReset = "🔄 重置番茄钟";
        private const string MenuSkip = "⏭️ 跳过当前阶段";

        /// <summary>
        /// 番茄钟完成事件（可通过托盘通知或外部订阅）
        /// </summary>
        public event EventHandler<PomodoroCompletedEventArgs>? PomodoroCompleted;

        public PomodoroTrayIntegration(
            IPomodoroService pomodoroService,
            ITrayIconService trayIconService,
            IHotkeyService hotkeyService,
            ILogger<PomodoroTrayIntegration>? logger = null)
        {
            _pomodoroService = pomodoroService ?? throw new ArgumentNullException(nameof(pomodoroService));
            _trayIconService = trayIconService ?? throw new ArgumentNullException(nameof(trayIconService));
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _logger = logger;

            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 每秒更新托盘提示
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // 捕获 UI 线程同步上下文：PomodoroService 的定时器在后台线程触发事件，
            // 需借此将托盘/UI 更新调度回 UI 线程，避免跨线程访问控件导致程序崩溃。
            _syncContext = WindowsFormsSynchronizationContext.Current ?? SynchronizationContext.Current;
        }

        /// <summary>
        /// 将动作调度到 UI 线程执行（PomodoroService 事件可能在后台线程触发）。
        /// 托盘控件的任何更新都必须在 UI 线程进行，并包裹异常防止崩溃。
        /// </summary>
        private void RunOnUiThread(Action action)
        {
            if (_disposed) return;

            var ctx = _syncContext;
            if (ctx != null && ctx != SynchronizationContext.Current)
            {
                ctx.Post(_ =>
                {
                    if (_disposed) return;
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "UI 线程更新托盘状态失败");
                    }
                }, null);
            }
            else
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "更新托盘状态失败");
                }
            }
        }

        /// <summary>
        /// 初始化番茄钟托盘集成
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // 添加番茄钟托盘菜单项
            AddPomodoroMenuItems();

            // 订阅番茄钟事件
            _pomodoroService.StateChanged += PomodoroService_StateChanged;
            _pomodoroService.SessionCompleted += PomodoroService_SessionCompleted;
            _pomodoroService.BreakCompleted += PomodoroService_BreakCompleted;
            _pomodoroService.Tick += PomodoroService_Tick;

            // 注册番茄钟快捷键
            RegisterPomodoroHotkeys();

            // 更新初始状态
            UpdateTrayStatus();

            // 启动更新定时器
            _updateTimer.Start();

            _isInitialized = true;
            _logger?.LogInformation("番茄钟托盘集成已初始化");
        }

        /// <summary>
        /// 添加番茄钟相关菜单项到托盘
        /// </summary>
        private void AddPomodoroMenuItems()
        {
            // 在"显示主窗口"之后插入番茄钟菜单（索引1-2）
            _trayIconService.AddSeparator(2);

            _trayIconService.AddMenuItem(MenuStartPause, StartPauseMenuItem_Click, 3);
            _trayIconService.AddMenuItem(MenuReset, ResetMenuItem_Click, 4);
            _trayIconService.AddMenuItem(MenuSkip, SkipMenuItem_Click, 5);

            _trayIconService.AddSeparator(6);
        }

        /// <summary>
        /// 注册番茄钟相关全局快捷键
        /// </summary>
        private void RegisterPomodoroHotkeys()
        {
            if (_hotkeysRegistered)
                return;

            // 注册番茄钟快捷键
            var hotkeys = new List<(string Id, string Name, Keys Key, bool Ctrl, bool Alt, bool Shift)>
            {
                ("pomodoro_start_pause", "番茄钟开始/暂停", Keys.S, true, true, false),
                ("pomodoro_reset", "番茄钟重置", Keys.R, true, true, false),
                ("pomodoro_skip", "番茄钟跳过", Keys.K, true, true, false)
            };

            foreach (var (id, name, key, ctrl, alt, shift) in hotkeys)
            {
                var hotkey = new HotkeyMapping
                {
                    ActionId = id,
                    ActionName = name,
                    Category = "番茄钟",
                    Key = key,
                    Ctrl = ctrl,
                    Alt = alt,
                    Shift = shift,
                    IsGlobal = true,
                    Enabled = true,
                    Description = name
                };

                _hotkeyService.RegisterHotkey(id, hotkey, OnPomodoroHotkeyPressed);
            }

            _hotkeysRegistered = true;
            _logger?.LogInformation("番茄钟快捷键已注册");
        }

        /// <summary>
        /// 处理番茄钟快捷键按下
        /// </summary>
        private void OnPomodoroHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
        {
            switch (e.ActionId)
            {
                case "pomodoro_start_pause":
                    ToggleStartPause();
                    break;
                case "pomodoro_reset":
                    _pomodoroService.Reset();
                    ShowNotification("番茄钟已重置", "番茄钟已重置为初始状态");
                    break;
                case "pomodoro_skip":
                    _pomodoroService.Skip();
                    ShowNotification("阶段已跳过", "当前阶段已跳过");
                    break;
            }
        }

        /// <summary>
        /// 切换开始/暂停状态
        /// </summary>
        private void ToggleStartPause()
        {
            var state = _pomodoroService.CurrentState;

            if (state == PomodoroState.Idle)
            {
                _pomodoroService.Start();
                ShowNotification("番茄钟已开始", "开始专注学习！");
            }
            else if (state == PomodoroState.Paused)
            {
                _pomodoroService.Resume();
                ShowNotification("番茄钟已恢复", "继续专注学习");
            }
            else if (state == PomodoroState.Studying || state == PomodoroState.ShortBreak || state == PomodoroState.LongBreak)
            {
                _pomodoroService.Pause();
                ShowNotification("番茄钟已暂停", "学习已暂停");
            }
        }

        #region 番茄钟事件处理

        private void PomodoroService_StateChanged(object? sender, PomodoroStateChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                UpdateTrayStatus();

                // 状态变化时的通知
                if (e.NewState == PomodoroState.Studying)
                {
                    ShowNotification("专注开始", $"开始 {FormatTime(_pomodoroService.TimeRemaining)} 的专注学习");
                }
                else if (e.NewState == PomodoroState.ShortBreak)
                {
                    ShowNotification("短休息开始", $"休息 {_pomodoroService.Settings.ShortBreakMinutes} 分钟");
                }
                else if (e.NewState == PomodoroState.LongBreak)
                {
                    ShowNotification("长休息开始", $"休息 {_pomodoroService.Settings.LongBreakMinutes} 分钟");
                }
            });
        }

        private void PomodoroService_SessionCompleted(object? sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                var completedCount = _pomodoroService.TodayCompletedPomodoros;
                ShowNotification("🎉 番茄完成！", $"已完成第 {completedCount} 个番茄钟，休息一下吧！");

                PomodoroCompleted?.Invoke(this, new PomodoroCompletedEventArgs(completedCount));
            });
        }

        private void PomodoroService_BreakCompleted(object? sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                ShowNotification("休息结束", "休息结束，准备开始新的番茄钟！");
            });
        }

        private void PomodoroService_Tick(object? sender, TimeSpan remaining)
        {
            // 每秒更新托盘提示（在定时器中处理，避免频繁调用）
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_disposed) return;
            UpdateTrayStatus();
        }

        #endregion

        #region 菜单项事件处理

        private void StartPauseMenuItem_Click(object? sender, EventArgs e)
        {
            ToggleStartPause();
        }

        private void ResetMenuItem_Click(object? sender, EventArgs e)
        {
            _pomodoroService.Reset();
            ShowNotification("番茄钟已重置", "番茄钟已重置为初始状态");
        }

        private void SkipMenuItem_Click(object? sender, EventArgs e)
        {
            _pomodoroService.Skip();
            ShowNotification("阶段已跳过", "当前阶段已跳过");
        }

        #endregion

        #region 托盘状态更新

        /// <summary>
        /// 更新托盘图标状态和提示文字
        /// </summary>
        private void UpdateTrayStatus()
        {
            try
            {
                var state = _pomodoroService.CurrentState;
                var remaining = _pomodoroService.TimeRemaining;

                // 更新托盘提示文字
                var tooltip = GetTooltipText(state, remaining);
                _trayIconService.SetTooltip(tooltip);

                // 更新菜单项状态
                UpdateMenuItems(state);

                // 尝试更新图标（根据状态显示不同颜色）
                UpdateTrayIconColor(state);
            }
            catch (Exception ex)
            {
                // 托盘更新属于次要 UI 行为，任何异常都不应导致程序崩溃
                _logger?.LogWarning(ex, "更新托盘状态失败");
            }
        }

        /// <summary>
        /// 获取托盘提示文字
        /// </summary>
        private string GetTooltipText(PomodoroState state, TimeSpan remaining)
        {
            var stateText = GetStateText(state);
            var timeText = FormatTime(remaining);

            if (state == PomodoroState.Idle)
            {
                return $"🍅 番茄钟 - {stateText}";
            }
            else
            {
                return $"🍅 {stateText} - {timeText}";
            }
        }

        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        private static string GetStateText(PomodoroState state) => state switch
        {
            PomodoroState.Idle => "空闲",
            PomodoroState.Studying => "专注中",
            PomodoroState.ShortBreak => "短休息",
            PomodoroState.LongBreak => "长休息",
            PomodoroState.Paused => "已暂停",
            _ => ""
        };

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        private static string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
        }

        /// <summary>
        /// 更新菜单项显示状态
        /// </summary>
        private void UpdateMenuItems(PomodoroState state)
        {
            if (state == PomodoroState.Idle || state == PomodoroState.Paused)
            {
                _trayIconService.UpdateMenuItem(MenuPause, MenuStartPause, true);
            }
            else
            {
                _trayIconService.UpdateMenuItem(MenuStartPause, MenuPause, true);
            }

            _trayIconService.UpdateMenuItem(MenuSkip, MenuSkip, state != PomodoroState.Idle);
        }

        /// <summary>
        /// 更新托盘图标颜色（根据番茄钟状态）
        /// </summary>
        private void UpdateTrayIconColor(PomodoroState state)
        {
            // 状态未变化时跳过重建，避免每秒重复创建图标导致 GDI 句柄泄漏
            if (_lastIconState == state && _iconCache.TryGetValue(state, out var cached) && cached != null)
            {
                return;
            }

            try
            {
                var iconColor = GetStateColor(state);
                var icon = CreateColoredIcon(iconColor);

                // 释放该状态原有的缓存图标，避免覆盖后句柄泄漏
                if (_iconCache.TryGetValue(state, out var oldIcon) && oldIcon != null && !ReferenceEquals(oldIcon, icon))
                {
                    oldIcon.Dispose();
                }

                _iconCache[state] = icon;
                _lastIconState = state;
                _trayIconService.SetIcon(icon);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "更新托盘图标颜色失败");
            }
        }

        /// <summary>
        /// 获取状态对应的颜色
        /// </summary>
        private static Color GetStateColor(PomodoroState state) => state switch
        {
            PomodoroState.Studying => Color.FromArgb(255, 87, 34),    // 红色 - 专注
            PomodoroState.ShortBreak => Color.FromArgb(76, 175, 80),   // 绿色 - 短休息
            PomodoroState.LongBreak => Color.FromArgb(33, 150, 243),   // 蓝色 - 长休息
            PomodoroState.Paused => Color.FromArgb(255, 152, 0),       // 橙色 - 暂停
            _ => Color.FromArgb(158, 158, 158)                         // 灰色 - 空闲
        };

        /// <summary>
        /// 创建带颜色的图标
        /// </summary>
        private static Icon CreateColoredIcon(Color color)
        {
            var bitmap = new Bitmap(32, 32);
            try
            {
                using var graphics = Graphics.FromImage(bitmap);

                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                // 绘制圆形番茄图标
                var radius = 14;
                var center = new Point(16, 16);
                using var brush = new SolidBrush(color);
                graphics.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);

                // 绘制顶部叶子
                using var leafBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
                var leafPath = new System.Drawing.Drawing2D.GraphicsPath();
                leafPath.AddArc(12, 2, 8, 8, 180, 180);
                graphics.FillPath(leafBrush, leafPath);

                // 添加高光效果
                using var highlightBrush = new SolidBrush(Color.FromArgb(100, Color.White));
                graphics.FillEllipse(highlightBrush, center.X - radius + 4, center.Y - radius + 4, 8, 8);

                // 使用 Icon 构造函数创建托管副本，避免 HIcon 句柄泄漏
                // （Icon.FromHandle 不接管句柄所有权，需要手动 DestroyIcon）
                var hicon = bitmap.GetHicon();
                try
                {
                    var tmpIcon = Icon.FromHandle(hicon);
                    var clonedIcon = (Icon)tmpIcon.Clone();
                    tmpIcon.Dispose();
                    return clonedIcon;
                }
                finally
                {
                    DestroyIcon(hicon);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 显示托盘通知
        /// </summary>
        private void ShowNotification(string title, string message)
        {
            if (_pomodoroService.Settings.ShowNotification)
            {
                _trayIconService.ShowNotification(title, message, 5000);
            }
        }

        #endregion

        /// <summary>
        /// 番茄钟完成事件参数
        /// </summary>
        public class PomodoroCompletedEventArgs : EventArgs
        {
            public int CompletedCount { get; }

            public PomodoroCompletedEventArgs(int completedCount)
            {
                CompletedCount = completedCount;
            }
        }

        public void Dispose()
        {
            _disposed = true;

            if (_isInitialized)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();

                _pomodoroService.StateChanged -= PomodoroService_StateChanged;
                _pomodoroService.SessionCompleted -= PomodoroService_SessionCompleted;
                _pomodoroService.BreakCompleted -= PomodoroService_BreakCompleted;
                _pomodoroService.Tick -= PomodoroService_Tick;

                // 注销快捷键
                if (_hotkeysRegistered)
                {
                    _hotkeyService.UnregisterHotkey("pomodoro_start_pause");
                    _hotkeyService.UnregisterHotkey("pomodoro_reset");
                    _hotkeyService.UnregisterHotkey("pomodoro_skip");
                }

                _isInitialized = false;
            }

            // 释放缓存的图标资源
            foreach (var icon in _iconCache.Values)
            {
                icon?.Dispose();
            }
            _iconCache.Clear();
            _lastIconState = null;
        }
    }
}