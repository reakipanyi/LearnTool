
namespace UnifiedLearningAssistant.Common.Themes
{
    public interface IThemeable
    {
        void ApplyTheme(ThemeColors colors);
    }

    public interface IThemeService
    {
        ThemeMode CurrentTheme { get; }
        ThemeColors CurrentColors { get; }
        void SetTheme(ThemeMode theme);
        void RegisterThemeable(IThemeable themeable);
        void UnregisterThemeable(IThemeable themeable);
    }
}

