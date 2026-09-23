
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
                    // 品牌/主色：紫色（设计系统 accent-primary）
                    Primary = Color.FromArgb(139, 92, 246),      // #8B5CF6
                    PrimaryLight = Color.FromArgb(167, 139, 250), // #A78BFA
                    PrimaryDark = Color.FromArgb(124, 58, 237),  // #7C3AED
                    Accent = Color.FromArgb(245, 158, 11),        // #F59E0B 橙色点缀
                    // 背景/表面：slate 灰阶
                    Background = Color.White,                     // #FFFFFF
                    Surface = Color.FromArgb(248, 250, 252),      // #F8FAFC
                    SurfaceElevated = Color.White,                // #FFFFFF
                    TextPrimary = Color.FromArgb(15, 23, 42),     // #0F172A
                    TextSecondary = Color.FromArgb(71, 85, 105),  // #475569
                    TextDisabled = Color.FromArgb(148, 163, 184), // #94A3B8
                    Divider = Color.FromArgb(226, 232, 240),      // #E2E8F0
                    // 语义色：绿=认识/成功，红=不认识/危险，橙=模糊/警告，蓝=信息
                    Success = Color.FromArgb(34, 197, 94),        // #22C55E
                    SuccessLight = Color.FromArgb(220, 252, 231), // #DCFCE7
                    Error = Color.FromArgb(239, 68, 68),          // #EF4444
                    ErrorLight = Color.FromArgb(254, 226, 226),   // #FEE2E2
                    Warning = Color.FromArgb(245, 158, 11),       // #F59E0B
                    WarningLight = Color.FromArgb(254, 243, 199), // #FEF3C7
                    Info = Color.FromArgb(59, 130, 246),          // #3B82F6
                    InfoLight = Color.FromArgb(219, 234, 254),    // #DBEAFE
                    Favorite = Color.FromArgb(245, 158, 11),      // #F59E0B
                    FavoriteLight = Color.FromArgb(254, 243, 199),
                    PanelGradientStart = Color.White,
                    PanelGradientEnd = Color.FromArgb(248, 250, 252),
                    ContentGradientStart = Color.White,
                    ContentGradientEnd = Color.FromArgb(248, 250, 252),
                    Shadow = Color.Black,
                    ShadowOpacity = 12,
                    BorderRadius = 16
                },
                ThemeMode.Dark => new ThemeColors
                {
                    ThemeMode = ThemeMode.Dark,
                    // 品牌/主色：紫色（深色下提亮保证对比度）
                    Primary = Color.FromArgb(167, 139, 250),      // #A78BFA
                    PrimaryLight = Color.FromArgb(196, 181, 253), // #C4B5FD
                    PrimaryDark = Color.FromArgb(139, 92, 246),   // #8B5CF6
                    Accent = Color.FromArgb(251, 191, 36),        // #FBBF24
                    // 背景/表面：深 slate
                    Background = Color.FromArgb(15, 23, 42),      // #0F172A
                    Surface = Color.FromArgb(30, 41, 59),         // #1E293B
                    SurfaceElevated = Color.FromArgb(51, 65, 85), // #334155
                    TextPrimary = Color.FromArgb(248, 250, 252),  // #F8FAFC
                    TextSecondary = Color.FromArgb(203, 213, 225),// #CBD5E1
                    TextDisabled = Color.FromArgb(100, 116, 139), // #64748B
                    Divider = Color.FromArgb(51, 65, 85),         // #334155
                    Success = Color.FromArgb(74, 222, 128),       // #4ADE80（深色下提亮）
                    SuccessLight = Color.FromArgb(20, 83, 45),    // #14532D
                    Error = Color.FromArgb(248, 113, 113),        // #F87171
                    ErrorLight = Color.FromArgb(69, 10, 10),      // #450A0A
                    Warning = Color.FromArgb(251, 191, 36),       // #FBBF24
                    WarningLight = Color.FromArgb(69, 26, 3),     // #451A03
                    Info = Color.FromArgb(96, 165, 250),          // #60A5FA
                    InfoLight = Color.FromArgb(30, 58, 138),      // #1E3A8A
                    Favorite = Color.FromArgb(251, 191, 36),
                    FavoriteLight = Color.FromArgb(66, 32, 6),    // #422006
                    PanelGradientStart = Color.FromArgb(30, 41, 59),
                    PanelGradientEnd = Color.FromArgb(15, 23, 42),
                    ContentGradientStart = Color.FromArgb(30, 41, 59),
                    ContentGradientEnd = Color.FromArgb(30, 41, 59),
                    Shadow = Color.Black,
                    ShadowOpacity = 40,
                    BorderRadius = 16
                },
                ThemeMode.EyeCare => new ThemeColors
                {
                    ThemeMode = ThemeMode.EyeCare,
                    Primary = Color.FromArgb(40, 140, 80),
                    PrimaryLight = Color.FromArgb(76, 175, 112),
                    PrimaryDark = Color.FromArgb(23, 96, 51),
                    Accent = Color.FromArgb(144, 202, 127),
                    Background = Color.FromArgb(220, 240, 220),
                    Surface = Color.FromArgb(235, 250, 235),
                    SurfaceElevated = Color.FromArgb(245, 255, 245),
                    TextPrimary = Color.FromArgb(30, 50, 30),
                    TextSecondary = Color.FromArgb(90, 120, 90),
                    TextDisabled = Color.FromArgb(150, 170, 150),
                    Divider = Color.FromArgb(180, 210, 180),
                    Success = Color.FromArgb(76, 175, 80),
                    SuccessLight = Color.FromArgb(200, 230, 200),
                    Error = Color.FromArgb(204, 68, 68),
                    ErrorLight = Color.FromArgb(250, 210, 210),
                    Warning = Color.FromArgb(204, 136, 0),
                    WarningLight = Color.FromArgb(245, 230, 190),
                    Info = Color.FromArgb(68, 158, 108),
                    InfoLight = Color.FromArgb(205, 235, 220),
                    Favorite = Color.FromArgb(204, 136, 0),
                    FavoriteLight = Color.FromArgb(245, 230, 190),
                    PanelGradientStart = Color.FromArgb(240, 252, 240),
                    PanelGradientEnd = Color.FromArgb(225, 245, 225),
                    ContentGradientStart = Color.FromArgb(235, 250, 235),
                    ContentGradientEnd = Color.FromArgb(220, 240, 220),
                    Shadow = Color.Black,
                    ShadowOpacity = 15,
                    BorderRadius = 12
                },
                ThemeMode.HighContrast => new ThemeColors
                {
                    ThemeMode = ThemeMode.HighContrast,
                    Primary = Color.FromArgb(0, 0, 0),
                    PrimaryLight = Color.FromArgb(60, 60, 60),
                    PrimaryDark = Color.FromArgb(0, 0, 0),
                    Accent = Color.FromArgb(255, 0, 0),
                    Background = Color.White,
                    Surface = Color.White,
                    SurfaceElevated = Color.White,
                    TextPrimary = Color.Black,
                    TextSecondary = Color.FromArgb(64, 64, 64),
                    TextDisabled = Color.FromArgb(160, 160, 160),
                    Divider = Color.Black,
                    Success = Color.FromArgb(0, 128, 0),
                    SuccessLight = Color.FromArgb(220, 240, 220),
                    Error = Color.FromArgb(255, 0, 0),
                    ErrorLight = Color.FromArgb(255, 220, 220),
                    Warning = Color.FromArgb(255, 128, 0),
                    WarningLight = Color.FromArgb(255, 240, 220),
                    Info = Color.FromArgb(0, 0, 255),
                    InfoLight = Color.FromArgb(220, 220, 255),
                    Favorite = Color.FromArgb(255, 128, 0),
                    FavoriteLight = Color.FromArgb(255, 240, 220),
                    PanelGradientStart = Color.White,
                    PanelGradientEnd = Color.White,
                    ContentGradientStart = Color.White,
                    ContentGradientEnd = Color.FromArgb(245, 245, 245),
                    Shadow = Color.Black,
                    ShadowOpacity = 30,
                    BorderRadius = 12
                },
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
        }
    }
}

