
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;

namespace LearningAssistant.Common
{
    public class ThemeService : IThemeService
    {
        private readonly IEventBus _eventBus;
        private readonly List<IThemeable> _themeables = new List<IThemeable>();
        private ThemeMode _currentMode = ThemeMode.Light;

        public ThemeMode CurrentTheme => _currentMode;
        public ThemeColors CurrentColors => GetColors(_currentMode);

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
                ThemeMode.Light => new ThemeColors
                {
                    ThemeMode = ThemeMode.Light,
                    Primary = Color.FromArgb(25, 118, 210),
                    PrimaryLight = Color.FromArgb(66, 165, 245),
                    PrimaryDark = Color.FromArgb(13, 71, 161),
                    Accent = Color.FromArgb(255, 193, 7),
                    Background = Color.White,
                    Surface = Color.FromArgb(248, 248, 252),
                    SurfaceElevated = Color.White,
                    TextPrimary = Color.FromArgb(33, 33, 33),
                    TextSecondary = Color.FromArgb(117, 117, 117),
                    TextDisabled = Color.FromArgb(186, 186, 186),
                    Divider = Color.FromArgb(224, 224, 224),
                    Success = Color.FromArgb(76, 175, 80),
                    SuccessLight = Color.FromArgb(200, 230, 200),
                    Error = Color.FromArgb(244, 67, 54),
                    ErrorLight = Color.FromArgb(255, 220, 220),
                    Warning = Color.FromArgb(255, 152, 0),
                    WarningLight = Color.FromArgb(255, 240, 220),
                    Info = Color.FromArgb(33, 150, 243),
                    InfoLight = Color.FromArgb(220, 240, 255),
                    Favorite = Color.FromArgb(255, 152, 0),
                    FavoriteLight = Color.FromArgb(255, 230, 200),
                    PanelGradientStart = Color.White,
                    PanelGradientEnd = Color.FromArgb(245, 250, 248),
                    ContentGradientStart = Color.White,
                    ContentGradientEnd = Color.FromArgb(255, 248, 240),
                    Shadow = Color.Black,
                    ShadowOpacity = 20,
                    BorderRadius = 12
                },
                ThemeMode.Dark => new ThemeColors
                {
                    ThemeMode = ThemeMode.Dark,
                    Primary = Color.FromArgb(100, 181, 246),
                    PrimaryLight = Color.FromArgb(144, 202, 249),
                    PrimaryDark = Color.FromArgb(66, 165, 245),
                    Accent = Color.FromArgb(255, 235, 59),
                    Background = Color.FromArgb(18, 18, 18),
                    Surface = Color.FromArgb(30, 30, 30),
                    SurfaceElevated = Color.FromArgb(40, 40, 40),
                    TextPrimary = Color.FromArgb(250, 250, 250),
                    TextSecondary = Color.FromArgb(176, 176, 176),
                    TextDisabled = Color.FromArgb(96, 96, 96),
                    Divider = Color.FromArgb(66, 66, 66),
                    Success = Color.FromArgb(102, 187, 106),
                    SuccessLight = Color.FromArgb(30, 60, 30),
                    Error = Color.FromArgb(239, 83, 80),
                    ErrorLight = Color.FromArgb(60, 30, 30),
                    Warning = Color.FromArgb(255, 167, 38),
                    WarningLight = Color.FromArgb(60, 50, 30),
                    Info = Color.FromArgb(66, 165, 245),
                    InfoLight = Color.FromArgb(30, 50, 70),
                    Favorite = Color.FromArgb(255, 167, 38),
                    FavoriteLight = Color.FromArgb(60, 50, 30),
                    PanelGradientStart = Color.FromArgb(35, 35, 35),
                    PanelGradientEnd = Color.FromArgb(25, 25, 30),
                    ContentGradientStart = Color.FromArgb(40, 40, 45),
                    ContentGradientEnd = Color.FromArgb(30, 30, 35),
                    Shadow = Color.Black,
                    ShadowOpacity = 40,
                    BorderRadius = 12
                },
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
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
        public ThemeMode ThemeMode { get; set; }
        public Color Primary { get; set; }
        public Color PrimaryLight { get; set; }
        public Color PrimaryDark { get; set; }
        public Color Accent { get; set; }
        public Color Background { get; set; }
        public Color Surface { get; set; }
        public Color SurfaceElevated { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color TextDisabled { get; set; }
        public Color Divider { get; set; }
        public Color Success { get; set; }
        public Color SuccessLight { get; set; }
        public Color Error { get; set; }
        public Color ErrorLight { get; set; }
        public Color Warning { get; set; }
        public Color WarningLight { get; set; }
        public Color Info { get; set; }
        public Color InfoLight { get; set; }
        public Color Favorite { get; set; }
        public Color FavoriteLight { get; set; }
        public Color PanelGradientStart { get; set; }
        public Color PanelGradientEnd { get; set; }
        public Color ContentGradientStart { get; set; }
        public Color ContentGradientEnd { get; set; }
        public Color Shadow { get; set; }
        public int ShadowOpacity { get; set; }
        public int BorderRadius { get; set; }
    }
}

