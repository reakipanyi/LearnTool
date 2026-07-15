using LearningAssistant.Common.UI;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Common
{
    /// <summary>
    /// 主题帮助类 - 提供统一的样式配置
    /// 基于 UI/UX 优化规范定义的设计令牌
    /// </summary>
    public static class ThemeHelper
    {
        // ==========================================
        // 设计令牌 - Design Tokens (UI/UX 优化规范)
        // ==========================================

        #region 设计令牌 - 颜色体系

        /// <summary>
        /// 主品牌色 - 紫罗兰，代表智慧与创造力
        /// </summary>
        public static class BrandColors
        {
            /// <summary>主品牌色 #6C5CE7</summary>
            public static Color Primary => Color.FromArgb(108, 92, 231);

            /// <summary>主品牌色深色变体</summary>
            public static Color PrimaryDark => Color.FromArgb(88, 72, 211);

            /// <summary>主品牌色浅色变体</summary>
            public static Color PrimaryLight => Color.FromArgb(138, 122, 251);

            /// <summary>渐变结束色</summary>
            public static Color GradientEnd => Color.FromArgb(155, 89, 182);
        }

        /// <summary>
        /// 成功/掌握色 - 翠绿，代表正确与进步
        /// </summary>
        public static class SuccessColors
        {
            public static Color Main => Color.FromArgb(0, 184, 148);
            public static Color Light => Color.FromArgb(0, 210, 165);
            public static Color Dark => Color.FromArgb(0, 150, 120);
        }

        /// <summary>
        /// 危险/未掌握色 - 珊瑚红，代表错误与提醒
        /// </summary>
        public static class DangerColors
        {
            public static Color Main => Color.FromArgb(255, 118, 117);
            public static Color Light => Color.FromArgb(255, 148, 147);
            public static Color Dark => Color.FromArgb(235, 98, 97);
        }

        /// <summary>
        /// 警告/待办色 - 琥珀黄，代表待复习与挑战
        /// </summary>
        public static class WarningColors
        {
            public static Color Main => Color.FromArgb(253, 203, 110);
            public static Color Light => Color.FromArgb(255, 218, 140);
            public static Color Dark => Color.FromArgb(240, 180, 70);
        }

        #endregion

        #region 设计令牌 - 圆角体系

        /// <summary>
        /// 圆角规范
        /// </summary>
        public static class BorderRadius
        {
            /// <summary>主卡片圆角 12px</summary>
            public const int Card = 12;

            /// <summary>徽章及图标圆角 50%</summary>
            public const int Badge = 50;

            /// <summary>按钮圆角 8px</summary>
            public const int Button = 8;

            /// <summary>小圆角 (输入框等)</summary>
            public const int Small = 6;

            /// <summary>大圆角 (大卡片等)</summary>
            public const int Large = 16;
        }

        #endregion

        #region 设计令牌 - 字体规范

        /// <summary>
        /// 字体大小规范
        /// </summary>
        public static class FontSizes
        {
            /// <summary>正文字号 14px</summary>
            public const float Body = 14f;

            /// <summary>标题字号 20px</summary>
            public const float Title = 20f;

            /// <summary>大标题 24px</summary>
            public const float LargeTitle = 24f;

            /// <summary>小文字号 12px</summary>
            public const float Small = 12f;

            /// <summary>按钮字号 14px</summary>
            public const float Button = 14f;

            /// <summary>辅助文字 10px</summary>
            public const float Caption = 10f;
        }

        /// <summary>
        /// 字体样式规范
        /// </summary>
        public static class FontStyles
        {
            public const FontStyle Regular = FontStyle.Regular;
            public const FontStyle Medium = FontStyle.Regular;
            public const FontStyle SemiBold = FontStyle.Bold;
            public const FontStyle Bold = FontStyle.Bold;
        }

        #endregion

        #region 设计令牌 - 间距体系

        /// <summary>
        /// 间距系统 - 四的倍数
        /// </summary>
        public static class Spacing
        {
            public const int Xs = 4;
            public const int Sm = 8;
            public const int Md = 16;
            public const int Lg = 24;
            public const int Xl = 32;
            public const int Xxl = 48;
        }

        #endregion

        #region 设计令牌 - 阴影层级

        /// <summary>
        /// 阴影层级
        /// </summary>
        public static class Shadows
        {
            /// <summary>卡片悬浮阴影: 0 2px 8px rgba(0,0,0,0.06)</summary>
            public static Color CardShadow => Color.FromArgb(15, 0, 0, 0);
            public const int CardBlur = 8;
            public const int CardOffset = 2;

            /// <summary>弹窗/浮层阴影: 0 8px 32px rgba(0,0,0,0.12)</summary>
            public static Color PopupShadow => Color.FromArgb(30, 0, 0, 0);
            public const int PopupBlur = 32;
            public const int PopupOffset = 8;
        }

        #endregion

        #region 设计令牌 - 组件尺寸

        /// <summary>
        /// 组件尺寸规范
        /// </summary>
        public static class ComponentSizes
        {
            /// <summary>主按钮高度 40px</summary>
            public const int ButtonHeight = 40;

            /// <summary>小按钮高度 32px</summary>
            public const int SmallButtonHeight = 32;

            /// <summary>徽章图标尺寸 48x48px</summary>
            public const int BadgeIconSize = 48;

            /// <summary>小图标尺寸 24x24px</summary>
            public const int IconSize = 24;

            /// <summary>输入框高度 36px</summary>
            public const int InputHeight = 36;

            /// <summary>环形进度条直径 40px</summary>
            public const int ProgressRingDiameter = 40;

            /// <summary>环形进度条线宽 4px</summary>
            public const int ProgressRingStroke = 4;
        }

        #endregion

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
                Math.Max(0, (int)originalColor.R + brightnessAdjustment),
                Math.Max(0, (int)originalColor.G + brightnessAdjustment),
                Math.Max(0, (int)originalColor.B + brightnessAdjustment));

            button.MouseEnter += (s, e) => button.BackColor = hoverColor;
            button.MouseLeave += (s, e) => button.BackColor = originalColor;
        }

        /// <summary>
        /// 添加按钮点击反馈效果
        /// </summary>
        public static void AddButtonPressEffect(Button button, Color originalColor, int pressAdjustment = -40)
        {
            Color pressedColor = Color.FromArgb(
                Math.Max(0, (int)originalColor.R + pressAdjustment),
                Math.Max(0, (int)originalColor.G + pressAdjustment),
                Math.Max(0, (int)originalColor.B + pressAdjustment));

            button.MouseDown += (s, e) => button.BackColor = pressedColor;
            button.MouseUp += (s, e) => button.BackColor = originalColor;
            button.MouseLeave += (s, e) => button.BackColor = originalColor;
        }

        /// <summary>
        /// 添加按钮完整交互效果（悬停+点击）
        /// </summary>
        public static void AddButtonInteractiveEffects(Button button, Color originalColor)
        {
            AddButtonHoverEffect(button, originalColor);
            AddButtonPressEffect(button, originalColor);
        }

        /// <summary>
        /// 计算悬停时的颜色
        /// </summary>
        public static Color GetHoverColor(Color baseColor, int adjustment = -30)
        {
            return Color.FromArgb(
                Math.Max(0, (int)baseColor.R + adjustment),
                Math.Max(0, (int)baseColor.G + adjustment),
                Math.Max(0, (int)baseColor.B + adjustment));
        }

        /// <summary>
        /// 计算按下时的颜色
        /// </summary>
        public static Color GetPressedColor(Color baseColor, int adjustment = -40)
        {
            return Color.FromArgb(
                Math.Max(0, (int)baseColor.R + adjustment),
                Math.Max(0, (int)baseColor.G + adjustment),
                Math.Max(0, (int)baseColor.B + adjustment));
        }

        // ==========================================
        // UI/UX 优化规范 - 组件样式辅助方法
        // ==========================================

        /// <summary>
        /// 应用主品牌色按钮样式
        /// </summary>
        public static void ApplyPrimaryButtonStyle(Button button)
        {
            button.BackColor = BrandColors.Primary;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Microsoft YaHei", FontSizes.Button, FontStyle.Regular);
            button.Height = ComponentSizes.ButtonHeight;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 应用成功/掌握色按钮样式
        /// </summary>
        public static void ApplySuccessButtonStyle(Button button)
        {
            button.BackColor = SuccessColors.Main;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Microsoft YaHei", FontSizes.Button, FontStyle.Regular);
            button.Height = ComponentSizes.ButtonHeight;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 应用危险/未掌握色按钮样式
        /// </summary>
        public static void ApplyDangerButtonStyle(Button button)
        {
            button.BackColor = DangerColors.Main;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Microsoft YaHei", FontSizes.Button, FontStyle.Regular);
            button.Height = ComponentSizes.ButtonHeight;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 应用警告/待办色按钮样式
        /// </summary>
        public static void ApplyWarningButtonStyle(Button button)
        {
            button.BackColor = WarningColors.Main;
            button.ForeColor = Color.FromArgb(60, 60, 60);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Microsoft YaHei", FontSizes.Button, FontStyle.Regular);
            button.Height = ComponentSizes.ButtonHeight;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 添加主品牌色按钮悬停效果
        /// </summary>
        public static void AddPrimaryButtonHover(Button button)
        {
            AddButtonHoverEffect(button, BrandColors.Primary, -20);
        }

        /// <summary>
        /// 添加成功色按钮悬停效果
        /// </summary>
        public static void AddSuccessButtonHover(Button button)
        {
            AddButtonHoverEffect(button, SuccessColors.Main, -20);
        }

        /// <summary>
        /// 获取圆角矩形路径（使用设计令牌圆角值）
        /// </summary>
        public static GraphicsPath CreateCardRoundedRectPath(Rectangle rect)
        {
            return GdiHelper.CreateRoundedRectPath(rect, BorderRadius.Card);
        }

        /// <summary>
        /// 获取按钮圆角矩形路径
        /// </summary>
        public static GraphicsPath CreateButtonRoundedRectPath(Rectangle rect)
        {
            return GdiHelper.CreateRoundedRectPath(rect, BorderRadius.Button);
        }

        /// <summary>
        /// 获取徽章圆角矩形路径（圆形）
        /// </summary>
        public static GraphicsPath CreateBadgeRoundedRectPath(Rectangle rect)
        {
            return GdiHelper.CreateRoundedRectPath(rect, Math.Min(rect.Width, rect.Height) / 2);
        }

        /// <summary>
        /// 应用主品牌色渐变（用于绘制）
        /// </summary>
        public static LinearGradientBrush CreatePrimaryGradientBrush(Rectangle rect)
        {
            return new LinearGradientBrush(
                rect,
                BrandColors.Primary,
                BrandColors.GradientEnd,
                LinearGradientMode.Horizontal);
        }

        /// <summary>
        /// 应用成功色渐变
        /// </summary>
        public static LinearGradientBrush CreateSuccessGradientBrush(Rectangle rect)
        {
            return new LinearGradientBrush(
                rect,
                SuccessColors.Light,
                SuccessColors.Main,
                LinearGradientMode.Horizontal);
        }

        /// <summary>
        /// 创建标准卡片阴影效果
        /// </summary>
        public static void DrawCardShadow(Graphics g, Rectangle rect)
        {
            using var shadowBrush = new SolidBrush(Shadows.CardShadow);
            var shadowRect = new Rectangle(rect.X + Shadows.CardOffset, rect.Y + Shadows.CardOffset, rect.Width, rect.Height);
            using var path = GdiHelper.CreateRoundedRectPath(shadowRect, BorderRadius.Card);
            g.FillPath(shadowBrush, path);
        }

        /// <summary>
        /// 创建弹窗阴影效果
        /// </summary>
        public static void DrawPopupShadow(Graphics g, Rectangle rect)
        {
            using var shadowBrush = new SolidBrush(Shadows.PopupShadow);
            var shadowRect = new Rectangle(rect.X + Shadows.PopupOffset, rect.Y + Shadows.PopupOffset, rect.Width, rect.Height);
            using var path = GdiHelper.CreateRoundedRectPath(shadowRect, BorderRadius.Large);
            g.FillPath(shadowBrush, path);
        }

        /// <summary>
        /// 获取文本颜色（基于主题）
        /// </summary>
        public static Color GetTextColor(bool isDarkTheme)
        {
            return isDarkTheme ? Color.FromArgb(250, 250, 250) : Color.FromArgb(33, 33, 33);
        }

        /// <summary>
        /// 获取次要文本颜色（基于主题）
        /// </summary>
        public static Color GetSecondaryTextColor(bool isDarkTheme)
        {
            return isDarkTheme ? Color.FromArgb(176, 176, 176) : Color.FromArgb(102, 102, 102);
        }

        /// <summary>
        /// 获取背景色（基于主题）
        /// </summary>
        public static Color GetBackgroundColor(bool isDarkTheme)
        {
            return isDarkTheme ? Color.FromArgb(18, 18, 18) : Color.FromArgb(250, 245, 235);
        }

        /// <summary>
        /// 获取卡片背景色（基于主题）
        /// </summary>
        public static Color GetCardBackgroundColor(bool isDarkTheme)
        {
            return isDarkTheme ? Color.FromArgb(30, 30, 30) : Color.White;
        }
    }
}
