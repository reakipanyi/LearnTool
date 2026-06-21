using LearningAssistant.Models.Hotkeys;

namespace LearningAssistant.Services.Hotkeys
{
    /// <summary>
    /// 快捷键管理服务接口
    /// 支持全局快捷键注册、自定义配置、导入导出等功能
    /// </summary>
    public interface IHotkeyService
    {
        /// <summary>
        /// 快捷键按下事件
        /// </summary>
        event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        /// <summary>
        /// 注册快捷键
        /// </summary>
        /// <param name="actionId">动作标识</param>
        /// <param name="hotkey">快捷键配置</param>
        /// <param name="handler">处理函数</param>
        /// <returns>是否成功</returns>
        bool RegisterHotkey(string actionId, HotkeyMapping hotkey, EventHandler<HotkeyPressedEventArgs> handler);

        /// <summary>
        /// 注销快捷键
        /// </summary>
        /// <param name="actionId">动作标识</param>
        /// <returns>是否成功</returns>
        bool UnregisterHotkey(string actionId);

        /// <summary>
        /// 更新快捷键配置
        /// </summary>
        /// <param name="actionId">动作标识</param>
        /// <param name="newHotkey">新的快捷键配置</param>
        /// <returns>是否成功</returns>
        bool UpdateHotkey(string actionId, HotkeyMapping newHotkey);

        /// <summary>
        /// 获取所有快捷键配置
        /// </summary>
        List<HotkeyMapping> GetAllHotkeys();

        /// <summary>
        /// 获取指定分类的快捷键
        /// </summary>
        /// <param name="category">分类名称</param>
        List<HotkeyMapping> GetHotkeysByCategory(string category);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        List<string> GetCategories();

        /// <summary>
        /// 获取指定动作的快捷键
        /// </summary>
        /// <param name="actionId">动作标识</param>
        HotkeyMapping? GetHotkey(string actionId);

        /// <summary>
        /// 检查快捷键是否已被占用
        /// </summary>
        /// <param name="hotkey">要检查的快捷键</param>
        /// <param name="excludeActionId">排除的动作ID</param>
        /// <returns>已占用则返回占用的动作ID，否则返回null</returns>
        string? IsHotkeyUsed(HotkeyMapping hotkey, string? excludeActionId = null);

        /// <summary>
        /// 重置为默认快捷键
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// 保存快捷键配置
        /// </summary>
        void SaveConfig();

        /// <summary>
        /// 加载快捷键配置
        /// </summary>
        void LoadConfig();

        /// <summary>
        /// 导出快捷键配置
        /// </summary>
        /// <param name="filePath">导出文件路径</param>
        bool ExportConfig(string filePath);

        /// <summary>
        /// 导入快捷键配置
        /// </summary>
        /// <param name="filePath">导入文件路径</param>
        bool ImportConfig(string filePath);

        /// <summary>
        /// 启用/禁用全局快捷键
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void SetGlobalHotkeysEnabled(bool enabled);

        /// <summary>
        /// 全局快捷键是否启用
        /// </summary>
        bool GlobalHotkeysEnabled { get; }
    }
}
