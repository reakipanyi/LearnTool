
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
}
