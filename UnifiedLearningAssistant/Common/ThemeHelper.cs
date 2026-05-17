namespace UnifiedLearningAssistant.Common
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
            public static Color LightSurface => Color.FromArgb(30, 30, 30);
        }

        // 字体配置
        public static class Fonts
        {
            public static Font Default => SystemFonts.DefaultFont;
            public static Font DefaultBold => new(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.Size, FontStyle.Bold);
            public static Font MicrosoftYaHei => new("Microsoft YaHei", 9f);
            public static Font MicrosoftYaHeiBold => new("Microsoft YaHei", 9f, FontStyle.Bold);
            public static Font MicrosoftYaHei10 => new("Microsoft YaHei", 10f);
            public static Font MicrosoftYaHei10Bold => new("Microsoft YaHei", 10f, FontStyle.Bold);
            public static Font MicrosoftYaHei11 => new("Microsoft YaHei", 11f);
            public static Font MicrosoftYaHei11Bold => new("Microsoft YaHei", 11f, FontStyle.Bold);
            public static Font MicrosoftYaHei12 => new("Microsoft YaHei", 12f);
            public static Font MicrosoftYaHei12Bold => new("Microsoft YaHei", 12f, FontStyle.Bold);
            public static Font MicrosoftYaHei13 => new("Microsoft YaHei", 13f);
            public static Font MicrosoftYaHei13Bold => new("Microsoft YaHei", 13f, FontStyle.Bold);
            public static Font MicrosoftYaHei14 => new("Microsoft YaHei", 14f);
            public static Font MicrosoftYaHei14Bold => new("Microsoft YaHei", 14f, FontStyle.Bold);
            public static Font MicrosoftYaHei16 => new("Microsoft YaHei", 16f);
            public static Font MicrosoftYaHei16Bold => new("Microsoft YaHei", 16f, FontStyle.Bold);
            public static Font MicrosoftYaHei20 => new("Microsoft YaHei", 20f);
            public static Font MicrosoftYaHei20Bold => new("Microsoft YaHei", 20f, FontStyle.Bold);
            public static Font MicrosoftYaHei22 => new("Microsoft YaHei", 22f);
            public static Font MicrosoftYaHei22Bold => new("Microsoft YaHei", 22f, FontStyle.Bold);
            public static Font MicrosoftYaHei28 => new("Microsoft YaHei", 28f);
            public static Font MicrosoftYaHei28Bold => new("Microsoft YaHei", 28f, FontStyle.Bold);
            public static Font MicrosoftYaHei32 => new("Microsoft YaHei", 32f);
            public static Font MicrosoftYaHei32Bold => new("Microsoft YaHei", 32f, FontStyle.Bold);
            public static Font MicrosoftYaHeiUI => new("Microsoft YaHei UI", 9f);
            public static Font MicrosoftYaHeiUI10 => new("Microsoft YaHei UI", 10f);
            public static Font MicrosoftYaHeiUI20 => new("Microsoft YaHei UI", 20f);
            public static Font SegoeUIEmoji => new("Segoe UI Emoji", 32f);
            public static Font Large => new("Microsoft YaHei", 12f);
            public static Font LargeBold => new("Microsoft YaHei", 12f, FontStyle.Bold);
            public static Font ExtraLarge => new("Microsoft YaHei", 28f, FontStyle.Bold);
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
