using LearningAssistant.Common;
using LearningAssistant.Models.Hotkeys;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace LearningAssistant.Services.Hotkeys
{
    /// <summary>
    /// 快捷键管理服务实现
    /// 支持全局快捷键和应用内快捷键管理
    /// </summary>
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly ILogger<HotkeyService>? _logger;
        private HotkeyConfig _config;
        private readonly Dictionary<string, HotkeyMapping> _hotkeyMappings;
        private readonly Dictionary<string, EventHandler<HotkeyPressedEventArgs>> _handlers;
        private readonly Dictionary<int, string> _hotkeyIdToAction;
        private int _nextHotkeyId;

        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        public bool GlobalHotkeysEnabled => _config.GlobalHotkeysEnabled;

        private static IntPtr? _windowHandle;

        public HotkeyService(ILogger<HotkeyService>? logger = null)
        {
            _logger = logger;
            _config = new HotkeyConfig();
            _hotkeyMappings = new Dictionary<string, HotkeyMapping>();
            _handlers = new Dictionary<string, EventHandler<HotkeyPressedEventArgs>>();
            _hotkeyIdToAction = new Dictionary<int, string>();
            _nextHotkeyId = 1;

            InitializeDefaultHotkeys();
            LoadConfig();
        }

        /// <summary>
        /// 设置接收全局快捷键消息的窗口句柄
        /// </summary>
        public static void SetWindowHandle(IntPtr handle)
        {
            _windowHandle = handle;
        }

        /// <inheritdoc/>
        public bool RegisterHotkey(string actionId, HotkeyMapping hotkey, EventHandler<HotkeyPressedEventArgs> handler)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(actionId))
                    return false;

                if (_hotkeyMappings.ContainsKey(actionId))
                {
                    UnregisterHotkey(actionId);
                }

                _hotkeyMappings[actionId] = hotkey;
                _handlers[actionId] = handler;

                if (hotkey.IsGlobal && _config.GlobalHotkeysEnabled && hotkey.Enabled)
                {
                    RegisterGlobalHotkey(actionId, hotkey);
                }

                _logger?.LogInformation($"快捷键已注册: {actionId} - {hotkey.DisplayText}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "注册快捷键失败: {ActionId}", actionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool UnregisterHotkey(string actionId)
        {
            try
            {
                if (!_hotkeyMappings.ContainsKey(actionId))
                    return false;

                if (_hotkeyMappings[actionId].IsGlobal)
                {
                    UnregisterGlobalHotkey(actionId);
                }

                _hotkeyMappings.Remove(actionId);
                _handlers.Remove(actionId);

                _logger?.LogInformation($"快捷键已注销: {actionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "注销快捷键失败: {ActionId}", actionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool UpdateHotkey(string actionId, HotkeyMapping newHotkey)
        {
            try
            {
                if (!_hotkeyMappings.ContainsKey(actionId))
                    return false;

                var oldHotkey = _hotkeyMappings[actionId];

                if (oldHotkey.IsGlobal)
                {
                    UnregisterGlobalHotkey(actionId);
                }

                _hotkeyMappings[actionId] = newHotkey;

                if (newHotkey.IsGlobal && _config.GlobalHotkeysEnabled && newHotkey.Enabled)
                {
                    RegisterGlobalHotkey(actionId, newHotkey);
                }

                SaveConfig();
                _logger?.LogInformation($"快捷键已更新: {actionId} - {newHotkey.DisplayText}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新快捷键失败: {ActionId}", actionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public List<HotkeyMapping> GetAllHotkeys()
        {
            return _hotkeyMappings.Values.ToList();
        }

        /// <inheritdoc/>
        public List<HotkeyMapping> GetHotkeysByCategory(string category)
        {
            return _hotkeyMappings.Values
                .Where(h => h.Category == category)
                .ToList();
        }

        /// <inheritdoc/>
        public List<string> GetCategories()
        {
            return _hotkeyMappings.Values
                .Select(h => h.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToList();
        }

        /// <inheritdoc/>
        public HotkeyMapping? GetHotkey(string actionId)
        {
            _hotkeyMappings.TryGetValue(actionId, out var hotkey);
            return hotkey;
        }

        /// <inheritdoc/>
        public string? IsHotkeyUsed(HotkeyMapping hotkey, string? excludeActionId = null)
        {
            foreach (var kvp in _hotkeyMappings)
            {
                if (excludeActionId != null && kvp.Key == excludeActionId)
                    continue;

                var existing = kvp.Value;
                if (existing.Key == hotkey.Key &&
                    existing.Ctrl == hotkey.Ctrl &&
                    existing.Alt == hotkey.Alt &&
                    existing.Shift == hotkey.Shift &&
                    existing.Enabled && hotkey.Enabled)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            foreach (var actionId in _hotkeyMappings.Keys.ToList())
            {
                UnregisterHotkey(actionId);
            }

            _hotkeyMappings.Clear();
            _handlers.Clear();
            InitializeDefaultHotkeys();
            SaveConfig();

            _logger?.LogInformation("快捷键已重置为默认");
        }

        /// <inheritdoc/>
        public void SaveConfig()
        {
            try
            {
                _config.Hotkeys = _hotkeyMappings.Values.ToList();
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var configPath = Path.Combine(AppPaths.ConfigDir, "HotkeySettings.json");
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存快捷键配置失败");
            }
        }

        /// <inheritdoc/>
        public void LoadConfig()
        {
            try
            {
                var configPath = Path.Combine(AppPaths.ConfigDir, "HotkeySettings.json");
                if (!File.Exists(configPath))
                    return;

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<HotkeyConfig>(json);
                if (config == null)
                    return;

                _config = config;

                foreach (var hotkey in config.Hotkeys)
                {
                    if (_hotkeyMappings.ContainsKey(hotkey.ActionId))
                    {
                        var oldHotkey = _hotkeyMappings[hotkey.ActionId];
                        hotkey.DefaultMapping = oldHotkey.DefaultMapping;

                        if (hotkey.IsGlobal && _config.GlobalHotkeysEnabled && hotkey.Enabled)
                        {
                            RegisterGlobalHotkey(hotkey.ActionId, hotkey);
                        }
                        _hotkeyMappings[hotkey.ActionId] = hotkey;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载快捷键配置失败");
            }
        }

        /// <inheritdoc/>
        public bool ExportConfig(string filePath)
        {
            try
            {
                _config.Hotkeys = _hotkeyMappings.Values.ToList();
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出快捷键配置失败");
                return false;
            }
        }

        /// <inheritdoc/>
        public bool ImportConfig(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<HotkeyConfig>(json);
                if (config == null)
                    return false;

                foreach (var hotkey in config.Hotkeys)
                {
                    if (_hotkeyMappings.ContainsKey(hotkey.ActionId))
                    {
                        UpdateHotkey(hotkey.ActionId, hotkey);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入快捷键配置失败");
                return false;
            }
        }

        /// <inheritdoc/>
        public void SetGlobalHotkeysEnabled(bool enabled)
        {
            _config.GlobalHotkeysEnabled = enabled;

            if (enabled)
            {
                foreach (var kvp in _hotkeyMappings)
                {
                    if (kvp.Value.IsGlobal && kvp.Value.Enabled)
                    {
                        RegisterGlobalHotkey(kvp.Key, kvp.Value);
                    }
                }
            }
            else
            {
                foreach (var kvp in _hotkeyMappings)
                {
                    if (kvp.Value.IsGlobal)
                    {
                        UnregisterGlobalHotkey(kvp.Key);
                    }
                }
            }

            SaveConfig();
        }

        #region 私有方法

        private void InitializeDefaultHotkeys()
        {
            var defaultHotkeys = new List<(string Id, string Name, string Category, Keys Key, bool Ctrl, bool Alt, bool Shift, bool Global, string Desc)>
            {
                ("open_ai_panel", "打开AI面板", "AI", Keys.Space, true, true, false, true, "快速打开AI对话面板"),
                ("open_learning", "打开学习中心", "学习", Keys.L, true, false, false, false, "打开学习中心界面"),
                ("next_item", "下一个学习项", "学习", Keys.Right, false, false, false, false, "切换到下一个学习内容"),
                ("prev_item", "上一个学习项", "学习", Keys.Left, false, false, false, false, "切换到上一个学习内容"),
                ("toggle_favorite", "收藏/取消收藏", "学习", Keys.D, true, false, false, false, "将当前内容加入收藏"),
                ("start_pause", "开始/暂停学习", "学习", Keys.P, true, false, false, false, "开始或暂停当前学习"),
                ("show_settings", "打开设置", "系统", Keys.Oemcomma, true, false, false, false, "打开应用设置"),
                ("search_content", "搜索内容", "系统", Keys.F, true, false, false, false, "打开搜索框"),
                ("new_note", "新建笔记", "笔记", Keys.N, true, false, false, false, "快速新建笔记"),
                ("screenshot", "截图", "工具", Keys.A, true, false, true, true, "启动截图工具"),
                ("show_main_window", "显示主窗口", "系统", Keys.M, true, true, false, true, "显示或隐藏主窗口"),
                ("quick_word", "快速查词", "工具", Keys.W, true, true, false, true, "快速查询单词"),
                ("pomodoro_start_pause", "番茄钟开始/暂停", "番茄钟", Keys.S, true, true, false, true, "开始或暂停番茄钟"),
                ("pomodoro_reset", "番茄钟重置", "番茄钟", Keys.R, true, true, false, true, "重置番茄钟"),
                ("pomodoro_skip", "番茄钟跳过", "番茄钟", Keys.K, true, true, false, true, "跳过当前阶段")
            };

            foreach (var (id, name, category, key, ctrl, alt, shift, global, desc) in defaultHotkeys)
            {
                var hotkey = new HotkeyMapping
                {
                    ActionId = id,
                    ActionName = name,
                    Category = category,
                    Key = key,
                    Ctrl = ctrl,
                    Alt = alt,
                    Shift = shift,
                    IsGlobal = global,
                    Description = desc,
                    Enabled = true
                };
                hotkey.DefaultMapping = new HotkeyMapping
                {
                    ActionId = id,
                    ActionName = name,
                    Category = category,
                    Key = key,
                    Ctrl = ctrl,
                    Alt = alt,
                    Shift = shift,
                    IsGlobal = global,
                    Description = desc,
                    Enabled = true
                };

                _hotkeyMappings[id] = hotkey;
            }
        }

        private void RegisterGlobalHotkey(string actionId, HotkeyMapping hotkey)
        {
            if (_windowHandle == null || _windowHandle.Value == IntPtr.Zero)
                return;

            int id = _nextHotkeyId++;
            uint modifiers = 0;
            if (hotkey.Ctrl) modifiers |= MOD_CONTROL;
            if (hotkey.Alt) modifiers |= MOD_ALT;
            if (hotkey.Shift) modifiers |= MOD_SHIFT;

            bool success = RegisterHotKey(_windowHandle.Value, id, modifiers, (uint)hotkey.Key);
            if (success)
            {
                _hotkeyIdToAction[id] = actionId;
            }
            else
            {
                _logger?.LogWarning("注册全局快捷键失败: {ActionId} - {Hotkey}", actionId, hotkey.DisplayText);
            }
        }

        private void UnregisterGlobalHotkey(string actionId)
        {
            if (_windowHandle == null || _windowHandle.Value == IntPtr.Zero)
                return;

            var idToRemove = _hotkeyIdToAction.FirstOrDefault(kvp => kvp.Value == actionId);
            if (idToRemove.Key != 0)
            {
                UnregisterHotKey(_windowHandle.Value, idToRemove.Key);
                _hotkeyIdToAction.Remove(idToRemove.Key);
            }
        }

        /// <summary>
        /// 处理全局快捷键消息
        /// </summary>
        public void ProcessHotkeyMessage(int id)
        {
            if (_hotkeyIdToAction.TryGetValue(id, out var actionId))
            {
                if (_hotkeyMappings.TryGetValue(actionId, out var hotkey))
                {
                    HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(actionId, hotkey));

                    if (_handlers.TryGetValue(actionId, out var handler))
                    {
                        handler?.Invoke(this, new HotkeyPressedEventArgs(actionId, hotkey));
                    }
                }
            }
        }

        #region Win32 API

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion

        public void Dispose()
        {
            foreach (var actionId in _hotkeyMappings.Keys.ToList())
            {
                UnregisterHotkey(actionId);
            }
        }

        #endregion
    }
}
