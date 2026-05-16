
using UnifiedLearningAssistant.Common.Events;
using UnifiedLearningAssistant.Common.Themes;

namespace UnifiedLearningAssistant.Common
{
    public class ThemeService : IThemeService
    {
        private readonly IEventBus _eventBus;
        private readonly List&lt;IThemeable&gt; _themeables = new List&lt;IThemeable&gt;();
        private ThemeMode _currentMode = ThemeMode.Light;

        public ThemeMode CurrentTheme =&gt; _currentMode;
        public ThemeColors CurrentColors =&gt; GetColors(_currentMode);

        public ThemeService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void SetTheme(ThemeMode mode)
        {
            if (_currentMode != mode)
            {
                var oldMode = _currentMode;
                _currentMode = mode;
                
                var colors = CurrentColors;
                foreach (var themeable in _themeables)
                {
                    try
                    {
                        themeable.ApplyTheme(colors);
                    }
                    catch
                    {
                        // 处理主题应用错误
                    }
                }

                _eventBus.Publish(new ThemeChangedEvent
                {
                    NewTheme = mode,
                    OldTheme = oldMode
                });
            }
        }

        public void RegisterThemeable(IThemeable themeable)
        {
            if (!_themeables.Contains(themeable))
            {
                _themeables.Add(themeable);
                themeable.ApplyTheme(CurrentColors);
            }
        }

        public void UnregisterThemeable(IThemeable themeable)
        {
            _themeables.Remove(themeable);
        }

        public static ThemeColors GetColors(ThemeMode mode)
        {
            return mode switch
            {
                ThemeMode.Light =&gt; new ThemeColors
                {
                    Primary = Color.FromArgb(25, 118, 210),
                    PrimaryLight = Color.FromArgb(66, 165, 245),
                    PrimaryDark = Color.FromArgb(13, 71, 161),
                    Accent = Color.FromArgb(255, 193, 7),
                    Background = Color.White,
                    Surface = Color.FromArgb(245, 245, 245),
                    TextPrimary = Color.FromArgb(33, 33, 33),
                    TextSecondary = Color.FromArgb(117, 117, 117),
                    Divider = Color.FromArgb(224, 224, 224),
                    Success = Color.FromArgb(76, 175, 80),
                    Error = Color.FromArgb(244, 67, 54),
                    Warning = Color.FromArgb(255, 152, 0),
                    Info = Color.FromArgb(33, 150, 243)
                },
                ThemeMode.Dark =&gt; new ThemeColors
                {
                    Primary = Color.FromArgb(100, 181, 246),
                    PrimaryLight = Color.FromArgb(144, 202, 249),
                    PrimaryDark = Color.FromArgb(66, 165, 245),
                    Accent = Color.FromArgb(255, 235, 59),
                    Background = Color.FromArgb(18, 18, 18),
                    Surface = Color.FromArgb(30, 30, 30),
                    TextPrimary = Color.FromArgb(250, 250, 250),
                    TextSecondary = Color.FromArgb(176, 176, 176),
                    Divider = Color.FromArgb(66, 66, 66),
                    Success = Color.FromArgb(102, 187, 106),
                    Error = Color.FromArgb(239, 83, 80),
                    Warning = Color.FromArgb(255, 167, 38),
                    Info = Color.FromArgb(66, 165, 245)
                },
                _ =&gt; throw new ArgumentOutOfRangeException(nameof(mode))
            };
        }
    }

    public enum ThemeMode
    {
        Light,
        Dark
    }

    public class ThemeColors
    {
        public Color Primary { get; set; }
        public Color PrimaryLight { get; set; }
        public Color PrimaryDark { get; set; }
        public Color Accent { get; set; }
        public Color Background { get; set; }
        public Color Surface { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color Divider { get; set; }
        public Color Success { get; set; }
        public Color Error { get; set; }
        public Color Warning { get; set; }
        public Color Info { get; set; }
    }
}

