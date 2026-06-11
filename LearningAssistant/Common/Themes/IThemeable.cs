
namespace LearningAssistant.Common.Themes
{
    /// <summary>
    /// 主题可应用接口 - 实现此接口的组件可以响应主题变化
    /// </summary>
    public interface IThemeable
    {
        /// <summary>
        /// 应用主题颜色到组件
        /// </summary>
        /// <param name="colors">主题颜色配置</param>
        void ApplyTheme(ThemeColors colors);
    }

    /// <summary>
    /// 主题服务接口 - 管理应用的主题切换和注册组件
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 当前主题模式（Light/Dark）
        /// </summary>
        ThemeMode CurrentTheme { get; }

        /// <summary>
        /// 当前主题颜色配置
        /// </summary>
        ThemeColors CurrentColors { get; }

        /// <summary>
        /// 切换主题
        /// </summary>
        /// <param name="theme">目标主题模式</param>
        void SetTheme(ThemeMode theme);

        /// <summary>
        /// 注册主题组件（当主题变化时自动应用新主题）
        /// </summary>
        /// <param name="themeable">实现了IThemeable接口的组件</param>
        void RegisterThemeable(IThemeable themeable);

        /// <summary>
        /// 注销主题组件
        /// </summary>
        /// <param name="themeable">之前注册的主题组件</param>
        void UnregisterThemeable(IThemeable themeable);
    }
}
