namespace LearningAssistant.Common
{
public static partial class ThemeHelper
    {
        // ==========================================
        // 原有配色方案（保持向后兼容）
        // ==========================================

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

            // UI/UX 优化规范新增颜色（映射到新体系）
            /// <summary>主品牌色（优化规范）</summary>
            public static Color BrandPrimary => BrandColors.Primary;

            /// <summary>成功/掌握色（优化规范）</summary>
            public static Color Mastery => SuccessColors.Main;

            /// <summary>危险/未掌握色（优化规范）</summary>
            public static Color Unmastered => DangerColors.Main;

            /// <summary>警告/待办色（优化规范）</summary>
            public static Color Pending => WarningColors.Main;
        }
    }
}
