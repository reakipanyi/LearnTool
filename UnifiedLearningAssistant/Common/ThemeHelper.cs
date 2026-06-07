namespace LearningAssistant.Common
{
    /// <summary>
    /// 主题帮助类 - 提供统一的样式配置
    /// </summary>
    public static class ThemeHelper
    {
        // 配色方案
        public static class Colors
        {
            public static Color WarmBackground => Color.FromArgb(250, 245, 235);
            public static Color WarmBeige => Color.FromArgb(255, 244, 230);
            public static Color WarmCream => Color.FromArgb(255, 250, 240);
            public static Color PanelLight => Color.FromArgb(255, 255, 255);
            public static Color PanelWarm => Color.FromArgb(252, 248, 240);
            public static Color TextPrimary => Color.FromArgb(70, 90, 110);
            public static Color TextSecondary => Color.FromArgb(100, 150, 180);
            public static Color TextDark => Color.FromArgb(33, 33, 33);
            public static Color TextGray => Color.FromArgb(117, 117, 117);
            public static Color Success => Color.FromArgb(76, 175, 80);
            public static Color Error => Color.FromArgb(244, 67, 54);
            public static Color Primary => Color.FromArgb(33, 150, 243);
            public static Color Purple => Color.FromArgb(156, 39, 176);
            public static Color PurpleDark => Color.FromArgb(103, 58, 183);
            public static Color Cyan => Color.FromArgb(0, 188, 212);
            public static Color Orange => Color.FromArgb(255, 152, 0);
            public static Color Gold => Color.FromArgb(255, 193, 7);
            public static Color Gray => Color.FromArgb(108, 117, 125);
            public static Color GrayLight => Color.FromArgb(158, 158, 158);
            public static Color Progress => Color.FromArgb(255, 140, 0);
            public static Color SoftBlue => Color.FromArgb(100, 181, 246);
            public static Color LightGrayBackground => Color.FromArgb(240, 240, 240);

            // 深色主题
            public static Color DarkBackground => Color.FromArgb(18, 18, 18);
            public static Color DarkSurface => Color.FromArgb(30, 30, 30);
            public static Color DarkTextPrimary => Color.FromArgb(250, 250, 250);
            public static Color DarkTextSecondary => Color.FromArgb(176, 176, 176);
            public static Color DarkGray => Color.FromArgb(66, 66, 66);

            public static Color LightBackground => Color.FromArgb(240, 240, 240);
            public static Color WarmOrange => Color.FromArgb(255, 152, 0);
            public static Color SuccessGreen => Color.FromArgb(76, 175, 80);
            // LightSurface 与 WarmCream 颜色值相同，使用 WarmCream 替代
            public static Color LightSurface => WarmCream;
        }

        // 字体配置 - 使用延迟初始化并支持释放
        public static class Fonts
        {
            private static readonly List<Font> _createdFonts = new();

            private static Font CreateFont(string familyName, float emSize, FontStyle style = FontStyle.Regular)
            {
                var font = new Font(familyName, emSize, style);
                _createdFonts.Add(font);
                return font;
            }

            private static Font _defaultBold;
            public static Font Default => SystemFonts.DefaultFont;
            public static Font DefaultBold => _defaultBold ??= CreateFont(SystemFonts.DefaultFont.FontFamily.Name, SystemFonts.DefaultFont.Size, FontStyle.Bold);
            
            private static Font _microsoftYaHei;
            public static Font MicrosoftYaHei => _microsoftYaHei ??= CreateFont("Microsoft YaHei", 9f);
            
            private static Font _microsoftYaHeiBold;
            public static Font MicrosoftYaHeiBold => _microsoftYaHeiBold ??= CreateFont("Microsoft YaHei", 9f, FontStyle.Bold);
            
            private static Font _microsoftYaHei10;
            public static Font MicrosoftYaHei10 => _microsoftYaHei10 ??= CreateFont("Microsoft YaHei", 10f);
            
            private static Font _microsoftYaHei10Bold;
            public static Font MicrosoftYaHei10Bold => _microsoftYaHei10Bold ??= CreateFont("Microsoft YaHei", 10f, FontStyle.Bold);
            
            private static Font _microsoftYaHei11;
            public static Font MicrosoftYaHei11 => _microsoftYaHei11 ??= CreateFont("Microsoft YaHei", 11f);
            
            private static Font _microsoftYaHei11Bold;
            public static Font MicrosoftYaHei11Bold => _microsoftYaHei11Bold ??= CreateFont("Microsoft YaHei", 11f, FontStyle.Bold);
            
            private static Font _microsoftYaHei12;
            public static Font MicrosoftYaHei12 => _microsoftYaHei12 ??= CreateFont("Microsoft YaHei", 12f);
            
            private static Font _microsoftYaHei12Bold;
            public static Font MicrosoftYaHei12Bold => _microsoftYaHei12Bold ??= CreateFont("Microsoft YaHei", 12f, FontStyle.Bold);
            
            private static Font _microsoftYaHei13;
            public static Font MicrosoftYaHei13 => _microsoftYaHei13 ??= CreateFont("Microsoft YaHei", 13f);
            
            private static Font _microsoftYaHei13Bold;
            public static Font MicrosoftYaHei13Bold => _microsoftYaHei13Bold ??= CreateFont("Microsoft YaHei", 13f, FontStyle.Bold);
            
            private static Font _microsoftYaHei14;
            public static Font MicrosoftYaHei14 => _microsoftYaHei14 ??= CreateFont("Microsoft YaHei", 14f);
            
            private static Font _microsoftYaHei14Bold;
            public static Font MicrosoftYaHei14Bold => _microsoftYaHei14Bold ??= CreateFont("Microsoft YaHei", 14f, FontStyle.Bold);
            
            private static Font _microsoftYaHei16;
            public static Font MicrosoftYaHei16 => _microsoftYaHei16 ??= CreateFont("Microsoft YaHei", 16f);
            
            private static Font _microsoftYaHei16Bold;
            public static Font MicrosoftYaHei16Bold => _microsoftYaHei16Bold ??= CreateFont("Microsoft YaHei", 16f, FontStyle.Bold);
            
            private static Font _microsoftYaHei20;
            public static Font MicrosoftYaHei20 => _microsoftYaHei20 ??= CreateFont("Microsoft YaHei", 20f);
            
            private static Font _microsoftYaHei20Bold;
            public static Font MicrosoftYaHei20Bold => _microsoftYaHei20Bold ??= CreateFont("Microsoft YaHei", 20f, FontStyle.Bold);
            
            private static Font _microsoftYaHei22;
            public static Font MicrosoftYaHei22 => _microsoftYaHei22 ??= CreateFont("Microsoft YaHei", 22f);
            
            private static Font _microsoftYaHei22Bold;
            public static Font MicrosoftYaHei22Bold => _microsoftYaHei22Bold ??= CreateFont("Microsoft YaHei", 22f, FontStyle.Bold);
            
            private static Font _microsoftYaHei28;
            public static Font MicrosoftYaHei28 => _microsoftYaHei28 ??= CreateFont("Microsoft YaHei", 28f);
            
            private static Font _microsoftYaHei28Bold;
            public static Font MicrosoftYaHei28Bold => _microsoftYaHei28Bold ??= CreateFont("Microsoft YaHei", 28f, FontStyle.Bold);
            
            private static Font _microsoftYaHei32;
            public static Font MicrosoftYaHei32 => _microsoftYaHei32 ??= CreateFont("Microsoft YaHei", 32f);
            
            private static Font _microsoftYaHei32Bold;
            public static Font MicrosoftYaHei32Bold => _microsoftYaHei32Bold ??= CreateFont("Microsoft YaHei", 32f, FontStyle.Bold);
            
            private static Font _microsoftYaHeiUI;
            public static Font MicrosoftYaHeiUI => _microsoftYaHeiUI ??= CreateFont("Microsoft YaHei UI", 9f);
            
            private static Font _microsoftYaHeiUI10;
            public static Font MicrosoftYaHeiUI10 => _microsoftYaHeiUI10 ??= CreateFont("Microsoft YaHei UI", 10f);
            
            private static Font _microsoftYaHeiUI20;
            public static Font MicrosoftYaHeiUI20 => _microsoftYaHeiUI20 ??= CreateFont("Microsoft YaHei UI", 20f);
            
            private static Font _segoeUIEmoji;
            public static Font SegoeUIEmoji => _segoeUIEmoji ??= CreateFont("Segoe UI Emoji", 32f);
            
            private static Font _large;
            public static Font Large => _large ??= CreateFont("Microsoft YaHei", 12f);
            
            private static Font _largeBold;
            public static Font LargeBold => _largeBold ??= CreateFont("Microsoft YaHei", 12f, FontStyle.Bold);
            
            private static Font _extraLarge;
            public static Font ExtraLarge => _extraLarge ??= CreateFont("Microsoft YaHei", 28f, FontStyle.Bold);

            internal static void DisposeAll()
            {
                foreach (var font in _createdFonts)
                {
                    try
                    {
                        font.Dispose();
                    }
                    catch
                    {
                    }
                }
                _createdFonts.Clear();
                _defaultBold = null;
                _microsoftYaHei = null;
                _microsoftYaHeiBold = null;
                _microsoftYaHei10 = null;
                _microsoftYaHei10Bold = null;
                _microsoftYaHei11 = null;
                _microsoftYaHei11Bold = null;
                _microsoftYaHei12 = null;
                _microsoftYaHei12Bold = null;
                _microsoftYaHei13 = null;
                _microsoftYaHei13Bold = null;
                _microsoftYaHei14 = null;
                _microsoftYaHei14Bold = null;
                _microsoftYaHei16 = null;
                _microsoftYaHei16Bold = null;
                _microsoftYaHei20 = null;
                _microsoftYaHei20Bold = null;
                _microsoftYaHei22 = null;
                _microsoftYaHei22Bold = null;
                _microsoftYaHei28 = null;
                _microsoftYaHei28Bold = null;
                _microsoftYaHei32 = null;
                _microsoftYaHei32Bold = null;
                _microsoftYaHeiUI = null;
                _microsoftYaHeiUI10 = null;
                _microsoftYaHeiUI20 = null;
                _segoeUIEmoji = null;
                _large = null;
                _largeBold = null;
                _extraLarge = null;
            }
        }

        public static void DisposeFonts()
        {
            Fonts.DisposeAll();
        }

        /// <summary>
        /// 应用温暖配色方案到窗体
        /// </summary>
        public static void ApplyWarmTheme(Form form)
        {
            form.BackColor = Colors.WarmBackground;
        }

        /// <summary>
        /// 配置按钮样式
        /// </summary>
        public static void ConfigureButton(Button button, Color backColor, string? text = null, Font? font = null)
        {
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;

            if (font != null)
                button.Font = font;

            if (!string.IsNullOrEmpty(text))
                button.Text = text;
        }

        /// <summary>
        /// 添加按钮悬停效果
        /// </summary>
        public static void AddButtonHoverEffect(Button button, Color originalColor, int brightnessAdjustment = -30)
        {
            Color hoverColor = Color.FromArgb(
                Math.Max(0, originalColor.R + brightnessAdjustment),
                Math.Max(0, originalColor.G + brightnessAdjustment),
                Math.Max(0, originalColor.B + brightnessAdjustment));

            button.MouseEnter += (s, e) => button.BackColor = hoverColor;
            button.MouseLeave += (s, e) => button.BackColor = originalColor;
        }

        /// <summary>
        /// 计算悬停时的颜色
        /// </summary>
        public static Color GetHoverColor(Color baseColor, int adjustment = -30)
        {
            return Color.FromArgb(
                Math.Max(0, baseColor.R + adjustment),
                Math.Max(0, baseColor.G + adjustment),
                Math.Max(0, baseColor.B + adjustment));
        }
    }
}
