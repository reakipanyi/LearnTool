using System.Text.Json.Serialization;

namespace LearningAssistant.Models.Hotkeys
{
    /// <summary>
    /// 快捷键配置
    /// </summary>
    public class HotkeyConfig
    {
        /// <summary>
        /// 全局快捷键列表
        /// </summary>
        public List<HotkeyMapping> Hotkeys { get; set; } = new();

        /// <summary>
        /// 是否启用全局快捷键
        /// </summary>
        public bool GlobalHotkeysEnabled { get; set; } = true;
    }

    /// <summary>
    /// 快捷键映射
    /// </summary>
    public class HotkeyMapping
    {
        /// <summary>
        /// 动作标识（唯一ID）
        /// </summary>
        public string ActionId { get; set; } = string.Empty;

        /// <summary>
        /// 动作名称（显示用）
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 快捷键键值
        /// </summary>
        public Keys Key { get; set; }

        /// <summary>
        /// 是否需要 Ctrl 修饰键
        /// </summary>
        public bool Ctrl { get; set; }

        /// <summary>
        /// 是否需要 Alt 修饰键
        /// </summary>
        public bool Alt { get; set; }

        /// <summary>
        /// 是否需要 Shift 修饰键
        /// </summary>
        public bool Shift { get; set; }

        /// <summary>
        /// 是否为全局快捷键
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 默认快捷键配置（用于重置）
        /// </summary>
        [JsonIgnore]
        public HotkeyMapping? DefaultMapping { get; set; }

        /// <summary>
        /// 显示的快捷键字符串
        /// </summary>
        [JsonIgnore]
        public string DisplayText
        {
            get
            {
                var parts = new List<string>();
                if (Ctrl) parts.Add("Ctrl");
                if (Alt) parts.Add("Alt");
                if (Shift) parts.Add("Shift");
                if (Key != Keys.None) parts.Add(Key.ToString());
                return string.Join(" + ", parts);
            }
        }

        /// <summary>
        /// 修饰键
        /// </summary>
        [JsonIgnore]
        public Keys Modifiers
        {
            get
            {
                var mods = Keys.None;
                if (Ctrl) mods |= Keys.Control;
                if (Alt) mods |= Keys.Alt;
                if (Shift) mods |= Keys.Shift;
                return mods;
            }
        }
    }

    /// <summary>
    /// 快捷键按下事件参数
    /// </summary>
    public class HotkeyPressedEventArgs : EventArgs
    {
        public string ActionId { get; }
        public HotkeyMapping Hotkey { get; }

        public HotkeyPressedEventArgs(string actionId, HotkeyMapping hotkey)
        {
            ActionId = actionId;
            Hotkey = hotkey;
        }
    }
}
